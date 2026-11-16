using AccountingSyncAPP.Data;
using AccountingSyncAPP.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;  

namespace AccountingSyncAPP.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RestaurantSyncController : ControllerBase
    {
        private readonly AccountingDbContext _context;
        private readonly ILogger<RestaurantSyncController> _logger;

        public RestaurantSyncController(AccountingDbContext context, ILogger<RestaurantSyncController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private string GetConnectionString()
        {
            return _context.Database.GetDbConnection().ConnectionString;
        }

        private string GetSystemYearSuffix(SqlConnection connection, SqlTransaction transaction)
        {
            string query = "SELECT COUNT(*), MAX(YRVALUE) FROM TSSYSYEAR";

            using (var command = new SqlCommand(query, connection, transaction))
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    int yearCount = reader.GetInt32(0);
                    if (yearCount > 1)
                    {
                        return reader[1].ToString();
                    }
                }
            }
            return "";
        }

        private string GetTableName(string baseTableName, string yearSuffix, string branchId)
        {
            return $"{baseTableName}{yearSuffix}{branchId}";
        }

        [HttpPost("sync-invoice")]
        public async Task<IActionResult> SyncInvoice([FromBody] RestaurantInvoiceDto invoice)
        {
            if (invoice == null || string.IsNullOrEmpty(invoice.InvoiceNumber) || !invoice.Details.Any())
                return BadRequest(new { message = "بيانات الفاتورة ناقصة أو غير صالحة." });

            if (string.IsNullOrEmpty(invoice.YearId))
                return BadRequest(new { message = "السنة المالية مطلوبة لحفظ الفاتورة." });

            if (string.IsNullOrEmpty(invoice.UserId))
                return BadRequest(new { message = "رقم المستخدم مطلوب لإنشاء رقم الفاتورة." });

            try
            {
                var connectionString = GetConnectionString();
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            string minDateStr = "";
                            string maxDateStr = "";
                            using (var yearCmd = new SqlCommand("SELECT YRMINDATE, YRMAXDATE FROM TSSYSYEAR WHERE YRVALUE = @YearId", connection, transaction))
                            {
                                yearCmd.Parameters.AddWithValue("@YearId", invoice.YearId);
                                using (var reader = await yearCmd.ExecuteReaderAsync())
                                {
                                    if (!await reader.ReadAsync())
                                        return BadRequest(new { message = "السنة المالية المحددة غير صالحة." });

                                    minDateStr = reader["YRMINDATE"].ToString() ?? "";
                                    maxDateStr = reader["YRMAXDATE"].ToString() ?? "";
                                }
                            }

                            DateTime transactionDate;
                            if (!DateTime.TryParseExact(invoice.TransactionDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out transactionDate) &&
                                !DateTime.TryParse(invoice.TransactionDate, out transactionDate))
                            {
                                transactionDate = DateTime.Now;
                            }

                            DateTime minDate;
                            DateTime maxDate;
                            
                            if (!DateTime.TryParseExact(minDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out minDate)) minDate = DateTime.MinValue;
                            if (!DateTime.TryParseExact(maxDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out maxDate)) maxDate = DateTime.MaxValue;

                            if (transactionDate.Date < minDate.Date || transactionDate.Date > maxDate.Date)
                            {
                                return BadRequest(new { message = "تاريخ الحركة خارج نطاق السنة المالية المحددة." });
                            }

                            string yearSuffix = GetSystemYearSuffix(connection, transaction);
                            string branchId = invoice.BranchId?.ToString() ?? "1";

                            string headTable = GetTableName("SUNTRANSHEAD", yearSuffix, branchId);
                            string headLineTable = GetTableName("SUNTRANSHEADLINE", yearSuffix, branchId);
                            string detailItemTable = GetTableName("SUNTRANSDETAILITEM", yearSuffix, branchId);
                            string jeTable = GetTableName("SUNTRANSJE", yearSuffix, branchId);

                            string nextTraNo;
                            string userIdStr = invoice.UserId;

                            if (userIdStr == "0")
                            {
                                var adminCmd = new SqlCommand($"SELECT MAX(CONVERT(BIGINT, TRANO)) FROM {headLineTable} WHERE TRAUID = '0' AND TRALSTID = @TraLstId", connection, transaction);
                                adminCmd.Parameters.AddWithValue("@TraLstId", invoice.TraLstId);
                                var lastAdminNo = await adminCmd.ExecuteScalarAsync();

                                long nextAdminSequence = 1;
                                if (lastAdminNo != null && lastAdminNo != DBNull.Value)
                                {
                                    nextAdminSequence = Convert.ToInt64(lastAdminNo) + 1;
                                }
                                nextTraNo = nextAdminSequence.ToString();
                            }
                            else
                            {
                                var userCmd = new SqlCommand($"SELECT MAX(CONVERT(BIGINT, TRANO)) FROM {headLineTable} WHERE TRAUID = @UserId AND TRALSTID = @TraLstId", connection, transaction);
                                userCmd.Parameters.AddWithValue("@UserId", userIdStr);
                                userCmd.Parameters.AddWithValue("@TraLstId", invoice.TraLstId);
                                var lastUserNo = await userCmd.ExecuteScalarAsync();

                                long nextUserSequence = 1;
                                if (lastUserNo != null && lastUserNo != DBNull.Value)
                                {
                                    string lastUserNoStr = lastUserNo.ToString() ?? "";
                                    if (lastUserNoStr.StartsWith(userIdStr) && lastUserNoStr.Length > userIdStr.Length)
                                    {
                                        string sequencePart = lastUserNoStr.Substring(userIdStr.Length);
                                        long.TryParse(sequencePart, out nextUserSequence);
                                        nextUserSequence++;
                                    }
                                }
                                nextTraNo = userIdStr + nextUserSequence.ToString("D4");
                            }

                            string traId = invoice.InvoiceNumber;
                            string traRefr = invoice.OrderNumber ?? "";
                            DateTime finalDateTime = transactionDate.Date + DateTime.Now.TimeOfDay;
                            var dateId = finalDateTime.ToString("yyyyMMdd");
                            var timeId = finalDateTime.ToString("HHmmss");
                            var formattedDate = finalDateTime.ToString("dd/MM/yyyy");
                            var formattedDateTime = finalDateTime.ToString("dd/MM/yyyy h:mm:ss tt", new CultureInfo("en-US"));

                            string head1Query = $@"INSERT INTO {headTable} (TRAID, TRALSTID, TRAYEARID, TRASYSID, TRABRAID, TRACOMID, TRASECID, TRAPOSID,
                                                                    TRAINDEX, TRANO, TRADATEID, TRATIMEID, TRADATETIMEID, TRADATETIME, TRADATE, TRASTATUSDEL, TRATITLE, TRASYNC) VALUES
                                                                   (@TRAID, @TraLstId, @YearId, '1', @BranchId, '1', @SectionId, '0', '0', @TRANO, @DateId, @TimeId, @DateId + @TimeId,
                                                                    @FormattedDateTime, @FormattedDate, '0', @TraTitle, '1')";
                            using (var cmdHead1 = new SqlCommand(head1Query, connection, transaction))
                            {
                                cmdHead1.Parameters.AddWithValue("@TRAID", traId);
                                cmdHead1.Parameters.AddWithValue("@TraLstId", invoice.TraLstId);
                                cmdHead1.Parameters.AddWithValue("@TraTitle", invoice.TraTitle ?? (invoice.TraLstId == "801" ? "طلبية زبون" : "طلبية زبون اجل"));
                                cmdHead1.Parameters.AddWithValue("@YearId", invoice.YearId);
                                cmdHead1.Parameters.AddWithValue("@BranchId", invoice.BranchId);
                                cmdHead1.Parameters.AddWithValue("@SectionId", invoice.SectionId ?? "1");
                                cmdHead1.Parameters.AddWithValue("@TRANO", nextTraNo);
                                cmdHead1.Parameters.AddWithValue("@DateId", dateId);
                                cmdHead1.Parameters.AddWithValue("@TimeId", timeId);
                                cmdHead1.Parameters.AddWithValue("@FormattedDateTime", formattedDateTime);
                                cmdHead1.Parameters.AddWithValue("@FormattedDate", formattedDate);
                                await cmdHead1.ExecuteNonQueryAsync();
                            }

                            string netTotalInWords = invoice.NetTotalInWords ?? "";
                            string head2Query = $@"INSERT INTO {headLineTable} (
    TRAID, TRATITLE, TRAREFR, TRACURID, TRACURVALUE, TRAACCID, TRAACCNAME, TRAPATNAME, 
    TRATOT, TRATAX, TRADIS, TRANET, TRASTOREID, TRASTORENAME, TRASIGND, TRAUID, TRAUNAME, 
    TRABRAID, TRAYEARID, TRANO, TRADATEID, TRASTATUS, TRAPRINTSTATUS, TRASTATUSDEL, TRANETLET,
    TRAREPTITLE, TRAPATPERVAL, TRAPATPERAMOUNT, TRAPATPAID, TRAPATTOTPAID, TRASRV, TRATCK,
    TRACBN, TRAPERDIS, TRAPERTAX, TRAPERSRV, TRADELID, TRADCTID, TRACHKUSR1, TRAPRINTINFO,
    TRALASTEDITUSRID, TRALASTEDITUSRNAME, TRALSTID, TRASYSID, TRACOMID, TRASECID, TRAPOSID,
    TRAINDEX, TRADATE, TRADATETIME, TRATIMEID, TRADATETIMEID
) VALUES (
    @TRAID, @TraTitle, @TRAREFR, @CurrencyId, @CurrencyValue, @BoxId, @BoxName, @CustomerName, 
    @Total, @Tax, @Discount, @NetTotal, @StoreId, @StoreName, '-1', @UserId, @UserName,
    @BranchId, @YearId, @TRANO, @DateId, '0', '0', '0', @NetInWords,
    @TraTitle, '0', '0', '0', '0', '0', '0',
    '0', '0', '0', '0', '1', @OrderTypeId, '0', '0', 
    @UserId, @UserName, @TraLstId, '1', '1', @SectionId, '0',
    '0', @FormattedDate, @FormattedDateTime, @TimeId, @DateId + @TimeId
)";

                            using (var cmdHead2 = new SqlCommand(head2Query, connection, transaction))
                            {
                                cmdHead2.Parameters.AddWithValue("@TRAID", traId);
                                cmdHead2.Parameters.AddWithValue("@TraTitle", invoice.TraTitle ?? (invoice.TraLstId == "801" ? "طلبية زبون" : "طلبية زبون اجل"));
                                cmdHead2.Parameters.AddWithValue("@TRAREFR", traRefr);
                                cmdHead2.Parameters.AddWithValue("@CurrencyId", invoice.CurrencyId ?? "1");
                                cmdHead2.Parameters.AddWithValue("@CurrencyValue", invoice.CurrencyValue);
                                // Soft-Swap: Map BoxNo to TRAACCID (BoxId param) and BoxParentName to BoxName param
                                cmdHead2.Parameters.AddWithValue("@BoxId", string.IsNullOrEmpty(invoice.BoxNo) ? (invoice.BoxId ?? "") : invoice.BoxNo);
                                cmdHead2.Parameters.AddWithValue("@BoxName", string.IsNullOrEmpty(invoice.BoxParentName) ? (invoice.BoxName ?? "") : invoice.BoxParentName);
                                cmdHead2.Parameters.AddWithValue("@CustomerName", invoice.CustomerName ?? "");
                                cmdHead2.Parameters.AddWithValue("@Total", invoice.Total);
                                cmdHead2.Parameters.AddWithValue("@Tax", invoice.Tax);
                                cmdHead2.Parameters.AddWithValue("@Discount", invoice.Discount);
                                cmdHead2.Parameters.AddWithValue("@NetTotal", invoice.NetTotal);
                                cmdHead2.Parameters.AddWithValue("@StoreId", invoice.StoreId ?? "1");
                                cmdHead2.Parameters.AddWithValue("@StoreName", invoice.StoreName ?? "");
                                cmdHead2.Parameters.AddWithValue("@UserId", invoice.UserId);
                                cmdHead2.Parameters.AddWithValue("@UserName", invoice.UserName ?? "");
                                cmdHead2.Parameters.AddWithValue("@BranchId", invoice.BranchId);
                                cmdHead2.Parameters.AddWithValue("@YearId", invoice.YearId);
                                cmdHead2.Parameters.AddWithValue("@TRANO", nextTraNo);
                                cmdHead2.Parameters.AddWithValue("@DateId", dateId);
                                cmdHead2.Parameters.AddWithValue("@NetInWords", netTotalInWords);
                                cmdHead2.Parameters.AddWithValue("@SectionId", invoice.SectionId ?? "1");
                                cmdHead2.Parameters.AddWithValue("@FormattedDate", formattedDate);
                                cmdHead2.Parameters.AddWithValue("@FormattedDateTime", formattedDateTime);
                                cmdHead2.Parameters.AddWithValue("@TimeId", timeId);
                                cmdHead2.Parameters.AddWithValue("@OrderTypeId", invoice.OrderTypeId ?? "0");
                                cmdHead2.Parameters.AddWithValue("@TraLstId", invoice.TraLstId);

                                await cmdHead2.ExecuteNonQueryAsync();
                            }

                            int detailIndex = 1;
                            foreach (var detail in invoice.Details)
                            {
                                decimal discountPercentForDb = detail.Discount > 0 ? (detail.Discount / 100m) : 0m;
                                decimal taxPercentForDb = detail.Tax > 0 ? (detail.Tax / 100m) : 0m;

                                string detailQuery = $@"INSERT INTO {detailItemTable} (
                            TRAID, TRADID, TRADINDEX, TRADITMID, TRADITMNAME, TRADITMNAMEE, TRADITMUNIT, TRADUNITSIZE, TRADSTORID,
                            TRADQTY, TRADQTYTOT, TRADUNTVALUE, TRADITMUNTVALUE, TRADAMOUNT, TRADNETVALUE, TRADNETAMOUNT,
                            TRADTRADATEID, TRADTRATIMEID, TRADLISTID, TRADSIGND, TRADSIGNDIN, TRADSIGNDOUT, TRADSIGNDCOST, TRADSIGNDREP, TRADREMARK,
                            TRADBONSIGND, TRADQTYDIS, TRADUNTDIS, TRADUNTDISPER, TRADUNTTAX, TRADUNTTAXPER, TRADITMCURID,
                            TRADITMCURVALUE, TRADTRACURVALUE, TRADEXPIREID, TRADBRAID
                        ) VALUES (
                            @TRAID, @TRADID, @TRADINDEX, @ItemId, @ItemName, @ItemName, @Unit, @PackFactor, @StoreId,
                            @Quantity, @Quantity, @Price, @Price, @Total, @Price, @Total,
                            @DateId, @TimeId, @TraLstId, '-1', '0', '1', '0', '1', @Notes,
                            '0', '0', @DiscountAmount, @DiscountPercent, @TaxAmount, @TaxPercent, @CurrencyId,
                            @CurrencyValue, @CurrencyValue, '0', @BranchId
                        )";

                                using (var cmdDetail = new SqlCommand(detailQuery, connection, transaction))
                                {
                                    cmdDetail.Parameters.AddWithValue("@TRAID", traId);
                                    cmdDetail.Parameters.AddWithValue("@TRADID", $"{traId}.{detailIndex}");
                                    cmdDetail.Parameters.AddWithValue("@TRADINDEX", detailIndex++);
                                    cmdDetail.Parameters.AddWithValue("@ItemId", detail.ItemId);
                                    cmdDetail.Parameters.AddWithValue("@ItemName", detail.ItemName);
                                    cmdDetail.Parameters.AddWithValue("@Unit", detail.Unit ?? "");
                                    cmdDetail.Parameters.AddWithValue("@PackFactor", (detail.PackFactor ?? 1m).ToString("0.##"));
                                    cmdDetail.Parameters.AddWithValue("@StoreId", invoice.StoreId ?? "1");
                                    cmdDetail.Parameters.AddWithValue("@Quantity", detail.Quantity);
                                    cmdDetail.Parameters.AddWithValue("@Price", detail.Price);
                                    cmdDetail.Parameters.AddWithValue("@Total", detail.Total ?? 0m);
                                    // Soft-Swap: Map Notes to TRADREMARK
                                    cmdDetail.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(detail.Notes) ? (object)DBNull.Value : detail.Notes);
                                    cmdDetail.Parameters.AddWithValue("@DateId", dateId);
                                    cmdDetail.Parameters.AddWithValue("@TimeId", timeId);
                                    cmdDetail.Parameters.AddWithValue("@TraLstId", invoice.TraLstId);
                                    cmdDetail.Parameters.AddWithValue("@CurrencyId", invoice.CurrencyId ?? "1");
                                    cmdDetail.Parameters.AddWithValue("@CurrencyValue", invoice.CurrencyValue);
                                    cmdDetail.Parameters.AddWithValue("@BranchId", invoice.BranchId);
                                    cmdDetail.Parameters.AddWithValue("@DiscountAmount", 0m);
                                    cmdDetail.Parameters.AddWithValue("@DiscountPercent", discountPercentForDb);
                                    cmdDetail.Parameters.AddWithValue("@TaxAmount", 0m);
                                    cmdDetail.Parameters.AddWithValue("@TaxPercent", taxPercentForDb);
                                    await cmdDetail.ExecuteNonQueryAsync();
                                }
                            }

                            string debitQuery = $@"INSERT INTO {jeTable} (
                    TRADID, TRAID, TRADINDEX, TRADTYPE, TRADLSTID, TRADACCID, TRADACCNAME, TRADTACCID, 
                    TRADTACCNAME, TRADDATNAME, TRADCCID, TRADCCNAME, TRADCURID, TRADVALUE, TRADVALUELOC, 
                    TRADVALUETRA, TRADCURVALUE, TRADSIG, TRADSIGD, TRADSIGC, TRADSIGO, TRADSIGY, 
                    TRADSIGQ, TRADSIGS, TRADDETAIL, TRADREFID, TRADDATE, TRADDATEID, SYNCFLAG
                ) VALUES (
                    @TRADID, @TRAID, @TRADINDEX, '0', @TraLstId, @AccId, @AccName, @AccId,  
                    @AccName, @CustomerName, '0', '-', @CurrencyId, @DebitAmount, @DebitAmount, 
                    @DebitAmount, @CurrencyValue, '-1', '1', '0', '0', '0', 
                    '0', '0', @DetailText, @OrderNumber, @FormattedDate, @DateId, '1'
                )";

                            int currentKvIndex = 1;

                            decimal cashAmount = invoice.IsNetworkPayment ? (invoice.NetTotal - invoice.PaidAmount) : invoice.NetTotal;
                            if (cashAmount > 0)
                            {
                                string cashBoxId = string.IsNullOrEmpty(invoice.BoxNo) ? (invoice.BoxId ?? "") : invoice.BoxNo;
                                string cashBoxName = string.IsNullOrEmpty(invoice.BoxParentName) ? (invoice.BoxName ?? "") : invoice.BoxParentName;

                                if (invoice.IsNetworkPayment)
                                {
                                    string userBoxQuery = @"SELECT U.UACCID, A.ACCNAME 
                                                    FROM SUNUSERS U 
                                                    INNER JOIN SUNDATAACCDIR A ON U.UACCID = A.ACCID 
                                                    WHERE U.UID = @UserId";

                                    using (var cmdUser = new SqlCommand(userBoxQuery, connection, transaction))
                                    {
                                        cmdUser.Parameters.AddWithValue("@UserId", invoice.UserId);
                                        using (var rdr = await cmdUser.ExecuteReaderAsync())
                                        {
                                            if (await rdr.ReadAsync())
                                            {
                                                cashBoxId = rdr["UACCID"].ToString() ?? cashBoxId;
                                                cashBoxName = rdr["ACCNAME"].ToString() ?? cashBoxName;
                                            }
                                        }
                                    }
                                }

                                using (var cmdDebitCash = new SqlCommand(debitQuery, connection, transaction))
                                {
                                    cmdDebitCash.Parameters.AddWithValue("@TRADID", $"{traId}.{currentKvIndex}");
                                    cmdDebitCash.Parameters.AddWithValue("@TRAID", traId);
                                    cmdDebitCash.Parameters.AddWithValue("@TRADINDEX", currentKvIndex);
                                    cmdDebitCash.Parameters.AddWithValue("@AccId", cashBoxId);
                                    cmdDebitCash.Parameters.AddWithValue("@AccName", cashBoxName);
                                    cmdDebitCash.Parameters.AddWithValue("@CustomerName", invoice.CustomerName ?? "");
                                    cmdDebitCash.Parameters.AddWithValue("@CurrencyId", invoice.CurrencyId ?? "1");
                                    cmdDebitCash.Parameters.AddWithValue("@DebitAmount", cashAmount);
                                    cmdDebitCash.Parameters.AddWithValue("@CurrencyValue", invoice.CurrencyValue);
                                    cmdDebitCash.Parameters.AddWithValue("@DetailText", $"مقابل {invoice.TraTitle} رقم {nextTraNo}");
                                    cmdDebitCash.Parameters.AddWithValue("@OrderNumber", invoice.OrderNumber ?? "");
                                    cmdDebitCash.Parameters.AddWithValue("@FormattedDate", formattedDate);
                                    cmdDebitCash.Parameters.AddWithValue("@DateId", dateId);
                                    cmdDebitCash.Parameters.AddWithValue("@TraLstId", invoice.TraLstId);
                                    await cmdDebitCash.ExecuteNonQueryAsync();
                                }
                                currentKvIndex++;
                            }

                            if (invoice.IsNetworkPayment && invoice.PaidAmount > 0)
                            {
                                using (var cmdDebitNet = new SqlCommand(debitQuery, connection, transaction))
                                {
                                    cmdDebitNet.Parameters.AddWithValue("@TRADID", $"{traId}.{currentKvIndex}");
                                    cmdDebitNet.Parameters.AddWithValue("@TRAID", traId);
                                    cmdDebitNet.Parameters.AddWithValue("@TRADINDEX", currentKvIndex);
                                    cmdDebitNet.Parameters.AddWithValue("@AccId", string.IsNullOrEmpty(invoice.BoxNo) ? (invoice.BoxId ?? "") : invoice.BoxNo);
                                    cmdDebitNet.Parameters.AddWithValue("@AccName", string.IsNullOrEmpty(invoice.BoxParentName) ? (invoice.BoxName ?? "") : invoice.BoxParentName);
                                    cmdDebitNet.Parameters.AddWithValue("@CustomerName", invoice.CustomerName ?? "");
                                    cmdDebitNet.Parameters.AddWithValue("@CurrencyId", invoice.CurrencyId ?? "1");
                                    cmdDebitNet.Parameters.AddWithValue("@DebitAmount", invoice.PaidAmount);
                                    cmdDebitNet.Parameters.AddWithValue("@CurrencyValue", invoice.CurrencyValue);
                                    cmdDebitNet.Parameters.AddWithValue("@DetailText", $"مقابل {invoice.TraTitle} رقم {nextTraNo} (شبكة)");
                                    cmdDebitNet.Parameters.AddWithValue("@OrderNumber", invoice.OrderNumber ?? "");
                                    cmdDebitNet.Parameters.AddWithValue("@FormattedDate", formattedDate);
                                    cmdDebitNet.Parameters.AddWithValue("@DateId", dateId);
                                    cmdDebitNet.Parameters.AddWithValue("@TraLstId", invoice.TraLstId);
                                    await cmdDebitNet.ExecuteNonQueryAsync();
                                }
                                currentKvIndex++;
                            }

                            int jeIndex = currentKvIndex;
                            foreach (var detail in invoice.Details)
                            {
                                string creditQuery = $@"INSERT INTO {jeTable} (
                        TRADID, TRAID, TRADINDEX, TRADTYPE, TRADLSTID, TRADACCID, TRADACCNAME, TRADTACCID, 
                        TRADTACCNAME, TRADDATNAME, TRADCCID, TRADCCNAME, TRADCURID, TRADVALUE, TRADVALUELOC, 
                        TRADVALUETRA, TRADCURVALUE, TRADSIG, TRADSIGD, TRADSIGC, TRADSIGO, TRADSIGY, 
                        TRADSIGQ, TRADSIGS, TRADDETAIL, TRADREFID, TRADDATE, TRADDATEID, SYNCFLAG
                    ) VALUES (
                        @TRADID, @TRAID, @Index, '0', @TraLstId, @SalesAccountId, @SalesAccountName, @SalesAccountId, 
                        @SalesAccountName, @CustomerName, '0', '-', @CurrencyId, @ItemTotal, @ItemTotal, 
                        @ItemTotal, @CurrencyValue, '1', '0', '1', '0', '0', 
                        '0', '0', @DetailText, '', @FormattedDate, @DateId, '1'
                    )";

                                using (var cmdCredit = new SqlCommand(creditQuery, connection, transaction))
                                {
                                    string salesAccountId = "41110001";
                                    string salesAccountName = "ايراد مبيعات البضائع";

                                    cmdCredit.Parameters.AddWithValue("@TRADID", $"{traId}.{jeIndex}");
                                    cmdCredit.Parameters.AddWithValue("@TRAID", traId);
                                    cmdCredit.Parameters.AddWithValue("@Index", jeIndex++);
                                    cmdCredit.Parameters.AddWithValue("@SalesAccountId", salesAccountId);
                                    cmdCredit.Parameters.AddWithValue("@SalesAccountName", salesAccountName);
                                    cmdCredit.Parameters.AddWithValue("@CustomerName", invoice.CustomerName ?? "");
                                    cmdCredit.Parameters.AddWithValue("@CurrencyId", invoice.CurrencyId ?? "1");
                                    cmdCredit.Parameters.AddWithValue("@ItemTotal", detail.Total ?? 0m);
                                    cmdCredit.Parameters.AddWithValue("@CurrencyValue", invoice.CurrencyValue);
                                    cmdCredit.Parameters.AddWithValue("@DetailText", $"مقابل {invoice.TraTitle} رقم {nextTraNo}");
                                    cmdCredit.Parameters.AddWithValue("@FormattedDate", formattedDate);
                                    cmdCredit.Parameters.AddWithValue("@DateId", dateId);
                                    cmdCredit.Parameters.AddWithValue("@TraLstId", invoice.TraLstId);
                                    await cmdCredit.ExecuteNonQueryAsync();
                                }
                            }

                            await transaction.CommitAsync();
                            return Ok(new { message = "تم مزامنة الفاتورة بنجاح", traId = traId, traNo = nextTraNo });
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            _logger.LogError(ex, "فشل حفظ الفاتورة");
                            return StatusCode(500, new { message = "فشل حفظ الفاتورة: " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في الاتصال بالخادم");
                return StatusCode(500, new { message = "حدث خطأ في الاتصال بالخادم: " + ex.Message });
            }
        }
    }
}
