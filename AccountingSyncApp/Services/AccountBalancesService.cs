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
    /// خدمة تقرير أرصدة الحسابات — مستقلة تماماً
    ///
    /// تستخدم استعلام SQL واحد مع Conditional Aggregation
    /// لحساب الأرصدة الافتتاحية وحركة الفترة في مسح واحد.
    /// </summary>
    public class AccountBalancesService
    {
        private readonly AccountingDbContext _context;

        public AccountBalancesService(AccountingDbContext context)
        {
            _context = context;
        }

        // =========================================================================
        public async Task<AccountBalancesResponseDto> GetAccountBalancesAsync(
            AccountBalancesRequestDto request)
        {
            // --- 1. تحديد اسم الجدول الديناميكي ---
            var yearsCount = await _context.Tssysyears.CountAsync();
            bool multipleYears = yearsCount > 1;

            string suffix = multipleYears
                ? $"{request.Year}{request.BranchId}"
                : $"{request.BranchId}";

            string jeTable   = $"SunTransJe{suffix}";
            string headTable = $"SunTransHeadline{suffix}";

            string fromId = request.FromDate.ToString("yyyyMMdd");
            string toId   = request.ToDate.ToString("yyyyMMdd");

            // --- 2. بناء أعمدة SELECT و GROUP BY حسب وضع التجميع ---
            string selectCols = BuildSelectColumns(request.GroupingMode);
            string groupByCols = BuildGroupByColumns(request.GroupingMode);
            string joinClauses = BuildJoinClauses(request.GroupingMode);

            // --- 3. بناء الاستعلام ---
            var sql = new StringBuilder();
            sql.Append($@"
                SELECT
                    {selectCols}

                    -- الأرصدة الافتتاحية (قبل FromDate أو قيد افتتاحي 112)
                    SUM(CASE WHEN je.TRADSIGD = '1'
                              AND (head.TRADATEID < {{0}} OR head.TRALSTID = '112')
                         THEN CAST(ISNULL(NULLIF(je.TRADVALUE, ''), '0') AS DECIMAL(18,3))
                         ELSE 0 END) AS OpeningDebit,

                    SUM(CASE WHEN je.TRADSIGC = '1'
                              AND (head.TRADATEID < {{0}} OR head.TRALSTID = '112')
                         THEN CAST(ISNULL(NULLIF(je.TRADVALUE, ''), '0') AS DECIMAL(18,3))
                         ELSE 0 END) AS OpeningCredit,

                    -- حركة الفترة (FromDate إلى ToDate — باستثناء القيد الافتتاحي)
                    SUM(CASE WHEN je.TRADSIGD = '1'
                              AND head.TRADATEID >= {{0}} AND head.TRADATEID <= {{1}}
                              AND head.TRALSTID != '112'
                         THEN CAST(ISNULL(NULLIF(je.TRADVALUE, ''), '0') AS DECIMAL(18,3))
                         ELSE 0 END) AS PeriodDebit,

                    SUM(CASE WHEN je.TRADSIGC = '1'
                              AND head.TRADATEID >= {{0}} AND head.TRADATEID <= {{1}}
                              AND head.TRALSTID != '112'
                         THEN CAST(ISNULL(NULLIF(je.TRADVALUE, ''), '0') AS DECIMAL(18,3))
                         ELSE 0 END) AS PeriodCredit

                FROM {jeTable} je
                INNER JOIN {headTable} head ON je.TRAID = head.TRAID
                {joinClauses}

                WHERE (head.TRADATEID <= {{1}} OR head.TRALSTID = '112')
                  AND head.TRASTATUSDEL = '0'
            ");

            var parameters = new List<object> { fromId, toId };
            int pIdx = 2;

            // --- 4. فلتر القسم ---
            if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
            {
                sql.Append($" AND head.TRASECID = {{{pIdx}}}");
                parameters.Add(request.DepartmentId.Value.ToString());
                pIdx++;
            }

            // --- 5. فلتر العملة ---
            if (request.CurrencyId.HasValue && request.CurrencyId.Value > 0)
            {
                sql.Append($" AND je.TRADCURID = {{{pIdx}}}");
                parameters.Add(request.CurrencyId.Value.ToString());
                pIdx++;
            }

            sql.Append($@"
                GROUP BY {groupByCols}
                ORDER BY EntityId
            ");

            // --- 6. تنفيذ الاستعلام ---
            try
            {
                var rawRows = await _context.Database
                    .SqlQueryRaw<AccountBalancesRawRow>(sql.ToString(), parameters.ToArray())
                    .ToListAsync();

                // --- 7. تحويل وحساب الأعمدة المشتقة في C# ---
                var resultRows = rawRows.Select(r =>
                {
                    decimal totalD = r.OpeningDebit + r.PeriodDebit;
                    decimal totalC = r.OpeningCredit + r.PeriodCredit;
                    decimal net    = totalD - totalC;

                    return new AccountBalancesRowDto
                    {
                        EntityId         = r.EntityId,
                        EntityName       = r.EntityName,
                        AccountName      = r.AccountName,
                        BeneficiaryName  = r.BeneficiaryName,
                        CostCenterName   = r.CostCenterName,
                        OpeningDebit     = r.OpeningDebit,
                        OpeningCredit    = r.OpeningCredit,
                        PeriodDebit      = r.PeriodDebit,
                        PeriodCredit     = r.PeriodCredit,
                        TotalDebit       = totalD,
                        TotalCredit      = totalC,
                        BalanceDebit     = net > 0 ? net : 0,
                        BalanceCredit    = net < 0 ? Math.Abs(net) : 0,
                        NetBalance       = net,
                        Value            = Math.Abs(net),
                    };
                }).ToList();

                // --- 8. المجاميع الكلية ---
                return new AccountBalancesResponseDto
                {
                    Rows              = resultRows,
                    RowCount          = resultRows.Count,
                    GrandOpeningDebit  = resultRows.Sum(r => r.OpeningDebit),
                    GrandOpeningCredit = resultRows.Sum(r => r.OpeningCredit),
                    GrandPeriodDebit   = resultRows.Sum(r => r.PeriodDebit),
                    GrandPeriodCredit  = resultRows.Sum(r => r.PeriodCredit),
                    GrandTotalDebit    = resultRows.Sum(r => r.TotalDebit),
                    GrandTotalCredit   = resultRows.Sum(r => r.TotalCredit),
                    GrandBalanceDebit  = resultRows.Sum(r => r.BalanceDebit),
                    GrandBalanceCredit = resultRows.Sum(r => r.BalanceCredit),
                    GrandNetBalance    = resultRows.Sum(r => r.NetBalance),
                    GrandValue         = resultRows.Sum(r => r.Value),
                };
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"خطأ في تقرير أرصدة الحسابات على الجداول {jeTable} & {headTable}: {ex.Message}",
                    ex);
            }
        }

        // =========================================================================
        // دوال مساعدة — بناء أعمدة SELECT و GROUP BY حسب وضع التجميع
        // =========================================================================

        private static string BuildSelectColumns(string mode)
        {
            return mode switch
            {
                // Bug 3 fix: Group by parent/main account (4-digit) via ACCMAID from SUNDATAACCDIR
                "byMainAccount" =>
                    @"ISNULL(LTRIM(RTRIM(aacc.ACCMAID)), '') AS EntityId,
                      ISNULL(acc.ACCNAME, '')                AS EntityName,
                      ''                                     AS AccountName,
                      ''                                     AS BeneficiaryName,
                      ''                                     AS CostCenterName,
                    ",

                "byTransactionName" =>
                    @"ISNULL(head.TRALSTID, '')               AS EntityId,
                      ISNULL(head.TRATITLE, '')               AS EntityName,
                      ''                                     AS AccountName,
                      ''                                     AS BeneficiaryName,
                      ''                                     AS CostCenterName,
                    ",

                // Bug 1 fix: byMovementAccount now uses TRADACCID (primary account) + SUNDATAACCDIR
                "byMovementAccount" =>
                    @"ISNULL(LTRIM(RTRIM(je.TRADACCID)), '') AS EntityId,
                      ISNULL(aacc.ACCNAME, '')               AS EntityName,
                      ''                                     AS AccountName,
                      ''                                     AS BeneficiaryName,
                      ''                                     AS CostCenterName,
                    ",

                // Bug 2 fix: byCostCenter no longer includes acc.ACCNAME — groups strictly by cost center
                "byCostCenter" =>
                    @"ISNULL(LTRIM(RTRIM(je.TRADCCID)), '')  AS EntityId,
                      ISNULL(je.TRADCCNAME, '')              AS EntityName,
                      ''                                     AS AccountName,
                      ''                                     AS BeneficiaryName,
                      ''                                     AS CostCenterName,
                    ",

                "byBeneficiary" =>
                    @"ISNULL(je.TRADDATNAME, '')              AS EntityId,
                      ISNULL(je.TRADDATNAME, '')              AS EntityName,
                      ISNULL(acc.ACCNAME, '')                 AS AccountName,
                      ISNULL(je.TRADDATNAME, '')              AS BeneficiaryName,
                      ''                                     AS CostCenterName,
                    ",

                "byCostCenterAndAccount" =>
                    @"ISNULL(LTRIM(RTRIM(je.TRADACCID)), '') AS EntityId,
                      ISNULL(acc.ACCNAME, '')                AS EntityName,
                      ISNULL(acc.ACCNAME, '')                AS AccountName,
                      ''                                     AS BeneficiaryName,
                      ISNULL(je.TRADCCNAME, '')              AS CostCenterName,
                    ",

                // Bug 1 fix: analyticalAll now uses TRADTACCID (contra/analytical side) + SUNACCDIR
                "analyticalAll" =>
                    @"ISNULL(LTRIM(RTRIM(je.TRADTACCID)), '') AS EntityId,
                      ISNULL(acc.ACCNAME, '')                AS EntityName,
                      ''                                     AS AccountName,
                      ''                                     AS BeneficiaryName,
                      ''                                     AS CostCenterName,
                    ",

                _ => // fallback → byMainAccount
                    @"ISNULL(LTRIM(RTRIM(je.TRADACCID)), '') AS EntityId,
                      ISNULL(acc.ACCNAME, '')                AS EntityName,
                      ''                                     AS AccountName,
                      ''                                     AS BeneficiaryName,
                      ''                                     AS CostCenterName,
                    ",
            };
        }

        private static string BuildGroupByColumns(string mode)
        {
            return mode switch
            {
                // Bug 3 fix: group by parent account code (ACCMAID) and its name
                "byMainAccount"          => "LTRIM(RTRIM(aacc.ACCMAID)), acc.ACCNAME",
                "byTransactionName"      => "head.TRALSTID, head.TRATITLE",
                // Bug 2 fix: byCostCenter groups only by cost center — acc.ACCNAME removed
                "byCostCenter"           => "LTRIM(RTRIM(je.TRADCCID)), je.TRADCCNAME",
                "byBeneficiary"          => "je.TRADDATNAME, acc.ACCNAME",
                "byCostCenterAndAccount" => "LTRIM(RTRIM(je.TRADACCID)), acc.ACCNAME, je.TRADCCNAME",
                // Bug 1 fix: analyticalAll groups by TRADTACCID (contra/analytical side)
                "analyticalAll"          => "LTRIM(RTRIM(je.TRADTACCID)), acc.ACCNAME",
                // Bug 1 fix: byMovementAccount groups by TRADACCID (primary account) + aacc.ACCNAME
                "byMovementAccount"      => "LTRIM(RTRIM(je.TRADACCID)), aacc.ACCNAME",
                _                        => "LTRIM(RTRIM(je.TRADACCID)), acc.ACCNAME",
            };
        }

        private static string BuildJoinClauses(string mode)
        {
            return mode switch
            {
                // Bug 3 fix: byMainAccount uses a double JOIN to resolve the 4-digit parent account
                // Step 1: join SUNDATAACCDIR to get ACCMAID (the parent account ID)
                // Step 2: join SUNACCDIR on ACCMAID to get the parent account name
                "byMainAccount" =>
                    @"INNER JOIN SUNDATAACCDIR aacc ON LTRIM(RTRIM(je.TRADACCID)) = LTRIM(RTRIM(aacc.ACCID))
                      LEFT  JOIN SUNACCDIR     acc  ON LTRIM(RTRIM(aacc.ACCMAID)) = LTRIM(RTRIM(acc.ACCID))",

                // Bug 1 fix: analyticalAll now looks up the CONTRA account (TRADTACCID) in SUNACCDIR
                "analyticalAll" =>
                    "LEFT JOIN SUNACCDIR acc ON LTRIM(RTRIM(je.TRADTACCID)) = LTRIM(RTRIM(acc.ACCID))",

                // Bug 1 fix: byMovementAccount now looks up the PRIMARY account (TRADACCID) in SUNDATAACCDIR
                "byMovementAccount" =>
                    "INNER JOIN SUNDATAACCDIR aacc ON LTRIM(RTRIM(je.TRADACCID)) = LTRIM(RTRIM(aacc.ACCID))",

                _ =>
                    "LEFT JOIN SUNACCDIR acc ON LTRIM(RTRIM(je.TRADACCID)) = LTRIM(RTRIM(acc.ACCID))",
            };
        }
    }
}
