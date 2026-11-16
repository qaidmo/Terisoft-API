using AccountingSyncAPP.Data;
using AccountingSyncAPP.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountingSyncAPP.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly AccountingDbContext _context;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(AccountingDbContext context, ILogger<TransactionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("sync-invoice")]
        public async Task<IActionResult> SyncInvoice([FromBody] SupabaseSale input)
        {
            if (input == null) return BadRequest("Invalid invoice data.");

            if (input.Items == null || !input.Items.Any())
            {
                return BadRequest("لا يمكن حفظ الفاتورة. يجب أن تحتوي على صنف واحد على الأقل.");
            }   

            if (input.Items.Any(i => i.Quantity <= 0 || i.ItemPrice <= 0))
            {
                return BadRequest("مرفوض: لا يمكن تزامن فاتورة تحتوي على أصناف بكمية أو سعر يساوي أو يقل عن صفر.");
            }

            const int REVENUE_ACCOUNT_TYPE_ID = 4;
            var invoiceTypeMap = new Dictionary<string, string>
            {
                { "ن", "601" }, { "رن", "602" }, { "ج", "603" },
                { "رج", "604" }, { "م", "633" }, { "رم", "634" },
                { "م ض", "507" }, { "ت", "605" }
            };

            Func<decimal, string> fmt = d => d.ToString("0.#############################", System.Globalization.CultureInfo.InvariantCulture);

            var years = await _context.Tssysyears.ToListAsync();

            Dictionary<string, string> yearMapByValue = new Dictionary<string, string>();
            string maxYearValue = "";

            if (years.Any())
            {
                maxYearValue = years.Max(y => y.Yrvalue);
                foreach (var y in years)
                {
                    if (!yearMapByValue.ContainsKey(y.Yrvalue))
                        yearMapByValue[y.Yrvalue] = y.Yrid.ToString();
                }
            }
            else
            {
                maxYearValue = DateTime.Now.Year.ToString();
            }

            bool multipleYears = (years.Count > 1);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                string lockName = $"SyncInv_{input.BranchId}_{input.Id}";
                await _context.Database.ExecuteSqlRawAsync("EXEC sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', @LockOwner = 'Transaction'", lockName);

                string sqlTraLstId = invoiceTypeMap.GetValueOrDefault(input.InvoiceTypeChar, "601");
                if (sqlTraLstId == "507") return Ok("Ignored 507");

                string year = input.SunYear.ToString();
                string branchId = input.BranchId.ToString();
                string departmentId = input.DepartmentId?.ToString() ?? "1";

                string tableSuffixYear = multipleYears ? maxYearValue : "";
                string tableSuffix = $"{tableSuffixYear}{branchId}";

                string headLineTable = $"SunTransHeadline{tableSuffix}";
                string headTable = $"SunTransHead{tableSuffix}";
                string detailItemTable = $"SunTransDetailItem{tableSuffix}";
                string jeTable = $"SunTransJe{tableSuffix}";

                bool isTreatedAsCredit = sqlTraLstId == "603" || sqlTraLstId == "604" || sqlTraLstId == "633" || sqlTraLstId == "634";
                bool isReturn = sqlTraLstId == "602" || sqlTraLstId == "604" || sqlTraLstId == "634";
                string headlineSign = isReturn ? "1" : "-1";

                var saudiDate = input.Date;
                var traDateId = saudiDate.ToString("yyyyMMdd");
                var traTimeId = saudiDate.ToString("HHmmss");
                var traDateTimeId = saudiDate.ToString("yyyyMMddHHmmss");
                var traTimeIdShort = long.Parse(saudiDate.ToString("HHmmss")).ToString();
                var traDateTime = saudiDate.ToString("dd/MM/yyyy hh:mm:ss tt");
                var traDate = saudiDate.ToString("dd/MM/yyyy");

                var checkQuery = $"SELECT TOP 1 TraId AS Value FROM {headLineTable} WHERE TraPatRefId = {{0}} AND TraBraId = {{1}} ORDER BY TraId DESC";
                var existingTraId = await _context.Database.SqlQueryRaw<string>(checkQuery, input.Guid ?? input.Id.ToString(), branchId).FirstOrDefaultAsync();

                string activeTraId;
                string activeTraNo;

                if (!string.IsNullOrEmpty(existingTraId))
                {
                    activeTraId = existingTraId;
                    bool isDirectCash = (sqlTraLstId == "601" || sqlTraLstId == "602");

                    var updateHeadQuery = $@"UPDATE {headLineTable} SET 
            TraTot = {{0}}, TraDis = {{1}}, TraTax = {{2}}, TraNet = {{3}}, TraCbn = {{4}}, TraNetLet = {{5}}, 
            TraAccId = {{6}}, TraAccName = {{7}}, TraPatId = {{8}}, TraPatName = {{9}}, TraStoreId = {{10}}, TraStoreName = {{11}},
            TraDateId = {{12}}, TraDate = {{13}}, TraTimeId = {{14}}, TraDateTime = {{15}}, TraDateTimeId = {{16}},
            TRACOLID = {{17}}, TRACOLNAME = {{18}}, TRADELID = {{19}}, TRADELNAME = {{20}}, 
            TraLstId = {{21}}, TrauId = {{22}}, TrauName = {{23}}
            WHERE TraId = {{24}}";

                    string finalAccId = isDirectCash ? input.BoxAccountId : input.AccountId;
                    string finalAccName = isDirectCash ? input.BoxName : input.CustomerName;

                    await _context.Database.ExecuteSqlRawAsync(updateHeadQuery,
                        fmt(input.Total),
                        input.Discount.HasValue ? fmt(input.Discount.Value) : "0",
                        input.Tax.HasValue ? fmt(input.Tax.Value) : "0",
                        fmt(input.NetTotal),
                        fmt(input.NetTotal),
                        Tafqeet(input.NetTotal, input.CurrencyId.ToString()),
                        Truncate(finalAccId, 30),
                        Truncate(finalAccName, 225),
                        isTreatedAsCredit ? input.AccountNo : "",
                        Truncate(input.CustomerName ?? ". . .", 225),
                        Truncate(input.StoreNo?.ToString(), 50),
                        Truncate(input.StoreName, 225),
                        traDateId, traDate, traTimeId, traDateTime, traDateTimeId,
                        Truncate(input.CollectorId?.ToString() ?? "", 50),
                        Truncate(input.CollectorName ?? "", 225),
                        Truncate(input.AgentId?.ToString() ?? "", 50),
                        Truncate(input.AgentName ?? "", 225),
                        sqlTraLstId,
                        Truncate(input.UserId.ToString(), 10),
                        Truncate(input.UserName, 150),
                        activeTraId
                    );

                    var updateHeadTableQuery = $@"UPDATE {headTable} SET 
                                TraDateId = '0', TraTimeId = {{0}}, TraDateTimeId = {{1}}, TraDateTime = {{2}}, TraDate = {{3}}
                                WHERE TraId = {{4}}";
                    await _context.Database.ExecuteSqlRawAsync(updateHeadTableQuery, traTimeId, traTimeIdShort, traDateTime, traDate, activeTraId);

                    await _context.Database.ExecuteSqlRawAsync($"DELETE FROM {detailItemTable} WHERE TraId = {{0}}", activeTraId);
                    await _context.Database.ExecuteSqlRawAsync($"DELETE FROM {jeTable} WHERE TraId = {{0}}", activeTraId);
                }
                else
                {
                    // بداية منطق توليد رقم المرجع الجديد
                    string finalTraRefr = "";
                    string prefixRef = "";

                    if (input.UserId == 0)
                    {
                        prefixRef = $"AD{input.BranchId.ToString("00")}";
                    }
                    else
                    {
                        prefixRef = $"A{input.UserId}{input.BranchId}";
                    }

                    var maxRefQuery = $@"SELECT TOP 1 TraRefr AS Value 
                     FROM {headLineTable} 
                     WHERE TraRefr LIKE {{0}} AND TraLstId = {{1}} 
                     ORDER BY LEN(TraRefr) DESC, TraRefr DESC";

                    var lastRefStr = await _context.Database.SqlQueryRaw<string>(maxRefQuery, prefixRef + "%", sqlTraLstId).FirstOrDefaultAsync();

                    long nextRefSequence = 1;

                    if (!string.IsNullOrEmpty(lastRefStr))
                    {
                        string numberPart = lastRefStr.Substring(prefixRef.Length);
                        if (long.TryParse(numberPart, out long currentMax))
                        {
                            nextRefSequence = currentMax + 1;
                        }
                    }

                    finalTraRefr = $"{prefixRef}{nextRefSequence.ToString("00000000")}";
                    // نهاية منطق توليد رقم المرجع الجديد

                    var lastTraNoQuery = $@"SELECT TOP 1 TraNo AS Value FROM {headLineTable} WITH (UPDLOCK) 
                        WHERE TraBraId = {{0}} AND TraLstId = {{1}} AND TraSecId = {{2}} 
                        ORDER BY CONVERT(BIGINT, TraNo) DESC";

                    var lastTraNoStr = await _context.Database.SqlQueryRaw<string>(lastTraNoQuery, branchId, sqlTraLstId, departmentId).FirstOrDefaultAsync();

                    long nextTraNo = (string.IsNullOrEmpty(lastTraNoStr) ? 0 : (long.TryParse(lastTraNoStr, out long num) ? num : 0)) + 1;
                    activeTraNo = nextTraNo.ToString();

                    string idPrefix = $"11{year}{departmentId}{branchId}{sqlTraLstId}";
                    var lastTraIdQuery = $"SELECT TOP 1 TraId AS Value FROM {headTable} WHERE TraId LIKE {{0}} ORDER BY LEN(TraId) DESC, TraId DESC";
                    var lastTraId = await _context.Database.SqlQueryRaw<string>(lastTraIdQuery, idPrefix + "%").FirstOrDefaultAsync();

                    long nextSequenceNumber = 1;
                    if (!string.IsNullOrEmpty(lastTraId))
                    {
                        string sequenceStr = lastTraId.Substring(idPrefix.Length);
                        if (long.TryParse(sequenceStr, out long lastSequenceNumber)) nextSequenceNumber = lastSequenceNumber + 1;
                    }
                    activeTraId = $"{idPrefix}{nextSequenceNumber}";

                    var headQuery = $@"INSERT INTO {headTable} 
                    (TraId, TraNo, TraLstId, TraYearId, TraSysId, TraBraId, TraComId, TraSecId, TraPosId, TraIndex, 
                    TraDateId, TraTimeId, TraDateTimeId, TraDateTime, TraDate, TraStatusDel, TraTitle, TraSync) 
                    VALUES 
                    ({{0}}, {{1}}, {{2}}, {{3}}, '1', {{4}}, '1', {{5}}, '0', '0', 
                    '0', {{6}}, {{7}}, {{8}}, {{9}}, '0', {{10}}, '1')";

                    await _context.Database.ExecuteSqlRawAsync(headQuery,
                        activeTraId, activeTraNo, sqlTraLstId, year, Truncate(branchId, 5), Truncate(departmentId, 5),
                        traTimeId, traTimeIdShort, traDateTime, traDate, Truncate(input.InvoiceType, 100));

                    decimal sysTotalVal = input.Items?.Sum(i => (i.StockBalance ?? 0) * i.ItemPrice) ?? 0;
                    string systemTotalStr = sysTotalVal.ToString("N0");

                    bool isDirectCash = (sqlTraLstId == "601" || sqlTraLstId == "602");
                    var headlineQuery = $@"INSERT INTO {headLineTable} 
                (TraId, TraNo, TraRefr, TraPatRefId, TraTitle, TraRepTitle, TraCurId, TraCurValue, TraAccId, TraAccName, 
                TraPatId, TraPatName, TraSignd, TraTot, TraDis, TraTax, TraNet, TraCbn, TraNetLet, TraStoreId, 
                TraStoreName, TrauId, TrauName, TraStatusDel, TraLstId, TraYearId, TraBraId, TraDateId, TraDate, TraTimeId, 
                TraDateTime, TraDateTimeId, TraComId, TraSecId, TraPosId, TraIndex, TraPatAge, TraPatSexId, TraPatSexName, TraPatBDate, 
                TraPatAddress, TraPatPaid, TraSrv, TraTck, 
                TraLastEditUsrId, TraLastEditUsrName, TraOptions, TraCheekDate, TraCheekDateId, TraShipDate, TraShipDateId, TRAPATCODEOK, TraSysId,
                TRACOLID, TRACOLNAME, TRADELID, TRADELNAME) 
                VALUES 
                ({{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {{5}}, {{6}}, {{7}}, {{8}}, {{9}}, 
                {{10}}, {{11}}, {{12}}, {{13}}, {{14}}, {{15}}, {{16}}, {{17}}, {{18}}, {{19}}, 
                {{20}}, {{21}}, {{22}}, '0', {{23}}, {{24}}, {{25}}, {{26}}, {{27}}, {{28}}, 
                {{29}}, {{30}}, {{31}}, {{32}}, '0', '0', '0', '0', '-', '0', 
                {{33}}, '0', '0', '0',
                {{34}}, {{35}}, '000000', {{36}}, {{37}}, {{36}}, {{37}}, {{38}}, {{39}},
                {{40}}, {{41}}, {{42}}, {{43}})";

                    string finalAccId = isDirectCash ? input.BoxAccountId : input.AccountId;
                    string finalAccName = isDirectCash ? input.BoxName : input.CustomerName;

                    await _context.Database.ExecuteSqlRawAsync(headlineQuery,
                        activeTraId,
                        activeTraNo,
                        Truncate(finalTraRefr, 150),
                        input.Guid ?? input.Id.ToString(),
                        Truncate(input.InvoiceType, 100),
                        Truncate(input.InvoiceType, 100),
                        input.CurrencyId.ToString(),
                        input.CurrencyRate.HasValue ? fmt(input.CurrencyRate.Value) : "1",
                        Truncate(finalAccId, 30),
                        Truncate(finalAccName, 225),
                        isTreatedAsCredit ? input.AccountNo : "",
                        Truncate(input.CustomerName ?? ". . .", 225),
                        headlineSign,
                        fmt(input.Total),
                        input.Discount.HasValue ? fmt(input.Discount.Value) : "0",
                        input.Tax.HasValue ? fmt(input.Tax.Value) : "0",
                        fmt(input.NetTotal),
                        fmt(input.NetTotal),
                        Tafqeet(input.NetTotal, input.CurrencyId.ToString() ?? "1"),
                        Truncate(input.StoreNo?.ToString(), 50),
                        Truncate(input.StoreName, 225),
                        Truncate(input.UserId.ToString(), 10),
                        Truncate(input.UserName, 150),
                        sqlTraLstId,
                        year,
                        Truncate(branchId, 5),
                        traDateId,
                        traDate,
                        traTimeId,
                        traDateTime,
                        traDateTimeId,
                        "1",
                        Truncate(departmentId, 5),
                        "",
                        Truncate(input.UserId.ToString(), 10),
                        Truncate(input.UserName, 150),
                        traDateTime,
                        traDateId,
                        "",
                        "1",
                        Truncate(input.CollectorId?.ToString() ?? "", 50),
                        Truncate(input.CollectorName ?? "", 225),

                        Truncate(input.AgentId?.ToString() ?? "", 50),
                        Truncate(input.AgentName ?? "", 225)
                    );
                }

                if (input.Items != null && input.Items.Any())
                {
                    string itemSign = isReturn ? "1" : "-1";
                    string itemSignIn = isReturn ? "1" : "0";
                    string itemSignOut = isReturn ? "0" : "1";

                    var detailQueryBuilder = new System.Text.StringBuilder();
                    var parameters = new List<object>();
                    int paramIndex = 0;

                    detailQueryBuilder.Append($@"INSERT INTO {detailItemTable} 
                    (TradId, TraId, TradIndex, TradItmId, TradItmName, TradItmNameE, TradItmUnit, TradUnitSize, TradStorId, TradQty, 
                    TradQtyDis, TradQtyTot, TradUntValue, TradUntDis, TradUntDisPer, TradUntTax, TradUntTaxPer, TradAmount, TradNetValue, TradNetAmount, 
                    TradExpireId, TradItmCurId, TradItmCurValue, TradTraCurValue, TradItmUntValue, TradListId, TradTraDateId, TradTraTimeId, TradSignd, TradSigndIn, 
                    TradSigndOut, TradSigndCost, TradSigndRep, TradBonSignd, TradCurChar, TradInfo, TradExpire, TradRefId, 
                    TradQtyOth, TradBraId, TradX, TradY, TRADOTH, TRADITMUNTCOST, TR_M_NAME, TRANS_FERST, TRANS_NEXT) VALUES ");

                    for (int i = 0; i < input.Items.Count; i++)
                    {
                        var item = input.Items[i];

                        string expiryDateId = "";
                        string expiryDateStr = "";
                        string expiryRef = !string.IsNullOrEmpty(item.BatchNo) ? item.BatchNo : "-";

                        if (!string.IsNullOrEmpty(item.ExpiryDate) && item.ExpiryDate != "-")
                        {
                            if (DateTime.TryParse(item.ExpiryDate, out DateTime parsedDate))
                            {
                                expiryDateId = parsedDate.ToString("yyyyMMdd");
                                expiryDateStr = parsedDate.ToString("dd/MM/yyyy");
                            }
                        }

                        string finalUnit = item.ItemUnit ?? "";
                        decimal packFactor = item.PackFactor ?? 1;
                        string finalPackFactor = fmt(packFactor);
                        string qtyOth = item.StockBalance.HasValue ? fmt(item.StockBalance.Value) : "0";
                        string tradOth = "0";

                        decimal diffVal = (item.DiffQuantity ?? 0) * item.ItemPrice;
                        string tradX = (diffVal > 0) ? fmt(diffVal) : "0";
                        string tradY = (diffVal < 0) ? fmt(Math.Abs(diffVal)) : "0";

                        decimal safeInvoiceRate = (input.CurrencyRate.HasValue && input.CurrencyRate.Value != 0) ? input.CurrencyRate.Value : 1m;
                        decimal finalExchangedCost = (item.CostPrice ?? 0) / safeInvoiceRate;
                        string itemCost = fmt(finalExchangedCost);

                        detailQueryBuilder.Append($@"
            ({{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, 
            {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, '0', '0.00', '0', {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, 
            {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, 
            {{{paramIndex++}}}, '0', '1', '0', {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, 
            {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}){(i < input.Items.Count - 1 ? "," : "")}");

                        parameters.AddRange(new object[] {
                            $"{activeTraId}.{i + 1}",
                            activeTraId,
                            (i + 1).ToString(),
                            Truncate(item.ItemNo, 25),
                            Truncate(item.ItemName, 500),
                            Truncate(item.ItemName, 450),
                            Truncate(finalUnit, 50),
                            Truncate(finalPackFactor, 50),
                            Truncate(input.StoreNo?.ToString(), 25),
                            fmt(item.Quantity),
                            item.BonusQuantity.HasValue ? fmt(item.BonusQuantity.Value) : "0",
                            fmt(item.Quantity + (item.BonusQuantity ?? 0)),
                            fmt(item.ItemPrice),
                            item.ItemDiscount.HasValue ? fmt(item.ItemDiscount.Value) : "0",
                            fmt(item.Quantity * item.ItemPrice),
                            fmt(item.ItemPrice - (item.ItemDiscount ?? 0)),
                            fmt(item.Total),
                            expiryDateId,
                            item.ItemCurrencyId?.ToString() ?? "1",
                            item.ItemCurrencyRate.HasValue ? fmt(item.ItemCurrencyRate.Value) : "1",
                            input.CurrencyRate.HasValue ? fmt(input.CurrencyRate.Value) : "1",
                            fmt(item.ItemPrice),
                            sqlTraLstId,
                            traDateId,
                            traTimeId,
                            itemSign,
                            itemSignIn,
                            itemSignOut,
                            Truncate(input.CurrencySymbol, 50),
                            Truncate(item.ItemName, 1500),
                            expiryDateStr,
                            expiryRef,
                            qtyOth,
                            Truncate(branchId, 10),
                            tradX,
                            tradY,
                            tradOth,
                            itemCost,
                            Truncate(item.PumpName ?? "", 50),
                            fmt(item.PreviousReading ?? 0),
                            fmt(item.CurrentReading ?? 0)
                        });
                    }
                    await _context.Database.ExecuteSqlRawAsync(detailQueryBuilder.ToString(), parameters.ToArray());
                }

                if (input.Postings != null && input.Postings.Any())
                {
                    bool isCashType = sqlTraLstId == "601" || sqlTraLstId == "602" || sqlTraLstId == "605";

                    var mainPost = input.Postings[0];

                    string headerSonAccId = mainPost.AccountNo ?? "";
                    string mainAccName = isCashType ? (input.BoxName ?? "") : (input.CustomerName ?? "");

                    string commonDatId = isCashType ? "" : (input.AccountNo ?? "");
                    string commonDatName = input.CustomerName ?? "";

                    var headerDetailBuilder = new System.Text.StringBuilder();
                    if (input.Items != null)
                    {
                        foreach (var item in input.Items)
                        {
                            headerDetailBuilder.Append($"{Truncate(item.ItemName, 50)} ,{item.ItemNo} ( {fmt(item.Quantity)} )* ({fmt(item.ItemPrice)}) {fmt(item.Total)} | ");
                        }
                    }
                    string fullHeaderDetail = headerDetailBuilder.ToString();

                    var jeQueryBuilder = new System.Text.StringBuilder();
                    var jeParameters = new List<object>();
                    int jeParamIndex = 0;

                    string lastCostAccId = "";
                    string lastCostAccName = "";

                    jeQueryBuilder.Append($@"INSERT INTO {jeTable} 
                        (TradId, TraId, TradIndex, TradType, TradLstId, TradAccId, TradAccName, TradTAccId, TradTaCcName, TradHAccId, 
                        TradHaCcName, TradCcId, TradCcName, TradDatId, TradDatName, TradCurId, TradValue, TradValueLoc, TradValueTra, TradCurValue, 
                        TradSig, TradSigD, TradSigC, TradSigO, TradSigY, TradSigQ, TradSigS, TradDetail, TradRefId, TradDate, TradDateId, SYNCFLAG, TRADHDETAIL) VALUES ");

                    for (int i = 0; i < input.Postings.Count; i++)
                    {
                        var post = input.Postings[i];

                        string currentRowAccId = !string.IsNullOrEmpty(post.ControlAccountId)
                                                 ? post.ControlAccountId
                                                 : (post.AccountNo ?? "");

                        string currentRowTAccId = post.AccountNo ?? "";
                        string currentRowAccName = post.AccountName ?? "";
                        string hAccId = "";
                        string hAccName = "";
                        string currentHeaderDetail = "";

                        if (i == 0)
                        {
                            hAccId = "";
                            hAccName = Truncate(input.StoreName, 225);
                            currentHeaderDetail = fullHeaderDetail;
                        }
                        else
                        {
                            int itemLineType = (i - 1) % 3;

                            switch (itemLineType)
                            {
                                case 0:
                                    hAccId = Truncate(headerSonAccId, 30);
                                    hAccName = Truncate(mainAccName, 225);
                                    break;

                                case 1:
                                    hAccId = "";
                                    hAccName = Truncate(input.StoreName, 225);

                                    lastCostAccId = post.AccountNo ?? "";
                                    lastCostAccName = post.AccountName ?? "";
                                    break;

                                case 2:
                                    hAccId = Truncate(lastCostAccId, 30);
                                    hAccName = Truncate(lastCostAccName, 225);
                                    break;
                            }
                        }

                        string sig, sigD, sigC;
                        if (post.Debit > 0) { sig = "-1"; sigD = "1"; sigC = "0"; }
                        else { sig = "1"; sigD = "0"; sigC = "1"; }

                        // === GL Math Heuristic ===
                        decimal localFromDb = (post.Debit ?? 0) > 0 ? post.Debit.Value : (post.Credit ?? 0);
                        decimal safeRate = post.ExchangeRate > 0 ? (decimal)post.ExchangeRate : 1m;
                        decimal foreignAmount;
                        decimal localAmount = localFromDb;
                        
                        if (post.OriginalAmount == localFromDb) {
                            // Equality hit: OriginalAmount is Local (Cost Rows)
                            foreignAmount = localFromDb / safeRate;
                        } else {
                            // No Equality: OriginalAmount is Foreign (Revenue/Main Rows)
                            foreignAmount = post.OriginalAmount;
                        }

                        jeQueryBuilder.Append($@"
                            ({{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, '0', {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, 
                            {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, 
                            {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, '0', '0', '0', '0', {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, '1', {{{jeParamIndex++}}}){(i < input.Postings.Count - 1 ? "," : "")}");

                                                    jeParameters.AddRange(new object[] {
                                        $"{activeTraId}.{i + 1}",
                                        activeTraId,
                                        (i + 1).ToString(),
                                        sqlTraLstId,
                                        Truncate(currentRowAccId, 30),
                                        Truncate(currentRowAccName, 225),
                                        Truncate(currentRowTAccId, 30),
                                        Truncate(currentRowAccName, 225),
                                        Truncate(hAccId, 30),
                                        Truncate(hAccName, 230),
                                        Truncate(post.CostCenterNo?.ToString() ?? "0", 50),
                                        Truncate(post.CostCenterName ?? "-", 225),
                                        Truncate(commonDatId, 50),
                                        Truncate(commonDatName, 225),
                                        post.CurrencyId.ToString(),
                                        fmt(foreignAmount),       // TradValue (Foreign)
                                        fmt(localAmount),         // TradValueLoc (Local)
                                        fmt(foreignAmount),       // TradValueTra (Transaction Value)
                                        fmt((decimal)post.ExchangeRate),
                                        sig,
                                        sigD,
                                        sigC,
                                        Truncate(post.Description, 1500),
                                        Truncate(input.TransactionNumber, 150),
                                        traDate,
                                        traDateId,
                                        currentHeaderDetail
                                    });
                    }

                    await _context.Database.ExecuteSqlRawAsync(jeQueryBuilder.ToString(), jeParameters.ToArray());
                }

                if (sqlTraLstId == "605" && input.Items != null)
                {
                    foreach (var item in input.Items)
                    {
                        if (!string.IsNullOrEmpty(item.PumpName) && item.CurrentReading.HasValue)
                        {
                            string formattedReading = fmt(item.CurrentReading.Value);
                            await _context.Database.ExecuteSqlRawAsync("UPDATE ALMAHTA SET M_RIGHT = {0}, DATE_M_RIGHT = {1} WHERE M_NAME = {2}", formattedReading, traDate, item.PumpName);
                        }
                    }
                }

                await transaction.CommitAsync();
                return Ok(new { Message = "Synced Successfully", TransactionId = activeTraId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to sync invoice.");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("sync-utility-invoice")]
        public async Task<IActionResult> SyncUtilityInvoice([FromBody] SupabaseSale input)
        {
            if (input == null) return BadRequest("Invalid utility invoice data.");

            if (input.Items == null || !input.Items.Any())
            {
                return BadRequest("لا يمكن حفظ الفاتورة. يجب أن تحتوي على صنف واحد على الأقل.");
            }

            Func<decimal, string> fmt = d => d.ToString("0.#############################", System.Globalization.CultureInfo.InvariantCulture);

            // Mapping:
            // "ك ن" -> "605" (Utility Cash / Network Bill)
            // "ك ج" -> "607" (Utility Credit Bill)
            string sqlTraLstId = (input.InvoiceTypeChar == "ك ج") ? "607" : "605";

            var years = await _context.Tssysyears.ToListAsync();

            Dictionary<string, string> yearMapByValue = new Dictionary<string, string>();
            string maxYearValue = "";

            if (years.Any())
            {
                maxYearValue = years.Max(y => y.Yrvalue);
                foreach (var y in years)
                {
                    if (!yearMapByValue.ContainsKey(y.Yrvalue))
                        yearMapByValue[y.Yrvalue] = y.Yrid.ToString();
                }
            }
            else
            {
                maxYearValue = DateTime.Now.Year.ToString();
            }

            bool multipleYears = (years.Count > 1);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                string lockName = $"SyncUtilInv_{input.BranchId}_{input.Id}";
                await _context.Database.ExecuteSqlRawAsync("EXEC sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', @LockOwner = 'Transaction'", lockName);

                string year = input.SunYear.ToString();
                string branchId = input.BranchId.ToString();
                string departmentId = input.DepartmentId?.ToString() ?? "1";

                string tableSuffixYear = multipleYears ? maxYearValue : "";
                string tableSuffix = $"{tableSuffixYear}{branchId}";

                string headLineTable = $"SunTransHeadline{tableSuffix}";
                string headTable = $"SunTransHead{tableSuffix}";
                string detailItemTable = $"SunTransDetailItem{tableSuffix}";
                string jeTable = $"SunTransJe{tableSuffix}";

                bool isTreatedAsCredit = (sqlTraLstId == "607");
                string headlineSign = "-1";

                var saudiDate = input.Date;
                var traDateId = saudiDate.ToString("yyyyMMdd");
                var traTimeId = saudiDate.ToString("HHmmss");
                var traDateTimeId = saudiDate.ToString("yyyyMMddHHmmss");
                var traTimeIdShort = long.Parse(saudiDate.ToString("HHmmss")).ToString();
                var traDateTime = saudiDate.ToString("dd/MM/yyyy hh:mm:ss tt");
                var traDate = saudiDate.ToString("dd/MM/yyyy");

                var checkQuery = $"SELECT TOP 1 TraId AS Value FROM {headLineTable} WHERE TraPatRefId = {{0}} AND TraBraId = {{1}} ORDER BY TraId DESC";
                var existingTraId = await _context.Database.SqlQueryRaw<string>(checkQuery, input.Guid ?? input.Id.ToString(), branchId).FirstOrDefaultAsync();

                string activeTraId;
                string activeTraNo;

                string traSrvStr = fmt(input.TraSrv ?? 0);

                if (!string.IsNullOrEmpty(existingTraId))
                {
                    activeTraId = existingTraId;
                    bool isDirectCash = !isTreatedAsCredit;

                    var updateHeadQuery = $@"UPDATE {headLineTable} SET 
            TraTot = {{0}}, TraDis = {{1}}, TraTax = {{2}}, TraNet = {{3}}, TraCbn = {{4}}, TraNetLet = {{5}}, 
            TraAccId = {{6}}, TraAccName = {{7}}, TraPatId = {{8}}, TraPatName = {{9}}, TraStoreId = {{10}}, TraStoreName = {{11}},
            TraDateId = {{12}}, TraDate = {{13}}, TraTimeId = {{14}}, TraDateTime = {{15}}, TraDateTimeId = {{16}},
            TRACOLID = {{17}}, TRACOLNAME = {{18}}, TRADELID = {{19}}, TRADELNAME = {{20}}, 
            TraLstId = {{21}}, TrauId = {{22}}, TrauName = {{23}}, TraSrv = {{24}}
            WHERE TraId = {{25}}";

                    string finalAccId = isDirectCash ? input.BoxAccountId : input.AccountId;
                    string finalAccName = isDirectCash ? input.BoxName : input.CustomerName;

                    await _context.Database.ExecuteSqlRawAsync(updateHeadQuery,
                        fmt(input.Total),
                        input.Discount.HasValue ? fmt(input.Discount.Value) : "0",
                        input.Tax.HasValue ? fmt(input.Tax.Value) : "0",
                        fmt(input.NetTotal),
                        fmt(input.NetTotal),
                        Tafqeet(input.NetTotal, input.CurrencyId?.ToString() ?? "1"),
                        Truncate(finalAccId, 30),
                        Truncate(finalAccName, 225),
                        isTreatedAsCredit ? input.AccountNo : "",
                        Truncate(input.CustomerName ?? ". . .", 225),
                        Truncate(input.StoreNo?.ToString(), 50),
                        Truncate(input.StoreName, 225),
                        traDateId, traDate, traTimeId, traDateTime, traDateTimeId,
                        Truncate(input.CollectorId?.ToString() ?? "", 50),
                        Truncate(input.CollectorName ?? "", 225),
                        Truncate(input.AgentId?.ToString() ?? "", 50),
                        Truncate(input.AgentName ?? "", 225),
                        sqlTraLstId,
                        Truncate(input.UserId.ToString(), 10),
                        Truncate(input.UserName, 150),
                        traSrvStr,
                        activeTraId
                    );

                    var updateHeadTableQuery = $@"UPDATE {headTable} SET 
                                TraDateId = '0', TraTimeId = {{0}}, TraDateTimeId = {{1}}, TraDateTime = {{2}}, TraDate = {{3}}
                                WHERE TraId = {{4}}";
                    await _context.Database.ExecuteSqlRawAsync(updateHeadTableQuery, traTimeId, traTimeIdShort, traDateTime, traDate, activeTraId);

                    await _context.Database.ExecuteSqlRawAsync($"DELETE FROM {detailItemTable} WHERE TraId = {{0}}", activeTraId);
                    await _context.Database.ExecuteSqlRawAsync($"DELETE FROM {jeTable} WHERE TraId = {{0}}", activeTraId);
                }
                else
                {
                    string prefixRef = (input.UserId == 0)
                        ? $"AD{input.BranchId.ToString("00")}"
                        : $"A{input.UserId}{input.BranchId}";

                    var maxRefQuery = $@"SELECT TOP 1 TraRefr AS Value 
                     FROM {headLineTable} 
                     WHERE TraRefr LIKE {{0}} AND TraLstId = {{1}} 
                     ORDER BY LEN(TraRefr) DESC, TraRefr DESC";

                    var lastRefStr = await _context.Database.SqlQueryRaw<string>(maxRefQuery, prefixRef + "%", sqlTraLstId).FirstOrDefaultAsync();

                    long nextRefSequence = 1;
                    if (!string.IsNullOrEmpty(lastRefStr))
                    {
                        string numberPart = lastRefStr.Substring(prefixRef.Length);
                        if (long.TryParse(numberPart, out long currentMax))
                        {
                            nextRefSequence = currentMax + 1;
                        }
                    }

                    string finalTraRefr = $"{prefixRef}{nextRefSequence.ToString("00000000")}";

                    var lastTraNoQuery = $@"SELECT TOP 1 TraNo AS Value FROM {headLineTable} WITH (UPDLOCK) 
                        WHERE TraBraId = {{0}} AND TraLstId = {{1}} AND TraSecId = {{2}} 
                        ORDER BY CONVERT(BIGINT, TraNo) DESC";

                    var lastTraNoStr = await _context.Database.SqlQueryRaw<string>(lastTraNoQuery, branchId, sqlTraLstId, departmentId).FirstOrDefaultAsync();

                    long nextTraNo = (string.IsNullOrEmpty(lastTraNoStr) ? 0 : (long.TryParse(lastTraNoStr, out long num) ? num : 0)) + 1;
                    activeTraNo = nextTraNo.ToString();

                    string idPrefix = $"11{year}{departmentId}{branchId}{sqlTraLstId}";
                    var lastTraIdQuery = $"SELECT TOP 1 TraId AS Value FROM {headTable} WHERE TraId LIKE {{0}} ORDER BY LEN(TraId) DESC, TraId DESC";
                    var lastTraId = await _context.Database.SqlQueryRaw<string>(lastTraIdQuery, idPrefix + "%").FirstOrDefaultAsync();

                    long nextSequenceNumber = 1;
                    if (!string.IsNullOrEmpty(lastTraId))
                    {
                        string sequenceStr = lastTraId.Substring(idPrefix.Length);
                        if (long.TryParse(sequenceStr, out long lastSeq)) nextSequenceNumber = lastSeq + 1;
                    }
                    activeTraId = $"{idPrefix}{nextSequenceNumber}";

                    var headQuery = $@"INSERT INTO {headTable} 
                    (TraId, TraNo, TraLstId, TraYearId, TraSysId, TraBraId, TraComId, TraSecId, TraPosId, TraIndex, 
                    TraDateId, TraTimeId, TraDateTimeId, TraDateTime, TraDate, TraStatusDel, TraTitle, TraSync) 
                    VALUES 
                    ({{0}}, {{1}}, {{2}}, {{3}}, '1', {{4}}, '1', {{5}}, '0', '0', 
                    '0', {{6}}, {{7}}, {{8}}, {{9}}, '0', {{10}}, '1')";

                    await _context.Database.ExecuteSqlRawAsync(headQuery,
                        activeTraId, activeTraNo, sqlTraLstId, year, Truncate(branchId, 5), Truncate(departmentId, 5),
                        traTimeId, traTimeIdShort, traDateTime, traDate, Truncate(input.InvoiceType, 100));

                    bool isDirectCash = !isTreatedAsCredit;
                    var headlineQuery = $@"INSERT INTO {headLineTable} 
                (TraId, TraNo, TraRefr, TraPatRefId, TraTitle, TraRepTitle, TraCurId, TraCurValue, TraAccId, TraAccName, 
                TraPatId, TraPatName, TraSignd, TraTot, TraDis, TraTax, TraNet, TraCbn, TraNetLet, TraStoreId, 
                TraStoreName, TrauId, TrauName, TraStatusDel, TraLstId, TraYearId, TraBraId, TraDateId, TraDate, TraTimeId, 
                TraDateTime, TraDateTimeId, TraComId, TraSecId, TraPosId, TraIndex, TraPatAge, TraPatSexId, TraPatSexName, TraPatBDate, 
                TraPatAddress, TraPatPaid, TraSrv, TraTck, 
                TraLastEditUsrId, TraLastEditUsrName, TraOptions, TraCheekDate, TraCheekDateId, TraShipDate, TraShipDateId, TRAPATCODEOK, TraSysId,
                TRACOLID, TRACOLNAME, TRADELID, TRADELNAME) 
                VALUES 
                ({{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {{5}}, {{6}}, {{7}}, {{8}}, {{9}}, 
                {{10}}, {{11}}, {{12}}, {{13}}, {{14}}, {{15}}, {{16}}, {{17}}, {{18}}, {{19}}, 
                {{20}}, {{21}}, {{22}}, '0', {{23}}, {{24}}, {{25}}, {{26}}, {{27}}, {{28}}, 
                {{29}}, {{30}}, {{31}}, {{32}}, '0', '0', '0', '0', '-', '0', 
                {{33}}, '0', {{34}}, '0',
                {{35}}, {{36}}, '000000', {{37}}, {{38}}, {{37}}, {{38}}, {{39}}, {{40}},
                {{41}}, {{42}}, {{43}}, {{44}})";

                    string finalAccId = isDirectCash ? input.BoxAccountId : input.AccountId;
                    string finalAccName = isDirectCash ? input.BoxName : input.CustomerName;

                    await _context.Database.ExecuteSqlRawAsync(headlineQuery,
                        activeTraId,
                        activeTraNo,
                        Truncate(finalTraRefr, 150),
                        input.Guid ?? input.Id.ToString(),
                        Truncate(input.InvoiceType, 100),
                        Truncate(input.InvoiceType, 100),
                        input.CurrencyId?.ToString() ?? "1",
                        input.CurrencyRate?.ToString() ?? "1",
                        Truncate(finalAccId, 30),
                        Truncate(finalAccName, 225),
                        isTreatedAsCredit ? input.AccountNo : "",
                        Truncate(input.CustomerName ?? ". . .", 225),
                        headlineSign,
                        fmt(input.Total),
                        input.Discount.HasValue ? fmt(input.Discount.Value) : "0",
                        input.Tax.HasValue ? fmt(input.Tax.Value) : "0",
                        fmt(input.NetTotal),
                        fmt(input.NetTotal),
                        Tafqeet(input.NetTotal, input.CurrencyId?.ToString() ?? "1"),
                        Truncate(input.StoreNo?.ToString(), 50),
                        Truncate(input.StoreName, 225),
                        input.UserId.ToString(),
                        Truncate(input.UserName, 50),
                        sqlTraLstId,
                        year,
                        Truncate(branchId, 5),
                        traDateId,
                        traDate,
                        traTimeId,
                        traDateTime,
                        traDateTimeId,
                        "1",
                        Truncate(departmentId, 5),
                        Truncate(input.CustomerName ?? ". . .", 225),
                        traSrvStr,
                        input.UserId.ToString(),
                        Truncate(input.UserName, 50),
                        traDate,
                        traDateId,
                        "",
                        "1",
                        Truncate(input.CollectorId?.ToString() ?? "", 50),
                        Truncate(input.CollectorName ?? "", 225),
                        Truncate(input.AgentId?.ToString() ?? "", 50),
                        Truncate(input.AgentName ?? "", 225)
                    );
                }

                // Detail Items Insertion
                if (input.Items != null && input.Items.Any())
                {
                    string itemSign = "-1";
                    string itemSignIn = "0";
                    string itemSignOut = "1";

                    var detailQueryBuilder = new System.Text.StringBuilder();
                    var parameters = new List<object>();
                    int paramIndex = 0;

                    detailQueryBuilder.Append($@"INSERT INTO {detailItemTable} 
                    (TradId, TraId, TradIndex, TradItmId, TradItmName, TradItmNameE, TradItmUnit, TradUnitSize, TradStorId, TradQty, 
                    TradQtyDis, TradQtyTot, TradUntValue, TradUntDis, TradUntDisPer, TradUntTax, TradUntTaxPer, TradAmount, TradNetValue, TradNetAmount, 
                    TradExpireId, TradItmCurId, TradItmCurValue, TradTraCurValue, TradItmUntValue, TradListId, TradTraDateId, TradTraTimeId, TradSignd, TradSigndIn, 
                    TradSigndOut, TradSigndCost, TradSigndRep, TradBonSignd, TradCurChar, TradInfo, TradExpire, TradRefId, 
                    TradQtyOth, TradBraId, TradX, TradY, TRADOTH, TRADITMUNTCOST, TR_M_NAME, TRANS_FERST, TRANS_NEXT, TRADTRATAX) VALUES ");

                    for (int i = 0; i < input.Items.Count; i++)
                    {
                        var item = input.Items[i];

                        string expiryDateId = "";
                        string expiryDateStr = "";
                        string expiryRef = !string.IsNullOrEmpty(item.BatchNo) ? item.BatchNo : "-";

                        if (!string.IsNullOrEmpty(item.ExpiryDate) && item.ExpiryDate != "-")
                        {
                            if (DateTime.TryParse(item.ExpiryDate, out DateTime parsedDate))
                            {
                                expiryDateId = parsedDate.ToString("yyyyMMdd");
                                expiryDateStr = parsedDate.ToString("dd/MM/yyyy");
                            }
                        }

                        string finalUnit = item.ItemUnit ?? "";
                        decimal packFactor = item.PackFactor ?? 1;
                        string finalPackFactor = fmt(packFactor);
                        string qtyOth = item.StockBalance.HasValue ? fmt(item.StockBalance.Value) : "0";
                        string tradOth = "0";

                        decimal diffVal = (item.DiffQuantity ?? 0) * item.ItemPrice;
                        string tradX = (diffVal > 0) ? fmt(diffVal) : "0";
                        string tradY = (diffVal < 0) ? fmt(Math.Abs(diffVal)) : "0";

                        decimal safeInvoiceRate = (input.CurrencyRate.HasValue && input.CurrencyRate.Value != 0) ? input.CurrencyRate.Value : 1m;
                        decimal finalExchangedCost = (item.CostPrice ?? 0) / safeInvoiceRate;
                        string itemCost = fmt(finalExchangedCost);
                        string tradTraTaxStr = fmt(item.TradTraTax ?? 0);

                        detailQueryBuilder.Append($@"
            ({{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, 
            {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, '0', '0.00', '0', {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, 
            {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, 
            {{{paramIndex++}}}, '0', '1', '0', {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, 
            {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}){(i < input.Items.Count - 1 ? "," : "")}");

                        parameters.AddRange(new object[] {
                            $"{activeTraId}.{i + 1}",
                            activeTraId,
                            (i + 1).ToString(),
                            Truncate(item.ItemNo, 25),
                            Truncate(item.ItemName, 500),
                            Truncate(item.ItemName, 450),
                            Truncate(finalUnit, 50),
                            Truncate(finalPackFactor, 50),
                            Truncate(input.StoreNo?.ToString(), 25),
                            fmt(item.Quantity),
                            item.BonusQuantity.HasValue ? fmt(item.BonusQuantity.Value) : "0",
                            fmt(item.Quantity + (item.BonusQuantity ?? 0)),
                            fmt(item.ItemPrice),
                            item.ItemDiscount.HasValue ? fmt(item.ItemDiscount.Value) : "0",
                            fmt(item.Quantity * item.ItemPrice),
                            fmt(item.ItemPrice - (item.ItemDiscount ?? 0)),
                            fmt(item.Total),
                            expiryDateId,
                            item.ItemCurrencyId?.ToString() ?? "1",
                            item.ItemCurrencyRate.HasValue ? fmt(item.ItemCurrencyRate.Value) : "1",
                            input.CurrencyRate.HasValue ? fmt(input.CurrencyRate.Value) : "1",
                            fmt(item.ItemPrice),
                            sqlTraLstId,
                            traDateId,
                            traTimeId,
                            itemSign,
                            itemSignIn,
                            itemSignOut,
                            Truncate(input.CurrencySymbol ?? input.Currency ?? "", 50),
                            Truncate(item.ItemName, 1500),
                            expiryDateStr,
                            expiryRef,
                            qtyOth,
                            Truncate(branchId, 10),
                            tradX,
                            tradY,
                            tradOth,
                            itemCost,
                            Truncate(item.PumpName ?? "", 50),
                            fmt(item.PreviousReading ?? 0),
                            fmt(item.CurrentReading ?? 0),
                            tradTraTaxStr
                        });
                    }
                    await _context.Database.ExecuteSqlRawAsync(detailQueryBuilder.ToString(), parameters.ToArray());
                }

                // General Ledger Postings
                if (input.Postings != null && input.Postings.Any())
                {
                    bool isCashType = !isTreatedAsCredit;

                    var mainPost = input.Postings[0];

                    string headerSonAccId = mainPost.AccountNo ?? "";
                    string mainAccName = isCashType ? (input.BoxName ?? "") : (input.CustomerName ?? "");

                    string commonDatId = isCashType ? "" : (input.AccountNo ?? "");
                    string commonDatName = input.CustomerName ?? "";

                    var headerDetailBuilder = new System.Text.StringBuilder();
                    if (input.Items != null)
                    {
                        foreach (var item in input.Items)
                        {
                            headerDetailBuilder.Append($"{Truncate(item.ItemName, 50)} ,{item.ItemNo} ( {fmt(item.Quantity)} )* ({fmt(item.ItemPrice)}) {fmt(item.Total)} | ");
                        }
                    }
                    string fullHeaderDetail = headerDetailBuilder.ToString();

                    var jeQueryBuilder = new System.Text.StringBuilder();
                    var jeParameters = new List<object>();
                    int jeParamIndex = 0;

                    string lastCostAccId = "";
                    string lastCostAccName = "";

                    jeQueryBuilder.Append($@"INSERT INTO {jeTable} 
                        (TradId, TraId, TradIndex, TradType, TradLstId, TradAccId, TradAccName, TradTAccId, TradTaCcName, TradHAccId, 
                        TradHaCcName, TradCcId, TradCcName, TradDatId, TradDatName, TradCurId, TradValue, TradValueLoc, TradValueTra, TradCurValue, 
                        TradSig, TradSigD, TradSigC, TradSigO, TradSigY, TradSigQ, TradSigS, TradDetail, TradRefId, TradDate, TradDateId, SYNCFLAG, TRADHDETAIL) VALUES ");

                    for (int i = 0; i < input.Postings.Count; i++)
                    {
                        var post = input.Postings[i];

                        string currentRowAccId = !string.IsNullOrEmpty(post.ControlAccountId)
                                                 ? post.ControlAccountId
                                                 : (post.AccountNo ?? "");

                        string currentRowTAccId = post.AccountNo ?? "";
                        string currentRowAccName = post.AccountName ?? "";
                        string hAccId = "";
                        string hAccName = "";
                        string currentHeaderDetail = "";

                        if (i == 0)
                        {
                            hAccId = "";
                            hAccName = Truncate(input.StoreName, 225);
                            currentHeaderDetail = fullHeaderDetail;
                        }
                        else
                        {
                            int itemLineType = (i - 1) % 3;

                            switch (itemLineType)
                            {
                                case 0:
                                    hAccId = Truncate(headerSonAccId, 30);
                                    hAccName = Truncate(mainAccName, 225);
                                    break;

                                case 1:
                                    hAccId = "";
                                    hAccName = Truncate(input.StoreName, 225);

                                    lastCostAccId = post.AccountNo ?? "";
                                    lastCostAccName = post.AccountName ?? "";
                                    break;

                                case 2:
                                    hAccId = Truncate(lastCostAccId, 30);
                                    hAccName = Truncate(lastCostAccName, 225);
                                    break;
                            }
                        }

                        string sig, sigD, sigC;
                        if (post.Debit > 0) { sig = "-1"; sigD = "1"; sigC = "0"; }
                        else { sig = "1"; sigD = "0"; sigC = "1"; }

                        // === GL Math Heuristic ===
                        decimal localFromDb = (post.Debit ?? 0) > 0 ? post.Debit.Value : (post.Credit ?? 0);
                        decimal safeRate = post.ExchangeRate > 0 ? (decimal)post.ExchangeRate : 1m;
                        decimal foreignAmount;
                        decimal localAmount = localFromDb;
                        
                        if (post.OriginalAmount == localFromDb) {
                            // Equality hit: OriginalAmount is Local (Cost Rows)
                            foreignAmount = localFromDb / safeRate;
                        } else {
                            // No Equality: OriginalAmount is Foreign (Revenue/Main Rows)
                            foreignAmount = post.OriginalAmount;
                        }

                        jeQueryBuilder.Append($@"
                            ({{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, '0', {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, 
                            {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, 
                            {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, '0', '0', '0', '0', {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, {{{jeParamIndex++}}}, '1', {{{jeParamIndex++}}}){(i < input.Postings.Count - 1 ? "," : "")}");

                        jeParameters.AddRange(new object[] {
                            $"{activeTraId}.{i + 1}",
                            activeTraId,
                            (i + 1).ToString(),
                            sqlTraLstId,
                            Truncate(currentRowAccId, 30),
                            Truncate(currentRowAccName, 225),
                            Truncate(currentRowTAccId, 30),
                            Truncate(currentRowAccName, 225),
                            Truncate(hAccId, 30),
                            Truncate(hAccName, 230),
                            Truncate(post.CostCenterNo?.ToString() ?? "0", 50),
                            Truncate(post.CostCenterName ?? "-", 225),
                            Truncate(commonDatId, 50),
                            Truncate(commonDatName, 225),
                            post.CurrencyId.ToString(),
                            fmt(foreignAmount),       // TradValue (Foreign)
                            fmt(localAmount),         // TradValueLoc (Local)
                            fmt(foreignAmount),       // TradValueTra (Transaction Value)
                            fmt((decimal)post.ExchangeRate),
                            sig,
                            sigD,
                            sigC,
                            Truncate(post.Description, 1500),
                            Truncate(input.TransactionNumber, 150),
                            traDate,
                            traDateId,
                            currentHeaderDetail
                        });
                    }

                    await _context.Database.ExecuteSqlRawAsync(jeQueryBuilder.ToString(), jeParameters.ToArray());
                }

                // Customer Reading Auto-Update (DO NOT touch ALMAHTA)
                if (input.Items != null)
                {
                    var itemWithReading = input.Items.FirstOrDefault(i => i.CurrentReading.HasValue && i.CurrentReading.Value > 0);
                    string targetCustId = !string.IsNullOrEmpty(input.AccountNo) ? input.AccountNo : (input.AccountId ?? "");
                    if (itemWithReading != null && itemWithReading.CurrentReading.HasValue && !string.IsNullOrEmpty(targetCustId))
                    {
                        string formattedReading = fmt(itemWithReading.CurrentReading.Value);
                        string currentDateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                        await _context.Database.ExecuteSqlRawAsync(
                            "UPDATE SUNCUSTOMER SET COST_NEXT = {0}, EDTDATE = {1} WHERE CUSTID = {2} OR CUSTACCID = {2}",
                            formattedReading, currentDateTime, targetCustId);
                    }
                }

                await transaction.CommitAsync();
                return Ok(new { Message = "Utility Invoice Synced Successfully", TransactionId = activeTraId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to sync utility invoice.");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("get-invoice-details")]
        public async Task<IActionResult> GetInvoiceDetails([FromQuery] string transactionId, [FromQuery] string branchId, [FromQuery] string year)
        {
            if (string.IsNullOrEmpty(transactionId) || string.IsNullOrEmpty(branchId) || string.IsNullOrEmpty(year))
            {
                return BadRequest("Missing required parameters.");
            }

            try
            {
                var yearsCount = await _context.Tssysyears.CountAsync();
                bool multipleYears = yearsCount > 1;
                string tableSuffix = multipleYears ? $"{year}{branchId}" : $"{branchId}";

                string headlineTableName = $"SunTransHeadline{tableSuffix}";
                string detailItemTableName = $"SunTransDetailItem{tableSuffix}";

                // التعديل: إضافة JOIN مع جدول SUNCURY لجلب CURNAME
                var rawHeader = await _context.Database.SqlQueryRaw<HeaderResultDto>($@"
            SELECT TOP 1
                CAST(0 AS BIGINT) AS Id, 
                T.TraNo AS TransactionNumber,
                T.TraRefr AS ReferenceNumber,
                T.TraDate AS Date,
                T.TraLstId AS InvoiceTypeChar,
                T.TraAccId AS AccountId, 
                T.TraAccName AS BoxName,
                T.TraPatId AS AccountNo,
                T.TraPatName AS CustomerName,
                T.TraStoreId AS StoreNo,
                T.TraStoreName AS StoreName,
                C.CURNAME AS CurrencyName, -- جلب اسم العملة من الجدول المرتبط
                T.TraCurId AS Currency,
                CAST(ISNULL(NULLIF(T.TraCurValue, ''), '1') AS DECIMAL(18, 3)) AS CurrencyRate,
                CAST(ISNULL(NULLIF(T.TraTot, ''), '0') AS DECIMAL(18, 3)) AS Total,
                CAST(ISNULL(NULLIF(T.TraDis, ''), '0') AS DECIMAL(18, 3)) AS Discount,
                CAST(ISNULL(NULLIF(T.TraTax, ''), '0') AS DECIMAL(18, 3)) AS Tax,
                CAST(ISNULL(NULLIF(T.TraNet, ''), '0') AS DECIMAL(18, 3)) AS NetTotal,
                T.TrauId AS UserId,
                T.TrauName AS UserName,
                T.TRACOLID AS CollectorId,
                T.TRACOLNAME AS CollectorName,
                T.TRADELID AS AgentId,
                T.TRADELNAME AS AgentName
            FROM {headlineTableName} T
            LEFT JOIN SUNCURY C ON T.TraCurId = C.CURID -- عملية الربط
            WHERE T.TraId = {{0}}", transactionId).FirstOrDefaultAsync();

                if (rawHeader == null)
                {
                    return NotFound($"Invoice {transactionId} not found.");
                }

                var itemsQuery = $@"
            SELECT 
                CAST(0 AS BIGINT) AS Id,
                CAST(0 AS BIGINT) AS SaleId,
                TradItmId AS ItemNo,
                TradItmName AS ItemName,
                CAST(ISNULL(NULLIF(TradQty, ''), '0') AS DECIMAL(18, 3)) AS Quantity,
                CAST(ISNULL(NULLIF(TradQtyDis, ''), '0') AS DECIMAL(18, 3)) AS BonusQuantity, 
                CAST(ISNULL(NULLIF(TradUntValue, ''), '0') AS DECIMAL(18, 3)) AS ItemPrice,
                CAST(ISNULL(NULLIF(TradAmount, ''), '0') AS DECIMAL(18, 3)) AS Total,
                CAST(ISNULL(NULLIF(TradUntDis, ''), '0') AS DECIMAL(18, 3)) AS ItemDiscount,
                TradItmUnit AS ItemUnit,
                CAST(ISNULL(NULLIF(TradUnitSize, ''), '1') AS DECIMAL(18, 3)) AS PackFactor,
                TradExpire AS ExpiryDate,
                TradRefId AS BatchNo,
                CAST(ISNULL(NULLIF(TRADITMUNTCOST, ''), '0') AS DECIMAL(18, 3)) AS CostPrice,
                CAST(ISNULL(NULLIF(TradQtyOth, ''), '0') AS DECIMAL(18, 3)) AS StockBalance, 
                CAST(0 AS DECIMAL(18, 3)) AS DiffQuantity,
                CAST(0 AS DECIMAL(18, 3)) AS DeficitValue,
                CAST(0 AS DECIMAL(18, 3)) AS SurplusValue
            FROM {detailItemTableName}
            WHERE TraId = {{0}}
            ORDER BY TradIndex";

                List<SupabaseSaleItem> items = await _context.Database.SqlQueryRaw<SupabaseSaleItem>(itemsQuery, transactionId).ToListAsync();

                var invoiceTypeMapReversed = new Dictionary<string, string>
        {
            { "601", "ن" }, { "602", "رن" }, { "603", "ج" },
            { "604", "رج" }, { "633", "م" }, { "634", "رم" },
            { "201", "ق" }, { "202", "ص" }
        };

                string typeChar = invoiceTypeMapReversed.ContainsKey(rawHeader.InvoiceTypeChar)
                    ? invoiceTypeMapReversed[rawHeader.InvoiceTypeChar]
                    : rawHeader.InvoiceTypeChar;

                DateTime parsedDate = DateTime.Now;
                if (!string.IsNullOrEmpty(rawHeader.Date))
                {
                    DateTime.TryParseExact(rawHeader.Date, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDate);
                }

                var saleObj = new SupabaseSale
                {
                    Id = 0,
                    TransactionNumber = rawHeader.TransactionNumber,
                    Date = parsedDate,
                    InvoiceTypeChar = typeChar,
                    StoreNo = int.TryParse(rawHeader.StoreNo, out int sNo) ? sNo : 0,
                    StoreName = rawHeader.StoreName,
                    AccountNo = rawHeader.AccountNo,
                    CustomerName = rawHeader.CustomerName,
                    BoxName = rawHeader.BoxName,

                    // ✅ تم التعديل هنا: إرسال اسم العملة النصي لضمان ظهورها في Dropdown الموبايل
                    Currency = rawHeader.CurrencyName ?? rawHeader.Currency,

                    CurrencyRate = rawHeader.CurrencyRate ?? 1,
                    Total = rawHeader.Total,
                    Discount = rawHeader.Discount,
                    Tax = rawHeader.Tax,
                    NetTotal = rawHeader.NetTotal,
                    UserName = rawHeader.UserName,
                    CollectorId = int.TryParse(rawHeader.CollectorId, out int cId) ? cId : (int?)null,
                    CollectorName = rawHeader.CollectorName,
                    AgentId = int.TryParse(rawHeader.AgentId, out int aId) ? aId : (int?)null,
                    AgentName = rawHeader.AgentName,
                    BranchId = int.Parse(branchId),
                    SunYear = int.Parse(year),
                    IsSynced = true
                };

                return Ok(new
                {
                    sale = saleObj,
                    items = items
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
        [HttpPost("sync-inventory")]
        public async Task<IActionResult> SyncInventory([FromBody] SupabaseSale input)
        {
            if (input == null) return BadRequest("Invalid inventory data.");

            Func<decimal, string> fmt = d => d.ToString("0.#############################", System.Globalization.CultureInfo.InvariantCulture);

            var years = await _context.Tssysyears.ToListAsync();

            Dictionary<string, string> yearMapByValue = new Dictionary<string, string>();
            string maxYearValue = "";

            if (years.Any())
            {
                maxYearValue = years.Max(y => y.Yrvalue);
                foreach (var y in years)
                {
                    if (!yearMapByValue.ContainsKey(y.Yrvalue))
                        yearMapByValue[y.Yrvalue] = y.Yrid.ToString();
                }
            }
            else
            {
                maxYearValue = DateTime.Now.Year.ToString();
            }

            bool multipleYears = (years.Count > 1);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                string sqlTraLstId = "507";
                string year = input.SunYear.ToString();
                string branchId = input.BranchId.ToString();
                string departmentId = input.DepartmentId?.ToString() ?? "1";

                string tableSuffixYear = multipleYears ? maxYearValue : "";
                string tableSuffix = $"{tableSuffixYear}{branchId}";

                string headLineTable = $"SunTransHeadline{tableSuffix}";
                string headTable = $"SunTransHead{tableSuffix}";
                string detailItemTable = $"SunTransDetailItem{tableSuffix}";

                var saudiDate = input.Date;
                var traDateId = saudiDate.ToString("yyyyMMdd");
                var traTimeId = saudiDate.ToString("HHmmss");
                var traDateTimeId = saudiDate.ToString("yyyyMMddHHmmss");
                var traTimeIdShort = long.Parse(saudiDate.ToString("HHmmss")).ToString();
                var traDateTime = saudiDate.ToString("dd/MM/yyyy hh:mm:ss tt");
                var traDate = saudiDate.ToString("dd/MM/yyyy");

                var checkQuery = $"SELECT TOP 1 TraId AS Value FROM {headLineTable} WHERE TraPatRefId = {{0}} AND TraDateTimeId = {{1}} ORDER BY TraId DESC";
                var existingTraId = await _context.Database.SqlQueryRaw<string>(checkQuery, input.Guid ?? input.Id.ToString(), traDateTimeId).FirstOrDefaultAsync();

                string activeTraId;
                string activeTraNo = "";

                if (!string.IsNullOrEmpty(existingTraId))
                {
                    activeTraId = existingTraId;

                    var updateHeadQuery = $@"UPDATE {headLineTable} SET 
            TraTot = {{0}}, TraDis = '0', TraTax = '0', TraNet = {{1}}, TraCbn = {{2}}, TraNetLet = {{3}}, 
            TraStoreId = {{4}}, TraStoreName = {{5}},
            TraDateId = {{6}}, TraDate = {{7}}, TraTimeId = {{8}}, TraDateTime = {{9}}, TraDateTimeId = {{10}}
            WHERE TraId = {{11}}";

                    await _context.Database.ExecuteSqlRawAsync(updateHeadQuery,
                        fmt(input.Total),
                        fmt(input.NetTotal),
                        fmt(input.NetTotal),
                        Tafqeet(input.NetTotal, input.CurrencyId?.ToString() ?? "1"),
                        Truncate(input.StoreNo?.ToString(), 50),
                        Truncate(input.StoreName, 225),
                        traDateId, traDate, traTimeId, traDateTime, traDateTimeId,
                        activeTraId
                    );

                    var updateHeadTableQuery = $@"UPDATE {headTable} SET 
                                TraDateId = '0', TraTimeId = {{0}}, TraDateTimeId = {{1}}, TraDateTime = {{2}}, TraDate = {{3}}
                                WHERE TraId = {{4}}";
                    await _context.Database.ExecuteSqlRawAsync(updateHeadTableQuery, traTimeId, traTimeIdShort, traDateTime, traDate, activeTraId);

                    await _context.Database.ExecuteSqlRawAsync($"DELETE FROM {detailItemTable} WHERE TraId = {{0}}", activeTraId);
                }
                else
                {
                    string finalTraRefr = "";
                    string prefixRef = "";

                    if (input.UserId == 0)
                    {
                        prefixRef = $"AD{input.BranchId.ToString("00")}";
                    }
                    else
                    {
                        prefixRef = $"A{input.UserId}{input.BranchId}";
                    }

                    var maxRefQuery = $@"SELECT TOP 1 TraRefr AS Value 
                     FROM {headLineTable} 
                     WHERE TraRefr LIKE {{0}} AND TraLstId = {{1}} 
                     ORDER BY LEN(TraRefr) DESC, TraRefr DESC";

                    var lastRefStr = await _context.Database.SqlQueryRaw<string>(maxRefQuery, prefixRef + "%", sqlTraLstId).FirstOrDefaultAsync();

                    long nextRefSequence = 1;
                    if (!string.IsNullOrEmpty(lastRefStr))
                    {
                        string numberPart = lastRefStr.Substring(prefixRef.Length);
                        if (long.TryParse(numberPart, out long currentMax))
                        {
                            nextRefSequence = currentMax + 1;
                        }
                    }

                    finalTraRefr = $"{prefixRef}{nextRefSequence.ToString("00000000")}";

                    string currentTrauId = input.UserId.ToString();

                    var lastTraNoQuery = $@"SELECT TOP 1 TraNo AS Value FROM {headLineTable} 
                                WHERE TraBraId = {{0}} AND TraLstId = {{1}} AND TrauId = {{2}}
                                ORDER BY CONVERT(BIGINT, TraNo) DESC";
                    var lastTraNoStr = await _context.Database.SqlQueryRaw<string>(lastTraNoQuery, branchId, sqlTraLstId, currentTrauId).FirstOrDefaultAsync();

                    long nextTraNo = (string.IsNullOrEmpty(lastTraNoStr) ? 0 : (long.TryParse(lastTraNoStr, out long num) ? num : 0)) + 1;
                    activeTraNo = nextTraNo.ToString();

                    string idPrefix = $"11{year}{departmentId}{branchId}{sqlTraLstId}";
                    var lastTraIdQuery = $"SELECT TOP 1 TraId AS Value FROM {headTable} WHERE TraId LIKE {{0}} ORDER BY LEN(TraId) DESC, TraId DESC";
                    var lastTraId = await _context.Database.SqlQueryRaw<string>(lastTraIdQuery, idPrefix + "%").FirstOrDefaultAsync();

                    long nextSequenceNumber = 1;
                    if (!string.IsNullOrEmpty(lastTraId))
                    {
                        string sequenceStr = lastTraId.Substring(idPrefix.Length);
                        if (long.TryParse(sequenceStr, out long lastSequenceNumber)) nextSequenceNumber = lastSequenceNumber + 1;
                    }
                    activeTraId = $"{idPrefix}{nextSequenceNumber}";

                    var headQuery = $@"INSERT INTO {headTable} 
                    (TraId, TraNo, TraLstId, TraYearId, TraSysId, TraBraId, TraComId, TraSecId, TraPosId, TraIndex, 
                    TraDateId, TraTimeId, TraDateTimeId, TraDateTime, TraDate, TraStatusDel, TraTitle, TraSync) 
                    VALUES 
                    ({{0}}, {{1}}, {{2}}, {{3}}, '1', {{4}}, '1', {{5}}, '0', '0', 
                    '0', {{6}}, {{7}}, {{8}}, {{9}}, '0', {{10}}, '1')";

                    await _context.Database.ExecuteSqlRawAsync(headQuery,
                        activeTraId, activeTraNo, sqlTraLstId, year, Truncate(branchId, 5), Truncate(departmentId, 5),
                        traTimeId, traTimeIdShort, traDateTime, traDate, Truncate(input.InvoiceType, 100));

                    string systemTotalStr = (input.SystemTotalValue ?? 0).ToString("0.##");

                    var headlineQuery = $@"INSERT INTO {headLineTable} 
                (TraId, TraNo, TraRefr, TraPatRefId, TraTitle, TraRepTitle, TraCurId, TraCurValue, TraAccId, TraAccName, 
                TraPatId, TraPatName, TraSignd, TraTot, TraDis, TraTax, TraNet, TraCbn, TraNetLet, TraStoreId, 
                TraStoreName, TrauId, TrauName, TraStatusDel, TraLstId, TraYearId, TraBraId, TraDateId, TraDate, TraTimeId, 
                TraDateTime, TraDateTimeId, TraComId, TraSecId, TraPosId, TraIndex, TraPatAge, TraPatSexId, TraPatSexName, TraPatBDate, 
                TraPatAddress, TraPatPaid, TraSrv, TraTck, 
                TraLastEditUsrId, TraLastEditUsrName, TraOptions, TraCheekDate, TraCheekDateId, TraShipDate, TraShipDateId, TRAPATCODEOK, TraSysId,
                TRACOLID, TRACOLNAME, TRADELID, TRADELNAME) 
                VALUES 
                ({{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {{5}}, {{6}}, {{7}}, '', '', 
                '', '', '1', {{8}}, '0', '0', {{9}}, {{10}}, {{11}}, {{12}}, 
                {{13}}, {{14}}, {{15}}, '0', {{16}}, {{17}}, {{18}}, {{19}}, {{20}}, {{21}}, 
                {{22}}, {{23}}, {{24}}, {{25}}, '0', '0', '0', '0', '-', '0', 
                {{26}}, '0', '0', '0',
                {{27}}, {{28}}, '000000', {{29}}, {{30}}, {{29}}, {{30}}, {{31}}, {{32}},
                '', '', '', '')";

                    await _context.Database.ExecuteSqlRawAsync(headlineQuery,
                        activeTraId,
                        activeTraNo,
                        Truncate(finalTraRefr, 150),
                        input.Guid ?? input.Id.ToString(),
                        Truncate(input.InvoiceType, 100),
                        Truncate(input.InvoiceType, 100),
                        input.CurrencyId?.ToString() ?? "1",
                        input.CurrencyRate?.ToString() ?? "1",
                        fmt(input.Total),
                        fmt(input.NetTotal),
                        fmt(input.NetTotal),
                        Tafqeet(input.NetTotal, input.CurrencyId?.ToString() ?? "1"),
                        Truncate(input.StoreNo?.ToString(), 50),
                        Truncate(input.StoreName, 225),
                        input.UserId.ToString(),
                        Truncate(input.UserName, 50),
                        sqlTraLstId,
                        year,
                        Truncate(branchId, 5),
                        traDateId,
                        traDate,
                        traTimeId,
                        traDateTime,
                        traDateTimeId,
                        "1",
                        Truncate(departmentId, 5),
                        Truncate(input.CustomerName ?? ". . .", 225),
                        input.UserId.ToString(),
                        Truncate(input.UserName, 50),
                        traDate,
                        traDateId,
                        "",
                        "1"
                    );
                }

                if (input.Items != null && input.Items.Any())
                {
                    var detailQueryBuilder = new System.Text.StringBuilder();
                    var parameters = new List<object>();
                    int paramIndex = 0;

                    detailQueryBuilder.Append($@"INSERT INTO {detailItemTable} 
                    (TRAID, TRADID, TRADINDEX, TRADITMID, TRADITMNAME, TRADITMNAMEE, TRADITMUNIT, TRADUNITSIZE, TRADSTORID, TRADQTY, 
                    TRADQTYDIS, TRADQTYOTH, TRADQTYTOT, TRADUNTVALUE, TRADUNTDIS, TRADUNTDISPER, TRADUNTTAX, TRADUNTTAXPER, TRADITMCURID, TRADITMCURVALUE, 
                    TRADTRACURVALUE, TRADITMUNTVALUE, TRADAMOUNT, TRADNETVALUE, TRADNETAMOUNT, TRADREFID, TRADEXPIREID, TRADEXPIRE, TRADMODEL, TRADX, 
                    TRADY, TRADWEIGHT, TRADREMARK, TRADTRADATEID, TRADTRATIMEID, TRADLISTID, TRADSIGND, TRADSIGNDIN, TRADSIGNDOUT, TRADSIGNDCOST, 
                    TRADSIGNDREP, TRADBRAID, TRADITMUNTCOST, TRADSIGNDRCV, TRADSTORESHILF, TRADOTH, TRADBONSIGND, TRADPARTNUMBER, TRADCLASSF, TRADCODE, 
                    TRADFACTORY, TRADNAME, TRADCITY, TRADCURCHAR, TRADPERLINE, TRADINFO, TRADCCID, TRADCCID2, TRADTRADISC, TRADTRATAX, 
                    TRADSYNC) VALUES ");

                    for (int i = 0; i < input.Items.Count; i++)
                    {
                        var item = input.Items[i];
                        
                        string qtyOth = item.DiffQuantity?.ToString("0.##") ?? "0";
                        string tradOth = item.StockBalance?.ToString("0.##") ?? "0";
                        string tradX = item.SurplusValue?.ToString("0.##") ?? "0";
                        string tradY = item.DeficitValue?.ToString("0.##") ?? "0";
                        string itemCost = item.CostPrice?.ToString("0.##") ?? item.ItemPrice.ToString("0.##");
                        
                        string expiryDateId = "20290101";
                        string expiryDateStr = "01/01/2029";
                        if (!string.IsNullOrEmpty(item.ExpiryDate) && DateTime.TryParse(item.ExpiryDate, out var parsedExpiry))
                        {
                            expiryDateId = parsedExpiry.ToString("yyyyMMdd");
                            expiryDateStr = parsedExpiry.ToString("dd/MM/yyyy");
                        }

                        string tradInfo = $"{Truncate(item.ItemName ?? item.ItemNo, 100)} {expiryDateStr}   {expiryDateId}"; 

                        detailQueryBuilder.Append($@"
                    ({{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, 
                    {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, 
                    {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, '', {{{paramIndex++}}}, 
                    {{{paramIndex++}}}, '1', '', {{{paramIndex++}}}, {{{paramIndex++}}}, {{{paramIndex++}}}, '0', '0', '0', '0', 
                    '1', {{{paramIndex++}}}, {{{paramIndex++}}}, '0', '', {{{paramIndex++}}}, '0', '', '', '', 
                    '', '', '', {{{paramIndex++}}}, '0', {{{paramIndex++}}}, '', '', '0', '0', 
                    '1'){(i < input.Items.Count - 1 ? "," : "")}");

                        parameters.AddRange(new object[] {
                            activeTraId,                                    // TRAID
                            $"{activeTraId}.{i + 1}",                       // TRADID
                            (i + 1).ToString(),                             // TRADINDEX
                            Truncate(item.ItemNo, 25),                      // TRADITMID
                            Truncate(item.ItemName ?? item.ItemNo, 500),    // TRADITMNAME
                            Truncate(item.ItemName ?? item.ItemNo, 450),    // TRADITMNAMEE
                            Truncate(item.ItemUnit, 50),                    // TRADITMUNIT
                            (item.PackFactor ?? 1m).ToString("0.##"),        // TRADUNITSIZE
                            Truncate(input.StoreNo?.ToString(), 25),        // TRADSTORID
                            item.Quantity.ToString("0.##"),                 // TRADQTY
                            "0",                                            // TRADQTYDIS
                            qtyOth,                                         // TRADQTYOTH
                            item.Quantity.ToString("0.##"),                 // TRADQTYTOT
                            item.ItemPrice.ToString("0.##"),                // TRADUNTVALUE
                            "0",                                            // TRADUNTDIS
                            "0",                                            // TRADUNTDISPER
                            "0",                                            // TRADUNTTAX
                            "0",                                            // TRADUNTTAXPER
                            "1",                                            // TRADITMCURID
                            "1",                                            // TRADITMCURVALUE
                            "1",                                            // TRADTRACURVALUE
                            item.ItemPrice.ToString("0.##"),                // TRADITMUNTVALUE
                            item.Total.ToString("0.##"),                    // TRADAMOUNT
                            item.ItemPrice.ToString("0.##"),                // TRADNETVALUE
                            item.Total.ToString("0.##"),                    // TRADNETAMOUNT
                            expiryDateId,                                   // TRADREFID
                            expiryDateId,                                   // TRADEXPIREID
                            expiryDateStr,                                  // TRADEXPIRE
                            tradX,                                          // TRADX
                            tradY,                                          // TRADY
                            traDateId,                                      // TRADTRADATEID
                            traTimeIdShort,                                 // TRADTRATIMEID
                            sqlTraLstId,                                    // TRADLISTID
                            Truncate(branchId, 10),                         // TRADBRAID
                            itemCost,                                       // TRADITMUNTCOST
                            tradOth,                                        // TRADOTH
                            Truncate(input.Currency, 50),                   // TRADCURCHAR
                            tradInfo                                        // TRADINFO
                        });
                    }
                    await _context.Database.ExecuteSqlRawAsync(detailQueryBuilder.ToString(), parameters.ToArray());
                }

                await transaction.CommitAsync();
                return Ok(new { success = true, id = activeTraId, transactionNumber = activeTraNo });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing inventory {Id}", input.Id);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        public class HeaderResultDto
        {
            public long Id { get; set; }
            public string TransactionNumber { get; set; }
            public string ReferenceNumber { get; set; }
            public string Date { get; set; }
            public string InvoiceTypeChar { get; set; }
            public string AccountId { get; set; }
            public string AccountNo { get; set; }
            public string CustomerName { get; set; }
            public string BoxName { get; set; }
            public string StoreNo { get; set; }
            public string StoreName { get; set; }
            public string Currency { get; set; }
            public string CurrencyName { get; set; } // هذا الحقل سيستقبل اسم العملة النصي
            public decimal? CurrencyRate { get; set; }
            public decimal Total { get; set; }
            public decimal Discount { get; set; }
            public decimal Tax { get; set; }
            public decimal NetTotal { get; set; }
            public string UserId { get; set; }
            public string UserName { get; set; }
            public string CollectorId { get; set; }
            public string CollectorName { get; set; }
            public string AgentId { get; set; }
            public string AgentName { get; set; }
        }
        private string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
         
        private string Tafqeet(decimal number, string currencyId)
        {
            string mainCurrency, fractionalCurrency;
            switch (currencyId)
            {
                case "1":
                    mainCurrency = "ريال يمني";
                    fractionalCurrency = "فلس";
                    break;
                default:
                    mainCurrency = "عملة";
                    fractionalCurrency = "جزء";
                    break;
            }

            if (number == 0) return $"فقط صفر {mainCurrency} لا غير.";

            string result = "فقط ";
            long intPart = (long)Math.Truncate(number);
            int fractionalPart = (int)Math.Round((number - intPart) * 100);

            if (intPart > 0)
            {
                result += NumberToArabicWords(intPart) + " " + mainCurrency;
            }

            if (fractionalPart > 0)
            {
                if (intPart > 0) result += " و ";
                result += NumberToArabicWords(fractionalPart) + " " + fractionalCurrency;
            }

            return result.Trim() + " لا غير.";
        }

        private string NumberToArabicWords(long number)
        {
            if (number == 0) return "صفر";

            string[] units = { "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة" };
            string[] teens = { "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر" };
            string[] tens = { "", "عشرة", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون" };
            string[] hundreds = { "", "مائة", "مئتان", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة" };

            string ConvertChunk(long chunk)
            {
                if (chunk == 0) return "";
                string chunkWords = "";

                if (chunk / 100 > 0)
                {
                    chunkWords += hundreds[chunk / 100];
                    chunk %= 100;
                }

                if (chunk > 0)
                {
                    if (chunkWords != "") chunkWords += " و ";
                    if (chunk < 10) chunkWords += units[chunk];
                    else if (chunk < 20) chunkWords += teens[chunk - 10];
                    else
                    {
                        chunkWords += units[chunk % 10];
                        if (chunk % 10 > 0) chunkWords += " و ";
                        chunkWords += tens[chunk / 10];
                    }
                }
                return chunkWords;
            }

            string words = "";
            long millions = number / 1000000;
            long thousands = number % 1000000 / 1000;
            long remainder = number % 1000;

            if (millions > 0)
            {
                if (millions == 1) words += "مليون";
                else if (millions == 2) words += "مليونان";
                else if (millions >= 3 && millions <= 10) words += ConvertChunk(millions) + " ملايين";
                else words += ConvertChunk(millions) + " مليون";
            }

            if (thousands > 0)
            {
                if (words != "") words += " و ";
                if (thousands == 1) words += "ألف";
                else if (thousands == 2) words += "ألفان";
                else if (thousands >= 3 && thousands <= 10) words += ConvertChunk(thousands) + " آلاف";
                else words += ConvertChunk(thousands) + " ألف";
            }

            if (remainder > 0)
            {
                if (words != "") words += " و ";
                words += ConvertChunk(remainder);
            }
            return words;
        }
    }
}