using AccountingSyncAPP.Data;
using AccountingSyncAPP.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountingSyncApp.Controllers
{
    [ApiController]
    [Route("api/CoreData")]
    public class AccountController : ControllerBase
    {
        private readonly AccountingDbContext _context;

        public AccountController(AccountingDbContext context)
        {
            _context = context;
        }

        [HttpGet("account-types")]
        [HttpGet("~/api/Account/account-types")]
        public async Task<ActionResult> GetAccountTypes([FromQuery] string? lastSyncDate)
        {
            var types = await _context.SunAcMains
                .AsNoTracking()
                .Select(t => new
                {
                    id = int.Parse(t.Acmaid),
                    name = t.Acmaname,
                    name_en = t.Acmanamee,
                    source_id = t.Acmaid
                })
                .ToListAsync();

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return Ok(new
            {
                server_sync_time = currentServerTime,
                data = types
            });
        }

        // تم تعديل المسار من "major-types" إلى "account-major-types" لحل الـ 404
        [HttpGet("account-major-types")]
        [HttpGet("~/api/Account/account-major-types")]
        public async Task<ActionResult> GetMajorTypes([FromQuery] string? lastSyncDate)
        {
            var majorTypes = await _context.SunAcTypes
                .AsNoTracking()
                .Select(t => new
                {
                    id = t.Actyid != null ? int.Parse(t.Actyid) : 0,
                    name = t.Actyname,
                    name_en = t.Actynamee,
                    source_id = t.Actyid,
                    main_type_id = int.Parse(t.Acmaid)
                })
                .ToListAsync();

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return Ok(new
            {
                server_sync_time = currentServerTime,
                data = majorTypes
            });
        }

        // تم تعديل المسار من "classifications" إلى "account-classifications" لحل الـ 404
        [HttpGet("account-classifications")]
        [HttpGet("~/api/Account/account-classifications")]
        public async Task<ActionResult> GetClassifications([FromQuery] string? lastSyncDate)
        {
            var classifications = await _context.SunAcTypeDetails
                .AsNoTracking()
                .Select(c => new
                {
                    id = c.Actydeid != null ? int.Parse(c.Actydeid) : 0,
                    name = c.Actydename,
                    name_en = c.Actydenamee,
                    source_id = c.Actydeid,
                    major_type_id = c.Actyid != null ? int.Parse(c.Actyid) : 0
                })
                .ToListAsync();

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return Ok(new
            {
                server_sync_time = currentServerTime,
                data = classifications
            });
        }

        [HttpGet("accounts")]
        public async Task<ActionResult> GetAccounts([FromQuery] string? lastSyncDate)
        {
            Console.WriteLine("--- Starting Master Accounts Sync ---");
            try
            {
                // ---------------------------------------------------------
                // 1. تجهيز قوائم المعرفات المطلوبة
                // ---------------------------------------------------------

                // أ) ✅ التعديل هنا: جلب "أبناء" الصناديق بناءً على قائمة "الآباء"
                // تماماً كما تفعل في دالة GetBoxes
                var boxParentIds = new List<string> { "12110001", "12120001", "12130001", "12210001", "12220001" };

                // نجلب أرقام الحسابات (AccId) التي يقع آباؤها (AccFid) ضمن القائمة أعلاه
                var boxAnalyticalIds = await _context.SunDataAnalyticalAccDirs.AsNoTracking()
                    .Where(a => boxParentIds.Contains(a.AccFid) && a.AccState == "0")
                    .Select(a => a.AccId) // نختار الـ ID الخاص بالابن (مثل 130100001)
                    .ToListAsync();

                // ب) أرقام حسابات العملاء
                var activeCustomers = await _context.SunCustomers.AsNoTracking()
                    .Where(c => c.CustState == "0" && c.CustAccId != null)
                    .Select(c => new { c.CustAccId, c.CustAccidFid, c.CustName, c.CustNameE })
                    .ToListAsync();

                var customerAccIds = activeCustomers.Select(c => c.CustAccId).ToHashSet();

                // ج) أرقام حسابات المناديب
                var dealerAccIds = await _context.Dealers.AsNoTracking()
                    .Where(d => d.DelState == "0")
                    .Select(d => new { d.DelAccId, d.DelOther2 })
                    .ToListAsync();

                var dealerAccIdsList = dealerAccIds
                    .SelectMany(d => new[] { d.DelAccId, d.DelOther2 })
                    .Where(id => !string.IsNullOrEmpty(id))
                    .ToList();

                // د) أرقام حسابات المحصلين
                var collectorAccIds = await _context.Collectors.AsNoTracking()
                    .Where(c => c.ColState == "0" && c.ColAccId != null)
                    .Select(c => c.ColAccId)
                    .ToListAsync();

                // هـ) أرقام حسابات الموردين
                var supplierAccIds = await _context.SunVenders.AsNoTracking()
                    .Where(v => v.VndrState == "0" && v.VndrAccId != null)
                    .Select(v => v.VndrAccId)
                    .ToListAsync();

                // تجميع الكل (الآن boxAnalyticalIds تحتوي على الأرقام الصحيحة مثل 130100001)
                var allAnalyticalIds = new HashSet<string>(boxAnalyticalIds);
                allAnalyticalIds.UnionWith(customerAccIds);
                allAnalyticalIds.UnionWith(dealerAccIdsList);
                allAnalyticalIds.UnionWith(collectorAccIds);
                allAnalyticalIds.UnionWith(supplierAccIds);

                // ---------------------------------------------------------
                // 2. جلب البيانات من قاعدة البيانات (SQL)
                // ---------------------------------------------------------

                // أ) الحسابات العامة
                var generalAccounts = await _context.SunDataAccDirs.AsNoTracking().ToListAsync();

                // ب) الحسابات التحليلية
                // الآن allAnalyticalIds تحتوي على أبناء الصناديق، لذا هذا الاستعلام سيجلبهم بنجاح
                var analyticalAccounts = new List<SunDataAnalyticalAccDir>();
                if (allAnalyticalIds.Any())
                {
                    analyticalAccounts = await _context.SunDataAnalyticalAccDirs.AsNoTracking()
                       .Where(a => allAnalyticalIds.Contains(a.AccId))
                       .ToListAsync();
                }

                // ---------------------------------------------------------
                // 3. التجميع والمعالجة (نفس الكود السابق تماماً)
                // ---------------------------------------------------------

                var analyticalQuery = allAnalyticalIds.Any()
                    ? _context.SunDataAnalyticalAccDirs.AsNoTracking().Where(a => allAnalyticalIds.Contains(a.AccId))
                    : _context.SunDataAnalyticalAccDirs.AsNoTracking().Where(a => false);

                if (!string.IsNullOrEmpty(lastSyncDate))
                {
                    analyticalQuery = analyticalQuery.Where(a => a.AccEdtDate != null && a.AccEdtDate.CompareTo(lastSyncDate) > 0);
                }

                analyticalAccounts = await analyticalQuery.ToListAsync();

                var finalResult = new Dictionary<string, object>();

                void AddToResult(string? id, object accountObj)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        var trimId = id.Trim();
                        if (!finalResult.ContainsKey(trimId))
                        {
                            finalResult.Add(trimId, accountObj);
                        }
                    }
                }

                // --- المرحلة 1: الحسابات العامة ---
                foreach (var acc in generalAccounts)
                {
                    if (string.IsNullOrEmpty(acc.AccId)) continue;

                    int? typeId = (int.TryParse(acc.AccMaid, out int t) && t != 0) ? t : null;
                    int? classId = (int.TryParse(acc.ActyDeId, out int c) && c != 0) ? c : null;
                    string? parentId = (string.IsNullOrEmpty(acc.AccFid) || acc.AccFid == "0") ? null : acc.AccFid.Trim();

                    AddToResult(acc.AccId, new
                    {
                        id = acc.AccId.Trim(),
                        account_number = acc.AccId.Trim(),
                        name = acc.AccName,
                        name_en = acc.AccNameE,
                        parent_id = parentId,
                        is_transactional = acc.AccTraState == "1" ? 1 : 0,
                        is_active = acc.AccState == "0" ? 1 : 0,
                        nature = acc.AccNetural == "0" ? "مدين" : "دائن",
                        account_type_id = typeId,
                        classification_id = classId,
                        source_id = acc.AccId.Trim()
                    });
                }

                // --- المرحلة 2: الحسابات التحليلية ---
                foreach (var acc in analyticalAccounts)
                {
                    if (string.IsNullOrEmpty(acc.AccId)) continue;

                    int? typeId = (int.TryParse(acc.Accmaid, out int t) && t != 0) ? t : null;
                    int? classId = (int.TryParse(acc.Actyid, out int c) && c != 0) ? c : null;

                    if (collectorAccIds.Contains(acc.AccId))
                    {
                        if (typeId == null) typeId = 2;
                        if (classId == null) classId = 27;
                    }

                    string? parentId = (string.IsNullOrEmpty(acc.AccFid) || acc.AccFid == "0") ? null : acc.AccFid.Trim();

                    AddToResult(acc.AccId, new
                    {
                        id = acc.AccId.Trim(),
                        account_number = acc.AccId.Trim(),
                        name = acc.AccName,
                        name_en = acc.AccNameE,
                        parent_id = parentId,
                        is_transactional = 1,
                        is_active = acc.AccState == "0" ? 1 : 0,
                        nature = acc.Accsig == "-1" ? "دائن" : "مدين",
                        account_type_id = typeId,
                        classification_id = classId,
                        source_id = acc.AccId.Trim()
                    });
                }

                // --- المرحلة 3: حسابات العملاء الافتراضية ---
                foreach (var cust in activeCustomers)
                {
                    if (string.IsNullOrEmpty(cust.CustAccId)) continue;
                    var accId = cust.CustAccId.Trim();

                    if (!finalResult.ContainsKey(accId))
                    {
                        string? parentId = null;
                        if (!string.IsNullOrEmpty(cust.CustAccidFid) && cust.CustAccidFid != "0")
                        {
                            parentId = cust.CustAccidFid.Trim();
                        }

                        AddToResult(accId, new
                        {
                            id = accId,
                            account_number = accId,
                            name = cust.CustName,
                            name_en = cust.CustNameE,
                            parent_id = parentId,
                            is_transactional = 1,
                            is_active = 1,
                            nature = "مدين",
                            account_type_id = 1,
                            classification_id = 31,
                            source_id = accId
                        });
                    }
                }

                string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                return Ok(new
                {
                    server_sync_time = currentServerTime,
                    data = finalResult.Values.OrderBy(x => ((dynamic)x).id).ToList()
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAccounts: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("account-currencies")]
        public async Task<ActionResult> GetAccountCurrencies([FromQuery] string? lastSyncDate)
        {
            var query = from link in _context.SunAccDirCuries.AsNoTracking()
                        join account in _context.SunDataAccDirs.AsNoTracking()
                        on link.AccId equals account.AccId
                        join currency in _context.SunCurrencies.AsNoTracking()
                        on link.CurId equals currency.CurId
                        select new
                        {
                            link.AccId,
                            link.CurId
                        };

            var data = await query.ToListAsync();

            var result = data.Select(x => new
            {
                account_id = x.AccId.Trim(),
                currency_id = int.TryParse(x.CurId, out int id) ? id : 0
            }).ToList();

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return Ok(new
            {
                server_sync_time = currentServerTime,
                data = result
            });
        }

        [HttpGet("active-account-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetActiveAccountIds()
        {
            var generalIds = await _context.SunDataAccDirs.AsNoTracking().Where(a => a.AccState == "0").Select(a => a.AccId).ToListAsync();
            var analyticalIds = await _context.SunDataAnalyticalAccDirs.AsNoTracking().Where(a => a.AccState == "0").Select(a => a.AccId).ToListAsync();
            var ids = new HashSet<string>(generalIds);
            ids.UnionWith(analyticalIds);
            return Ok(ids.ToList());
        }
    }
}