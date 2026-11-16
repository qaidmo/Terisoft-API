using AccountingSyncAPP.Data;
using AccountingSyncAPP.Data.Dtos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountingSyncAPP.Services
{
    /// <summary>
    /// خدمة تقرير هامش أرباح مراكز التكلفة — مستقلة تماماً.
    ///
    /// تجمع حركات المبيعات والمردودات من SunTransDetailItem + SunTransHeadline
    /// مجمّعةً حسب مركز التكلفة (TRACCID / TRACCNAME)، وتحسب:
    ///   - صافي المنصرف مخزنياً (NetCogs)   = Qty × UnitCost  (مبيعات - مردودات)
    ///   - المبيعات الإجمالية  (GrossSales)  = sum(TRADNETAMOUNT) للمبيعات فقط
    ///   - المردودات           (Returns)      = sum(TRADNETAMOUNT) للمردودات فقط
    ///   - صافي المبيعات       (NetSales)     = GrossSales - Returns
    ///   - صافي الربح          (NetProfit)    = NetSales  - NetCogs
    ///   - نسبة الربح          (ProfitPct)    = NetProfit / NetCogs * 100
    ///
    /// كودات الحركات مأخوذة من ProfitMarginService الحالي:
    ///   مبيعات  : 601, 603, 605, 607, 633, 801, 803
    ///   مردودات : 602, 604, 606, 608, 634, 802, 804
    /// </summary>
    public class CostCenterProfitMarginService
    {
        private readonly AccountingDbContext _context;

        public CostCenterProfitMarginService(AccountingDbContext context)
        {
            _context = context;
        }

        // ---- كودات حركات المبيعات الأصلية (خروج من المخزون) ----
        private static readonly string[] SalesTypes   = { "601", "603", "605", "607", "633", "801", "803" };

        // ---- كودات مردودات المبيعات (إعادة دخول للمخزون) ----
        private static readonly string[] ReturnTypes  = { "602", "604", "606", "608", "634", "802", "804" };

        // =========================================================================
        public async Task<CostCenterProfitMarginResponseDto> GetCostCenterProfitMarginAsync(
            CostCenterProfitMarginRequestDto request)
        {
            // --- 1. تحديد اسم الجداول الديناميكية ---
            var yearsCount = await _context.Tssysyears.CountAsync();
            bool multipleYears = yearsCount > 1;

            string suffix = multipleYears
                ? $"{request.Year}{request.BranchId}"
                : $"{request.BranchId}";

            string headlineTable   = $"SunTransHeadline{suffix}";
            string detailItemTable = $"SunTransDetailItem{suffix}";

            string fromDateId = request.FromDate.ToString("yyyyMMdd");
            string toDateId   = request.ToDate.ToString("yyyyMMdd");

            // --- 2. قائمة IN للكودات ---
            string salesIn   = string.Join("','", SalesTypes);
            string returnsIn = string.Join("','", ReturnTypes);
            string allIn     = string.Join("','", SalesTypes.Concat(ReturnTypes));

            // --- 3. بناء الاستعلام الرئيسي ---
            // مركز التكلفة محفوظ في جدول تفاصيل الأصناف (d.TRADCCID)
            // واسمه يُجلب من دليل مراكز التكلفة SUNCCDIR (cc.ACCID → cc.ACCNAME)
            var sql = new StringBuilder();
            sql.Append($@"
                SELECT
                    ISNULL(LTRIM(RTRIM(d.TRADCCID)), '')  AS CostCenterId,
                    ISNULL(cc.ACCNAME, '')                AS CostCenterName,

                    -- صافي المنصرف مخزنياً: qty × unit_cost (+ مبيعات، - مردودات)
                    SUM(CASE
                        WHEN h.TRALSTID IN ('{salesIn}')
                            THEN  ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                                * CAST(ISNULL(NULLIF(d.TRADITMUNTCOST,''),'0') AS DECIMAL(18,3))
                        WHEN h.TRALSTID IN ('{returnsIn}')
                            THEN -ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                                * CAST(ISNULL(NULLIF(d.TRADITMUNTCOST,''),'0') AS DECIMAL(18,3))
                        ELSE 0
                    END) AS NetCogs,

                    -- المبيعات الإجمالية (حركات الخروج الأصلية فقط)
                    SUM(CASE
                        WHEN h.TRALSTID IN ('{salesIn}')
                            THEN ABS(CAST(ISNULL(NULLIF(d.TRADNETAMOUNT,''),'0') AS DECIMAL(18,3)))
                                * CAST(ISNULL(NULLIF(h.TRACURVALUE,''),'1') AS DECIMAL(18,5))
                        ELSE 0
                    END) AS GrossSales,

                    -- المردودات (حركات الإعادة فقط)
                    SUM(CASE
                        WHEN h.TRALSTID IN ('{returnsIn}')
                            THEN ABS(CAST(ISNULL(NULLIF(d.TRADNETAMOUNT,''),'0') AS DECIMAL(18,3)))
                                * CAST(ISNULL(NULLIF(h.TRACURVALUE,''),'1') AS DECIMAL(18,5))
                        ELSE 0
                    END) AS Returns

                FROM {detailItemTable} d
                INNER JOIN {headlineTable} h  ON d.TRAID = h.TRAID
                LEFT  JOIN SUNCCDIR        cc ON LTRIM(RTRIM(d.TRADCCID)) = LTRIM(RTRIM(cc.ACCID))

                WHERE h.TRADATEID >= {{0}}
                  AND h.TRADATEID <= {{1}}
                  AND h.TRASTATUSDEL = '0'
                  AND ISNULL(LTRIM(RTRIM(d.TRADCCID)),'') != ''
                  AND h.TRALSTID IN ('{allIn}')
            ");

            var parameters = new List<object> { fromDateId, toDateId };
            int pIdx = 2;

            // --- 4. فلتر القسم ---
            if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
            {
                sql.Append($" AND h.TRASECID = {{{pIdx}}}");
                parameters.Add(request.DepartmentId.Value.ToString());
                pIdx++;
            }

            sql.Append(@"
                GROUP BY LTRIM(RTRIM(d.TRADCCID)), cc.ACCNAME
                ORDER BY CostCenterId
            ");


            // --- 5. تنفيذ الاستعلام ---
            try
            {
                var rawRows = await _context.Database
                    .SqlQueryRaw<CostCenterProfitMarginRawRow>(sql.ToString(), parameters.ToArray())
                    .ToListAsync();

                // --- 6. حساب الأعمدة المشتقة في C# ---
                var resultRows = rawRows.Select(r =>
                {
                    decimal netSales  = r.GrossSales - r.Returns;
                    decimal netProfit = netSales - r.NetCogs;
                    decimal profitPct = r.NetCogs == 0 ? 0m
                        : Math.Round(netProfit / r.NetCogs * 100, 2);

                    return new CostCenterProfitMarginRowDto
                    {
                        CostCenterId   = r.CostCenterId,
                        CostCenterName = r.CostCenterName,
                        NetCogs        = Math.Round(r.NetCogs,    3),
                        GrossSales     = Math.Round(r.GrossSales, 3),
                        Returns        = Math.Round(r.Returns,    3),
                        NetSales       = Math.Round(netSales,     3),
                        NetProfit      = Math.Round(netProfit,    3),
                        ProfitPct      = profitPct,
                    };
                }).ToList();

                // --- 7. المجاميع الكلية ---
                return new CostCenterProfitMarginResponseDto
                {
                    Rows           = resultRows,
                    RowCount       = resultRows.Count,
                    GrandNetCogs    = resultRows.Sum(r => r.NetCogs),
                    GrandGrossSales = resultRows.Sum(r => r.GrossSales),
                    GrandReturns    = resultRows.Sum(r => r.Returns),
                    GrandNetSales   = resultRows.Sum(r => r.NetSales),
                    GrandNetProfit  = resultRows.Sum(r => r.NetProfit),
                };
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"خطأ في تقرير هامش أرباح مراكز التكلفة على الجداول {headlineTable} & {detailItemTable}: {ex.Message}",
                    ex);
            }
        }
    }

    // =========================================================================
    // نموذج الصف الخام من قاعدة البيانات (لـ SqlQueryRaw)
    // =========================================================================
    public class CostCenterProfitMarginRawRow
    {
        public string  CostCenterId   { get; set; } = string.Empty;
        public string  CostCenterName { get; set; } = string.Empty;
        public decimal NetCogs        { get; set; }
        public decimal GrossSales     { get; set; }
        public decimal Returns        { get; set; }
    }
}
