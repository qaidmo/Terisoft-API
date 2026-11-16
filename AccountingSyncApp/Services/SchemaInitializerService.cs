// Services/SchemaInitializerService.cs
//
// PURPOSE
// -------
// Runs once at application boot (as an IHostedService) to guarantee that
// every active branch's transaction-detail table contains the three
// pump/meter station columns:
//
//   TR_M_NAME   – the pump identifier (meter name)
//   TRANS_FERST – the previous meter reading
//   TRANS_NEXT  – the current meter reading
//
// The legacy Sun accounting system creates one set of transaction tables
// per branch (e.g., SunTransDetailItem1, SunTransDetailItem2 …).
// Branch 1's table already contains these columns (they were added manually),
// but all other branch tables are missing them. Without this initializer a
// sync request for any invoice on a non-default branch throws:
//
//   Invalid column name 'TR_M_NAME'.
//   Invalid column name 'TRANS_FERST'.
//   Invalid column name 'TRANS_NEXT'.
//
// SAFETY PROPERTIES
// -----------------
// • Runs exactly once per host boot (IHostedService.StartAsync lifetime).
// • Fully idempotent – uses INFORMATION_SCHEMA.COLUMNS before every ALTER.
// • Never crashes the application – all exceptions are caught and logged.
// • Skips tables that do not yet exist (legacy system creates them lazily).
// • Uses the same table-suffix formula as TransactionController so the
//   computed names are always in sync with the names used during invoice sync.

using AccountingSyncAPP.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AccountingSyncAPP.Services
{
    public class SchemaInitializerService : IHostedService
    {
        // ── Constants ────────────────────────────────────────────────────────

        /// <summary>
        /// The three pump/meter columns that must exist on every active
        /// branch's SunTransDetailItem table.
        /// </summary>
        private static readonly string[] PumpColumns = new[]
        {
            "TR_M_NAME",
            "TRANS_FERST",
            "TRANS_NEXT"
        };

        /// <summary>
        /// DDL type definition for each column.
        /// NVARCHAR is used because all other numeric/text columns in these
        /// legacy tables are stored as VARCHAR/NVARCHAR strings, not as
        /// SQL numeric types. NULL is required because only pump invoices
        /// (type 605) write these columns – all others leave them blank.
        /// </summary>
        private static readonly Dictionary<string, string> ColumnDefinitions =
            new Dictionary<string, string>
            {
                { "TR_M_NAME",   "NVARCHAR(255) NULL" },
                { "TRANS_FERST", "NVARCHAR(50)  NULL" },
                { "TRANS_NEXT",  "NVARCHAR(50)  NULL" }
            };

        /// <summary>
        /// Utility station columns for electricity and water subscriptions on SUNCUSTOMER.
        /// </summary>
        private static readonly string[] CustomerUtilityColumns = new[]
        {
            "COST_DATA",
            "COST_NEXT",
            "COST_RSOM"
        };

        private static readonly Dictionary<string, string> CustomerColumnDefinitions =
            new Dictionary<string, string>
            {
                { "COST_DATA", "NVARCHAR(50) NULL" },
                { "COST_NEXT", "NVARCHAR(50) NULL" },
                { "COST_RSOM", "NVARCHAR(50) NULL" }
            };

        // ── Dependencies ─────────────────────────────────────────────────────

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SchemaInitializerService> _logger;

        public SchemaInitializerService(
            IServiceScopeFactory scopeFactory,
            ILogger<SchemaInitializerService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // ── IHostedService implementation ─────────────────────────────────

        /// <summary>
        /// Called once by the host framework when the application starts.
        /// All real work is delegated to <see cref="RunMigrationAsync"/> so
        /// that this method stays clean and always completes – even on error.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "[SchemaInitializer] ──── Startup schema auto-migration starting ────");
            try
            {
                await RunMigrationAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // CRITICAL: we must NEVER throw from StartAsync.
                // A thrown exception would crash the ASP.NET host before it
                // starts accepting requests. Log as Warning so it is visible
                // but non-fatal.
                _logger.LogWarning(
                    ex,
                    "[SchemaInitializer] Schema migration encountered an unexpected error. " +
                    "The application will continue, but some branch tables may be missing " +
                    "pump/meter columns (TR_M_NAME, TRANS_FERST, TRANS_NEXT).");
            }
        }

        /// <summary>
        /// No cleanup required – this service performs one-shot work only.
        /// </summary>
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        // ── Core migration logic ──────────────────────────────────────────

        private async Task RunMigrationAsync(CancellationToken cancellationToken)
        {
            // Use an async DI scope so the scoped AccountingDbContext is
            // properly disposed after this method completes.
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider
                               .GetRequiredService<AccountingDbContext>();

            // ── Step 1: Resolve the table-suffix year component ─────────────
            //
            // This exactly replicates the logic in TransactionController
            // (lines 51-70 / 86-87) so the table names we compute here are
            // always identical to the names used during invoice sync.
            //
            //   multipleYears     → true when TSSYSYEAR has more than one row
            //   maxYearValue      → MAX(YRVALUE) e.g. "2026"
            //   tableSuffixYear   → maxYearValue when multiple years, else ""
            //
            // Examples:
            //   Single year,  branch 2  → suffix = "2"  → SunTransDetailItem2
            //   Multiple years (2026), branch 2 → suffix = "20262" → SunTransDetailItem20262

            var years = await context.Tssysyears
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            bool multipleYears = years.Count > 1;

            string maxYearValue = years.Any()
                ? years.Max(y => y.Yrvalue)!
                : DateTime.Now.Year.ToString();

            string tableSuffixYear = multipleYears ? maxYearValue : string.Empty;

            _logger.LogInformation(
                "[SchemaInitializer] Year context → multipleYears={MultipleYears}, " +
                "maxYearValue='{MaxYear}', tableSuffixYear='{SuffixYear}'",
                multipleYears, maxYearValue, tableSuffixYear);

            // ── Step 2: Collect active branch IDs ───────────────────────────
            //
            // BraState == "0" is the active-record convention used throughout
            // the legacy Sun system (UState, CustState, VndrState all follow
            // the same pattern).

            var activeBranchIds = await context.SunBranches
                .AsNoTracking()
                .Where(b => b.BraState == "0")
                .Select(b => b.BraId)
                .ToListAsync(cancellationToken);

            if (activeBranchIds.Count == 0)
            {
                _logger.LogWarning(
                    "[SchemaInitializer] No active branches found in SUNBRANCH. " +
                    "Skipping schema migration.");
                return;
            }

            _logger.LogInformation(
                "[SchemaInitializer] Active branches to check: [{Branches}]",
                string.Join(", ", activeBranchIds.Where(id => id != null)));

            // ── Step 3: Check and patch each branch's detail-item table ─────

            foreach (var branchId in activeBranchIds)
            {
                // Guard: skip null / whitespace branch IDs (shouldn't happen,
                // but the EF model allows nullable string).
                if (string.IsNullOrWhiteSpace(branchId)) continue;

                // Replicate TransactionController formula:
                //   tableSuffix = tableSuffixYear + branchId
                //   tableName   = "SunTransDetailItem" + tableSuffix
                string tableSuffix = $"{tableSuffixYear}{branchId}";
                string tableName   = $"SunTransDetailItem{tableSuffix}";

                // ── 3a: Verify the table itself exists ───────────────────
                //
                // The legacy system creates branch tables lazily on first
                // use, so an absent table is completely normal and must NOT
                // be treated as an error.

                int tableCount = await context.Database
                    .SqlQueryRaw<int>(
                        "SELECT COUNT(*) AS Value " +
                        "FROM INFORMATION_SCHEMA.TABLES " +
                        "WHERE TABLE_NAME = {0}",
                        tableName)
                    .FirstOrDefaultAsync(cancellationToken);

                if (tableCount == 0)
                {
                    _logger.LogInformation(
                        "[SchemaInitializer] Table '{Table}' does not exist yet " +
                        "(branch has no transactions). Skipping.",
                        tableName);
                    continue;
                }

                _logger.LogInformation(
                    "[SchemaInitializer] Checking columns on table '{Table}' …",
                    tableName);

                // ── 3b: Check and add each pump column ───────────────────

                foreach (string column in PumpColumns)
                {
                    int colCount = await context.Database
                        .SqlQueryRaw<int>(
                            "SELECT COUNT(*) AS Value " +
                            "FROM INFORMATION_SCHEMA.COLUMNS " +
                            "WHERE TABLE_NAME = {0} AND COLUMN_NAME = {1}",
                            tableName, column)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (colCount == 0)
                    {
                        // tableName  = "SunTransDetailItem" + digits  (alphanumeric only)
                        // column     = one of the three known constant strings above
                        // Both are safe to interpolate – no user-supplied input reaches here.
                        string columnType = ColumnDefinitions[column];
                        string ddl = $"ALTER TABLE [{tableName}] ADD [{column}] {columnType}";

                        await context.Database.ExecuteSqlRawAsync(ddl, cancellationToken);

                        _logger.LogInformation(
                            "[SchemaInitializer] ✅ Added [{Column}] {Type} to '{Table}'.",
                            column, columnType, tableName);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "[SchemaInitializer] ✔  [{Column}] already exists in '{Table}'. Skipped.",
                            column, tableName);
                    }
                }
            }
            
            // ── Step 4: Check and patch SUNCUSTOMER table for utility fields ──

            int custTableCount = await context.Database
                .SqlQueryRaw<int>(
                    "SELECT COUNT(*) AS Value " +
                    "FROM INFORMATION_SCHEMA.TABLES " +
                    "WHERE TABLE_NAME = {0}",
                    "SUNCUSTOMER")
                .FirstOrDefaultAsync(cancellationToken);

            if (custTableCount > 0)
            {
                _logger.LogInformation(
                    "[SchemaInitializer] Checking utility columns on table 'SUNCUSTOMER' …");

                foreach (string column in CustomerUtilityColumns)
                {
                    int colCount = await context.Database
                        .SqlQueryRaw<int>(
                            "SELECT COUNT(*) AS Value " +
                            "FROM INFORMATION_SCHEMA.COLUMNS " +
                            "WHERE TABLE_NAME = {0} AND COLUMN_NAME = {1}",
                            "SUNCUSTOMER", column)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (colCount == 0)
                    {
                        string columnType = CustomerColumnDefinitions[column];
                        string ddl = $"ALTER TABLE [SUNCUSTOMER] ADD [{column}] {columnType}";

                        await context.Database.ExecuteSqlRawAsync(ddl, cancellationToken);

                        _logger.LogInformation(
                            "[SchemaInitializer] ✅ Added [{Column}] {Type} to 'SUNCUSTOMER'.",
                            column, columnType);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "[SchemaInitializer] ✔  [{Column}] already exists in 'SUNCUSTOMER'. Skipped.",
                            column);
                    }
                }
            }

            _logger.LogInformation(
                "[SchemaInitializer] ──── Schema auto-migration complete ────");
        }
    }
}
