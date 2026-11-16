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
    [Route("api/Inventory")]
    public class InventoryController : ControllerBase
    {
        private readonly AccountingDbContext _context;

        public InventoryController(AccountingDbContext context)
        {
            _context = context;
        }

        [HttpGet("active-item-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetActiveItemIds()
        {
            var ids = await _context.SunItems
                .AsNoTracking()
                .Where(i => i.ItmState == "0")
                .Select(i => i.ItmId)
                .ToListAsync();

            return Ok(ids);
        }

        [HttpGet("main-groups")]
        public async Task<ActionResult> GetMainGroups([FromQuery] string? lastSyncDate)
        {
            var query = _context.SunItemGroupMains.AsNoTracking().AsQueryable();

            if (string.IsNullOrEmpty(lastSyncDate))
            {
                query = query.Where(g => g.ItmGrpMinState == "0");
            }
            else
            {
                query = query.Where(g => g.EdtDate != null && g.EdtDate.CompareTo(lastSyncDate) > 0);
            }

            var groups = await query.ToListAsync();

            var result = groups.Select(g => new
            {
                main_group_no = g.ItmGrpMinId,
                main_group_name = g.ItmGrpMinName,
                source_id = g.ItmGrpMinId,
                last_updated = g.EdtDate,
                created_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return Ok(new { server_sync_time = currentServerTime, data = result });
        }

        [HttpGet("active-main-group-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetActiveMainGroupIds()
        {
            var ids = await _context.SunItemGroupMains.AsNoTracking().Where(g => g.ItmGrpMinState == "0").Select(g => g.ItmGrpMinId).ToListAsync();
            return Ok(ids);
        }

        [HttpGet("item-groups")]
        public async Task<ActionResult> GetItemGroups([FromQuery] string? lastSyncDate)
        {
            var query = _context.SunItemGroups.AsNoTracking().AsQueryable();

            if (string.IsNullOrEmpty(lastSyncDate))
            {
                query = query.Where(g => g.ItmGrpState == "0");
            }
            else
            {
                query = query.Where(g => g.EdtDate != null && g.EdtDate.CompareTo(lastSyncDate) > 0);
            }

            var groups = await query.ToListAsync();

            var result = groups.Select(g => new
            {
                group_no = g.ItmGrpId,
                group_name = g.ItmGrpName,
                main_group_no = g.ItmGrpMinId,
                group_length = int.TryParse(g.ItmGrpLen, out var len) && len > 0 ? len : 3,
                source_id = g.ItmGrpId,
                last_updated = g.EdtDate,
                created_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return Ok(new { server_sync_time = currentServerTime, data = result });
        }

        [HttpGet("active-item-group-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetActiveItemGroupIds()
        {
            var ids = await _context.SunItemGroups.AsNoTracking().Where(g => g.ItmGrpState == "0").Select(g => g.ItmGrpId).ToListAsync();
            return Ok(ids);
        }

        [HttpGet("cost-centers")]
        public async Task<ActionResult> GetCostCenters([FromQuery] string? lastSyncDate)
        {
            var query = _context.SunCostCenters.AsNoTracking().AsQueryable();

            if (string.IsNullOrEmpty(lastSyncDate))
            {
                query = query.Where(c => c.AccState == "0");
            }
            else
            {
                query = query.Where(c => c.AccEdtDate != null && c.AccEdtDate.CompareTo(lastSyncDate) > 0);
            }

            var centers = await query.ToListAsync();

            var result = centers.Select(c => new
            {
                center_no = int.TryParse(c.AccId, out var id) ? id : 0,
                center_name = c.AccName,
                source_id = c.AccId,
                last_updated = c.AccEdtDate
            }).ToList();

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return Ok(new { server_sync_time = currentServerTime, data = result });
        }

        [HttpGet("active-cost-center-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetActiveCostCenterIds()
        {
            var ids = await _context.SunCostCenters.AsNoTracking().Where(c => c.AccState == "0").Select(c => c.AccId).ToListAsync();
            return Ok(ids);
        }
       
        [HttpGet("items")]
        public async Task<ActionResult> GetItems([FromQuery] string? lastSyncDate)
        {
            var query = from i in _context.SunItems.AsNoTracking()
                        join img in _context.SunImages.AsNoTracking() on i.ItmId equals img.ImgId into imgGroup
                        from img in imgGroup.DefaultIfEmpty()
                        select new { Item = i, Image = img.ImgData };

            if (string.IsNullOrEmpty(lastSyncDate))
            {
                query = query.Where(x => x.Item.ItmState == "0");
            }
            else
            {
                query = query.Where(x => x.Item.EdtDate != null && x.Item.EdtDate.CompareTo(lastSyncDate) > 0);
            }

            var rawItems = await query.ToListAsync();

            var result = rawItems.Select(x =>
            {
                var sqlItem = x.Item;
                var imgData = x.Image;

                decimal taxRate = 0;
                decimal discountRate = 0;
                var otherData = sqlItem.ItmNaOther?.Split('@');
                if (otherData != null && otherData.Length >= 4)
                {
                    decimal.TryParse(otherData[3], out taxRate);
                    decimal.TryParse(otherData[2], out discountRate);
                }

                int isCashReserved = (!string.IsNullOrEmpty(sqlItem.ItmOptions) &&
                                      sqlItem.ItmOptions.Length >= 3 &&
                                      sqlItem.ItmOptions[2] == '1') ? 1 : 0;

                return new
                {
                    item_no = sqlItem.ItmId,
                    item_type_id = sqlItem.ItmTyId,
                    item_name = sqlItem.ItmName,
                    item_barcode = sqlItem.ItmBarcode,
                    item_unit = sqlItem.ItmUntName,
                    item_price = double.TryParse(sqlItem.ItmSale, out var price) ? price : 0.0,
                    average_cost = !string.IsNullOrEmpty(sqlItem.ItmCost3) ? sqlItem.ItmCost3 : (!string.IsNullOrEmpty(sqlItem.ItmCost) ? sqlItem.ItmCost : "0"),
                    section_no = sqlItem.ItmSecId,
                    currency_no = int.TryParse(sqlItem.ItmCursId, out var cur) ? cur : 1,
                    item_group_no = sqlItem.ItmGrpId,
                    cost_center_no = int.TryParse(sqlItem.ItmCcId, out var cId) ? cId : (int?)null,
                    inventory_account_id = sqlItem.ItmAccId,
                    cogs_account_id = sqlItem.ItmAccCost,
                    sales_revenue_account_id = sqlItem.ItmAccIncome,
                    sales_discount_account_id = sqlItem.ItmAccDisc,
                    tax_rate = (double)taxRate,
                    discount_rate = (double)discountRate,
                    is_taxable = taxRate > 0 ? 1 : 0,
                    is_discountable = discountRate > 0 ? 1 : 0,
                    is_cash_reserved = isCashReserved,
                    image_base64 = imgData != null ? Convert.ToBase64String(imgData) : null,
                    last_updated = sqlItem.EdtDate
                };
            }).ToList();

            // ============================================================
            // التعديل الضروري: إضافة توقيت السيرفر
            // ============================================================
            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");

            return Ok(new
            {
                server_sync_time = currentServerTime,
                data = result
            });
        }

        [HttpGet("item-units")]
        public async Task<ActionResult> GetItemUnits([FromQuery] string? lastSyncDate)
        {
            var query = _context.SunItemSizes.AsNoTracking().AsQueryable();

            if (string.IsNullOrEmpty(lastSyncDate))
            {
                query = query.Where(u => u.SizState == "0");
            }
            else
            {
                query = query.Where(u => u.EdtDate != null && u.EdtDate.CompareTo(lastSyncDate) > 0);
            }

            var rawUnits = await query.ToListAsync();

            var result = rawUnits.Select(u => new
            {
                unit_id = u.SizId,
                item_no = u.SizItmId,
                unit_name = u.SizName,
                unit_index = int.TryParse(u.SizIndex, out var idx) ? idx : 0,
                conversion_factor = double.TryParse(u.SizValue, out var val) ? val : 1.0,
                sale_price = double.TryParse(u.SizPrice, out var price) ? price : 0.0,
                barcode = u.SizCode,
                is_active = (u.SizState == "0") ? 1 : 0,
                last_updated = u.EdtDate
            }).ToList();

            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            return Ok(new { server_sync_time = currentServerTime, data = result });
        }

        [HttpGet("active-item-unit-ids")]
        public async Task<ActionResult<IEnumerable<string>>> GetActiveItemUnitIds()
        {
            var ids = await _context.SunItemSizes.AsNoTracking().Where(u => u.SizState == "0").Select(u => u.SizId).ToListAsync();
            return Ok(ids);
        }
        [HttpGet("stock-balances")]
        public async Task<ActionResult> GetStockBalances([FromQuery] string? lastSyncDate)
        {
            var stockList = new List<object>();
            string latestYear = await GetLatestFinancialYearAsync();
            string defaultYear = await GetDefaultFinancialYearAsync();

            string dateFilterSql = "";
            // تحويل التاريخ لفلترة البيانات المعدلة حديثاً فقط
            if (!string.IsNullOrEmpty(lastSyncDate) && DateTime.TryParse(lastSyncDate, out DateTime parsedDate))
            {
                dateFilterSql = $" AND MAX(ISNULL(T.TRADTRADATEID, '') + ISNULL(T.TRADTRATIMEID, '')) > '{parsedDate.ToString("yyyyMMddHHmmss")}'";
            }

            // جلب الفروع النشطة
            var branches = await _context.SunBranches
                .AsNoTracking()
                .Where(b => b.BraState == "0")
                .Select(b => b.BraId)
                .ToListAsync();

            var unionQueries = new List<string>();

            foreach (var branchId in branches)
            {
                if (string.IsNullOrEmpty(branchId)) continue;

                // تحديد اسم الجدول بناءً على السنة المالية
                string tableName = (latestYear == defaultYear)
                    ? $"SunTransDetailItem{branchId}"
                    : $"SunTransDetailItem{latestYear}{branchId}";

                // بنينا الاستعلام ليعتمد على التكلفة من جدول الأصناف مباشرة (S.ITMCOST3)
                // بدلاً من حسابها رياضياً، مما يجعله سريعاً جداً ودقيقاً
                string querySegment = $@"
        SELECT 
            T.TRADITMID AS ItemNo, 
            CAST(T.TRADSTORID AS VARCHAR(20)) + '|' + '{branchId}' AS StoreNo, 
            ISNULL(T.TRADEXPIRE, '') AS ExpiryDate, 
            ISNULL(T.TRADREFID, '') AS BatchNo,
            
            -- حساب الكمية المتبقية (وارد - صادر)
            SUM((CAST(ISNULL(T.TRADQTYTOT, '0') AS FLOAT) * CAST(ISNULL(T.TRADUNITSIZE, '1') AS FLOAT)) * (CAST(ISNULL(T.TRADSIGNDIN, '0') AS FLOAT) - CAST(ISNULL(T.TRADSIGNDOUT, '0') AS FLOAT))) AS Quantity,
            
            -- جلب التكلفة من جدول الأصناف مباشرة (الأولوية لـ Cost3 ثم Cost)
            -- نستخدم MAX لأننا في جملة تجميع، وبما أن الصنف واحد فالتكلفة واحدة
            MAX(CAST(ISNULL(NULLIF(S.ITMCOST3, ''), ISNULL(NULLIF(S.ITMCOST, ''), '0')) AS FLOAT)) AS CostPrice,

            MAX(ISNULL(T.TRADTRADATEID, '') + ISNULL(T.TRADTRATIMEID, '')) AS LastTransactionStr
        FROM {tableName} T 
        LEFT JOIN SUNITEM S ON T.TRADITMID = S.ITMID
        GROUP BY T.TRADITMID, T.TRADSTORID, T.TRADEXPIRE, T.TRADREFID
        HAVING SUM((CAST(ISNULL(T.TRADQTYTOT, '0') AS FLOAT) * CAST(ISNULL(T.TRADUNITSIZE, '1') AS FLOAT)) * (CAST(ISNULL(T.TRADSIGNDIN, '0') AS FLOAT) - CAST(ISNULL(T.TRADSIGNDOUT, '0') AS FLOAT))) <> 0
        {dateFilterSql}";

                unionQueries.Add(querySegment);
            }

            if (unionQueries.Count > 0)
            {
                // دمج الاستعلامات لكل الفروع
                string finalQuery = string.Join(" UNION ALL ", unionQueries);
                try
                {
                    var allStocks = await _context.Database.SqlQueryRaw<StockResultDto>(finalQuery).ToListAsync();

                    // تنسيق النتيجة النهائية
                    var result = allStocks.Select(s => {
                        var parts = s.StoreNo.Split('|');
                        return new
                        {
                            item_no = s.ItemNo,
                            store_no = int.TryParse(parts[0], out var sn) ? sn : 0,
                            branch_id = int.TryParse(parts.Length > 1 ? parts[1] : "0", out var bi) ? bi : 0,
                            batch_no = s.BatchNo,
                            expiry_date = s.ExpiryDate,
                            quantity = s.Quantity,
                            cost_price = s.CostPrice, // القيمة القادمة من ITMCOST3
                            last_updated_str = s.LastTransactionStr,
                            updated_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                    });

                    stockList.AddRange(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error executing stock union query: {ex.Message}");
                }
            }


            string currentServerTime = DateTime.Now.ToString("yyyyMMddHHmmss");

            return Ok(new
            {
                server_sync_time = currentServerTime,
                data = stockList
            });
        }

        [HttpPost("create-item")]
        public async Task<ActionResult> CreateItem([FromBody] CreateItemDto input)
        {
            if (string.IsNullOrWhiteSpace(input?.ItemNo))
                return BadRequest("رقم الصنف مطلوب.");
            if (string.IsNullOrWhiteSpace(input.ItemName))
                return BadRequest("اسم الصنف مطلوب.");
            if (string.IsNullOrWhiteSpace(input.ItemUnit))
                return BadRequest("وحدة الصنف مطلوبة.");

            try
            {
                // 1. Duplicate check
                bool exists = await _context.SunItems.AnyAsync(i => i.ItmId == input.ItemNo);
                if (exists)
                    return Conflict(new { success = false, message = "رقم الصنف مستخدم مسبقاً" });

                // 2. Encode ITMNAOTHER: "0@0@{discountRate}@{taxRate}@0@0@0@0@0@"
                string discountRateStr = input.IsDiscountable && input.DiscountRate.HasValue
                    ? input.DiscountRate.Value.ToString("0.##")
                    : "0";
                string taxRateStr = input.IsTaxable ? input.TaxRate.ToString("0.##") : "0";
                string itmNaOther = $"0@0@{discountRateStr}@{taxRateStr}@0@0@0@0@0@";

                // 3. Encode ITMOPTIONS: position [2] == '1' means cash-reserved
                string itmOptions = input.IsCashReserved ? "001" : "000";

                // 4. Use group ID directly
                string groupId = input.ItemGroupNo;

                // 5. Dynamic Section ID (from Flutter payload)
                string itmSecId = !string.IsNullOrWhiteSpace(input.SectionId) ? input.SectionId : "1";

                // 6. Auto-Incrementing Index
                var existingIndexes = await _context.SunItems
                    .AsNoTracking()
                    .Where(i => i.ItmGrpId == groupId)
                    .Select(i => i.ItmIndex)
                    .ToListAsync();

                int nextIndex = 1;
                if (existingIndexes.Any()) {
                    nextIndex = existingIndexes
                        .Select(idx => int.TryParse(idx, out var val) ? val : 0)
                        .DefaultIfEmpty(0)
                        .Max() + 1;
                }
                string itmIndex = nextIndex.ToString();

                // 6.5 Fetch Item Group Name for ITMSGRPNAME
                var group = await _context.SunItemGroups.AsNoTracking().FirstOrDefaultAsync(g => g.ItmGrpId == groupId);
                string groupName = group?.ItmGrpName ?? "";
                string unitId = $"{input.ItemNo}.0";

                // 7. Insert into SUNITEM
                var newItem = new SunItem
                {
                    ItmId       = input.ItemNo,
                    ItmNo       = input.ItemNo,
                    ItmName     = input.ItemName,
                    ItmNamee    = input.ItemName,
                    ItmBarcode  = string.IsNullOrEmpty(input.ItemBarcode) ? null : input.ItemBarcode,
                    ItmUntName  = input.ItemUnit,
                    ItmSale     = input.ItemPrice.ToString("0.##"),
                    ItmGrpId    = groupId,
                    GrpId       = groupId,
                    ItmCursId   = input.CurrencyNo.ToString(),
                    ItmAccId    = input.InventoryAccountId,
                    ItmAccCost  = input.CogsAccountId,
                    ItmAccIncome = input.SalesRevenueAccountId,
                    ItmAccDisc  = input.SalesDiscountAccountId,
                    ItmCcId     = input.CostCenterNo?.ToString(),
                    ItmNaOther  = itmNaOther,
                    ItmOptions  = itmOptions,
                    ItmSecId    = itmSecId,
                    ItmComId    = "1",
                    ItmCost     = "0",
                    ItmCost3    = "0",
                    ItmState    = "0",
                    EdtDate     = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    ItmTyId     = "0",
                    ItmGrpMinId = input.MainGroupNo,
                    ItmIndex    = itmIndex,
                    ItmAccBonus = input.SalesRevenueAccountId,
                    ItmAccTax   = input.SalesRevenueAccountId,
                    ItmAccTaxOth= input.SalesRevenueAccountId,
                    ItmAccOth   = input.SalesRevenueAccountId,
                    ItmUntMaxSizName = input.ItemUnit,
                    ItmUntMinSizName = input.ItemUnit,
                    ItmUntMaxSizValue= (input.ItemUnitSizeValue ?? 1.0).ToString("0.##"),
                    ItmUntMinSizValue= (input.ItemUnitSizeValue ?? 1.0).ToString("0.##"),
                    ItmShurtCode= input.ItemNo?.Replace("-", ""),
                    ItmSgrpName = groupName,
                    ItmUntMaxSizId = unitId,
                    ItmUntMinSizId = unitId,
                    ItmUntSizSale = input.ItemPrice.ToString("0.##")
                };

                _context.SunItems.Add(newItem);

                // 8. Insert into SUNITEMSIZE
                var newItemSize = new SunItemSize
                {
                    SizItmId = input.ItemNo,
                    SizId = unitId,
                    SizName = input.ItemUnit,
                    SizValue = (input.ItemUnitSizeValue ?? 1.0).ToString("0.##"),
                    SizIndex = "0",
                    SizMaxNo = "1",
                    SizCode = input.ItemBarcode ?? "",
                    SizCodeW = input.ItemNo?.Replace("-", ""),
                    SizPrice = input.ItemPrice.ToString("0.##"),
                    SizPrice2 = "",
                    SizPrice3 = "",
                    SizClass = "",
                    SizRemarks = "",
                    SizState = "0",
                    SizInvState = "",
                    SizStop = "0",
                    SizStopDate = "",
                    SizStopDateId = "",
                    SizMaxState = "0",
                    SizMinState = "0",
                    EdtDate = DateTime.Now.ToString("yyyyMMddHHmmss"),
                    SyncFlag = "1"
                };

                _context.SunItemSizes.Add(newItemSize);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success  = true,
                    item_no  = input.ItemNo,
                    message  = "تم إضافة الصنف بنجاح"
                });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No inner exception";
                Console.WriteLine($"Error creating item: {ex.Message} | {inner}");
                return StatusCode(500, $"فشل إضافة الصنف: {ex.Message} \n {inner}");
            }
        }

        [HttpPost("update-item")]
        public async Task<ActionResult> UpdateItem([FromBody] UpdateItemDto input)
        {
            if (string.IsNullOrWhiteSpace(input?.ItemNo))
                return BadRequest("رقم الصنف مطلوب.");
            if (string.IsNullOrWhiteSpace(input.ItemName))
                return BadRequest("اسم الصنف مطلوب.");

            try
            {
                // 1. Find existing record
                var existing = await _context.SunItems.FindAsync(input.ItemNo);
                if (existing == null)
                    return NotFound(new { success = false, message = "الصنف غير موجود" });

                // 2. Encode ITMNAOTHER
                string discountRateStr = input.IsDiscountable && input.DiscountRate.HasValue
                    ? input.DiscountRate.Value.ToString("0.##")
                    : "0";
                string taxRateStr = input.IsTaxable ? input.TaxRate.ToString("0.##") : "0";
                string itmNaOther = $"0@0@{discountRateStr}@{taxRateStr}@0@0@0@0@0@";

                // 3. Encode ITMOPTIONS
                string itmOptions = input.IsCashReserved ? "001" : "000";

                // 4. Update mutable fields (ITMID and ITMGRPID are intentionally not changed)
                existing.ItmName     = input.ItemName;
                existing.ItmBarcode  = string.IsNullOrEmpty(input.ItemBarcode) ? null : input.ItemBarcode;
                existing.ItmUntName  = input.ItemUnit;
                existing.ItmSale     = input.ItemPrice.ToString("0.##");
                existing.ItmUntSizSale = input.ItemPrice.ToString("0.##");
                existing.ItmCursId   = input.CurrencyNo.ToString();
                existing.ItmAccId    = input.InventoryAccountId;
                existing.ItmAccCost  = input.CogsAccountId;
                existing.ItmAccIncome = input.SalesRevenueAccountId;
                existing.ItmAccDisc  = input.SalesDiscountAccountId;
                existing.ItmCcId     = input.CostCenterNo?.ToString();
                existing.ItmNaOther  = itmNaOther;
                existing.ItmOptions  = itmOptions;
                existing.EdtDate     = DateTime.Now.ToString("yyyyMMddHHmmss");

                var existingSize = await _context.SunItemSizes.FirstOrDefaultAsync(s => s.SizItmId == input.ItemNo);
                if (existingSize != null)
                {
                    existingSize.SizPrice = input.ItemPrice.ToString("0.##");
                    existingSize.SizName = input.ItemUnit;
                    existingSize.SizCode = input.ItemBarcode ?? "";
                    existingSize.EdtDate = DateTime.Now.ToString("yyyyMMddHHmmss");
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    item_no = input.ItemNo,
                    message = "تم تحديث الصنف بنجاح"
                });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No inner exception";
                Console.WriteLine($"Error updating item: {ex.Message} | {inner}");
                return StatusCode(500, $"فشل تحديث الصنف: {ex.Message} \n {inner}");
            }
        }

        private async Task<string> GetLatestFinancialYearAsync()
        {
            var maxYear = await _context.Tssysyears.AsNoTracking().OrderByDescending(y => y.Yrid).Select(y => y.Yrvalue).FirstOrDefaultAsync();
            return maxYear ?? DateTime.Now.Year.ToString();
        }

        private async Task<string> GetDefaultFinancialYearAsync()
        {
            var defaultYear = await _context.Tssysyears.AsNoTracking().Where(y => y.Yrid == "1").Select(y => y.Yrvalue).FirstOrDefaultAsync();
            return defaultYear ?? await GetLatestFinancialYearAsync();
        }
    }

    public class StockResultDto
    {
        public string ItemNo { get; set; }
        public string StoreNo { get; set; }
        public string ExpiryDate { get; set; }
        public string BatchNo { get; set; }
        public double Quantity { get; set; }
        public double CostPrice { get; set; }
        public string LastTransactionStr { get; set; }
    }

    public class CreateItemDto
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string? ItemBarcode { get; set; }
        public string ItemUnit { get; set; }
        public double ItemPrice { get; set; }
        public string ItemGroupNo { get; set; }
        public int CurrencyNo { get; set; }
        public string? InventoryAccountId { get; set; }
        public string? CogsAccountId { get; set; }
        public string? SalesRevenueAccountId { get; set; }
        public string? SalesDiscountAccountId { get; set; }
        public bool IsTaxable { get; set; }
        public double TaxRate { get; set; }
        public bool IsDiscountable { get; set; }
        public double? DiscountRate { get; set; }
        public bool IsCashReserved { get; set; }
        public int? CostCenterNo { get; set; }
        public string? AppUserName { get; set; }
        public string MainGroupNo { get; set; }
        public string? SectionId { get; set; }
        public double? ItemUnitSizeValue { get; set; }
    }

    public class UpdateItemDto
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string? ItemBarcode { get; set; }
        public string ItemUnit { get; set; }
        public double ItemPrice { get; set; }
        public int CurrencyNo { get; set; }
        public string? InventoryAccountId { get; set; }
        public string? CogsAccountId { get; set; }
        public string? SalesRevenueAccountId { get; set; }
        public string? SalesDiscountAccountId { get; set; }
        public bool IsTaxable { get; set; }
        public double TaxRate { get; set; }
        public bool IsDiscountable { get; set; }
        public double? DiscountRate { get; set; }
        public bool IsCashReserved { get; set; }
        public int? CostCenterNo { get; set; }
    }
}