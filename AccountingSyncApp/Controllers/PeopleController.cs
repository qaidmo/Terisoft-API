using AccountingSyncAPP.Data;
using AccountingSyncAPP.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace AccountingSyncApp.Controllers
{
    [ApiController]
    [Route("api/People")]
    public class PeopleController : ControllerBase
    {
        private readonly AccountingDbContext _context;

        public PeopleController(AccountingDbContext context)
        {
            _context = context;
        }

        [HttpGet("active-user-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetActiveUserIds()
        {
            var ids = await _context.SunUsers
                .AsNoTracking()
                .Where(u => u.UState == "0")
                .Select(u => u.Uid)
                .ToListAsync();

            return Ok(ids);
        }

        [HttpGet("users")]
        public async Task<ActionResult> GetUsers([FromQuery] string? lastSyncDate)
        {
            var query = _context.SunUsers.AsNoTracking().AsQueryable();

            if (string.IsNullOrEmpty(lastSyncDate))
            {
                query = query.Where(u => u.UState == "0");
            }
            else
            {
                query = query.Where(u => u.EDTDATE != null && u.EDTDATE.CompareTo(lastSyncDate) > 0);
            }

            var users = await query.ToListAsync();

            var result = users.Select(u =>
            {
                long? receiptBox = null;
                long? paymentBox = null;

                if (!string.IsNullOrEmpty(u.Uprivdata))
                {
                    var dataParts = u.Uprivdata.Split('@');
                    if (dataParts.Length > 6)
                    {
                        if (long.TryParse(dataParts[5].Trim(), out long rVal)) receiptBox = rVal;
                        if (long.TryParse(dataParts[6].Trim(), out long pVal)) paymentBox = pVal;
                    }
                    else if (dataParts.Length > 5)
                    {
                        if (long.TryParse(dataParts[5].Trim(), out long rVal)) receiptBox = rVal;
                    }
                }

                string determinedUserType = "user";

                if (u.Uid == "0")
                {
                    determinedUserType = "admin";
                }
                else if (!string.IsNullOrEmpty(u.Uprivwindow) && u.Uprivwindow.Length > 0)
                {
                    if (u.Uprivwindow[0] == '1')
                    {
                        determinedUserType = "admin";
                    }
                }

                bool canChangeBranch = determinedUserType == "admin"
                    || (!string.IsNullOrEmpty(u.Uprivwindow) && u.Uprivwindow.Length > 16 && u.Uprivwindow[16] == '1');

                return new
                {
                    id = int.TryParse(u.Uid, out var uid) ? uid : 0,
                    user_no = int.TryParse(u.Uid, out var uNo) ? uNo : 0,
                    user_name = u.UName ?? "Unknown User",
                    user_type = determinedUserType,
                    user_group = $"{(u.Uid == "0" ? "AD" : "A")}{u.Uid}{u.UBranchId}{u.USectionId}",
                    user_branch = int.TryParse(u.UBranchId, out var bid) ? bid : 0,
                    user_department = int.TryParse(u.USectionId, out var sid) ? sid : 0,
                    no_store = int.TryParse(u.UStoreId, out var sId) ? sId : (int?)null,
                    box_no = u.UAccId,
                    receipt_box_no = receiptBox,
                    payment_box_no = paymentBox,
                    password_hash = u.UPassword ?? string.Empty,
                    source_id = u.Uid,
                    user_status = (u.UState == "0") ? "active" : "inactive",
                    can_change_branch = canChangeBranch,
                    last_updated = u.EDTDATE,
                    modified_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }).ToList();

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");

            return Ok(new
            {
                server_sync_time = currentServerTime,
                data = result
            });
        }

        [HttpGet("permissions")]
        public async Task<ActionResult> GetPermissions([FromQuery] string? lastSyncDate)
        {
            var screenMappings = new Dictionary<string, string>
    {
        { "601", "CashSaleScreen" },
        { "603", "CreditSaleScreen" },
        { "633", "AbsoluteCashSaleScreen" },
        { "602", "CashSaleReturnScreen" },
        { "604", "CreditSaleReturnScreen" },
        { "634", "AbsoluteCashSaleReturnScreen" },
        { "201", "ReceiptScreen" },
        { "202", "CashPaymentVoucherScreen" },
        { "903", "ReportsScreen" },
        { "90101", "AccountStatementScreen" },
        { "90301", "SalesMovementScreen" },
        { "90309", "ClientsMovementScreen" },
        { "90103", "CostCenterScreen" },

        // تقرير حركة المبيعات الجديد (التقارير العامة)
        { "90304", "SalesMovementNewScreen" },

        // تقارير الأصناف والمخزون
        { "90204", "ItemMovementScreen" },
        { "90221", "ProfitMarginScreen" },
        { "90202", "ItemBalancesScreen" },

        // تقرير حركة الموردين (التقارير العامة)
        { "90310", "SupplierMovementScreen" },

        { "801",   "RestaurantCashScreen" },
        { "803",   "RestaurantCreditScreen" }
    };

            var targetScreenIds = screenMappings.Keys.ToList();

            var query = _context.SunUsersListDetails
                .AsNoTracking()
                .Where(p => targetScreenIds.Contains(p.ListId));

            if (!string.IsNullOrEmpty(lastSyncDate))
            {
                query = query.Where(p =>
                    (p.EditDate != null && p.EditDate.CompareTo(lastSyncDate) > 0) ||
                    (p.InfoDate != null && p.InfoDate.CompareTo(lastSyncDate) > 0));
            }

            var permissionsList = await query.ToListAsync();

            var result = permissionsList.Select(p => new SupabasePermission
            {
                UserNo = int.TryParse(p.UserId, out var uid) ? uid : 0,
                ScreenName = screenMappings[p.ListId],
                CanAccess = p.CanEnter != null && p.CanEnter.ToLower() == "true",
                CanAdd = p.CanAdd != null && p.CanAdd.ToLower() == "true",
                CanEdit = p.CanEdit != null && p.CanEdit.ToLower() == "true",
                CanSearch = p.CanFind != null && p.CanFind.ToLower() == "true",
                CanPrint = p.CanPrint != null && p.CanPrint.ToLower() == "true",
                CanChangePrice = p.CanChangeSale != null && p.CanChangeSale.ToLower() == "true",
                CreatedAt = p.InfoDate,
                UpdatedAt = p.EditDate
            }).ToList();

            // ============================================================
            // التعديل هنا: التقاط توقيت السيرفر الحالي
            // ============================================================
            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");

            return Ok(new
            {
                server_sync_time = currentServerTime,
                data = result
            });
        }
        [HttpGet("active-customer-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetActiveCustomerIds()
        {
            var ids = await _context.SunCustomers
                .AsNoTracking()
                .Where(c => c.CustState == "0")
                .Select(c => c.CustId)
                .ToListAsync();

            return Ok(ids);
        }

        [HttpGet("customers")]
        public async Task<ActionResult> GetCustomers([FromQuery] string? lastSyncDate)
        {
            var query = _context.SunCustomers.AsNoTracking().AsQueryable();

            if (string.IsNullOrEmpty(lastSyncDate))
            {
                query = query.Where(c => c.CustState == "0");
            }
            else
            {
                query = query.Where(c => c.EdtDate != null && c.EdtDate.CompareTo(lastSyncDate) > 0);
            }

            var customers = await query.ToListAsync();

            var result = customers.Select(c => new
            {
                customer_no = int.TryParse(c.CustId, out var id) ? id : 0,
                customer_name = c.CustName,
                customer_foreign_name = c.CustNameE,
                phone_number = c.CustMobilNo ?? c.CustPhone,
                address = c.CustAddress,
                email = c.CustEmail,
                source_id = c.CustId,
                account_id = c.CustAccId,
                control_account_id = c.CustAccidFid,
                cost_data = c.CostData,
                cost_next = c.CostNext,
                cost_rsom = c.CostRsom,
                last_updated = c.EdtDate,
                created_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            // ============================================================
            // التعديل: إرسال توقيت السيرفر لتوحيد الزمن مع التطبيق
            // ============================================================
            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");

            return Ok(new
            {
                server_sync_time = currentServerTime,
                data = result
            });
        }

        [HttpPost("create")]
        public async Task<ActionResult> CreateCustomer([FromBody] CreateCustomerDto input)
        {
            if (input == null || string.IsNullOrEmpty(input.Name))
            {
                return BadRequest("بيانات العميل غير صحيحة أو الاسم فارغ.");
            }

            const string PARENT_ACCOUNT_ID = "12310001";
            const string ACCOUNT_LEVEL = "5";
            const string ACCOUNT_TYPE_ID = "15";
            const string ACCOUNT_TYPE_DETAIL = "1501";
            const string GROUP_ID_CONST = "15.1";
            const string CUST_OPTIONS_MASK = "0000000000000000000000000000@0@0@0@";
            const string DEFAULT_CURRENCY_ID = "1";

            var now = DateTime.Now;
            var currentDateTime = now.ToString("yyyyMMddHHmmss");
            var dateOnlyDisplay = now.ToString("yyyy/MM/dd");
            var dateOnlyId = now.ToString("yyyyMMdd");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var currencySymbolResult = await _context.SunCurrencies
                    .Where(c => c.CurId == DEFAULT_CURRENCY_ID)
                    .Select(c => c.CurChar)
                    .FirstOrDefaultAsync();

                string currencySymbol = currencySymbolResult ?? "";

                var siblings = await _context.SunDataAnalyticalAccDirs
                    .Where(a => a.AccFid == PARENT_ACCOUNT_ID)
                    .Select(a => new { a.AccId, a.AccIndex })
                    .ToListAsync();

                long newAccountNum;
                int newIndexNum;

                if (siblings.Any())
                {
                    var maxId = siblings.Max(x => long.TryParse(x.AccId, out var val) ? val : 0);
                    newAccountNum = maxId + 1;

                    var maxIndex = siblings.Max(x => int.TryParse(x.AccIndex, out var val) ? val : 0);
                    newIndexNum = maxIndex + 1;
                }
                else
                {
                    newAccountNum = long.Parse(PARENT_ACCOUNT_ID + "01");
                    newIndexNum = 1;
                }

                string newAccId = newAccountNum.ToString();
                string newIndexStr = newIndexNum.ToString();

                var newAccount = new SunDataAnalyticalAccDir
                {
                    AccId = newAccId,
                    AccLvid = ACCOUNT_LEVEL,
                    GrpId = GROUP_ID_CONST,
                    AccNo = PARENT_ACCOUNT_ID,
                    AccName = input.Name,
                    AccNameE = input.ForeignName ?? input.Name,
                    AccNameB = input.Name,
                    AccFid = PARENT_ACCOUNT_ID,
                    AccNetural = "0",
                    Accsig = "1",
                    AccRepId = "0",
                    AccIndex = newIndexStr,
                    AccGrpId = " ",
                    AccGrpName = " ",
                    AccChar = newIndexStr,
                    AccTraState = "1",
                    AccDefCurId = "1",
                    AccHide = "0",
                    AccState = "0",
                    AccMaxNo = "0",
                    AccEdtDate = currentDateTime,
                    Accmaid = "1",
                    Actyid = ACCOUNT_TYPE_ID,
                    ActyDeId = ACCOUNT_TYPE_DETAIL,
                    AccSyncFlag = "1"
                };

                _context.SunDataAnalyticalAccDirs.Add(newAccount);

                string newAccCurId = $"{newAccId}.{DEFAULT_CURRENCY_ID}";
                string accountNameWithSymbol = $"{input.Name} {currencySymbol}".Trim();

                var accCurrencyLink = new SunAccDirCury
                {
                    AccCurId = newAccCurId,
                    AccId = newAccId,
                    CurId = DEFAULT_CURRENCY_ID,
                    AccCName = accountNameWithSymbol,
                    AccCurIndex = "1",
                    AccCurState = "0",
                    EdtDate = currentDateTime,
                    SyncFlag = "1"
                };
                _context.SunAccDirCuries.Add(accCurrencyLink);

                var allCustIds = await _context.SunCustomers.Select(c => c.CustId).ToListAsync();
                long newCustNum = 1;
                if (allCustIds.Any())
                {
                    var maxCustId = allCustIds.Max(id => long.TryParse(id, out var val) ? val : 0);
                    newCustNum = maxCustId + 1;
                }
                string newCustId = newCustNum.ToString();

                var newCustomer = new SunCustomer
                {
                    CustId = newCustId,
                    CustName = input.Name,
                    CustNameE = input.ForeignName,
                    CustPhone = input.Phone ?? "",
                    CustAddress = input.Address ?? "",
                    CustEmail = input.Email ?? "",
                    CustAccId = newAccId,
                    CustAccidFid = PARENT_ACCOUNT_ID,
                    CustAGrpAccId = PARENT_ACCOUNT_ID,
                    CustAGrpId = GROUP_ID_CONST,
                    CustDateOpen = dateOnlyDisplay,
                    CustDateOpenId = dateOnlyId,
                    CustOptions = CUST_OPTIONS_MASK,
                    CustState = "0",
                    CustClass = " ",
                    CustAGrpName = "عملاء",
                    SyncFlag = "1",
                    CustCuryId = "1",
                    CustBraId = "1",
                    CustSecId = "1",
                    CustLimit = "0",
                    CustPerDis = "0",
                    EdtDate = currentDateTime,
                    CustUserName = input.AppUserName ?? "AppUser",
                    CostData = input.CostData,
                    CostNext = input.CostNext,
                    CostRsom = input.CostRsom
                };

                _context.SunCustomers.Add(newCustomer);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    customer_no = newCustId,
                    account_id = newAccId,
                    message = "تم إنشاء العميل والحساب بنجاح"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception";
                Console.WriteLine($"Error creating customer: {ex.Message}");
                Console.WriteLine($"Inner Details: {innerMessage}");
                return StatusCode(500, $"فشل إنشاء العميل: {ex.Message} \n {innerMessage}");
            }
        }

        public class CreateCustomerDto
        {
            public string Name { get; set; }
            public string ForeignName { get; set; }
            public string Phone { get; set; }
            public string Address { get; set; }
            public string Email { get; set; }
            public string AppUserName { get; set; }
            public string? CostData { get; set; }
            public string? CostNext { get; set; }
            public string? CostRsom { get; set; }
        }

        [HttpPost("update")]
        public async Task<ActionResult> UpdateCustomer([FromBody] UpdateCustomerDto input)
        {
            if (input == null || string.IsNullOrEmpty(input.CustomerNo))
            {
                return BadRequest("رقم العميل مطلوب للتحديث.");
            }

            var customer = await _context.SunCustomers.FirstOrDefaultAsync(c => c.CustId == input.CustomerNo);
            if (customer == null)
            {
                return NotFound("العميل غير موجود.");
            }

            if (!string.IsNullOrEmpty(input.Name)) customer.CustName = input.Name;
            if (input.ForeignName != null) customer.CustNameE = input.ForeignName;
            if (input.Phone != null) customer.CustPhone = input.Phone;
            if (input.Address != null) customer.CustAddress = input.Address;
            if (input.Email != null) customer.CustEmail = input.Email;

            // Optional utility metadata fields
            if (input.CostData != null) customer.CostData = input.CostData;
            if (input.CostNext != null) customer.CostNext = input.CostNext;
            if (input.CostRsom != null) customer.CostRsom = input.CostRsom;

            customer.EdtDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            customer.SyncFlag = "1";

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new
                {
                    success = true,
                    message = "تم تحديث بيانات العميل بنجاح"
                });
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "No Inner Exception";
                Console.WriteLine($"Error updating customer: {ex.Message} \n {innerMessage}");
                return StatusCode(500, $"فشل تحديث العميل: {ex.Message}");
            }
        }

        public class UpdateCustomerDto
        {
            public string CustomerNo { get; set; } = string.Empty;
            public string? Name { get; set; }
            public string? ForeignName { get; set; }
            public string? Phone { get; set; }
            public string? Address { get; set; }
            public string? Email { get; set; }
            public string? CostData { get; set; }
            public string? CostNext { get; set; }
            public string? CostRsom { get; set; }
        }


        [HttpGet("active-salesperson-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetActiveSalespersonIds()
        {
            var ids = await _context.Dealers
                .AsNoTracking()
                .Where(d => d.DelState == "0")
                .Select(d => d.DelId)
                .ToListAsync();

            return Ok(ids);
        }

        [HttpGet("salespersons")]
        public async Task<ActionResult> GetSalespersons([FromQuery] string? lastSyncDate)
        {
            var query = _context.Dealers.AsNoTracking().AsQueryable();

            if (string.IsNullOrEmpty(lastSyncDate))
            {
                query = query.Where(d => d.DelState == "0");
            }
            else
            {
                query = query.Where(d => d.EdtDate != null && d.EdtDate.CompareTo(lastSyncDate) > 0);
            }

            var salespeople = await query.ToListAsync();

            var result = salespeople.Select(d => new
            {
                salesperson_no = int.TryParse(d.DelId, out var id) ? id : 0,
                salesperson_name = d.DelName,
                phone_number = d.DelMobilNo,
                percentage = decimal.TryParse(d.DelMaxNo, out var p) ? p : 0,
                commissions_disabled = d.DelPerState == "1" ? 1 : 0,
                source_id = d.DelId,
                account_id = d.DelAccId,
                expense_account_id = d.DelOther2,
                last_updated = d.EdtDate,
                created_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return Ok(new { server_sync_time = currentServerTime, data = result });
        }

        [HttpGet("active-collector-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetActiveCollectorIds()
        {
            var ids = await _context.Collectors
                .AsNoTracking()
                .Where(c => c.ColState == "0")
                .Select(c => c.ColId)
                .ToListAsync();

            return Ok(ids);
        }

        [HttpGet("collectors")]
        public async Task<ActionResult> GetCollectors([FromQuery] string? lastSyncDate)
        {
            var query = _context.Collectors.AsNoTracking().AsQueryable();

            if (string.IsNullOrEmpty(lastSyncDate))
            {
                query = query.Where(c => c.ColState == "0");
            }
            else
            {
                query = query.Where(c => c.EdtDate != null && c.EdtDate.CompareTo(lastSyncDate) > 0);
            }

            var collectors = await query.ToListAsync();

            var result = collectors.Select(c => new
            {
                collector_no = int.TryParse(c.ColId, out var id) ? id : 0,
                collector_name = c.ColName,
                phone_number = c.ColMobilNo,
                source_id = c.ColId,
                account_id = c.ColAccId,
                last_updated = c.EdtDate,
                created_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return Ok(new { server_sync_time = currentServerTime, data = result });
        }

        [HttpGet("customer-currencies")]
        public async Task<ActionResult> GetCustomerCurrencies()
        {
            var queryLinks = from customer in _context.SunCustomers.AsNoTracking()
                             join link in _context.SunAccDirCuries.AsNoTracking()
                             on customer.CustAccId equals link.AccId
                             where customer.CustState == "0"
                             select new
                             {
                                 CustomerId = customer.CustId,
                                 CurrencyId = link.CurId
                             };

            var linksData = await queryLinks.ToListAsync();

            var result = new List<object>();
            var uniqueCheck = new HashSet<string>();

            foreach (var item in linksData)
            {
                if (int.TryParse(item.CustomerId, out var custId) && int.TryParse(item.CurrencyId, out var curId))
                {
                    string key = $"{custId}-{curId}";
                    if (uniqueCheck.Add(key))
                    {
                        result.Add(new
                        {
                            customer_no = custId,
                            currency_no = curId
                        });
                    }
                }
            }

            var defaultCurrencies = await _context.SunCustomers
                .AsNoTracking()
                .Where(c => c.CustState == "0" && c.CustCuryId != null)
                .Select(c => new { c.CustId, c.CustCuryId })
                .ToListAsync();

            foreach (var item in defaultCurrencies)
            {
                if (int.TryParse(item.CustId, out var custId) && int.TryParse(item.CustCuryId, out var curId))
                {
                    string key = $"{custId}-{curId}";
                    if (uniqueCheck.Add(key))
                    {
                        result.Add(new
                        {
                            customer_no = custId,
                            currency_no = curId
                        });
                    }
                }
            }

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return Ok(new { server_sync_time = currentServerTime, data = result });
        }

        [HttpGet("active-supplier-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetActiveSupplierIds()
        {
            var ids = await _context.SunVenders
                .AsNoTracking()
                .Where(v => v.VndrState == "0")
                .Select(v => v.VndrId)
                .ToListAsync();

            return Ok(ids);
        }

        [HttpGet("suppliers")]
        public async Task<ActionResult> GetSuppliers([FromQuery] string? lastSyncDate)
        {
            var query = _context.SunVenders.AsNoTracking().AsQueryable();

            if (string.IsNullOrEmpty(lastSyncDate))
            {
                query = query.Where(v => v.VndrState == "0");
            }
            else
            {
                query = query.Where(v => v.EdtDate != null && v.EdtDate.CompareTo(lastSyncDate) > 0);
            }

            var suppliers = await query.ToListAsync();

            var result = suppliers.Select(v => new
            {
                supplier_no = int.TryParse(v.VndrId, out var id) ? id : 0,
                supplier_name = v.VndrName,
                supplier_foreign_name = v.VndrNameE,
                phone_number = v.VndrMobilNo ?? v.VndrPhone,
                address = v.VndrAddress,
                email = v.VndrEmail,
                account_id = v.VndrAccId,
                control_account_id = v.VndrAccIdFid,
                source_id = v.VndrId,
                last_updated = v.EdtDate,
                created_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");

            return Ok(new
            {
                server_sync_time = currentServerTime,
                data = result
            });
        }
    }
}