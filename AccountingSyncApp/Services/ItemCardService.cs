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
    /// خدمة تقرير بطاقة الصنف - مستقلة تماماً
    /// تجمع بيانات من SunTransHeadline JOIN SunTransDetailItem
    /// وتحسب الرصيد الجاري (Running Balance) بـ SUM() OVER
    /// </summary>
    public class ItemCardService
    {
        private readonly AccountingDbContext _context;

        public ItemCardService(AccountingDbContext context)
        {
            _context = context;
        }

        // -------------------------------------------------------------------------
        // تصنيف كودات الحركات (مطابق لـ ItemBalancesService)
        // -------------------------------------------------------------------------

        // دخول للمخزون (وارد): مشتريات + مردودات مبيعات (بما في ذلك المطعم والمعلقة)
        private const string InTypes =
            "'421','423','602','604','606','608','634','802','804'";

        // خروج من المخزون (صادر): مبيعات + مردودات مشتريات (بما في ذلك المطعم والمعلقة)
        private const string OutTypes =
            "'601','603','605','607','633','801','803','422','424'";

        // جرد المخزن (يُعامل بإشارته الأصلية)
        private const string InventAdj = "'507'";

        // جميع الكودات المعنية (19 كود)
        private const string AllTypes =
            "'421','423','602','604','606','608','634','802','804'," +
            "'601','603','605','607','633','801','803','422','424'," +
            "'507'";

        // =========================================================================
        // حساب معامل التحويل الحقيقي
        // =========================================================================
        /// <summary>
        /// يُنتج تعبير SQL يحسب معامل التحويل الحقيقي مباشرة من TRADUNITSIZE:
        /// </summary>
        private const string RealPackFactorExpr = @"CAST(ISNULL(NULLIF(d.TRADUNITSIZE, ''), '1') AS DECIMAL(18,3))";


        // =========================================================================
        public async Task<ItemCardResponseDto> GetItemCardAsync(ItemCardRequestDto request)
        {
            // --- 1. تحديد لاحقة اسم الجدول (نفس منطق ItemMovementService) ---
            var yearsCount = await _context.Tssysyears.CountAsync();
            bool multipleYears = yearsCount > 1;

            string tableSuffix = multipleYears
                ? $"{request.Year}{request.BranchId}"
                : $"{request.BranchId}";

            string headlineTable   = $"SunTransHeadline{tableSuffix}";
            string detailItemTable = $"SunTransDetailItem{tableSuffix}";

            string fromDateId = request.FromDate.ToString("yyyyMMdd");
            string toDateId   = request.ToDate.ToString("yyyyMMdd");

            // --- 1.5. حساب الرصيد الافتتاحي (ما قبل الفترة) ---
            decimal openingBalance = 0;
            if (!request.ExcludePreviousBalance)
            {
                var obSql = new StringBuilder();
                obSql.Append($@"
                    SELECT ISNULL(SUM(
                        CASE
                            WHEN h.TRALSTID IN ({InTypes})
                                 THEN  ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                                       * ({RealPackFactorExpr})
                            WHEN h.TRALSTID IN ({OutTypes})
                                 THEN -ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                                       * ({RealPackFactorExpr})
                            WHEN h.TRALSTID = '507'
                                 THEN  CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3))
                                       * ({RealPackFactorExpr})
                            ELSE CAST(0 AS DECIMAL(18,3))
                        END
                    ), CAST(0 AS DECIMAL(18,3))) AS Value
                    FROM {detailItemTable} d
                    INNER JOIN {headlineTable} h ON d.TRAID = h.TRAID
                    WHERE h.TRADATEID < {{0}}
                      AND h.TRASTATUSDEL = '0'
                      AND h.TRALSTID IN ({AllTypes})
                ");

                var obParams = new List<object> { fromDateId };
                int obP = 1;

                // نفس فلاتر الصنف والوحدة والدفعة
                if (!string.IsNullOrWhiteSpace(request.ItemName))
                {
                    obSql.Append($" AND d.TRADITMNAME LIKE {{{obP}}}");
                    obParams.Add($"%{request.ItemName.Trim()}%");
                    obP++;
                }
                if (!string.IsNullOrWhiteSpace(request.UnitName))
                {
                    obSql.Append($" AND d.TRADITMUNIT = {{{obP}}}");
                    obParams.Add(request.UnitName.Trim());
                    obP++;
                }
                if (!string.IsNullOrWhiteSpace(request.BatchNumber))
                {
                    obSql.Append($" AND d.TRADREFID = {{{obP}}}");
                    obParams.Add(request.BatchNumber.Trim());
                    obP++;
                }
                if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
                {
                    obSql.Append($" AND h.TRASECID = {{{obP}}}");
                    obParams.Add(request.DepartmentId.Value.ToString());
                    obP++;
                }
                if (request.ExcludeOpeningBalance)
                {
                    obSql.Append(" AND h.TRALSTID <> '507'");
                }

                try
                {
                    var obResult = await _context.Database
                        .SqlQueryRaw<decimal>(obSql.ToString(), obParams.ToArray())
                        .FirstOrDefaultAsync();
                    openingBalance = obResult;
                }
                catch
                {
                    openingBalance = 0;
                }
            }

            // --- 2. بناء جملة SQL الديناميكية ---
            var sql = new StringBuilder();

            sql.Append($@"
                SELECT
                    ISNULL(d.TRADITMID,    '')                                                AS ItemNo,
                    ISNULL(h.TRABRAID,     '')                                                AS BranchName,
                    ISNULL(h.TRACCNAME,    '')                                                AS CostCenterName,
                    ISNULL(h.TRAACCNAME,   '')                                                AS AccountName,
                    ISNULL(h.TRASTORENAME, '')                                                AS StoreName,
                    ISNULL(h.TRAREFR,      '')                                                AS ReferenceNumber,
                    ISNULL(h.TRADATEID,    '')                                                AS DateNo,

                    CAST(ISNULL(NULLIF(d.TRADUNTVALUE,  ''), '0') AS DECIMAL(18,3))           AS UnitPrice,
                    ABS(CAST(ISNULL(NULLIF(d.TRADAMOUNT,''), '0') AS DECIMAL(18,3)))          AS TotalAmount,

                    ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''), '0') AS DECIMAL(18,3)))
                        * CAST(ISNULL(NULLIF(d.TRADITMUNTCOST,''), '0') AS DECIMAL(18,3))     AS TotalCost,

                    CAST(({RealPackFactorExpr}) AS VARCHAR(50))                               AS PackFactor,
                    ISNULL(d.TRADEXPIRE,   '')                                                AS ExpiryDate,
                    ISNULL(h.TRAPATNAME,   '')                                                AS CustomerName,
                    ISNULL(h.TRANO,        '')                                                AS TransactionNumber,
                    ISNULL(h.TRATITLE,     '')                                                AS Description,
                    ISNULL(h.TRADATE,      '')                                                AS Date,
                    ISNULL(d.TRADITMUNIT,  '')                                                AS ItemUnit,

                    CAST(ISNULL(NULLIF(d.TRADITMUNTCOST,''), '0') AS DECIMAL(18,3))           AS CostPrice,
                    ISNULL(d.TRADREFID,    '')                                                AS BatchNumber,

                    CASE
                        WHEN h.TRALSTID IN ({InTypes})
                             THEN ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                                  * ({RealPackFactorExpr})
                        WHEN h.TRALSTID = '507'
                         AND CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)) > 0
                             THEN ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                                  * ({RealPackFactorExpr})
                        ELSE 0
                    END AS QuantityIn,

                    CASE
                        WHEN h.TRALSTID IN ({OutTypes})
                             THEN ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                                  * ({RealPackFactorExpr})
                        WHEN h.TRALSTID = '507'
                         AND CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)) < 0
                             THEN ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                                  * ({RealPackFactorExpr})
                        ELSE 0
                    END AS QuantityOut,

                    {openingBalance.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)} +
                    SUM(
                        CASE
                            WHEN h.TRALSTID IN ({InTypes})
                                 THEN  ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                                       * ({RealPackFactorExpr})
                            WHEN h.TRALSTID IN ({OutTypes})
                                 THEN -ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                                       * ({RealPackFactorExpr})
                            WHEN h.TRALSTID = '507'
                                 THEN  CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3))
                                       * ({RealPackFactorExpr})
                            ELSE 0
                        END
                    ) OVER (ORDER BY h.TRADATEID ASC, h.TRATIMEID ASC, d.TRADINDEX ASC
                            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS Balance

                FROM {detailItemTable} d
                INNER JOIN {headlineTable} h ON d.TRAID = h.TRAID

                WHERE h.TRADATEID >= {{0}}
                  AND h.TRADATEID <= {{1}}
                  AND h.TRASTATUSDEL = '0'
                  AND h.TRALSTID IN ({AllTypes})
            ");

            var parameters = new List<object> { fromDateId, toDateId };
            int p = 2;

            // --- 3. فلترة اسم الصنف (LIKE جزئي) ---
            if (!string.IsNullOrWhiteSpace(request.ItemName))
            {
                sql.Append($" AND d.TRADITMNAME LIKE {{{p}}}");
                parameters.Add($"%{request.ItemName.Trim()}%");
                p++;
            }

            // --- 4. فلترة الوحدة (مطابقة تامة) ---
            if (!string.IsNullOrWhiteSpace(request.UnitName))
            {
                sql.Append($" AND d.TRADITMUNIT = {{{p}}}");
                parameters.Add(request.UnitName.Trim());
                p++;
            }

            // --- 5. فلترة رقم الدفعة (مطابقة تامة) ---
            if (!string.IsNullOrWhiteSpace(request.BatchNumber))
            {
                sql.Append($" AND d.TRADREFID = {{{p}}}");
                parameters.Add(request.BatchNumber.Trim());
                p++;
            }

            // --- 6. فلترة نوع الحركة ---
            if (!string.IsNullOrWhiteSpace(request.TransactionType) &&
                !string.Equals(request.TransactionType, "all", StringComparison.OrdinalIgnoreCase))
            {
                var types = request.TransactionType.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                   .Select(t => $"'{t.Trim()}'");
                sql.Append($" AND h.TRALSTID IN ({string.Join(",", types)})");
            }

            // --- 7. فلترة القسم (أمان نطاق المستخدم) ---
            if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
            {
                sql.Append($" AND h.TRASECID = {{{p}}}");
                parameters.Add(request.DepartmentId.Value.ToString());
                p++;
            }

            // --- 8. فلترة الأرصدة الافتتاحية والسابقة ---
            if (request.ExcludeOpeningBalance)
            {
                sql.Append(" AND h.TRALSTID <> '507'");
            }

            // --- 8. فلترة المستخدم ---
            if (request.UserId.HasValue && request.UserId.Value > 0)
            {
                sql.Append($" AND h.TRAUID = {{{p}}}");
                parameters.Add(request.UserId.Value.ToString());
                p++;
            }

            // --- 9. الترتيب (ASC إجباري لحساب الرصيد الجاري بشكل صحيح) ---
            sql.Append(" ORDER BY h.TRADATEID ASC, h.TRATIMEID ASC, d.TRADINDEX ASC");

            // --- 10. تنفيذ الاستعلام ---
            try
            {
                var rows = await _context.Database
                    .SqlQueryRaw<ItemCardRowDto>(sql.ToString(), parameters.ToArray())
                    .ToListAsync();

                return new ItemCardResponseDto
                {
                    RowCount         = rows.Count,
                    TotalQuantityIn  = rows.Sum(r => r.QuantityIn),
                    TotalQuantityOut = rows.Sum(r => r.QuantityOut),
                    FinalBalance     = rows.Any() ? rows.Last().Balance : openingBalance,
                    Rows             = rows,
                };
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"خطأ في تقرير بطاقة الصنف على الجداول {headlineTable} & {detailItemTable}: {ex.Message}",
                    ex);
            }
        }
    }
}

