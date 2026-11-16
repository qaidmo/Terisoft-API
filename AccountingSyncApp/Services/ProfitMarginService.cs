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
    /// خدمة تقرير هامش الربح — مستقلة تماماً
    /// تجمع بيانات المبيعات والرصيد من SunTransHeadline + SunTransDetailItem + SUNITEM + SUNITEMGROUP
    /// </summary>
    public class ProfitMarginService
    {
        private readonly AccountingDbContext _context;

        public ProfitMarginService(AccountingDbContext context)
        {
            _context = context;
        }

        // -------------------------------------------------------------------------
        // كودات حركات المبيعات (الخروج من المخزون بسبب البيع)
        // -------------------------------------------------------------------------
        private static readonly string[] SalesTransactionTypes = new[]
        {
            "601", "602", "603", "604", "605", "606", "607", "608", "633", "634"
        };

        // كودات مردودات المبيعات (تُطرح من المجموع)
        private static readonly string[] ReturnTransactionTypes = new[]
        {
            "602", "604", "606", "608", "634"
        };

        // كودات جميع الحركات المخزنية (مبيعات + مشتريات + جرد) لحساب الرصيد
        private static readonly string[] AllInventoryTypes = new[]
        {
            "601","602","603","604","605","606","607","608",
            "421","422","423","424",
            "633","634",
            "801","802","803","804",
            "507"
        };

        // =========================================================================
        public async Task<ProfitMarginResponseDto> GetProfitMarginAsync(ProfitMarginRequestDto request)
        {
            // --- 1. تحديد لاحقة اسم الجدول ---
            var yearsCount = await _context.Tssysyears.CountAsync();
            bool multipleYears = yearsCount > 1;

            string tableSuffix = multipleYears
                ? $"{request.Year}{request.BranchId}"
                : $"{request.BranchId}";

            string headlineTable   = $"SunTransHeadline{tableSuffix}";
            string detailItemTable = $"SunTransDetailItem{tableSuffix}";

            string fromDateId = request.FromDate.ToString("yyyyMMdd");
            string toDateId   = request.ToDate.ToString("yyyyMMdd");

            // --- 2. تحديد أعمدة التجميع ---
            string groupSelectKeys;
            string groupByClause;

            switch (request.GroupBy?.ToLower())
            {
                case "group":
                    groupSelectKeys = @"
                        ISNULL(LTRIM(RTRIM(grp.ITMGRPID)),   '') AS PrimaryKey,
                        ISNULL(grp.ITMGRPNAME,               '') AS PrimaryName,
                        ''                                        AS ItemUnit";
                    groupByClause = "LTRIM(RTRIM(grp.ITMGRPID)), grp.ITMGRPNAME";
                    break;

                case "user":
                    groupSelectKeys = @"
                        ISNULL(LTRIM(RTRIM(h.TRAUID)),        '') AS PrimaryKey,
                        ISNULL(h.TRAUNAME,                    '') AS PrimaryName,
                        ''                                        AS ItemUnit";
                    groupByClause = "LTRIM(RTRIM(h.TRAUID)), h.TRAUNAME";
                    break;

                default: // "item"
                    groupSelectKeys = @"
                        ISNULL(LTRIM(RTRIM(d.TRADITMID)),     '') AS PrimaryKey,
                        ISNULL(d.TRADITMNAME,                 '') AS PrimaryName,
                        ISNULL(d.TRADITMUNIT,                 '') AS ItemUnit";
                    groupByClause = "LTRIM(RTRIM(d.TRADITMID)), d.TRADITMNAME, d.TRADITMUNIT";
                    break;
            }

            // --- 3. أعمدة اختيارية ---
            string optionalSelects = BuildOptionalSelects(request);
            string optionalGroupBy = BuildOptionalGroupBy(request);

            // --- 4. بناء قائمة IN للمبيعات ---
            string salesInList   = string.Join("','", SalesTransactionTypes);
            string returnsInList = string.Join("','", ReturnTransactionTypes);

            // --- 5. استعلام المبيعات المجمّعة ---
            var salesSql = new StringBuilder();
            salesSql.Append($@"
                SELECT
                    {groupSelectKeys},
                    {optionalSelects}

                    SUM(CASE
                        WHEN h.TRALSTID NOT IN ('{returnsInList}')
                        THEN  ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                        ELSE -ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                    END)                                                                    AS SalesQty,

                    SUM(
                        ABS(CAST(ISNULL(NULLIF(d.TRADNETAMOUNT,''),'0') AS DECIMAL(18,3)))
                        * CAST(ISNULL(NULLIF(h.TRACURVALUE,''),'1') AS DECIMAL(18,5))
                        * CASE WHEN h.TRALSTID IN ('{returnsInList}') THEN -1 ELSE 1 END
                    )                                                                       AS SalesValue,

                    SUM(
                        ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                        * CAST(ISNULL(NULLIF(d.TRADITMUNTCOST,''),'0') AS DECIMAL(18,3))
                        * CASE WHEN h.TRALSTID IN ('{returnsInList}') THEN -1 ELSE 1 END
                    )                                                                       AS SalesCost

                FROM {detailItemTable} d
                INNER JOIN {headlineTable}  h   ON d.TRAID = h.TRAID
                LEFT  JOIN SUNITEM          itm ON LTRIM(RTRIM(d.TRADITMID)) = LTRIM(RTRIM(itm.ITMNO))
                LEFT  JOIN SUNITEMGROUP     grp ON LTRIM(RTRIM(itm.ITMGRPID)) = LTRIM(RTRIM(grp.ITMGRPID))

                WHERE h.TRADATEID >= {{0}}
                  AND h.TRADATEID <= {{1}}
                  AND h.TRASTATUSDEL = '0'
                  AND h.TRALSTID IN ('{salesInList}')
            ");

            var salesParams = new List<object> { fromDateId, toDateId };
            int sp = 2;

            // فلترة القسم
            if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
            {
                salesSql.Append($" AND h.TRASECID = {{{sp}}}");
                salesParams.Add(request.DepartmentId.Value.ToString());
                sp++;
            }

            // فلترة المستخدم (أمان نطاق البيانات)
            if (request.UserId.HasValue && request.UserId.Value > 0)
            {
                salesSql.Append($" AND h.TRAUID = {{{sp}}}");
                salesParams.Add(request.UserId.Value.ToString());
                sp++;
            }

            // فلترة مجموعة الأصناف
            if (!string.IsNullOrWhiteSpace(request.ItemGroupId))
            {
                salesSql.Append($" AND LTRIM(RTRIM(itm.ITMGRPID)) = {{{sp}}}");
                salesParams.Add(request.ItemGroupId.Trim());
                sp++;
            }

            salesSql.Append($" GROUP BY {groupByClause}{optionalGroupBy}");
            salesSql.Append("  ORDER BY PrimaryKey");

            // --- 6. استعلام رصيد المخزون ---
            // يجمع الرصيد الفعلي من جميع الحركات (مبيعات + مشتريات + جرد)
            // الإشارة: حركات الخروج (مبيعات وما شابه) تُطرح، وحركات الدخول تُضاف
            // الكودات الزوجية (مردودات) تعكس الإشارة

            // كودات الخروج الأصلية (مبيعات، طلبات عملاء)
            // الكودات الزوجية لها تُعيد الدخول
            // نعتمد على TRADSIGND / TRADSIGNDIN / TRADSIGNOUT المتوفرة في جدول التفاصيل
            // لكن الأبسط والأكثر موثوقية هو: كل حركة مبيعات تخرج (TRADQTY سالب)
            // وكل حركة مشتريات تدخل، والجرد يُعدل.
            // نستخدم نفس منطق السجل التاريخي: نجمع الكميات بإشارتها الطبيعية.

            string allInvList = string.Join("','", AllInventoryTypes);

            // كودات حركات الخروج من المخزون (مبيعات، طلبات عملاء)
            string[] outTypes = new[] { "601","602","603","604","605","606","607","608","633","634","801","802","803","804" };
            // المردودات (تعيد بضاعة للمخزن — دخول)
            string[] outReturnTypes = new[] { "602","604","606","608","634","802","804" };
            // كودات الدخول (مشتريات)
            string[] inTypes = new[] { "421","422","423","424" };
            // كودات مردودات المشتريات (خروج من المخزن)
            string[] inReturnTypes = new[] { "422","424" };

            // الجرد (507): TRADQTY موجبة = دخول، سالبة = خروج — نستخدم القيمة كما هي
            // نبني الاستعلام بـ CASE:
            //   دخول أصلي  (421,423) → +qty
            //   مردود دخول (422,424) → -qty  (خروج)
            //   خروج أصلي (601,603,605,607,633,801,803) → -qty
            //   مردود خروج(602,604,606,608,634,802,804) → +qty  (إعادة دخول)
            //   جرد (507) → القيمة كما هي (يمكن أن تكون موجبة أو سالبة)

            string balanceGroupByClause = groupByClause; // نفس التجميع

            // نبني SELECT الرصيد مع نفس مفاتيح التجميع فقط (لا نحتاج الأعمدة الاختيارية)
            string balanceGroupSelectKeys;
            switch (request.GroupBy?.ToLower())
            {
                case "group":
                    balanceGroupSelectKeys = "ISNULL(LTRIM(RTRIM(grp.ITMGRPID)), '') AS PrimaryKey";
                    break;
                case "user":
                    balanceGroupSelectKeys = "ISNULL(LTRIM(RTRIM(h.TRAUID)), '') AS PrimaryKey";
                    break;
                default:
                    balanceGroupSelectKeys = "ISNULL(LTRIM(RTRIM(d.TRADITMID)), '') AS PrimaryKey";
                    break;
            }

            var balanceSql = new StringBuilder();
            balanceSql.Append($@"
                SELECT
                    {balanceGroupSelectKeys},

                    SUM(CASE
                        WHEN h.TRALSTID IN ('421','423')
                            THEN  ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                        WHEN h.TRALSTID IN ('422','424')
                            THEN -ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                        WHEN h.TRALSTID IN ('601','603','605','607','633','801','803')
                            THEN -ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                        WHEN h.TRALSTID IN ('602','604','606','608','634','802','804')
                            THEN  ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                        ELSE CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3))
                    END)                                                                AS BalanceQty,

                    SUM(CASE
                        WHEN h.TRALSTID IN ('421','423')
                            THEN  ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                                 * CAST(ISNULL(NULLIF(d.TRADITMUNTCOST,''),'0') AS DECIMAL(18,3))
                        WHEN h.TRALSTID IN ('422','424')
                            THEN -ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                                 * CAST(ISNULL(NULLIF(d.TRADITMUNTCOST,''),'0') AS DECIMAL(18,3))
                        WHEN h.TRALSTID IN ('601','603','605','607','633','801','803')
                            THEN -ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                                 * CAST(ISNULL(NULLIF(d.TRADITMUNTCOST,''),'0') AS DECIMAL(18,3))
                        WHEN h.TRALSTID IN ('602','604','606','608','634','802','804')
                            THEN  ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                                 * CAST(ISNULL(NULLIF(d.TRADITMUNTCOST,''),'0') AS DECIMAL(18,3))
                        ELSE CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3))
                           * CAST(ISNULL(NULLIF(d.TRADITMUNTCOST,''),'0') AS DECIMAL(18,3))
                    END)                                                                AS BalanceCost

                FROM {detailItemTable} d
                INNER JOIN {headlineTable}  h   ON d.TRAID = h.TRAID
                LEFT  JOIN SUNITEM          itm ON LTRIM(RTRIM(d.TRADITMID)) = LTRIM(RTRIM(itm.ITMNO))
                LEFT  JOIN SUNITEMGROUP     grp ON LTRIM(RTRIM(itm.ITMGRPID)) = LTRIM(RTRIM(grp.ITMGRPID))

                WHERE h.TRADATEID <= {{0}}
                  AND h.TRASTATUSDEL = '0'
                  AND h.TRALSTID IN ('{allInvList}')
            ");

            var balanceParams = new List<object> { toDateId };
            int bp = 1;

            // فلترة القسم للرصيد
            if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
            {
                balanceSql.Append($" AND h.TRASECID = {{{bp}}}");
                balanceParams.Add(request.DepartmentId.Value.ToString());
                bp++;
            }

            // فلترة مجموعة الأصناف للرصيد
            if (!string.IsNullOrWhiteSpace(request.ItemGroupId))
            {
                balanceSql.Append($" AND LTRIM(RTRIM(itm.ITMGRPID)) = {{{bp}}}");
                balanceParams.Add(request.ItemGroupId.Trim());
                bp++;
            }

            balanceSql.Append($" GROUP BY {balanceGroupByClause}");

            // --- 7. تنفيذ الاستعلامين ---
            try
            {
                var salesRows = await _context.Database
                    .SqlQueryRaw<ProfitMarginSalesRaw>(salesSql.ToString(), salesParams.ToArray())
                    .ToListAsync();

                var balanceRows = await _context.Database
                    .SqlQueryRaw<ProfitMarginBalanceRaw>(balanceSql.ToString(), balanceParams.ToArray())
                    .ToListAsync();

                // --- 8. دمج النتيجتين وحساب الأعمدة المشتقة ---
                var balanceLookup = balanceRows
                    .GroupBy(b => b.PrimaryKey)
                    .ToDictionary(g => g.Key, g => g.First());

                var resultRows = salesRows.Select(s =>
                {
                    decimal margin    = s.SalesValue - s.SalesCost;
                    decimal profitPct = s.SalesCost == 0 ? 0m
                                        : Math.Round(margin / s.SalesCost * 100, 2);

                    balanceLookup.TryGetValue(s.PrimaryKey, out var bal);
                    decimal balQty  = bal?.BalanceQty  ?? 0m;
                    decimal balCost = bal?.BalanceCost ?? 0m;

                    // نسبة مبيعات الرصيد = SalesQty / BalanceQty * 100
                    decimal balSalesPct = balQty == 0 ? 0m
                                          : Math.Round(s.SalesQty / balQty * 100, 2);

                    return new ProfitMarginRowDto
                    {
                        PrimaryKey      = s.PrimaryKey,
                        PrimaryName     = s.PrimaryName,
                        ItemUnit        = s.ItemUnit,
                        BranchId        = s.BranchId,
                        DepartmentId    = s.DepartmentId,
                        BatchNumber     = s.BatchNumber,
                        CostCenterName  = s.CostCenterName,
                        SalesQty        = s.SalesQty,
                        SalesValue      = s.SalesValue,
                        SalesCost       = s.SalesCost,
                        Margin          = margin,
                        ProfitPct       = profitPct,
                        BalanceQty      = balQty,
                        BalanceCost     = balCost,
                        BalanceSalesPct = balSalesPct,
                    };
                }).ToList();

                // --- 9. تطبيق فلتر الهامش السالب ---
                if (request.NegativeMarginOnly)
                {
                    resultRows = resultRows.Where(r => r.Margin < 0).ToList();
                }

                // --- 10. حساب المجاميع الكلية ---
                return new ProfitMarginResponseDto
                {
                    RowCount         = resultRows.Count,
                    GrandSalesQty    = resultRows.Sum(r => r.SalesQty),
                    GrandSalesValue  = resultRows.Sum(r => r.SalesValue),
                    GrandSalesCost   = resultRows.Sum(r => r.SalesCost),
                    GrandMargin      = resultRows.Sum(r => r.Margin),
                    GrandBalanceQty  = resultRows.Sum(r => r.BalanceQty),
                    GrandBalanceCost = resultRows.Sum(r => r.BalanceCost),
                    Rows             = resultRows,
                };
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"خطأ في تقرير هامش الربح على الجداول {headlineTable} & {detailItemTable}: {ex.Message}",
                    ex);
            }
        }

        // =========================================================================
        // دوال مساعدة لبناء أعمدة SELECT الاختيارية
        // =========================================================================
        private static string BuildOptionalSelects(ProfitMarginRequestDto req)
        {
            var sb = new StringBuilder();
            sb.Append(req.IncludeBranch     ? "ISNULL(h.TRABRAID,'')   AS BranchId,\n"      : "'' AS BranchId,\n");
            sb.Append(req.IncludeDepartment ? "ISNULL(h.TRASECID,'')   AS DepartmentId,\n"  : "'' AS DepartmentId,\n");
            sb.Append(req.IncludeBatch      ? "ISNULL(d.TRADREFID,'')  AS BatchNumber,\n"   : "'' AS BatchNumber,\n");
            sb.Append(req.IncludeCostCenter ? "ISNULL(h.TRACCNAME,'')  AS CostCenterName,\n": "'' AS CostCenterName,\n");
            return sb.ToString();
        }

        private static string BuildOptionalGroupBy(ProfitMarginRequestDto req)
        {
            var parts = new List<string>();
            if (req.IncludeBranch)     parts.Add("h.TRABRAID");
            if (req.IncludeDepartment) parts.Add("h.TRASECID");
            if (req.IncludeBatch)      parts.Add("d.TRADREFID");
            if (req.IncludeCostCenter) parts.Add("h.TRACCNAME");
            return parts.Count > 0 ? ", " + string.Join(", ", parts) : "";
        }
    }
}
