using AccountingSyncAPP.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountingSyncAPP.Data
{
    public class AccountingDbContext : DbContext
    {
        public AccountingDbContext(DbContextOptions<AccountingDbContext> options) : base(options)
        {
        }

        public DbSet<SunUser> SunUsers { get; set; }
        public DbSet<Tssysyear> Tssysyears { get; set; }
        public DbSet<SunDataAccDir> SunDataAccDirs { get; set; }
        public DbSet<SunStore> SunStores { get; set; }
        public DbSet<SunItemGroupMain> SunItemGroupMains { get; set; }
        public DbSet<SunItemGroup> SunItemGroups { get; set; }
        public DbSet<SunItem> SunItems { get; set; }
        public DbSet<SunCostCenter> SunCostCenters { get; set; }
        public DbSet<SunCurrency> SunCurrencies { get; set; }
        public DbSet<SunCustomer> SunCustomers { get; set; }
        public DbSet<SunAccDirCury> SunAccDirCuries { get; set; }
        public DbSet<SunAcMain> SunAcMains { get; set; }
        public DbSet<SunAcType> SunAcTypes { get; set; }
        public DbSet<SunAcTypeDetail> SunAcTypeDetails { get; set; }
        public  DbSet<SunDataAnalyticalAccDir> SunDataAnalyticalAccDirs { get; set; }
        public DbSet<SunTransHead1> SunTransHead1s { get; set; }
        public DbSet<SunTransHeadline1> SunTransHeadline1s { get; set; }
        public DbSet<SunTransDetailItem1> SunTransDetailItem1s { get; set; }
        public DbSet<SunTransJe1> SunTransJe1s { get; set; }
        public DbSet<SunBranch> SunBranches { get; set; }
        public DbSet<SunImage> SunImages { get; set; }
        public DbSet<SunSection> SunSections { get; set; }
        public DbSet<SunDealer> Dealers { get; set; }
        public DbSet<SunCollector> Collectors { get; set; }
        public DbSet<SunTransAcc1> SunTransAcc1s { get; set; }
        public  DbSet<TsSystemInfo> TsSystemInfos { get; set; }
        public  DbSet<SunItemSize> SunItemSizes { get; set; }
        public  DbSet<SunItemBalance> SunItemBalances { get; set; }
        public DbSet<SunUsersListDetail> SunUsersListDetails { get; set; }
        public DbSet<Almahta> Almahtas { get; set; }
        public DbSet<SunVender> SunVenders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // التعديل هنا:
            modelBuilder.Entity<SunItemBalance>(entity =>
            {
                
                entity.HasNoKey();

            });
        }


    }
}