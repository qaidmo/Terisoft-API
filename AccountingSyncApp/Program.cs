using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows;
using AccountingSyncAPP.Configuration;
using AccountingSyncAPP.Data;
using AccountingSyncAPP.Services;
using Microsoft.EntityFrameworkCore;

namespace AccountingSyncApp
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            var settingsService = new SettingsService();
            var settings = settingsService.LoadSettings();
            if (settings != null)
            {
                builder.Services.AddSingleton(settings);
                builder.Services.AddDbContext<AccountingDbContext>(options =>
                    options.UseSqlServer(settings.ToSqlConnectionString(), sqlOptions => sqlOptions.CommandTimeout(120)), ServiceLifetime.Scoped);
                builder.Services.AddScoped<IReportsService, ReportsService>();
                builder.Services.AddScoped<ItemMovementService>();
                builder.Services.AddScoped<ProfitMarginService>();
                builder.Services.AddScoped<ItemBalancesService>();
                builder.Services.AddScoped<ItemCardService>();
                builder.Services.AddScoped<AccountBalancesService>();
                builder.Services.AddScoped<CostCenterProfitMarginService>();

                // Startup schema auto-migration: ensures every active branch's
                // SunTransDetailItem table has the three pump/meter columns
                // (TR_M_NAME, TRANS_FERST, TRANS_NEXT) before any invoice sync
                // request is served. See Services/SchemaInitializerService.cs.
                builder.Services.AddHostedService<SchemaInitializerService>();

            }
            builder.Services.AddResponseCompression(options => { options.EnableForHttps = true; });
            builder.Services.AddControllers();
            var webApp = builder.Build();
            webApp.UseResponseCompression();
            webApp.UseRouting();
            webApp.MapControllers();
            webApp.Urls.Add("http://0.0.0.0:5000");
            var apiTask = Task.Run(() => webApp.RunAsync());
            var wpfApp = new App();
            wpfApp.InitializeComponent();

            try
            {
                wpfApp.Run();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Critical Error: {ex.Message}");
            }
            finally
            {
                Task.Run(async () => await webApp.StopAsync()).Wait();
            }
        }
    }
}