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
    /// خدمة تقرير حركة الأصناف - مستقلة تماماً عن IReportsService
    /// تجمع بيانات من SunTransHeadline JOIN SunTransDetailItem JOIN SUNITEM/SUNITEMGROUP
    /// </summary>
    public class ItemMovementService
    {
        private readonly AccountingDbContext _context;

        public ItemMovementService(AccountingDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// أنواع الحركات التي تحتوي على أصناف (بناءً على invoiceTypeMap في TransactionController):
        /// 601-608: مبيعات/POS
        /// 421-424: مشتريات
        /// 633, 634: مبيعات خاصة
        /// 801-804: طلبات عملاء
        /// 507: جرد مخزن
        /// </summary>
        private static readonly string[] AllValidTransactionTypes = new[]
        {
            "601", "602", "603", "604", "605", "606", "607", "608",
            "421", "422", "423", "424",
            "633", "634",
            "801", "802", "803", "804",
            "507"
        };

        public async Task<ItemMovementResponseDto> GetItemMovementAsync(ItemMovementRequestDto request)
        {
            // --- 1. تحديد لاحقة اسم الجدول (نفس منطق ReportsService) ---
            var yearsCount = await _context.Tssysyears.CountAsync();
            bool multipleYears = yearsCount > 1;

            string tableSuffix = multipleYears
                ? $"{request.Year}{request.BranchId}"
                : $"{request.BranchId}";

            string headlineTable   = $"SunTransHeadline{tableSuffix}";
            string detailItemTable = $"SunTransDetailItem{tableSuffix}";

            string fromDateId = request.FromDate.ToString("yyyyMMdd");
            string toDateId   = request.ToDate.ToString("yyyyMMdd");

            // --- 2. بناء جملة SQL الديناميكية المُحسنة مع جدول مؤقت مفهرس عند فلترة المجموعة ---
            bool hasItemGroupFilter = !string.IsNullOrWhiteSpace(request.ItemGroupId);
            var parameters = new List<object>();
            var sql = new StringBuilder();
            int p = 0;

            if (hasItemGroupFilter)
            {
                sql.Append($@"
                    SET NOCOUNT ON;
                    CREATE TABLE #CleanGroupItems (
                        ItemKey VARCHAR(50) COLLATE DATABASE_DEFAULT NOT NULL,
                        PRIMARY KEY CLUSTERED (ItemKey)
                    );
                    INSERT INTO #CleanGroupItems (ItemKey)
                    SELECT DISTINCT itm.ITMNO
                    FROM SUNITEM itm
                    WHERE LTRIM(RTRIM(itm.ITMGRPID)) = {{{p}}}
                    UNION
                    SELECT DISTINCT LTRIM(RTRIM(itm.ITMNO))
                    FROM SUNITEM itm
                    WHERE LTRIM(RTRIM(itm.ITMGRPID)) = {{{p + 1}}};
                ");
                parameters.Add(request.ItemGroupId.Trim());
                parameters.Add(request.ItemGroupId.Trim());
                p += 2;
            }

            int pFromDate = p;
            int pToDate   = p + 1;
            parameters.Add(fromDateId);
            parameters.Add(toDateId);
            p += 2;

            // ABS() على الكميات والقيم حتى تكون دائماً موجبة (المردودات لا تظهر بالسالب)
            sql.Append($@"
                SELECT
                    ISNULL(d.TRADITMID, '')                                                                 AS ItemNo,
                    ISNULL(d.TRADITMNAME, '')                                                               AS ItemName,
                    ISNULL(d.TRADITMUNIT, '')                                                               AS ItemUnit,
                    ISNULL(d.TRADUNITSIZE, '')                                                              AS PackFactor,

                    ABS(CAST(ISNULL(NULLIF(d.TRADQTY,    ''), '0') AS DECIMAL(18,3)))                     AS Quantity,
                    ABS(CAST(ISNULL(NULLIF(d.TRADQTYDIS, ''), '0') AS DECIMAL(18,3)))                     AS FreeQuantity,

                    CAST(ISNULL(NULLIF(d.TRADUNTVALUE,  ''), '0') AS DECIMAL(18,3))                       AS UnitPrice,
                    ABS(CAST(ISNULL(NULLIF(d.TRADAMOUNT,''), '0') AS DECIMAL(18,3)))                      AS TotalAmount,
                    CAST(ISNULL(NULLIF(d.TRADNETVALUE,  ''), '0') AS DECIMAL(18,3))                       AS NetUnitPrice,

                    ABS(CAST(ISNULL(NULLIF(d.TRADQTYDIS,''), '0') AS DECIMAL(18,3)))
                        * CAST(ISNULL(NULLIF(d.TRADUNTVALUE,''), '0') AS DECIMAL(18,3))                   AS FreeValue,

                    ISNULL(h.TRADATE,  '')                                                                  AS Date,
                    ISNULL(h.TRATITLE, '')                                                                  AS Description,
                    ISNULL(h.TRANO,    '')                                                                  AS TransactionNumber,
                    ISNULL(h.TRAREFR,  '')                                                                  AS ReferenceNumber,
                    ISNULL(h.TRAPATNAME, '')                                                                AS CustomerName,

                    ISNULL(d.TRADEXPIRE, '')                                                                AS ExpiryDate,
                    ISNULL(d.TRADREFID,  '')                                                                AS BatchNumber,

                    ABS(CAST(ISNULL(NULLIF(d.TRADNETAMOUNT,''), '0') AS DECIMAL(18,3)))                   AS NetAmount,
                    ISNULL(d.TRADCURCHAR, '')                                                               AS Currency,

                    CAST(ISNULL(NULLIF(d.TRADITMUNTCOST,''), '0') AS DECIMAL(18,3))                       AS CostPrice,

                    ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''), '0') AS DECIMAL(18,3)))
                        * CAST(ISNULL(NULLIF(d.TRADITMUNTCOST,''), '0') AS DECIMAL(18,3))                 AS TotalCost,

                    (ABS(CAST(ISNULL(NULLIF(d.TRADNETAMOUNT,''), '0') AS DECIMAL(18,3)))
                        * CAST(ISNULL(NULLIF(h.TRACURVALUE,''), '1') AS DECIMAL(18,5)))
                    - (ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''), '0') AS DECIMAL(18,3)))
                        * CAST(ISNULL(NULLIF(d.TRADITMUNTCOST,''), '0') AS DECIMAL(18,3)))                AS Margin,

                    CAST(ISNULL(NULLIF(h.TRACURVALUE,''), '1') AS DECIMAL(18,5))                          AS ExchangeRate,

                    ABS(CAST(ISNULL(NULLIF(d.TRADNETAMOUNT,''), '0') AS DECIMAL(18,3)))
                        * CAST(ISNULL(NULLIF(h.TRACURVALUE,''), '1') AS DECIMAL(18,5))                    AS LocalNetAmount,

                    ISNULL(h.TRAPATID,   '')                                                                AS AccountNumber,
                    ISNULL(h.TRAACCNAME, '')                                                                AS AccountName,
                    ISNULL(h.TRACCNAME,  '')                                                                AS CostCenterName,
                    ISNULL(h.TRADELNAME, '')                                                                AS SalespersonName,
                    ISNULL(h.TRACOLNAME, '')                                                                AS CollectorName,
                    ISNULL(h.TRABRAID,   '')                                                                AS BranchId,
                    ISNULL(h.TRASECID,   '')                                                                AS DepartmentId,
                    ISNULL(h.TRASTOREID,   '')                                                              AS StoreNo,
                    ISNULL(h.TRASTORENAME, '')                                                              AS StoreName,
                    ISNULL(h.TRAUNAME,   '')                                                                AS UserName,
                    ISNULL(h.TRALSTID,   '')                                                                AS TransactionTypeId,
                    ISNULL(h.TRATITLE,   '')                                                                AS TransactionTypeName

                FROM {detailItemTable} d
                INNER JOIN {headlineTable} h ON d.TRAID = h.TRAID
                {(hasItemGroupFilter ? "INNER JOIN #CleanGroupItems grp ON d.TRADITMID = grp.ItemKey" : "")}

                WHERE h.TRADATEID >= {{{pFromDate}}}
                  AND h.TRADATEID <= {{{pToDate}}}
                  AND h.TRASTATUSDEL = '0'
            ");

            // --- 3. فلترة نوع الحركة ---
            if (!string.IsNullOrWhiteSpace(request.TransactionType) &&
                !string.Equals(request.TransactionType, "all", StringComparison.OrdinalIgnoreCase))
            {
                // تأكد من أن الكود المطلوب موجود في قائمة الأنواع المسموحة
                if (Array.IndexOf(AllValidTransactionTypes, request.TransactionType) >= 0)
                {
                    sql.Append($" AND h.TRALSTID = {{{p}}}");
                    parameters.Add(request.TransactionType);
                    p++;
                }
                else
                {
                    // كود غير معروف — نُطبق مجموعة كاملة آمنة
                    string inList = string.Join("','", AllValidTransactionTypes);
                    sql.Append($" AND h.TRALSTID IN ('{inList}')");
                }
            }
            else
            {
                // الكل — نفلتر بالمجموعة الكاملة المسموحة
                string inList = string.Join("','", AllValidTransactionTypes);
                sql.Append($" AND h.TRALSTID IN ('{inList}')");
            }

            // --- 4. فلترة القسم (أمان نطاق المستخدم) ---
            if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
            {
                sql.Append($" AND h.TRASECID = {{{p}}}");
                parameters.Add(request.DepartmentId.Value.ToString());
                p++;
            }

            // --- 5. فلترة المستخدم (المستخدم العادي يرى حركاته فقط) ---
            if (request.UserId.HasValue && request.UserId.Value > 0)
            {
                sql.Append($" AND h.TRAUID = {{{p}}}");
                parameters.Add(request.UserId.Value.ToString());
                p++;
            }

            sql.Append(" ORDER BY h.TRADATEID DESC, h.TRATIMEID DESC, d.TRADINDEX ASC;");
            if (hasItemGroupFilter)
            {
                sql.Append(" DROP TABLE IF EXISTS #CleanGroupItems;");
            }

            // --- 7. تنفيذ الاستعلام ---
            try
            {
                var rows = await _context.Database
                    .SqlQueryRaw<ItemMovementRowDto>(sql.ToString(), parameters.ToArray())
                    .ToListAsync();

                return new ItemMovementResponseDto
                {
                    RowCount         = rows.Count,
                    GrandTotalAmount = rows.Sum(r => r.TotalAmount * r.ExchangeRate),
                    GrandNetAmount   = rows.Sum(r => r.LocalNetAmount),
                    GrandTotalCost   = rows.Sum(r => r.TotalCost),
                    GrandMargin      = rows.Sum(r => r.Margin),
                    Rows             = rows,
                };
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"خطأ في تقرير حركة الأصناف على الجداول {headlineTable} & {detailItemTable}: {ex.Message}",
                    ex);
            }
        }
    }
}
