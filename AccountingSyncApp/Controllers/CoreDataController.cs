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
    [Route("api/[controller]")]
    public class CoreDataController : ControllerBase
    {
        private readonly AccountingDbContext _context;

        public CoreDataController(AccountingDbContext context)
        {
            _context = context;
        }

        // =================================================================
        // 0. اختبار الاتصال (Check Connection) 
        // =================================================================
        [HttpGet("check-connection")]
        public IActionResult CheckConnection()
        {
            return Ok(new { status = "Connected", message = "API is running on ZeroTier" });
        }

        [HttpGet("pumps")]
        public async Task<ActionResult> GetPumps()
        {
            try
            {
                var pumps = await _context.Almahtas.AsNoTracking().ToListAsync();
                return Ok(new { data = pumps, serverTime = DateTime.Now.ToString("yyyyMMddHHmmss") });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // =================================================================
        // 1. الفروع (Branches) كما هي لديك
        // =================================================================
        [HttpGet("branches")]
        public async Task<ActionResult> GetBranches([FromQuery] string? lastSyncDate)
        {
            var branches = await _context.SunBranches
                .AsNoTracking()
                .Where(b => b.BraState == "0")
                .ToListAsync();

            var result = branches.Select(b => new SupabaseBranch
            {
                BranchId = int.TryParse(b.BraId, out var id) ? id : 0,
                BranchName = b.BraName,
                SourceId = b.BraId
            }).ToList();

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return Ok(new { server_sync_time = currentServerTime, data = result });
        }

        [HttpGet("active-branch-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetActiveBranchIds()
        {
            var ids = await _context.SunBranches.AsNoTracking().Where(b => b.BraState == "0").Select(b => b.BraId).ToListAsync();
            return Ok(ids);
        }

        // =================================================================
        // 2. الأقسام (Departments)
        // =================================================================
        [HttpGet("departments")]
        public async Task<ActionResult> GetDepartments([FromQuery] string? lastSyncDate)
        {
            var departments = await _context.SunSections
                .AsNoTracking()
                .Where(d => d.SecState == "0")
                .ToListAsync();

            var result = departments.Select(d => new SupabaseDepartment
            {
                DepartmentId = int.TryParse(d.SecId, out var id) ? id : 0,
                DepartmentName = d.SecName,
                SourceId = d.SecId
            }).ToList();

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return Ok(new { server_sync_time = currentServerTime, data = result });
        }

        [HttpGet("active-department-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetActiveDepartmentIds()
        {
            var ids = await _context.SunSections.AsNoTracking().Where(d => d.SecState == "0").Select(d => d.SecId).ToListAsync();
            return Ok(ids);
        }

        // =================================================================
        // 3. السنوات المالية (Financial Years)
        // =================================================================
        [HttpGet("financial-years")]
        public async Task<ActionResult> GetFinancialYears([FromQuery] string? lastSyncDate)
        {
            var years = await _context.Tssysyears
                .AsNoTracking()
                .Where(y => y.Yrstatus == "0")
                .ToListAsync();

            var result = new List<SupabaseFinancialYear>();
            foreach (var y in years)
            {
                // التحقق الصارم من صحة التواريخ لتجنب أخطاء النظام القديم
                if (long.TryParse(y.Yrid, out var id) &&
                    System.DateTime.TryParseExact(y.Yrmindate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var start) &&
                    System.DateTime.TryParseExact(y.Yrmaxdate, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var end))
                {
                    result.Add(new SupabaseFinancialYear
                    {
                        Id = id,
                        YearName = y.Yrvalue,
                        StartDate = start,
                        EndDate = end,
                        SourceId = y.Yrid
                    });
                }
            }

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return Ok(new { server_sync_time = currentServerTime, data = result });
        }

        [HttpGet("active-financial-year-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetActiveFinancialYearIds()
        {
            var ids = await _context.Tssysyears.AsNoTracking().Where(y => y.Yrstatus == "0").Select(y => y.Yrid).ToListAsync();
            return Ok(ids);
        }

        // =================================================================
        // 4. العملات (Currencies)
        // =================================================================
        [HttpGet("currencies")]
        public async Task<ActionResult> GetCurrencies([FromQuery] string? lastSyncDate)
        {
            var currencies = await _context.SunCurrencies
                .AsNoTracking()
                .Where(c => c.CurStatus == "0")
                .ToListAsync();

            var result = currencies.Select(c => new SupabaseCurrency
            {
                CurrencyNo = int.TryParse(c.CurId, out var id) ? id : 0,
                CurrencyName = c.CurName,
                CurrencyFkah = c.CurPartName,
                CurrencySymbol = c.CurChar,
                CurrencyRate = decimal.TryParse(c.CurValue, out var rate) ? rate : 0,
                CurrencyHighest = decimal.TryParse(c.CurMaxValue, out var max) ? max : 0,
                CurrencyLowest = decimal.TryParse(c.CurMinValue, out var min) ? min : 0,
                SourceId = c.CurId
            }).ToList();

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return Ok(new { server_sync_time = currentServerTime, data = result });
        }

        [HttpGet("active-currency-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetActiveCurrencyIds()
        {
            var ids = await _context.SunCurrencies.AsNoTracking().Where(c => c.CurStatus == "0").Select(c => c.CurId).ToListAsync();
            return Ok(ids);
        }

        // =================================================================
        // 5. الصناديق (Boxes)
        // =================================================================
        [HttpGet("boxes")]
        public async Task<ActionResult> GetBoxes([FromQuery] string? lastSyncDate)
        {
            var boxes = new List<SupabaseBox>();
            var addedIds = new HashSet<string>();

            var analyticalConfig = new List<string> { "12110001", "12120001", "12130001", "12210001", "12220001" };

            var analytical = await _context.SunDataAnalyticalAccDirs
                .AsNoTracking()
                .Where(a => analyticalConfig.Contains(a.AccFid) && a.AccState == "0")
                .ToListAsync();

            foreach (var a in analytical)
            {
                boxes.Add(new SupabaseBox
                {
                    BoxNo = int.TryParse(a.AccId, out var id) ? id : 0,
                    BoxName = a.AccName,
                    SourceId = a.AccId,
                    AccountId = a.AccId,
                    ControlAccountId = a.AccFid
                });

                if (!string.IsNullOrEmpty(a.AccId))
                {
                    addedIds.Add(a.AccId);
                }
            }

            var general = await _context.SunDataAccDirs
                 .AsNoTracking()
                 .Where(a => a.ActyId == "13" && a.AccLvid == "5")
                 .ToListAsync();

            foreach (var g in general)
            {
                if (!string.IsNullOrEmpty(g.AccId) && !addedIds.Contains(g.AccId))
                {
                    boxes.Add(new SupabaseBox
                    {
                        BoxNo = int.TryParse(g.AccId, out var id) ? id : 0,
                        BoxName = g.AccName,
                        SourceId = g.AccId,
                        AccountId = g.AccId,
                        ControlAccountId = !string.IsNullOrEmpty(g.AccFid) ? g.AccFid : null
                    });

                    addedIds.Add(g.AccId);
                }
            }

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return Ok(new { server_sync_time = currentServerTime, data = boxes });
        }

        [HttpGet("active-box-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetActiveBoxIds()
        {
            var analyticalConfig = new List<string> { "12110001", "12120001", "12130001", "12210001", "12220001" };
            var a_ids = await _context.SunDataAnalyticalAccDirs.AsNoTracking().Where(a => analyticalConfig.Contains(a.AccFid) && a.AccState == "0").Select(a => a.AccId).ToListAsync();
            var g_ids = await _context.SunDataAccDirs.AsNoTracking().Where(a => a.ActyId == "13" && a.AccLvid == "5").Select(a => a.AccId).ToListAsync();
            
            var allIds = new HashSet<string>(a_ids);
            allIds.UnionWith(g_ids);
            return Ok(allIds.ToList());
        }

        // =================================================================
        // 6. عملات الصناديق (Box Currencies)
        // =================================================================
        [HttpGet("box-currencies")]
        public async Task<ActionResult> GetBoxCurrencies([FromQuery] string? lastSyncDate)
        {
            var result = new List<SupabaseBoxCurrency>();
            var uniqueCheck = new HashSet<string>();

            var directLinks = await _context.SunAccDirCuries
                .AsNoTracking()
                .Select(x => new { x.AccId, x.CurId })
                .ToListAsync();

            foreach (var link in directLinks)
            {
                if (int.TryParse(link.AccId, out var boxNo) && int.TryParse(link.CurId, out var curId))
                {
                    string key = $"{boxNo}-{curId}";
                    if (uniqueCheck.Add(key))
                    {
                        result.Add(new SupabaseBoxCurrency { BoxNo = boxNo, CurrencyId = curId });
                    }
                }
            }

            var defaultCurrencies = await _context.SunDataAccDirs
                .AsNoTracking()
                .Where(a => a.ActyId == "13" && a.AccLvid == "5" && a.AccDefCurId != null)
                .Select(a => new { a.AccId, a.AccDefCurId })
                .ToListAsync();

            foreach (var item in defaultCurrencies)
            {
                if (int.TryParse(item.AccId, out var boxNo) && int.TryParse(item.AccDefCurId, out var curId))
                {
                    string key = $"{boxNo}-{curId}";
                    if (uniqueCheck.Add(key))
                    {
                        result.Add(new SupabaseBoxCurrency { BoxNo = boxNo, CurrencyId = curId });
                    }
                }
            }

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return Ok(new { server_sync_time = currentServerTime, data = result });
        }

        // =================================================================
        // 7. المخازن (Stores)
        // =================================================================
        [HttpGet("stores")]
        public async Task<ActionResult> GetStores([FromQuery] string? lastSyncDate)
        {
            var stores = await _context.SunStores
                .AsNoTracking()
                .Where(s => s.StrState == "0")
                .ToListAsync();

            var result = stores.Select(s => new SupabaseStore
            {
                NoStore = int.TryParse(s.StrId, out var id) ? id : 0,
                NameStore = s.StrName,
                SourceId = s.StrId,
                BranchId = int.TryParse(s.StrFid, out var bid) ? bid : 0,
                // 👇 التغيير: نمرر رقم الحساب مباشرة كنص
                InventoryAccountId = s.StrAccId
            }).ToList();

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return Ok(new { server_sync_time = currentServerTime, data = result });
        }

        [HttpGet("active-store-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetActiveStoreIds()
        {
            var ids = await _context.SunStores.AsNoTracking().Where(s => s.StrState == "0").Select(s => s.StrId).ToListAsync();
            return Ok(ids);
        }

        // =================================================================
        // 8. تفاصيل القسم والشعار (للمزامنة مع قوالب الفواتير)
        // =================================================================
        [HttpGet("department-details/{departmentId}")]
        public async Task<ActionResult<BranchInfoResponse>> GetDepartmentDetails(string departmentId)
        {
            try
            {
                var response = new BranchInfoResponse();

                // 1. جلب بيانات القسم (الاسم العربي والعنوان)
                var department = await _context.SunSections
                    .Where(d => d.SecId == departmentId)
                    .Select(d => new
                    {
                        d.SecHeaderR,
                        d.SecAddress
                    })
                    .FirstOrDefaultAsync();

                if (department != null)
                {
                    response.CompanyName = department.SecHeaderR ?? "";
                    response.Address = department.SecAddress ?? "";
                }
                else
                {
                    return NotFound(new { message = "القسم غير موجود" });
                }

                // 2. جلب الشعار (صورة رقم 021021)
                // نستخدم Select لجلب عمود الصورة فقط لتخفيف الحمل على الشبكة والذاكرة
                var logoImage = await _context.SunImages
                    .Where(img => img.ImgId == "021021")
                    .Select(img => img.ImgData)
                    .FirstOrDefaultAsync();

                if (logoImage != null)
                {
                    // تحويل البايتات إلى Base64 String
                    response.LogoBase64 = Convert.ToBase64String(logoImage);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "حدث خطأ أثناء جلب بيانات الشركة: " + ex.Message });
            }
        }
        public class BranchInfoResponse
        {
            public string CompanyName { get; set; } = "";
            public string Address { get; set; } = "";
            public string LogoBase64 { get; set; } = "";
        }
    }
}