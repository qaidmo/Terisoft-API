using AccountingSyncAPP.Data;
using AccountingSyncAPP.Data.Dtos;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AccountingSyncAPP.Services
{
    public class ReportsService : IReportsService
    {
        private readonly AccountingDbContext _context;

        public ReportsService(AccountingDbContext context)
        {
            _context = context;
        }

        public async Task<AccountStatementResponseDto> GetAccountStatementAsync(AccountStatementRequestDto request)
        {
            var yearsCount = await _context.Tssysyears.CountAsync();
            bool multipleYears = yearsCount > 1;

            string tableSuffix;
            if (multipleYears)
            {
                tableSuffix = $"{request.Year}{request.BranchId}";
            }
            else
            {
                tableSuffix = $"{request.BranchId}";
            }

            string jeTableName = $"SunTransJe{tableSuffix}";
            string headlineTableName = $"SunTransHeadline{tableSuffix}";

            // Department row-level filter (only applied when DepartmentId is provided)
            string departmentWhereClause = request.DepartmentId.HasValue
                ? $"AND head.TraSecId = '{request.DepartmentId.Value}'"
                : "";

            string fromDateId = request.FromDate.ToString("yyyyMMdd");
            string toDateId = request.ToDate.ToString("yyyyMMdd");

            string query = $@"
                SELECT 
                    je.TRAID AS TraId,
                    ISNULL(head.TRANO, '') AS TraNo,
                    ISNULL(je.TRADDATE, '') AS Date,
                    ISNULL(head.TRATITLE, '') AS Type,
                    ISNULL(je.TRADDETAIL, '') AS Description,
                    ISNULL(je.TRADDATNAME, '') AS Beneficiary,
                    
                    CASE 
                        WHEN je.TRADSIGD = '1' THEN 
                            CAST(ISNULL(NULLIF(je.TRADVALUE, ''), '0') AS DECIMAL(18, 3))
                        ELSE CAST(0 AS DECIMAL(18,3)) 
                    END AS Debit,

                    CASE 
                        WHEN je.TRADSIGC = '1' THEN 
                            CAST(ISNULL(NULLIF(je.TRADVALUE, ''), '0') AS DECIMAL(18, 3))
                        ELSE CAST(0 AS DECIMAL(18,3)) 
                    END AS Credit,
                    
                    CAST(0 AS DECIMAL(18,3)) AS Balance

                FROM {jeTableName} je
                INNER JOIN {headlineTableName} head ON je.TRAID = head.TRAID
                WHERE 
                    (je.TRADACCID = {{0}} OR je.TRADTACCID = {{0}}) 
                    AND je.TRADCURID = {{1}} 
                    AND je.TRADDATEID >= {{2}} 
                    AND je.TRADDATEID <= {{3}} 
                    -- Removed: AND head.TraLstId != '112'
                    -- '112' (opening-balance journal entries) are now shown as
                    -- regular rows in the period list instead of being silently
                    -- routed into the opening-balance scalar. The complementary
                    -- change in openingBalanceQuery prevents double-counting.
                    AND head.TraStatusDel = '0'
                    {departmentWhereClause}
                ORDER BY je.TRADDATEID, je.TRAID";

            string openingBalanceQuery = $@"
                SELECT ISNULL(SUM(
                    CASE WHEN je.TRADSIGD = '1' THEN CAST(ISNULL(NULLIF(je.TRADVALUE, ''), '0') AS DECIMAL(18, 3)) ELSE CAST(0 AS DECIMAL(18,3)) END
                    -
                    CASE WHEN je.TRADSIGC = '1' THEN CAST(ISNULL(NULLIF(je.TRADVALUE, ''), '0') AS DECIMAL(18, 3)) ELSE CAST(0 AS DECIMAL(18,3)) END
                ), CAST(0 AS DECIMAL(18,3)))
                FROM {jeTableName} je
                INNER JOIN {headlineTableName} head ON je.TRAID = head.TRAID
                WHERE 
                    (je.TRADACCID = {{0}} OR je.TRADTACCID = {{0}}) 
                    AND je.TRADCURID = {{1}} 
                    AND head.TraStatusDel = '0'
                    -- Only transactions strictly before the period start date
                    -- contribute to the carried-forward opening balance.
                    -- '112' entries are no longer caught here; they appear
                    -- as visible rows in the period query instead.
                    AND je.TRADDATEID < {{2}}
                    {departmentWhereClause}";

            try
            {
                var openingBalanceParams = new object[] { request.AccountId, request.CurrencyId, fromDateId };
                // Use ADO.NET directly if SqlQueryRaw for decimal scalar is tricky, or just adapt via EF
                // But since we are using EF Core 8+, SqlQuery is generic:
                decimal openingBalance = 0;
                var dbConnection = _context.Database.GetDbConnection();
                if (dbConnection.State != System.Data.ConnectionState.Open)
                {
                    await dbConnection.OpenAsync();
                }
                using (var command = dbConnection.CreateCommand())
                {
                    command.CommandTimeout = 120;
                    command.CommandText = string.Format(openingBalanceQuery, $"'{request.AccountId}'", $"'{request.CurrencyId}'", $"'{fromDateId}'");
                    var scalarResult = await command.ExecuteScalarAsync();
                    if (scalarResult != null && scalarResult != DBNull.Value)
                    {
                        openingBalance = Convert.ToDecimal(scalarResult);
                    }
                }
                
                var transactions = await _context.Database
                    .SqlQueryRaw<StatementTransactionDto>(
                        query,
                        request.AccountId,
                        request.CurrencyId,
                        fromDateId,
                        toDateId
                    )
                    .ToListAsync();

                decimal totalPeriodDebit = transactions.Sum(t => t.Debit);
                decimal totalPeriodCredit = transactions.Sum(t => t.Credit);

                decimal runningBalance = openingBalance;
                for (int i = 0; i < transactions.Count; i++)
                {
                    runningBalance += (transactions[i].Debit - transactions[i].Credit);
                    transactions[i].Balance = runningBalance;
                }

                if (openingBalance != 0)
                {
                    transactions.Insert(0, new StatementTransactionDto
                    {
                        TraId = "",
                        TraNo = "0",
                        Date = request.FromDate.AddDays(-1).ToString("dd/MM/yyyy"),
                        Type = "رصيد مرحل",
                        Description = "رصيد مرحل",
                        Beneficiary = "",
                        Debit = openingBalance > 0 ? openingBalance : 0,
                        Credit = openingBalance < 0 ? Math.Abs(openingBalance) : 0,
                        Balance = openingBalance
                    });
                }

                var response = new AccountStatementResponseDto
                {
                    Transactions = transactions,
                    OpeningBalance = openingBalance,
                    TotalPeriodDebit = totalPeriodDebit,
                    TotalPeriodCredit = totalPeriodCredit,
                    ClosingBalance = runningBalance
                };

                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing SQL on tables {jeTableName} & {headlineTableName}: {ex.Message}", ex);
            }
        }

        public async Task<AccountStatementResponseDto> GetDeltaAccountStatementAsync(AccountStatementDeltaRequestDto request)
        {
            var yearsCount = await _context.Tssysyears.CountAsync();
            string tableSuffix = yearsCount > 1 ? $"{request.Year}{request.BranchId}" : $"{request.BranchId}";
            string jeTableName = $"SunTransJe{tableSuffix}";
            string headlineTableName = $"SunTransHeadline{tableSuffix}";

            string departmentWhereClause = request.DepartmentId.HasValue
                ? $"AND head.TraSecId = '{request.DepartmentId.Value}'"
                : "";

            string signatureQuery = $@"
                SELECT 
                    CAST(COUNT(1) AS VARCHAR(50)) + '_' + 
                    CAST(ISNULL(SUM(CAST(ISNULL(NULLIF(je.TRADVALUE, ''), '0') AS DECIMAL(18, 3))), 0) AS VARCHAR(50))
                FROM {jeTableName} je
                INNER JOIN {headlineTableName} head ON je.TRAID = head.TRAID
                WHERE (je.TRADACCID = '{request.AccountId}' OR je.TRADTACCID = '{request.AccountId}')
                AND je.TRADCURID = '{request.CurrencyId}'
                AND head.TraStatusDel = '0'
                {departmentWhereClause}";

            string currentSignature = "";
            var dbConnection = _context.Database.GetDbConnection();

            if (dbConnection.State != System.Data.ConnectionState.Open)
            {
                await dbConnection.OpenAsync();
            }

            using (var command = dbConnection.CreateCommand())
            {
                command.CommandTimeout = 120;
                command.CommandText = signatureQuery;
                var scalarResult = await command.ExecuteScalarAsync();
                if (scalarResult != null && scalarResult != DBNull.Value)
                {
                    currentSignature = scalarResult.ToString();
                }
            }

            if (!string.IsNullOrEmpty(request.LedgerSignature) && currentSignature == request.LedgerSignature)
            {
                return null;
            }

            var response = await GetAccountStatementAsync(request);
            response.LedgerSignature = currentSignature;
            return response;
        }

        public async Task<TransactionHistoryResponseDto> GetTransactionHistoryAsync(TransactionHistoryRequestDto request)
        {
            var yearsCount = await _context.Tssysyears.CountAsync();
            bool multipleYears = yearsCount > 1;

            string tableSuffix;
            if (multipleYears)
            {
                tableSuffix = $"{request.Year}{request.BranchId}";
            }
            else
            {
                tableSuffix = $"{request.BranchId}";
            }

            string headlineTableName = $"SunTransHeadline{tableSuffix}";

            string fromDateId = request.FromDate.ToString("yyyyMMdd");
            string toDateId = request.ToDate.ToString("yyyyMMdd");

            var sqlBuilder = new System.Text.StringBuilder();
            sqlBuilder.Append($@"
        SELECT 
            TraId AS TransactionId,
            ISNULL(TraNo, '') AS TransactionNumber,
            ISNULL(TraRefr, '') AS ReferenceNumber,
            ISNULL(TraDate, '') AS Date,
            ISNULL(TraTimeId, '') AS Time,
            ISNULL(TraPatName, '') AS CustomerName,
            CAST(ISNULL(NULLIF(TraNet, ''), '0') AS DECIMAL(18, 3)) AS NetTotal,
            ISNULL(TrauName, '') AS UserName,
            ISNULL(TraLstId, '') AS TransactionTypeId,
            ISNULL(TraTitle, '') AS TransactionTypeName
        FROM {headlineTableName}
        WHERE TraDateId >= {{0}} 
          AND TraDateId <= {{1}} 
          AND TraStatusDel = '0' 
    ");

            var parameters = new List<object> { fromDateId, toDateId };
            int paramIndex = 2;

            if (!string.IsNullOrWhiteSpace(request.TransactionType) && 
                !string.Equals(request.TransactionType, "all", StringComparison.OrdinalIgnoreCase))
            {
                var typeMapping = new Dictionary<string, string>
        {
            { "ن", "601" },
            { "رن", "602" },
            { "ج", "603" },
            { "رج", "604" },
            { "م", "633" },
            { "رم", "634" },
            { "ق", "201" },
            { "ص", "202" }
        };

                string searchType = request.TransactionType;

                if (typeMapping.ContainsKey(request.TransactionType))
                {
                    searchType = typeMapping[request.TransactionType];
                }

                sqlBuilder.Append($" AND TraLstId = {{{paramIndex}}}");
                parameters.Add(searchType);
                paramIndex++;
            }

            if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
            {
                sqlBuilder.Append($" AND TraSecId = {{{paramIndex}}}");
                parameters.Add(request.DepartmentId.Value.ToString());
                paramIndex++;
            }

            if (request.UserId.HasValue && request.UserId.Value > 0)
            {
                sqlBuilder.Append($" AND TrauId = {{{paramIndex}}}");
                parameters.Add(request.UserId.Value.ToString());
                paramIndex++;
            }

            sqlBuilder.Append(" ORDER BY TraDateId DESC, TraTimeId DESC");

            try
            {
                var rawData = await _context.Database
                    .SqlQueryRaw<TransactionHistoryItemDto>(sqlBuilder.ToString(), parameters.ToArray())
                    .ToListAsync();

                var response = new TransactionHistoryResponseDto
                {
                    Transactions = rawData,
                    ItemsCount = rawData.Count,
                    GrandTotal = rawData.Sum(x => x.NetTotal)
                };

                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching transaction history from table {headlineTableName}: {ex.Message}", ex);
            }
        }

        public async Task<CustomerActivityResponseDto> GetCustomerActivitySummaryAsync(CustomerActivityRequestDto request)
        {
            var yearsCount = await _context.Tssysyears.CountAsync();
            bool multipleYears = yearsCount > 1;

            string tableSuffix = multipleYears
                ? $"{request.Year}{request.BranchId}"
                : $"{request.BranchId}";

            string headlineTableName = $"SunTransHeadline{tableSuffix}";
            string jeTableName = $"SunTransJe{tableSuffix}";
            string customerTableName = "SUNCUSTOMER";

            string fromDateId = request.FromDate.ToString("yyyyMMdd");
            string toDateId = request.ToDate.ToString("yyyyMMdd");

            var sqlBuilder = new System.Text.StringBuilder();

            sqlBuilder.Append($@"
        SELECT 
            cust.CUSTACCID AS CustomerNo,
            cust.CUSTNAME AS CustomerName,

            SUM(CASE 
                WHEN (head.TraDateId < {{0}} OR head.TraLstId = '112') THEN 
                    CASE WHEN je.TRADSIGD = '1' THEN CAST(ISNULL(NULLIF(je.TRADVALUE, ''), '0') AS DECIMAL(18, 3)) ELSE CAST(0 AS DECIMAL(18,3)) END
                    -
                    CASE WHEN je.TRADSIGC = '1' THEN CAST(ISNULL(NULLIF(je.TRADVALUE, ''), '0') AS DECIMAL(18, 3)) ELSE CAST(0 AS DECIMAL(18,3)) END
                ELSE CAST(0 AS DECIMAL(18,3)) 
            END) AS OpeningBalance,

            SUM(CASE 
                WHEN (head.TraDateId >= {{0}} AND head.TraDateId <= {{1}} AND head.TraLstId != '112') AND head.TraLstId IN ('601', '603', '633') 
                THEN CAST(ISNULL(NULLIF(je.TRADVALUE, ''), '0') AS DECIMAL(18, 3)) 
                ELSE CAST(0 AS DECIMAL(18,3)) 
            END) AS TotalSales,

            SUM(CASE 
                WHEN (head.TraDateId >= {{0}} AND head.TraDateId <= {{1}} AND head.TraLstId != '112') AND head.TraLstId IN ('602', '604', '634') 
                THEN CAST(ISNULL(NULLIF(je.TRADVALUE, ''), '0') AS DECIMAL(18, 3)) 
                ELSE CAST(0 AS DECIMAL(18,3)) 
            END) AS TotalReturns,

            SUM(CASE 
                WHEN (head.TraDateId >= {{0}} AND head.TraDateId <= {{1}} AND head.TraLstId != '112') AND head.TraLstId = '201' 
                THEN CAST(ISNULL(NULLIF(je.TRADVALUE, ''), '0') AS DECIMAL(18, 3)) 
                WHEN (head.TraDateId >= {{0}} AND head.TraDateId <= {{1}} AND head.TraLstId != '112') AND head.TraLstId = '202' 
                THEN -1 * CAST(ISNULL(NULLIF(je.TRADVALUE, ''), '0') AS DECIMAL(18, 3)) 
                ELSE CAST(0 AS DECIMAL(18,3)) 
            END) AS TotalCollections,

            ISNULL(MAX(CASE WHEN (head.TraDateId >= {{0}} AND head.TraDateId <= {{1}} AND head.TraLstId != '112') AND head.TraLstId IN ('601', '603', '633') THEN head.TraDate ELSE NULL END), '') AS LastInvoiceDate,
            ISNULL(MAX(CASE WHEN (head.TraDateId >= {{0}} AND head.TraDateId <= {{1}} AND head.TraLstId != '112') AND head.TraLstId = '201' THEN head.TraDate ELSE NULL END), '') AS LastPaymentDate

        FROM {jeTableName} je
        INNER JOIN {headlineTableName} head ON je.TRAID = head.TRAID
        INNER JOIN {customerTableName} cust ON LTRIM(RTRIM(je.TRADTACCID)) = LTRIM(RTRIM(cust.CUSTACCID))
        WHERE 
            (head.TraDateId <= {{1}} OR head.TraLstId = '112')
            AND head.TraStatusDel = '0'
            AND je.TRADCURID = {{2}}
            AND head.TraSecId = {{3}}

        GROUP BY cust.CUSTACCID, cust.CUSTNAME
    ");

            var parameters = new List<object> {
        fromDateId,
        toDateId,
        request.CurrencyId,
        request.DepartmentId
    };

            int paramIndex = 4;

            if (request.UserId.HasValue && request.UserId.Value > 0)
            {
                sqlBuilder.Append($" AND head.TrauId = {{{paramIndex}}}");
                parameters.Add(request.UserId.Value.ToString());
                paramIndex++;
            }

            try
            {
                var rawDataList = await _context.Database
                    .SqlQueryRaw<CustomerActivityRawData>(sqlBuilder.ToString(), parameters.ToArray())
                    .ToListAsync();

                var processedData = rawDataList.Select(raw =>
                {
                    var item = new CustomerActivityItemDto
                    {
                        CustomerNo = raw.CustomerNo ?? "",
                        CustomerName = raw.CustomerName ?? "",
                        OpeningBalance = raw.OpeningBalance,
                        TotalSales = raw.TotalSales,
                        TotalReturns = raw.TotalReturns,
                        TotalCollections = raw.TotalCollections,
                        LastInvoiceDate = raw.LastInvoiceDate ?? "",
                        LastPaymentDate = raw.LastPaymentDate ?? ""
                    };

                    item.NetSales = item.TotalSales - item.TotalReturns;
                    item.PeriodBalance = item.OpeningBalance + item.NetSales - item.TotalCollections;

                    if (item.NetSales > 0)
                    {
                        item.BalancePercentage = Math.Round((item.PeriodBalance / (item.OpeningBalance + item.NetSales == 0 ? 1 : item.OpeningBalance + item.NetSales)) * 100, 2);
                    }
                    else
                    {
                        item.BalancePercentage = 0;
                    }

                    if (!string.IsNullOrEmpty(item.LastInvoiceDate))
                    {
                        DateTime lastInv;
                        bool parsed = false;
                        if (DateTime.TryParse(item.LastInvoiceDate, out lastInv)) parsed = true;
                        else if (DateTime.TryParseExact(item.LastInvoiceDate, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out lastInv)) parsed = true;

                        if (parsed) item.DebtAge = (DateTime.Now - lastInv).Days;
                        else item.DebtAge = 0;
                    }
                    else
                    {
                        item.DebtAge = 0;
                    }

                    return item;
                }).ToList();

                return new CustomerActivityResponseDto
                {
                    Data = processedData,
                    TotalSalesAll = processedData.Sum(x => x.TotalSales),
                    TotalReturnsAll = processedData.Sum(x => x.TotalReturns),
                    TotalCollectionsAll = processedData.Sum(x => x.TotalCollections)
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing Customer Activity Query: {ex.Message}", ex);
            }
        }

        public async Task<CostCenterReportResponseDto> GetCostCenterTransactionsAsync(CostCenterReportRequestDto request)
        {
            var yearsCount = await _context.Tssysyears.CountAsync();
            bool multipleYears = yearsCount > 1;

            string tableSuffix = multipleYears
                ? $"{request.Year}{request.BranchId}"
                : $"{request.BranchId}";

            string jeTableName = $"SunTransJe{tableSuffix}";
            string headlineTableName = $"SunTransHeadline{tableSuffix}";
            string accountTableName = "SUNACCDIR";

            string fromDateId = request.FromDate.ToString("yyyyMMdd");
            string toDateId = request.ToDate.ToString("yyyyMMdd");

            string query = $@"
                SELECT 
                    ISNULL(je.TRADDATE, '') AS TransactionDate,
                    ISNULL(head.TRANO, '') AS TransactionRef,
                    ISNULL(je.TRADDETAIL, '') AS Description,
                    ISNULL(head.TRATITLE, '') AS TransactionType,
                    ISNULL(je.TRADACCID, '') AS AccountNumber,
                    ISNULL(acc.ACCNAME, '') AS AccountName,
                    CASE WHEN je.TRADSIGD = '1' THEN CAST(ISNULL(NULLIF(je.TRADVALUE, ''), '0') AS DECIMAL(18, 3)) ELSE CAST(0 AS DECIMAL(18,3)) END AS Debit,
                    CASE WHEN je.TRADSIGC = '1' THEN CAST(ISNULL(NULLIF(je.TRADVALUE, ''), '0') AS DECIMAL(18, 3)) ELSE CAST(0 AS DECIMAL(18,3)) END AS Credit
                FROM {jeTableName} je
                INNER JOIN {headlineTableName} head ON je.TRAID = head.TRAID
                LEFT JOIN {accountTableName} acc ON je.TRADACCID = acc.ACCID
                WHERE je.TRADCCID = {{0}}
                AND je.TRADDATEID >= {{1}} AND je.TRADDATEID <= {{2}}
                AND head.TraStatusDel = '0'
                ORDER BY je.TRADDATEID, je.TRAID";

            try
            {
                var transactions = await _context.Database
                    .SqlQueryRaw<CostCenterTransactionDto>(
                        query,
                        request.CostCenterNo.ToString(),
                        fromDateId,
                        toDateId
                    )
                    .ToListAsync();

                return new CostCenterReportResponseDto
                {
                    Transactions = transactions
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing Cost Center Query: {ex.Message}", ex);
            }
        }

        public class CustomerActivityRawData
        {
            public string CustomerNo { get; set; }
            public string CustomerName { get; set; }
            public decimal OpeningBalance { get; set; }
            public decimal TotalSales { get; set; }
            public decimal TotalReturns { get; set; }
            public decimal TotalCollections { get; set; }
            public string? LastInvoiceDate { get; set; }
            public string? LastPaymentDate { get; set; }
        }

        // =====================================================================
        // تقرير حركة المبيعات الجديد - مستقل تماماً عن Transaction History
        // =====================================================================
        public async Task<SalesMovementResponseDto> GetSalesMovementAsync(SalesMovementRequestDto request)
        {
            var yearsCount = await _context.Tssysyears.CountAsync();
            string tableSuffix = yearsCount > 1
                ? $"{request.Year}{request.BranchId}"
                : $"{request.BranchId}";

            string headlineTable = $"SunTransHeadline{tableSuffix}";
            string fromDateId = request.FromDate.ToString("yyyyMMdd");
            string toDateId   = request.ToDate.ToString("yyyyMMdd");

            var sql = new System.Text.StringBuilder();
            sql.Append($@"
                SELECT
                    head.TraId                      AS TransactionId,
                    ISNULL(head.TraNo, '')          AS TransactionNumber,
                    ISNULL(head.TraDate, '')        AS Date,
                    ISNULL(head.TraTimeId, '')      AS Time,
                    ISNULL(head.TraLstId, '')       AS TransactionTypeId,
                    ISNULL(head.TraTitle, '')       AS TransactionTypeName,
                    ISNULL(head.TraPatName, '')     AS BeneficiaryName,
                    ISNULL(head.TraAccName, '')     AS AccountName,
                    ISNULL(head.TrauName, '')       AS UserName,
                    ISNULL(head.TraBraId, '')       AS BranchName,
                    ISNULL(head.TraSecId, '')       AS DepartmentName,
                    CAST(ISNULL(NULLIF(head.TraTot, ''), '0') AS DECIMAL(18,3)) * CAST(ISNULL(NULLIF(head.TraCurValue, ''), '1') AS DECIMAL(18,5)) * CASE WHEN head.TraLstId IN ('602', '606', '604', '634', '608') THEN -1 ELSE 1 END AS GrossTotal,
                    CAST(ISNULL(NULLIF(head.TraDis,  ''), '0') AS DECIMAL(18,3)) * CAST(ISNULL(NULLIF(head.TraCurValue, ''), '1') AS DECIMAL(18,5)) * CASE WHEN head.TraLstId IN ('602', '606', '604', '634', '608') THEN -1 ELSE 1 END AS DiscountAmount,
                    CAST(ISNULL(NULLIF(head.TraTax,   ''), '0') AS DECIMAL(18,3)) * CAST(ISNULL(NULLIF(head.TraCurValue, ''), '1') AS DECIMAL(18,5)) * CASE WHEN head.TraLstId IN ('602', '606', '604', '634', '608') THEN -1 ELSE 1 END AS TaxAmount,
                    CAST(ISNULL(NULLIF(head.TraNet,   ''), '0') AS DECIMAL(18,3)) * CAST(ISNULL(NULLIF(head.TraCurValue, ''), '1') AS DECIMAL(18,5)) * CASE WHEN head.TraLstId IN ('602', '606', '604', '634', '608') THEN -1 ELSE 1 END AS NetValue,
                    CAST(ISNULL(NULLIF(head.TraCbn, ''), '0') AS DECIMAL(18,3)) * CAST(ISNULL(NULLIF(head.TraCurValue, ''), '1') AS DECIMAL(18,5)) * CASE WHEN head.TraLstId IN ('602', '606', '604', '634', '608') THEN -1 ELSE 1 END AS FinalValue
                FROM {headlineTable} head
                WHERE head.TraDateId >= {{0}}
                  AND head.TraDateId <= {{1}}
                  AND head.TraStatusDel = '0'
            ");

            var parameters = new List<object> { fromDateId, toDateId };
            int p = 2;

            // فلترة نوع الحركة
            if (!string.IsNullOrEmpty(request.TransactionType) &&
                !string.Equals(request.TransactionType, "all", StringComparison.OrdinalIgnoreCase))
            {
                if (request.TransactionType == "ن")
                {
                    sql.Append(" AND head.TraLstId IN ('601', '602', '605', '606')");
                }
                else if (request.TransactionType == "ج")
                {
                    sql.Append(" AND head.TraLstId IN ('603', '604', '633', '634', '607', '608')");
                }
            }
            else
            {
                sql.Append(" AND head.TraLstId IN ('601', '602', '605', '606', '603', '604', '633', '634', '607', '608')");
            }

            // فلترة القسم (ضمان نطاق البيانات الآمن للمستخدم غير الأدمن)
            if (request.DepartmentId.HasValue && request.DepartmentId.Value > 0)
            {
                sql.Append($" AND head.TraSecId = {{{p}}}");
                parameters.Add(request.DepartmentId.Value.ToString());
                p++;
            }

            // فلترة المستخدم (المستخدم العادي يرى حركاته فقط)
            if (request.UserId.HasValue && request.UserId.Value > 0)
            {
                sql.Append($" AND head.TrauId = {{{p}}}");
                parameters.Add(request.UserId.Value.ToString());
                p++;
            }

            sql.Append(" ORDER BY head.TraDateId DESC, head.TraTimeId DESC");

            try
            {
                var items = await _context.Database
                    .SqlQueryRaw<SalesMovementItemDto>(sql.ToString(), parameters.ToArray())
                    .ToListAsync();

                return new SalesMovementResponseDto
                {
                    Items           = items,
                    ItemsCount      = items.Count,
                    GrandNetValue   = items.Sum(x => x.NetValue),
                    GrandFinalValue = items.Sum(x => x.FinalValue),
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetSalesMovementAsync on table {headlineTable}: {ex.Message}", ex);
            }
        }

        // =====================================================================
        // تقرير حركة الموردين — مستقل تماماً
        // كودات الحركات المؤكدة من ItemBalancesService:
        //   421, 423  = مشتريات (Purchase In)
        //   422, 424  = مردودات مشتريات (Purchase Returns)
        //   202       = سند صرف (دفع للمورد)
        //   201       = تسوية / سند قبض عكسي
        //   112       = رصيد مرحل (علامة الافتتاحي)
        // =====================================================================
        public async Task<SupplierMovementResponseDto> GetSupplierMovementAsync(SupplierMovementRequestDto request)
        {
            var yearsCount = await _context.Tssysyears.CountAsync();
            bool multipleYears = yearsCount > 1;

            string tableSuffix = multipleYears
                ? $"{request.Year}{request.BranchId}"
                : $"{request.BranchId}";

            string jeTable       = $"SunTransJe{tableSuffix}";
            string headlineTable = $"SunTransHeadline{tableSuffix}";

            string fromDateId = request.FromDate.ToString("yyyyMMdd");
            string toDateId   = request.ToDate.ToString("yyyyMMdd");

            // ------------------------------------------------------------------
            // استعلام التجميع الرئيسي — مرور واحد على الجدول
            // ------------------------------------------------------------------
            string sql = $@"
                SELECT
                    ISNULL(LTRIM(RTRIM(je.TRADTACCID)), '')    AS AccountNumber,
                    ISNULL(MAX(acc.ACCNAME), '')               AS AccountName,
                    ISNULL(MAX(vnd.VNDRNAME), '')              AS SupplierName,
                    ISNULL(head.TraSecId, '')                  AS DepartmentId,
                    ISNULL(head.TraBraId, '')                  AS BranchId,

                    /* ---- الرصيد الافتتاحي: كل ما قبل fromDate + الأرصدة المرحلة ---- */
                    SUM(CASE
                        WHEN (head.TraDateId < {{0}} OR head.TraLstId = '112') THEN
                            CASE WHEN je.TRADSIGC = '1' THEN CAST(ISNULL(NULLIF(je.TRADVALUE,''),'0') AS DECIMAL(18,3)) ELSE CAST(0 AS DECIMAL(18,3)) END
                          - CASE WHEN je.TRADSIGD = '1' THEN CAST(ISNULL(NULLIF(je.TRADVALUE,''),'0') AS DECIMAL(18,3)) ELSE CAST(0 AS DECIMAL(18,3)) END
                        ELSE CAST(0 AS DECIMAL(18,3))
                    END)                                       AS OpeningBalance,

                    /* ---- مشتريات الفترة: 421, 423 ---- */
                    SUM(CASE
                        WHEN head.TraDateId >= {{0}} AND head.TraDateId <= {{1}}
                             AND head.TraLstId != '112'
                             AND head.TraLstId IN ('421','423')
                        THEN CAST(ISNULL(NULLIF(je.TRADVALUE,''),'0') AS DECIMAL(18,3))
                        ELSE CAST(0 AS DECIMAL(18,3))
                    END)                                       AS TotalPurchases,

                    /* ---- مردودات الفترة: 422, 424 ---- */
                    SUM(CASE
                        WHEN head.TraDateId >= {{0}} AND head.TraDateId <= {{1}}
                             AND head.TraLstId != '112'
                             AND head.TraLstId IN ('422','424')
                        THEN CAST(ISNULL(NULLIF(je.TRADVALUE,''),'0') AS DECIMAL(18,3))
                        ELSE CAST(0 AS DECIMAL(18,3))
                    END)                                       AS TotalReturns,

                    /* ---- سداد الفترة: 202 (سند صرف = دفع للمورد) ---- */
                    SUM(CASE
                        WHEN head.TraDateId >= {{0}} AND head.TraDateId <= {{1}}
                             AND head.TraLstId != '112'
                             AND head.TraLstId = '202'
                        THEN CAST(ISNULL(NULLIF(je.TRADVALUE,''),'0') AS DECIMAL(18,3))
                        ELSE CAST(0 AS DECIMAL(18,3))
                    END)                                       AS TotalPayments,

                    /* ---- تسويات الفترة: 201 (سند قبض عكسي) ---- */
                    SUM(CASE
                        WHEN head.TraDateId >= {{0}} AND head.TraDateId <= {{1}}
                             AND head.TraLstId != '112'
                             AND head.TraLstId = '201'
                        THEN CAST(ISNULL(NULLIF(je.TRADVALUE,''),'0') AS DECIMAL(18,3))
                        ELSE CAST(0 AS DECIMAL(18,3))
                    END)                                       AS TotalAdjustments,

                    /* ---- تاريخ آخر سداد ---- */
                    ISNULL(MAX(CASE
                        WHEN head.TraDateId >= {{0}} AND head.TraDateId <= {{1}}
                             AND head.TraLstId != '112'
                             AND head.TraLstId = '202'
                        THEN head.TraDate ELSE NULL
                    END), '')                                  AS LastPaymentDate

                FROM {jeTable} je
                INNER JOIN {headlineTable} head ON je.TRAID = head.TRAID
                INNER JOIN SUNVENDER vnd  ON LTRIM(RTRIM(je.TRADTACCID)) = LTRIM(RTRIM(vnd.VNDRACCID))
                LEFT  JOIN SUNACCDIR acc  ON LTRIM(RTRIM(je.TRADTACCID)) = LTRIM(RTRIM(acc.ACCID))
                WHERE
                    (head.TraDateId <= {{1}} OR head.TraLstId = '112')
                    AND head.TraStatusDel = '0'
                    AND je.TRADCURID = {{2}}
                    AND head.TraSecId = {{3}}
                GROUP BY
                    je.TRADTACCID, head.TraSecId, head.TraBraId
            ";

            var parameters = new object[]
            {
                fromDateId,
                toDateId,
                request.CurrencyId.ToString(),
                request.DepartmentId.ToString()
            };

            try
            {
                // ------------------------------------------------------------------
                // تفريع: وضع الملخص vs وضع التفصيلي
                // ------------------------------------------------------------------
                if (request.IsDetailedMode)
                {
                    return await GetDetailedModeAsync(request, jeTable, headlineTable, fromDateId, toDateId);
                }

                var rawList = await _context.Database
                    .SqlQueryRaw<SupplierMovementRawData>(sql, parameters)
                    .ToListAsync();

                // C# post-processing: حساب الحقول المشتقة + Fallback لاسم الحساب
                var processedList = rawList.Select(raw =>
                {
                    var item = new SupplierMovementItemDto
                    {
                        AccountNumber    = raw.AccountNumber ?? "",
                        // Fallback: إذا كان اسم الحساب فارغاً، يُعرض اسم المورد
                        AccountName      = string.IsNullOrWhiteSpace(raw.AccountName)
                                               ? (raw.SupplierName ?? "")
                                               : raw.AccountName,
                        SupplierName     = raw.SupplierName ?? "",
                        DepartmentId     = raw.DepartmentId ?? "",
                        BranchId         = raw.BranchId ?? "",
                        OpeningBalance   = raw.OpeningBalance,
                        TotalPurchases   = raw.TotalPurchases,
                        TotalReturns     = raw.TotalReturns,
                        TotalPayments    = raw.TotalPayments,
                        TotalAdjustments = raw.TotalAdjustments,
                        LastPaymentDate  = raw.LastPaymentDate ?? "",
                        InventoryValue   = 0m,  // مؤجل لحين التطوير المستقبلي في Summary Mode
                        InvoiceDate      = "",
                        InvoiceNumber    = "",
                    };

                    item.NetPurchases = item.TotalPurchases - item.TotalReturns;
                    item.Balance      = item.OpeningBalance + item.NetPurchases - item.TotalPayments - item.TotalAdjustments;
                    item.Difference   = item.Balance - item.InventoryValue;

                    return item;
                }).ToList();

                return new SupplierMovementResponseDto
                {
                    Data                  = processedList,
                    GrandOpeningBalance   = processedList.Sum(x => x.OpeningBalance),
                    GrandTotalPurchases   = processedList.Sum(x => x.TotalPurchases),
                    GrandTotalReturns     = processedList.Sum(x => x.TotalReturns),
                    GrandNetPurchases     = processedList.Sum(x => x.NetPurchases),
                    GrandTotalPayments    = processedList.Sum(x => x.TotalPayments),
                    GrandTotalAdjustments = processedList.Sum(x => x.TotalAdjustments),
                    GrandBalance          = processedList.Sum(x => x.Balance),
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetSupplierMovementAsync on tables {jeTable} & {headlineTable}: {ex.Message}", ex);
            }
        }

        // ======================================================================
        // وضع التفصيلي — صف لكل فاتورة شراء مع قيمة المخزون الحقيقية
        // الرصيد الافتتاحي والرصيد النهائي يظهران على الصف الأول فقط لكل مورد
        // ======================================================================
        private async Task<SupplierMovementResponseDto> GetDetailedModeAsync(
            SupplierMovementRequestDto request,
            string jeTable,
            string headlineTable,
            string fromDateId,
            string toDateId)
        {
            // جدول تفاصيل الأصناف — نفس اللاحقة
            var yearsCount  = await _context.Tssysyears.CountAsync();
            bool multiYear  = yearsCount > 1;
            string suffix   = multiYear
                ? $"{request.Year}{request.BranchId}"
                : $"{request.BranchId}";
            string detailTable = $"SunTransDetailItem{suffix}";

            // ------------------------------------------------------------------
            // استعلام الرصيد الافتتاحي — مجمَّع لكل مورد (ما قبل fromDate)
            // ------------------------------------------------------------------
            string openingSql = $@"
                SELECT
                    ISNULL(LTRIM(RTRIM(je.TRADTACCID)), '')  AS AccountNumber,
                    ISNULL(MAX(acc.ACCNAME), '')              AS AccountName,
                    ISNULL(MAX(vnd.VNDRNAME), '')             AS SupplierName,
                    ISNULL(head.TraSecId, '')                 AS DepartmentId,
                    ISNULL(head.TraBraId, '')                 AS BranchId,
                    CAST(0 AS DECIMAL(18,3))                  AS InvoiceDate,
                    CAST(0 AS DECIMAL(18,3))                  AS InvoiceNumber,
                    SUM(CASE
                        WHEN je.TRADSIGC = '1'
                        THEN CAST(ISNULL(NULLIF(je.TRADVALUE,''),'0') AS DECIMAL(18,3))
                        ELSE CAST(0 AS DECIMAL(18,3))
                    END
                    -CASE
                        WHEN je.TRADSIGD = '1'
                        THEN CAST(ISNULL(NULLIF(je.TRADVALUE,''),'0') AS DECIMAL(18,3))
                        ELSE CAST(0 AS DECIMAL(18,3))
                    END)                                      AS OpeningBalance,
                    CAST(0 AS DECIMAL(18,3))                  AS TotalPurchases,
                    CAST(0 AS DECIMAL(18,3))                  AS TotalReturns,
                    CAST(0 AS DECIMAL(18,3))                  AS TotalPayments,
                    CAST(0 AS DECIMAL(18,3))                  AS TotalAdjustments,
                    CAST(0 AS DECIMAL(18,3))                  AS InvoiceInventoryValue
                FROM {jeTable} je
                INNER JOIN {headlineTable} head ON je.TRAID = head.TRAID
                INNER JOIN SUNVENDER vnd  ON LTRIM(RTRIM(je.TRADTACCID)) = LTRIM(RTRIM(vnd.VNDRACCID))
                LEFT  JOIN SUNACCDIR acc  ON LTRIM(RTRIM(je.TRADTACCID)) = LTRIM(RTRIM(acc.ACCID))
                WHERE
                    (head.TraDateId < {{0}} OR head.TraLstId = '112')
                    AND head.TraStatusDel = '0'
                    AND je.TRADCURID = {{1}}
                    AND head.TraSecId = {{2}}
                GROUP BY
                    je.TRADTACCID, head.TraSecId, head.TraBraId
            ";

            // ------------------------------------------------------------------
            // استعلام التفاصيل — صف لكل فاتورة شراء في الفترة
            // ------------------------------------------------------------------
            string detailSql = $@"
                SELECT
                    ISNULL(LTRIM(RTRIM(je.TRADTACCID)), '')                            AS AccountNumber,
                    ISNULL(MAX(acc.ACCNAME), '')                                        AS AccountName,
                    ISNULL(MAX(vnd.VNDRNAME), '')                                       AS SupplierName,
                    ISNULL(head.TraSecId, '')                                           AS DepartmentId,
                    ISNULL(head.TraBraId, '')                                           AS BranchId,
                    ISNULL(head.TraDate,  '')                                           AS InvoiceDate,
                    ISNULL(head.TraNo,    '')                                           AS InvoiceNumber,

                    /* Purchases (421, 423) */
                    SUM(CASE WHEN head.TraLstId IN ('421','423') THEN
                        CAST(ISNULL(NULLIF(je.TRADVALUE,''),'0') AS DECIMAL(18,3)) ELSE 0 END) AS TotalPurchases,

                    /* Returns (422, 424) */
                    SUM(CASE WHEN head.TraLstId IN ('422','424') THEN
                        CAST(ISNULL(NULLIF(je.TRADVALUE,''),'0') AS DECIMAL(18,3)) ELSE 0 END) AS TotalReturns,

                    /* Payments (202) */
                    SUM(CASE WHEN head.TraLstId = '202' THEN
                        CAST(ISNULL(NULLIF(je.TRADVALUE,''),'0') AS DECIMAL(18,3)) ELSE 0 END) AS TotalPayments,

                    /* Adjustments (201) */
                    SUM(CASE WHEN head.TraLstId = '201' THEN
                        CAST(ISNULL(NULLIF(je.TRADVALUE,''),'0') AS DECIMAL(18,3)) ELSE 0 END) AS TotalAdjustments,

                    CAST(0 AS DECIMAL(18,3))                                                    AS OpeningBalance,

                    /* قيمة المخزون: SUM(الكمية × تكلفة الوحدة) من تفاصيل الأصناف */
                    ISNULL(SUM(
                        ABS(CAST(ISNULL(NULLIF(d.TRADQTY,''),'0') AS DECIMAL(18,3)))
                        * CAST(ISNULL(NULLIF(d.TRADITMUNTCOST,''),'0') AS DECIMAL(18,3))
                    ), 0)                                                               AS InvoiceInventoryValue

                FROM {jeTable} je
                INNER JOIN {headlineTable} head ON je.TRAID = head.TRAID
                INNER JOIN SUNVENDER vnd  ON LTRIM(RTRIM(je.TRADTACCID)) = LTRIM(RTRIM(vnd.VNDRACCID))
                LEFT  JOIN SUNACCDIR acc  ON LTRIM(RTRIM(je.TRADTACCID)) = LTRIM(RTRIM(acc.ACCID))
                LEFT  JOIN {detailTable}  d   ON je.TRAID = d.TRAID
                WHERE
                    head.TraDateId >= {{0}}
                    AND head.TraDateId <= {{1}}
                    AND head.TraStatusDel = '0'
                    AND je.TRADCURID = {{2}}
                    AND head.TraSecId = {{3}}
                GROUP BY
                    je.TRADTACCID, head.TraSecId, head.TraBraId,
                    head.TraDate, head.TraDateId, head.TraNo, head.TRAID
                ORDER BY
                    je.TRADTACCID, head.TraDateId, head.TRAID
            ";

            try
            {
                var openingParams = new object[]
                {
                    fromDateId,
                    request.CurrencyId.ToString(),
                    request.DepartmentId.ToString()
                };

                var detailParams = new object[]
                {
                    fromDateId,
                    toDateId,
                    request.CurrencyId.ToString(),
                    request.DepartmentId.ToString()
                };

                // تنفيذ الاستعلامَين
                var openingRaw = await _context.Database
                    .SqlQueryRaw<SupplierMovementDetailRawData>(openingSql, openingParams)
                    .ToListAsync();

                var detailRaw = await _context.Database
                    .SqlQueryRaw<SupplierMovementDetailRawData>(detailSql, detailParams)
                    .ToListAsync();

                // ------------------------------------------------------------------
                // C# Post-Processing:
                // لكل مورد، أضف سطر الرصيد الافتتاحي أولاً ثم سطور الفواتير
                // ------------------------------------------------------------------
                var processedList = new List<SupplierMovementItemDto>();

                // تجميع الفواتير حسب رقم الحساب مع الحفاظ على الترتيب
                var grouped = detailRaw
                    .GroupBy(d => d.AccountNumber?.Trim() ?? "")
                    .ToList();

                foreach (var group in grouped)
                {
                    string accNo      = group.Key;
                    var    firstInv   = group.First();
                    string supplierName = string.IsNullOrWhiteSpace(firstInv.SupplierName)
                        ? (firstInv.AccountName ?? "") : firstInv.SupplierName;
                    string accountName  = string.IsNullOrWhiteSpace(firstInv.AccountName)
                        ? supplierName : firstInv.AccountName;

                    // الرصيد الافتتاحي لهذا المورد
                    var openRow = openingRaw
                        .FirstOrDefault(o => (o.AccountNumber?.Trim() ?? "") == accNo);
                    decimal openingBalance = openRow?.OpeningBalance ?? 0m;

                    // حساب الرصيد النهائي للمورد من كل الحركات
                    decimal totalPurchases   = group.Sum(x => x.TotalPurchases);
                    decimal totalReturns     = group.Sum(x => x.TotalReturns);
                    decimal totalPayments    = group.Sum(x => x.TotalPayments);
                    decimal totalAdjustments = group.Sum(x => x.TotalAdjustments);
                    decimal netPurchasesAll  = totalPurchases - totalReturns;
                    decimal finalBalance     = openingBalance + netPurchasesAll - totalPayments - totalAdjustments;

                    decimal totalInventory   = group.Sum(x => x.InvoiceInventoryValue);
                    decimal totalDifference  = finalBalance - totalInventory;

                    bool isFirstRow = true;
                    foreach (var inv in group)
                    {
                        decimal netPurchases = inv.TotalPurchases - inv.TotalReturns;
                        decimal transactionBalanceImpact = netPurchases - inv.TotalPayments - inv.TotalAdjustments;
                        
                        // الفارق على مستوى الحركة
                        decimal invDiff = transactionBalanceImpact - inv.InvoiceInventoryValue;

                        processedList.Add(new SupplierMovementItemDto
                        {
                            AccountNumber    = inv.AccountNumber ?? "",
                            AccountName      = accountName,
                            SupplierName     = supplierName,
                            DepartmentId     = inv.DepartmentId ?? "",
                            BranchId         = inv.BranchId ?? "",
                            // افتتاحي ونهائي وفارق المورد يظهران على الصف الأول فقط
                            OpeningBalance   = isFirstRow ? openingBalance  : 0m,
                            Balance          = isFirstRow ? finalBalance    : 0m,
                            // الفارق: الصف الأول يحمل فارق المورد ككل، باقي الصفوف تحمل فارق حركتها
                            Difference       = isFirstRow ? totalDifference : invDiff,
                            // بيانات الفاتورة
                            TotalPurchases   = inv.TotalPurchases,
                            TotalReturns     = inv.TotalReturns,
                            NetPurchases     = netPurchases,
                            TotalPayments    = inv.TotalPayments,
                            TotalAdjustments = inv.TotalAdjustments,
                            InventoryValue   = inv.InvoiceInventoryValue,
                            LastPaymentDate  = "",
                            InvoiceDate      = inv.InvoiceDate  ?? "",
                            InvoiceNumber    = inv.InvoiceNumber ?? "",
                        });
                        isFirstRow = false;
                    }
                }

                return new SupplierMovementResponseDto
                {
                    Data                  = processedList,
                    GrandOpeningBalance   = openingRaw.Sum(x => x.OpeningBalance),
                    GrandTotalPurchases   = detailRaw.Sum(x => x.TotalPurchases),
                    GrandTotalReturns     = detailRaw.Sum(x => x.TotalReturns),
                    GrandNetPurchases     = detailRaw.Sum(x => x.TotalPurchases - x.TotalReturns),
                    GrandTotalPayments    = detailRaw.Sum(x => x.TotalPayments),
                    GrandTotalAdjustments = detailRaw.Sum(x => x.TotalAdjustments),
                    GrandBalance          = openingRaw.Sum(x => x.OpeningBalance) + detailRaw.Sum(x => (x.TotalPurchases - x.TotalReturns) - x.TotalPayments - x.TotalAdjustments),
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetDetailedModeAsync on table {detailTable}: {ex.Message}", ex);
            }
        }
    }
}
