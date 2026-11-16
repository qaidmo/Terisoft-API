using AccountingSyncAPP.Data;
using AccountingSyncAPP.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountingSyncAPP.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReceiptController : ControllerBase
    {
        private readonly AccountingDbContext _context;

        public ReceiptController(AccountingDbContext context)
        {
            _context = context;
        }

        [HttpPost("sync-receipt")]
        public async Task<IActionResult> SyncReceipt([FromBody] SupabaseReceipt input)
        {
            if (input == null) return BadRequest("Invalid receipt data.");

            if (input.Items == null || !input.Items.Any())
            {
                return BadRequest("لا يمكن حفظ السند. يجب أن يحتوي على بند واحد على الأقل.");
            }

            if (input.TotalAmount <= 0)
            {
                return BadRequest("مرفوض: لا يمكن تزامن سند بقيمة إجمالية تساوي أو تقل عن صفر.");
            }

            var voucherTypeMap = new Dictionary<string, string>
    {
        { "ق", "201" },
        { "ص", "202" }
    };

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
                string lockName = $"SyncRec_{input.BranchId}_{input.Id}";
                await _context.Database.ExecuteSqlRawAsync("EXEC sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', @LockOwner = 'Transaction'", lockName);

                Func<decimal, string> fmt = d => d.ToString("0.#############################", System.Globalization.CultureInfo.InvariantCulture);
                string sqlTraLstId = voucherTypeMap.GetValueOrDefault(input.InvoiceTypeChar, "201");

                string year = input.SunYear.ToString();
                string branchId = input.BranchId.ToString();
                string departmentId = input.DepartmentId?.ToString() ?? "1";

                string tableSuffixYear = multipleYears ? maxYearValue : "";
                string tableSuffix = $"{tableSuffixYear}{branchId}";

                string headLineTable = $"SunTransHeadline{tableSuffix}";
                string headTable = $"SunTransHead{tableSuffix}";
                string accTable = $"SunTransAcc{tableSuffix}";
                string jeTable = $"SunTransJe{tableSuffix}";

                var saudiDate = input.Date;
                var traDateId = saudiDate.ToString("yyyyMMdd");
                var traTimeId = saudiDate.ToString("HHmmss");
                var traDateTimeId = saudiDate.ToString("yyyyMMddHHmmss");
                var traTimeIdShort = long.Parse(saudiDate.ToString("HHmmss")).ToString();
                var traDateTime = saudiDate.ToString("dd/MM/yyyy hh:mm:ss tt");
                var traDate = saudiDate.ToString("dd/MM/yyyy");

                string headerSignal = (sqlTraLstId == "201") ? "1" : "-1";

                var checkQuery = $"SELECT TOP 1 TraId AS Value FROM {headLineTable} WHERE TraPatRefId = {{0}} AND TraBraId = {{1}} ORDER BY TraId DESC";
                var existingTraId = await _context.Database.SqlQueryRaw<string>(checkQuery, input.Guid ?? input.Id.ToString(), branchId).FirstOrDefaultAsync();

                string activeTraId;
                string activeTraNo;

                if (!string.IsNullOrEmpty(existingTraId))
                {
                    activeTraId = existingTraId;
                    activeTraNo = "EXISTING";

                    var updateHeadlineQuery = $@"UPDATE {headLineTable} SET
            TraCurId={{0}}, TraCurValue={{1}}, TraAccId={{2}}, TraAccName={{3}},
            TraPatId={{4}}, TraPatName={{5}}, TraTot={{6}}, TraNet={{7}}, TraCbn={{8}}, TraNetLet={{9}},
            TrauId={{10}}, TrauName={{11}}, TraDateId={{12}}, TraDate={{13}}, TraTimeId={{14}},
            TraDateTime={{15}}, TraDateTimeId={{16}}, TraRemarks={{17}},
            TRACCID={{18}}, TRACCNAME={{19}}, TraLstId={{20}}, 
            TraCheekDate={{13}}, TraCheekDateId={{12}}, 
            TraShipDate={{13}}, TraShipDateId={{12}}
            WHERE TraId = {{21}}";

                    await _context.Database.ExecuteSqlRawAsync(updateHeadlineQuery,
                        input.CurrencyId.ToString(), fmt(input.CurrencyRate ?? 1m),
                        Truncate(input.BoxAccountId, 30), Truncate(input.CashBox, 225),
                        "", Truncate(input.AccountName ?? ". . .", 225),
                        fmt(input.TotalAmount), fmt(input.TotalAmount), fmt(input.TotalAmount),
                        Tafqeet(input.TotalAmount, input.CurrencyId.ToString()),
                        Truncate(input.UserId.ToString(), 10), Truncate(input.UserName, 150),
                        traDateId, traDate, traTimeId,
                        traDateTime, traDateTimeId,
                        Truncate(input.Description ?? "", 500),
                        input.CostCenterNo?.ToString() ?? "0",
                        Truncate(input.CostCenter ?? "", 100),
                        sqlTraLstId,
                        activeTraId
                    );

                    var updateHeadQuery = $@"UPDATE {headTable} SET 
                TraDateId={{0}}, TraTimeId={{1}}, TraDateTimeId={{2}}, TraDateTime={{3}}, TraDate={{4}}
                WHERE TraId = {{5}}";

                    await _context.Database.ExecuteSqlRawAsync(updateHeadQuery,
                        traDateId, traTimeId, traDateTimeId, traDateTime, traDate, activeTraId);

                    await _context.Database.ExecuteSqlRawAsync($"DELETE FROM {accTable} WHERE TRAID = {{0}}", activeTraId);
                    await _context.Database.ExecuteSqlRawAsync($"DELETE FROM {jeTable} WHERE TRAID = {{0}}", activeTraId);
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

                    var lastTraNoQuery = $@"SELECT TOP 1 TraNo AS Value FROM {headLineTable} WITH (UPDLOCK) 
                        WHERE TraBraId = {{0}} AND TraLstId = {{1}} AND TraSecId = {{2}} 
                        ORDER BY CONVERT(BIGINT, TraNo) DESC";

                    var lastTraNoStr = await _context.Database.SqlQueryRaw<string>(lastTraNoQuery, branchId, sqlTraLstId, departmentId).FirstOrDefaultAsync();

                    long nextTraNo = (string.IsNullOrEmpty(lastTraNoStr) ? 0 : (long.TryParse(lastTraNoStr, out long num) ? num : 0)) + 1;
                    activeTraNo = nextTraNo.ToString();

                    var idPrefix = $"11{year}{departmentId}{branchId}{sqlTraLstId}";
                    var lastTraIdQuery = $"SELECT TOP 1 TraId AS Value FROM {headTable} WHERE TraId LIKE {{0}} ORDER BY LEN(TraId) DESC, TraId DESC";
                    var lastTraId = await _context.Database.SqlQueryRaw<string>(lastTraIdQuery, idPrefix + "%").FirstOrDefaultAsync();

                    long nextSequenceNumber = 1;
                    if (!string.IsNullOrEmpty(lastTraId) && lastTraId.Length > idPrefix.Length)
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
                {{6}}, {{7}}, {{8}}, {{9}}, {{10}}, '0', {{11}}, '1')";

                    await _context.Database.ExecuteSqlRawAsync(headQuery,
                        activeTraId, activeTraNo, sqlTraLstId, year, Truncate(branchId, 5), Truncate(departmentId, 5),
                        traDateId, traTimeId, traDateTimeId, traDateTime, traDate, Truncate(input.InvoiceType, 100));

                    var headlineQuery = $@"INSERT INTO {headLineTable} 
            (TraId, TraNo, TraRefr, TraPatRefId, TraTitle, TraRepTitle, TraCurId, TraCurValue, TraAccId, TraAccName, 
            TraPatId, TraPatName, TraTot, TraDis, TraTax, TraNet, TraCbn, TraNetLet, TraStoreId, 
            TraStoreName, TrauId, TrauName, TraStatusDel, TraLstId, TraYearId, TraBraId, TraDateId, TraDate, TraTimeId, 
            TraDateTime, TraDateTimeId, TraComId, TraSecId, TraPosId, TraIndex, 
            TraLastEditUsrId, TraLastEditUsrName, TraOptions, TraCheekDate, TraCheekDateId, TRAPATCODEOK, TraSysId, TraRemarks,
            TRASIGND, TRACCID, TRACCNAME, TRASHIPDATE, TRASHIPDATEID)
            VALUES 
            ({{0}}, {{1}}, {{2}}, {{3}}, {{4}}, {{5}}, {{6}}, {{7}}, {{8}}, {{9}}, 
            {{10}}, {{11}}, {{12}}, {{13}}, {{14}}, {{15}}, {{16}}, {{17}}, {{18}}, 
            {{19}}, {{20}}, {{21}}, '0', {{22}}, {{23}}, {{24}}, {{25}}, {{26}}, {{27}}, 
            {{28}}, {{29}}, {{30}}, {{31}}, '0', '0', 
            {{32}}, {{33}}, '', {{26}}, {{25}}, '', {{35}}, {{36}},
            {{37}}, {{38}}, {{39}}, {{26}}, {{25}})";

                    await _context.Database.ExecuteSqlRawAsync(headlineQuery,
                        activeTraId, activeTraNo, Truncate(finalTraRefr, 150), input.Guid ?? input.Id.ToString(),
                        Truncate(input.InvoiceType, 100), Truncate(input.InvoiceType, 100),
                        input.CurrencyId.ToString(), fmt(input.CurrencyRate ?? 1m),
                        Truncate(input.BoxAccountId, 30), Truncate(input.CashBox, 225),
                        "", Truncate(input.AccountName ?? ". . .", 225),
                        fmt(input.TotalAmount), "0", "0", fmt(input.TotalAmount), fmt(input.TotalAmount),
                        Tafqeet(input.TotalAmount, input.CurrencyId.ToString()),
                        "", "",
                        Truncate(input.UserId.ToString(), 10), Truncate(input.UserName, 150),
                        sqlTraLstId, year, Truncate(branchId, 5), traDateId, traDate, traTimeId,
                        traDateTime, traDateTimeId, "1", Truncate(departmentId, 5),
                        Truncate(input.UserId.ToString(), 10), Truncate(input.UserName, 150),
                        traDateId,
                         "1",
                        Truncate(input.Description ?? "", 500),
                        headerSignal,
                        input.CostCenterNo?.ToString() ?? "0",
                        Truncate(input.CostCenter ?? "", 100)
                    );
                }



                bool isReceipt = sqlTraLstId == "201";
                decimal headerRate = input.CurrencyRate ?? 1m;
                decimal headerForeignAmount = input.TotalAmount;

                var accQueryBuilder = new System.Text.StringBuilder();
                var accParams = new List<object>();
                int pIdx = 0;

                string headerCurId = input.CurrencyId.ToString();
                string headerCurChar = input.Currency;
                string description = Truncate(input.Description ?? "", 1500);

                if (input.Items != null && input.Items.Any())
                {
                    accQueryBuilder.Append($@"INSERT INTO {accTable} 
             (TRADID, TRAID, TRADINDEX, TRADSIGNAL, TRADSIGDBT, TRADSIGCDT, 
             TRADACCID, TRADACCNAME, TRADTACCID, TRADTACCNAME, 
             TRADTAXACC, TRADDISACC, 
             TRADAMOUNT, TRADPRICE, TRADQTY, 
             TRADACCCURID, TRADACCCURVALUE, TRADACCCURCHAR, 
             TRADDEBIT, TRADCREDIT, TRADDETAIL, 
             TRADSHEEQDATE, TRADSHEEQDATEID) VALUES ");

                    int itemIndex = 1;
                    for (int i = 0; i < input.Items.Count; i++)
                    {
                        var item = input.Items[i];
                        if (i > 0) accQueryBuilder.Append(",");

                        string itemCurId = item.ItemCurrencyId.ToString();
                        string itemCurChar = item.ItemCurrency ?? "";
                        decimal itemRate = item.ExchangeRate;
                        decimal itemForeignAmount = item.Amount;

                        string tradSignal = isReceipt ? "1" : "-1";
                        string tradSigDbt = isReceipt ? "1" : "-1";
                        string tradSigCdt = isReceipt ? "1" : "-1";

                        bool hasControlAccount = !string.IsNullOrEmpty(item.ControlAccountId);
                        string tradAccId = hasControlAccount ? Truncate(item.ControlAccountId, 50) : Truncate(item.AccountNo, 50);
                        string tradTAccId = hasControlAccount ? Truncate(item.AccountNo, 30) : "";
                        string tradTaxAcc = Truncate(item.AccountNo, 30);
                        string tradDisAcc = Truncate(item.AccountNo, 30);

                        accQueryBuilder.Append($@"
                     ({{{pIdx++}}}, {{{pIdx++}}}, {{{pIdx++}}}, '{tradSignal}', '{tradSigDbt}', '{tradSigCdt}', 
                     {{{pIdx++}}}, {{{pIdx++}}}, {{{pIdx++}}}, '', 
                     {{{pIdx++}}}, {{{pIdx++}}}, 
                     {{{pIdx++}}}, {{{pIdx++}}}, '1', 
                     {{{pIdx++}}}, {{{pIdx++}}}, {{{pIdx++}}}, 
                     {{{pIdx++}}}, {{{pIdx++}}}, {{{pIdx++}}}, 
                     {{{pIdx++}}}, {{{pIdx++}}})");

                                    accParams.AddRange(new object[] {
                         $"{activeTraId}.{itemIndex}",
                         activeTraId,
                         itemIndex.ToString(),
                         tradAccId,
                         Truncate(input.AccountName, 150),
                         tradTAccId,
                         tradTaxAcc,
                         tradDisAcc,
                         fmt(itemForeignAmount),       // TRADAMOUNT: كما هو (itemForeignAmount)
                         fmt(item.EquivalentAmount),   // TRADPRICE: تم التعديل ليأخذ القيمة المقابلة من الموديل
                         itemCurId,
                         fmt(itemRate),
                         Truncate(itemCurChar, 10),
                         fmt(itemForeignAmount),       // TRADDEBIT: كما هو (itemForeignAmount)
                         fmt(itemForeignAmount),       // TRADCREDIT: كما هو (itemForeignAmount)
                         description,
                         traDate,
                         traDateId
                     });
                        itemIndex++;
                    }
                    await _context.Database.ExecuteSqlRawAsync(accQueryBuilder.ToString(), accParams.ToArray());
                }

                if (input.Postings != null && input.Postings.Any())
                {
                    string jeQuery = $@"INSERT INTO {jeTable} 
(TRADID, TRAID, TRADINDEX, TRADTYPE, TRADLSTID, 
TRADACCID, TRADACCNAME, TRADTACCID, TRADTACCNAME, TRADHACCID, TRADHACCNAME, 
TRADDATID, TRADDATNAME, TRADFACCID, TRADFFACCID, TRADNACCID, TRADMACCID, 
TRADCCID, TRADCCNAME, TRADCLASSID, TRADCLASS, 
TRADCURID, TRADVALUE, TRADVALUELOC, TRADVALUETRA, TRADCURVALUE, 
TRADSIG, TRADSIGD, TRADSIGC, TRADSIGO, TRADSIGY, TRADSIGQ, TRADSIGS, 
TRADDETAIL, TRADREFID, TRADREFNO, TRADDATE, TRADDATEID, 
TRADOTHER1, TRADOTHER2, TRADOTHER3, TRADOTHER4, TRADOTHER5, 
TRADOTHER6, TRADOTHER7, TRADOTHER8, TRADOTHER9, SYNCFLAG, TRADHDETAIL) VALUES 
({{0}}, {{1}}, {{2}}, '0', {{3}}, 
{{4}}, {{5}}, {{6}}, {{7}}, {{8}}, {{9}}, 
'', {{10}}, '', '', '', '', 
{{11}}, {{12}}, '', '', 
{{13}}, {{14}}, {{15}}, {{16}}, {{17}}, 
{{18}}, {{19}}, {{20}}, '0', '0', '0', '0', 
{{21}}, '', {{22}}, {{23}}, {{24}}, 
'', '', '', '', '', 
'', '', '', '', '1', '')";

                    int jeIndex = 1;

                    foreach (var posting in input.Postings)
                    {
                        bool hasControl = !string.IsNullOrEmpty(posting.ControlAccountId);
                        string jeAccId = hasControl ? Truncate(posting.ControlAccountId, 50) : Truncate(posting.AccountNo, 50);
                        string jeTAccId = Truncate(posting.AccountNo, 50);
                        string jeAccName = Truncate(posting.AccountName, 150);
                        string jeTAccName = jeAccName;

                        string jeHAccId = "", jeHAccName = "";

                        if (input.Postings.Count == 2)
                        {
                            var partner = input.Postings.FirstOrDefault(p => p.Id != posting.Id);
                            if (partner != null)
                            {
                                jeHAccId = Truncate(partner.AccountNo, 50);
                                jeHAccName = Truncate(partner.AccountName, 150);
                            }
                        }

                        decimal debit = posting.Debit ?? 0;

                        var matchedItem = input.Items.FirstOrDefault(x => x.AccountNo == posting.AccountNo);

                        decimal originalAmount;
                        decimal equivalentAmount;
                        string currencyId;

                        // معالجة المبالغ مع التقريب (Math.Round)
                        if (matchedItem != null)
                        {
                            originalAmount = matchedItem.EquivalentAmount;
                            equivalentAmount = Math.Round(originalAmount * posting.ExchangeRate, 2);
                            currencyId = matchedItem.ItemCurrencyId.ToString();
                        }
                        else
                        {
                            originalAmount = posting.OriginalAmount;
                            equivalentAmount = Math.Round(originalAmount * posting.ExchangeRate, 2);
                            currencyId = posting.CurrencyId.ToString();
                        }

                        decimal transactionTotalAmount = input.TotalAmount;

                        // ✅ تصحيح الإشارات (TRADSIG)
                        string jeSig, jeSigD, jeSigC;

                        if (sqlTraLstId == "201") // سند قبض
                        {
                            // القبض: المدين (الصندوق) -1، الدائن (العميل) 1
                            if (debit > 0) { jeSig = "-1"; jeSigD = "1"; jeSigC = "0"; }
                            else { jeSig = "1"; jeSigD = "0"; jeSigC = "1"; }
                        }
                        else // سند صرف (202)
                        {
                            // ✅ التعديل هنا للصرف: 
                            // المدين (العميل) يأخذ -1
                            // الدائن (الصندوق) يأخذ 1
                            if (debit > 0)
                            {
                                jeSig = "-1"; // العميل (مدين)
                                jeSigD = "1";
                                jeSigC = "0";
                            }
                            else
                            {
                                jeSig = "1";  // الصندوق (دائن)
                                jeSigD = "0";
                                jeSigC = "1";
                            }
                        }

                        string jeCCId = posting.CostCenterNo?.ToString() ?? "0";
                        string jeCCName = "-";
                        if (jeCCId == (input.CostCenterNo?.ToString() ?? "-99"))
                        {
                            jeCCName = Truncate(input.CostCenter ?? "-", 50);
                        }
                        else if (!string.IsNullOrEmpty(posting.CostCenterName))
                        {
                            jeCCName = Truncate(posting.CostCenterName, 50);
                        }

                        object[] jeParams = new object[] {
            $"{activeTraId}.{jeIndex}", activeTraId, jeIndex.ToString(), sqlTraLstId,
            jeAccId, jeAccName, jeTAccId, jeTAccName, jeHAccId, jeHAccName, jeHAccName,
            jeCCId, jeCCName, currencyId,
            fmt(originalAmount),
            fmt(equivalentAmount),
            fmt(transactionTotalAmount),
            fmt(posting.ExchangeRate),
            jeSig, jeSigD, jeSigC, Truncate(posting.Description ?? "", 1500), "", traDate, traDateId
        };

                        await _context.Database.ExecuteSqlRawAsync(jeQuery, jeParams);
                        jeIndex += 2;
                    }
                }

                await transaction.CommitAsync();
                return Ok(new { Message = "Synced Successfully", TransactionId = activeTraId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet("get-receipt-details")]
        public async Task<IActionResult> GetReceiptDetails([FromQuery] string transactionId, [FromQuery] string branchId, [FromQuery] string year)
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
                string accountTableName = $"SunTransAcc{tableSuffix}";

                // 1. استعلام الرأس
                var rawHeader = await _context.Database.SqlQueryRaw<ReceiptHeaderResultDto>($@"
            SELECT TOP 1
                T.TraPatRefId AS MobileId,
                T.TraNo AS TransactionNumber,
                T.TraDate AS Date,
                T.TraLstId AS InvoiceTypeChar,
                T.TraAccId AS BoxAccountId,
                T.TraAccName AS CashBox,
                T.TraPatName AS AccountName,
                T.TRACCID AS CostCenterNo,
                T.TRACCNAME AS CostCenter,
                ISNULL(T.TraRemarks, '') AS Description, 
                ISNULL(C.CURNAME, '') AS CurrencyName,
                T.TraCurId AS CurrencyId,
                CAST(ISNULL(NULLIF(T.TraCurValue, ''), '1') AS DECIMAL(18, 3)) AS CurrencyRate,
                CAST(ISNULL(NULLIF(T.TraTot, ''), '0') AS DECIMAL(18, 3)) AS TotalAmount,
                T.TraNetLet AS AmountInWords,
                T.TrauId AS UserId,
                T.TrauName AS UserName
            FROM {headlineTableName} T
            LEFT JOIN SUNCURY C ON T.TraCurId = C.CURID
            WHERE T.TraId = {{0}}", transactionId).FirstOrDefaultAsync();

                if (rawHeader == null)
                {
                    return NotFound($"Receipt {transactionId} not found.");
                }

                // 2. استعلام البنود (تم التعديل للربط المباشر بالأعمدة TRADPRICE و TRADAMOUNT)
                var itemsQuery = $@"
            SELECT 
                CAST(0 AS BIGINT) AS Id,
                TRAID AS ReceiptPaymentId,
                ISNULL(NULLIF(TRADTACCID, ''), ISNULL(TRADACCID, '')) AS AccountNo,
                ISNULL(TRADACCNAME, '') AS AccountName,
                
                -- المبلغ (العملة الأجنبية)
                CAST(ISNULL(NULLIF(TRADAMOUNT, ''), '0') AS DECIMAL(18, 3)) AS Amount,
                
                ISNULL(C.CURNAME, '') AS ItemCurrency,
                CAST(ISNULL(NULLIF(TRADACCCURID, ''), '1') AS INT) AS ItemCurrencyId,
                CAST(ISNULL(NULLIF(TRADACCCURVALUE, ''), '1') AS DECIMAL(18, 3)) AS ExchangeRate,

                -- ✅✅✅ التعديل هنا: جلب المبلغ المقابل من عمود TRADPRICE مباشرة بدون حسابات
                CAST(ISNULL(NULLIF(TRADPRICE, ''), '0') AS DECIMAL(18, 3)) AS EquivalentAmount,
                
                '' AS ControlAccountId 
            FROM {accountTableName} A
            LEFT JOIN SUNCURY C ON A.TRADACCCURID = C.CURID
            WHERE TRAID = {{0}} 
            AND TRADACCID <> '{rawHeader.BoxAccountId}'
            ORDER BY CAST(TRADINDEX AS INT)";

                List<ReceiptItemResultDto> items = await _context.Database.SqlQueryRaw<ReceiptItemResultDto>(itemsQuery, transactionId).ToListAsync();

                var invoiceTypeMapReversed = new Dictionary<string, string>
        {
            { "201", "ق" }, { "202", "ص" }
        };

                string typeChar = invoiceTypeMapReversed.ContainsKey(rawHeader.InvoiceTypeChar)
                    ? invoiceTypeMapReversed[rawHeader.InvoiceTypeChar]
                    : "ق";

                string typeName = (typeChar == "ق") ? "سند قبض" : "سند صرف";

                DateTime parsedDate = DateTime.Now;
                if (!string.IsNullOrEmpty(rawHeader.Date))
                {
                    DateTime.TryParseExact(rawHeader.Date, "dd/MM/yyyy",
                       System.Globalization.CultureInfo.InvariantCulture,
                       System.Globalization.DateTimeStyles.None, out parsedDate);
                }

                var receiptObj = new
                {
                    id = int.TryParse(rawHeader.MobileId, out int mid) ? mid : 0,
                    transactionNumber = rawHeader.TransactionNumber,
                    date = parsedDate,
                    cashBox = rawHeader.CashBox,
                    boxAccountId = rawHeader.BoxAccountId,
                    currency = rawHeader.CurrencyName ?? "",
                    currencyId = int.TryParse(rawHeader.CurrencyId, out int cid) ? cid : 1,
                    currencyRate = rawHeader.CurrencyRate,
                    accountName = rawHeader.AccountName,
                    costCenter = rawHeader.CostCenter,
                    costCenterNo = int.TryParse(rawHeader.CostCenterNo, out int ccNo) ? ccNo : (int?)null,
                    description = rawHeader.Description,
                    totalAmount = rawHeader.TotalAmount,
                    amountInWords = rawHeader.AmountInWords,
                    userId = int.TryParse(rawHeader.UserId, out int uid) ? uid : 0,
                    userName = rawHeader.UserName,
                    invoiceTypeChar = typeChar,
                    invoiceType = typeName,
                    isSynced = true,
                    sunYear = int.Parse(year),
                    branchId = int.Parse(branchId),
                    createdAt = parsedDate,
                    updatedAt = DateTime.Now
                };

                var itemsObj = items.Select(i => new
                {
                    id = 0,
                    receiptPaymentId = receiptObj.id,
                    accountNo = i.AccountNo,
                    accountName = i.AccountName,
                    amount = i.Amount,
                    itemCurrency = i.ItemCurrency ?? "",
                    itemCurrencyId = i.ItemCurrencyId,
                    exchangeRate = i.ExchangeRate,
                    equivalentAmount = i.EquivalentAmount,
                    controlAccountId = i.ControlAccountId
                }).ToList();

                return Ok(new
                {
                    receipt = receiptObj,
                    items = itemsObj
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        public class ReceiptHeaderResultDto
        {
            public string MobileId { get; set; }
            public string TransactionNumber { get; set; }
            public string Date { get; set; }
            public string InvoiceTypeChar { get; set; }
            public string BoxAccountId { get; set; }
            public string CashBox { get; set; }
            public string AccountName { get; set; }
            public string CostCenterNo { get; set; }
            public string CostCenter { get; set; }
            public string Description { get; set; }
            public string CurrencyName { get; set; }
            public string CurrencyId { get; set; }
            public decimal? CurrencyRate { get; set; }
            public decimal TotalAmount { get; set; }
            public string AmountInWords { get; set; }
            public string UserId { get; set; }
            public string UserName { get; set; }
        }

        public class ReceiptItemResultDto
        {
            public long Id { get; set; }
            public string ReceiptPaymentId { get; set; }
            public string AccountNo { get; set; }
            public string AccountName { get; set; }
            public decimal Amount { get; set; }
            public string ItemCurrency { get; set; }
            public int? ItemCurrencyId { get; set; }
            public decimal ExchangeRate { get; set; } 
            public decimal EquivalentAmount { get; set; }
            public string ControlAccountId { get; set; }
        }

        private string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value ?? "";
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