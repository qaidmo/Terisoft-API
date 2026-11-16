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
    /// خدمة تقرير أرصدة الأصناف — مستقلة تماماً
    ///
    /// تستخدم استعلامَين SQL منفصلَين:
    ///   Query A — حركات الفترة (FROM → TO)
    ///   Query B — الرصيد الافتتاحي (قبل FROM)
    /// ثم تدمجهما في C# لحساب الكمية النهائية والتكاليف.
    /// </summary>
    public class ItemBalancesService
    {
        private readonly AccountingDbContext _context;

        public ItemBalancesService(AccountingDbContext context)
        {
            _context = context;
        }

        // -------------------------------------------------------------------------
        // تصنيف كودات الحركات
        // -------------------------------------------------------------------------

        // مبيعات أصلية (خروج من المخزون)
        private const string SalesOut = "'601','603','605','607','633','801','803'";

        // مردودات مبيعات (دخول للمخزون)
        private const string SalesRet = "'602','604','606','608','634','802','804'";

        // مشتريات أصلية (دخول للمخزون)
        private const string PurchIn  = "'421','423'";

        // مردودات مشتريات (خروج من المخزون)
        private const string PurchRet = "'422','424'";

        // جرد مخزن (قد يكون دخولاً أو خروجاً — نستخدم الإشارة كما هي)
        private const string InventAdj = "'507'";

        // جميع الكودات المعنية
        private const string AllCodes =
            "'601','602','603','604','605','606','607','608'," +
            "'421','422','423','424'," +
            "'633','634'," +
            "'801','802','803','804'," +
            "'507'";

        // =========================================================================
        public async Task<ItemBalancesResponseDto> GetItemBalancesAsync(
            ItemBalancesRequestDto request)
        {
            // --- 1. تحديد اسم الجدول ---
            var yearsCount = await _context.Tssysyears.CountAsync();
            bool multipleYears = yearsCount > 1;

            string suffix = multipleYears
                ? $"{request.Year}{request.BranchId}"
                : $"{request.BranchId}";

            string hTable = $"SunTransHeadline{suffix}";
            string dTable = $"SunTransDetailItem{suffix}";

            string fromId = request.FromDate.ToString("yyyyMMdd");
            string toId   = request.ToDate.ToString("yyyyMMdd");

            // --- 2. بناء أعمدة التجميع الاختيارية ---
            string optSelA  = BuildOptionalSelects(request);
            string optGrpA  = BuildOptionalGroupBy(request);

            // مفاتيح التجميع لـ Query B (الرصيد الافتتاحي) — نفس مفاتيح Query A الاختيارية
            string optGrpB  = BuildOpeningGroupBy(request);
            string optSelB  = BuildOpeningSelects(request);

            // --- 3. Query A — حركات الفترة ---
            var sqlA = new StringBuilder();
            sqlA.Append($@"
                SELECT
                    ISNULL(LTRIM(RTRIM(d.TRADITMID)),'')                                     AS ItemNo,
                    ISNULL(d.TRADITMNAME,'')                                                  AS ItemName,
                    ISNULL(d.TRADITMUNIT,'')                                                  AS ItemUnit,
                    CAST(ISNULL(NULLIF(itm.ITMSALE,''),'0') AS DECIMAL(18,5))                AS SellingPrice,
                    {optSelA}
                    MAX(CAST(ISNULL(NULLIF(d.TRADITMUNTCOST,''),'0') AS DECIMAL(18,3)))       AS UnitCost,

                    SUM(CASE WHEN h.TRALSTID IN ({SalesOut})
                             THEN ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                             ELSE 0 END)                                                      AS SalesQty,

                    SUM(CASE WHEN h.TRALSTID IN ({PurchIn})
                             THEN ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                             ELSE 0 END)                                                      AS PurchaseQty,

                    SUM(CASE WHEN h.TRALSTID IN ({PurchRet})
                             THEN ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                             ELSE 0 END)                                                      AS PurchReturnQty,

                    SUM(CASE WHEN h.TRALSTID IN ({SalesRet})
                             THEN ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                             ELSE 0 END)                                                      AS SalesReturnQty,

                    SUM(CASE WHEN h.TRALSTID IN ({InventAdj})
                             THEN CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3))
                             ELSE 0 END)                                                      AS InventoryQty,

                    SUM(CASE WHEN h.TRALSTID IN ({PurchIn},{SalesRet})
                             THEN ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                             ELSE 0 END)                                                      AS IncomingQty,

                    SUM(CASE WHEN h.TRALSTID IN ({SalesOut},{PurchRet})
                             THEN ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                             ELSE 0 END)                                                      AS OutgoingQty,

                    SUM(CASE WHEN h.TRALSTID IN ({PurchIn})
                             THEN ABS(CAST(ISNULL(NULLIF(d.TRADQTYDIS,''),'0') AS DECIMAL(18,3)))
                             ELSE 0 END)                                                      AS BonusQty,

                    SUM(CASE WHEN h.TRALSTID IN ({SalesOut})
                             THEN ABS(CAST(ISNULL(NULLIF(d.TRADQTYDIS,''),'0') AS DECIMAL(18,3)))
                             ELSE 0 END)                                                      AS SalesBonusQty

                FROM {dTable} d
                INNER JOIN {hTable}  h   ON d.TRAID = h.TRAID
                LEFT  JOIN SUNITEM   itm ON LTRIM(RTRIM(d.TRADITMID)) = LTRIM(RTRIM(itm.ITMNO))

                WHERE h.TRADATEID >= {{0}}
                  AND h.TRADATEID <= {{1}}
                  AND h.TRASTATUSDEL = '0'
                  AND h.TRALSTID IN ({AllCodes})
            ");

            var paramsA = new List<object> { fromId, toId };
            int pA = 2;

            if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
            {
                sqlA.Append($" AND h.TRASECID = {{{pA}}}");
                paramsA.Add(request.DepartmentId.Value.ToString());
                pA++;
            }
            if (request.UserId.HasValue && request.UserId.Value > 0)
            {
                sqlA.Append($" AND h.TRAUID = {{{pA}}}");
                paramsA.Add(request.UserId.Value.ToString());
                pA++;
            }

            sqlA.Append($@"
                GROUP BY
                    LTRIM(RTRIM(d.TRADITMID)),
                    d.TRADITMNAME,
                    d.TRADITMUNIT,
                    itm.ITMSALE
                    {optGrpA}
                ORDER BY ItemNo
            ");

            // --- 4. Query B — الرصيد الافتتاحي (قبل الفترة) ---
            var sqlB = new StringBuilder();
            sqlB.Append($@"
                SELECT
                    ISNULL(LTRIM(RTRIM(d.TRADITMID)),'')  AS ItemNo,
                    {optSelB}
                    SUM(CASE
                        WHEN h.TRALSTID IN ({PurchIn},{SalesRet})
                             THEN  ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                        WHEN h.TRALSTID IN ({SalesOut},{PurchRet})
                             THEN -ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                        WHEN h.TRALSTID IN ({InventAdj})
                             THEN  CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3))
                        ELSE 0
                    END)                                   AS OpeningQty,

                    SUM(CASE
                        WHEN h.TRALSTID IN ({PurchIn},{SalesRet})
                             THEN  ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                                   * CAST(ISNULL(NULLIF(d.TRADITMUNTCOST,''),'0') AS DECIMAL(18,3))
                        WHEN h.TRALSTID IN ({SalesOut},{PurchRet})
                             THEN -ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                                   * CAST(ISNULL(NULLIF(d.TRADITMUNTCOST,''),'0') AS DECIMAL(18,3))
                        WHEN h.TRALSTID IN ({InventAdj})
                             THEN  CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3))
                                   * CAST(ISNULL(NULLIF(d.TRADITMUNTCOST,''),'0') AS DECIMAL(18,3))
                        ELSE 0
                    END)                                   AS OpeningCost

                FROM {dTable} d
                INNER JOIN {hTable} h ON d.TRAID = h.TRAID

                WHERE h.TRADATEID < {{0}}
                  AND h.TRASTATUSDEL = '0'
                  AND h.TRALSTID IN ({AllCodes})
            ");

            var paramsB = new List<object> { fromId };
            int pB = 1;

            if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
            {
                sqlB.Append($" AND h.TRASECID = {{{pB}}}");
                paramsB.Add(request.DepartmentId.Value.ToString());
                pB++;
            }
            if (request.UserId.HasValue && request.UserId.Value > 0)
            {
                sqlB.Append($" AND h.TRAUID = {{{pB}}}");
                paramsB.Add(request.UserId.Value.ToString());
                pB++;
            }

            sqlB.Append($@"
                GROUP BY
                    LTRIM(RTRIM(d.TRADITMID))
                    {optGrpB}
            ");

            // --- 5. تنفيذ الاستعلامَين ---
            try
            {
                var movementRows = await _context.Database
                    .SqlQueryRaw<ItemBalancesMovementRaw>(sqlA.ToString(), paramsA.ToArray())
                    .ToListAsync();

                var openingRows = await _context.Database
                    .SqlQueryRaw<ItemBalancesOpeningRaw>(sqlB.ToString(), paramsB.ToArray())
                    .ToListAsync();

                // --- 6. دمج النتيجتَين ---
                // مفتاح الدمج: ItemNo + (StoreNo + ExpiryDate + BatchNumber إذا مُفعّلة)
                var openingLookup = openingRows.ToDictionary(
                    o => BuildOpeningKey(o, request),
                    o => o);

                var resultRows = movementRows.Select(m =>
                {
                    openingLookup.TryGetValue(BuildMovementKey(m, request), out var op);

                    decimal openingQty = request.ExcludePreviousBalance ? 0m : (op?.OpeningQty ?? 0m);

                    // كمية اخر المدة = افتتاحي + مشتريات + مردودات بيع - مبيعات - مردودات مشتريات + جرد
                    decimal endingQty = openingQty
                                        + m.PurchaseQty
                                        + m.SalesReturnQty
                                        - m.SalesQty
                                        - m.PurchReturnQty
                                        + m.InventoryQty;

                    decimal balanceQty      = endingQty;
                    decimal balanceCost     = balanceQty * m.UnitCost;
                    decimal balanceAtSell   = balanceQty * m.SellingPrice;
                    decimal otherQty        = 0;

                    return new ItemBalancesRowDto
                    {
                        ItemNo           = m.ItemNo,
                        ItemName         = m.ItemName,
                        ItemUnit         = m.ItemUnit,
                        SellingPrice     = m.SellingPrice,
                        BranchId         = m.BranchId,
                        DepartmentId     = m.DepartmentId,
                        StoreNo          = m.StoreNo,
                        StoreName        = m.StoreName,
                        ExpiryDate       = m.ExpiryDate,
                        BatchNumber      = m.BatchNumber,
                        BalanceQty       = balanceQty,
                        UnitCost         = m.UnitCost,
                        BalanceCost      = balanceCost,
                        OpeningQty       = openingQty,
                        PurchaseQty      = m.PurchaseQty,
                        SalesQty         = m.SalesQty,
                        OtherQty         = otherQty,
                        EndingQty        = endingQty,
                        BalanceAtSelling = balanceAtSell,
                        IncomingQty      = m.IncomingQty,
                        OutgoingQty      = m.OutgoingQty,
                        BuyQty           = m.PurchaseQty,
                        SellQty          = m.SalesQty,
                        ReturnSellQty    = m.SalesReturnQty,
                        BonusQty         = m.BonusQty,
                        SalesBonusQty    = m.SalesBonusQty,
                        ItemBalance      = balanceQty,   // نفس الرصيد — يُعرض فقط عند IncludeStore
                    };
                }).ToList();

                // --- 7. تطبيق الفلاتر ---
                if (request.ExcludeZeros)
                    resultRows = resultRows.Where(r => r.BalanceQty != 0m).ToList();

                if (request.NegativeQtyOnly)
                    resultRows = resultRows.Where(r => r.BalanceQty < 0m).ToList();

                if (request.ZeroBalancesOnly)
                    resultRows = resultRows.Where(r => r.BalanceQty == 0m).ToList();

                // --- 8. المجاميع الكلية ---
                return new ItemBalancesResponseDto
                {
                    RowCount         = resultRows.Count,
                    GrandBalance     = resultRows.Sum(r => r.BalanceQty),
                    GrandBalanceCost = resultRows.Sum(r => r.BalanceCost),
                    GrandSalesQty    = resultRows.Sum(r => r.SalesQty),
                    GrandPurchaseQty = resultRows.Sum(r => r.PurchaseQty),
                    Rows             = resultRows,
                };
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"خطأ في تقرير أرصدة الأصناف على الجداول {hTable} & {dTable}: {ex.Message}",
                    ex);
            }
        }

        // =========================================================================
        // دوال مساعدة — بناء أعمدة SELECT و GROUP BY الاختيارية
        // =========================================================================

        /// <summary>أعمدة SELECT الاختيارية لاستعلام الحركات (Query A)</summary>
        private static string BuildOptionalSelects(ItemBalancesRequestDto req)
        {
            var sb = new StringBuilder();
            sb.Append(req.IncludeBranch
                ? "ISNULL(h.TRABRAID,'')       AS BranchId,\n"
                : "''                           AS BranchId,\n");

            sb.Append(req.IncludeDepartment
                ? "ISNULL(h.TRASECID,'')       AS DepartmentId,\n"
                : "''                           AS DepartmentId,\n");

            sb.Append(req.IncludeStore
                ? "ISNULL(h.TRASTOREID,'')     AS StoreNo,\n    ISNULL(h.TRASTORENAME,'')   AS StoreName,\n"
                : "''                           AS StoreNo,\n    ''                           AS StoreName,\n");

            sb.Append(req.IncludeExpiry
                ? "ISNULL(d.TRADEXPIRE,'')     AS ExpiryDate,\n"
                : "''                           AS ExpiryDate,\n");

            sb.Append(req.IncludeBatch
                ? "ISNULL(d.TRADREFID,'')      AS BatchNumber,\n"
                : "''                           AS BatchNumber,\n");

            return sb.ToString();
        }

        /// <summary>إضافة للـ GROUP BY لـ Query A</summary>
        private static string BuildOptionalGroupBy(ItemBalancesRequestDto req)
        {
            var parts = new List<string>();
            if (req.IncludeBranch)     parts.Add("h.TRABRAID");
            if (req.IncludeDepartment) parts.Add("h.TRASECID");
            if (req.IncludeStore)      parts.Add("h.TRASTOREID, h.TRASTORENAME");
            if (req.IncludeExpiry)     parts.Add("d.TRADEXPIRE");
            if (req.IncludeBatch)      parts.Add("d.TRADREFID");
            return parts.Count > 0 ? ", " + string.Join(", ", parts) : "";
        }

        /// <summary>أعمدة SELECT لاستعلام الرصيد الافتتاحي (Query B)</summary>
        private static string BuildOpeningSelects(ItemBalancesRequestDto req)
        {
            var sb = new StringBuilder();
            if (req.IncludeStore)
            {
                sb.Append("ISNULL(h.TRASTOREID,'') AS StoreNo,\n");
            }
            else
            {
                sb.Append("'' AS StoreNo,\n");
            }

            if (req.IncludeExpiry)
            {
                sb.Append("ISNULL(d.TRADEXPIRE,'') AS ExpiryDate,\n");
            }
            else
            {
                sb.Append("'' AS ExpiryDate,\n");
            }

            if (req.IncludeBatch)
            {
                sb.Append("ISNULL(d.TRADREFID,'') AS BatchNumber,\n");
            }
            else
            {
                sb.Append("'' AS BatchNumber,\n");
            }

            return sb.ToString();
        }

        /// <summary>إضافة للـ GROUP BY لـ Query B</summary>
        private static string BuildOpeningGroupBy(ItemBalancesRequestDto req)
        {
            var parts = new List<string>();
            if (req.IncludeStore)   parts.Add("h.TRASTOREID");
            if (req.IncludeExpiry)  parts.Add("d.TRADEXPIRE");
            if (req.IncludeBatch)   parts.Add("d.TRADREFID");
            return parts.Count > 0 ? ", " + string.Join(", ", parts) : "";
        }

        /// <summary>بناء مفتاح الدمج من صف حركات (Query A)</summary>
        private static string BuildMovementKey(ItemBalancesMovementRaw m, ItemBalancesRequestDto req)
        {
            var key = m.ItemNo;
            if (req.IncludeStore)  key += "|" + m.StoreNo;
            if (req.IncludeExpiry) key += "|" + m.ExpiryDate;
            if (req.IncludeBatch)  key += "|" + m.BatchNumber;
            return key;
        }

        /// <summary>بناء مفتاح الدمج من صف رصيد افتتاحي (Query B)</summary>
        private static string BuildOpeningKey(ItemBalancesOpeningRaw o, ItemBalancesRequestDto req)
        {
            var key = o.ItemNo;
            if (req.IncludeStore)  key += "|" + o.StoreNo;
            if (req.IncludeExpiry) key += "|" + o.ExpiryDate;
            if (req.IncludeBatch)  key += "|" + o.BatchNumber;
            return key;
        }
    }
}
