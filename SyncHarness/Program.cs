using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using AccountingSyncAPP.Data;
using AccountingSyncAPP.Services;
using Supabase;
using System;
using System.Threading;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("STARTING SYNC HARNESS");

        var services = new ServiceCollection();

        // Settings from settings_check.txt
        // Server: DESKTOP-DHV2UG0, Instance: TS, Database: DEMO
        string connectionString = "Server=DESKTOP-DHV2UG0\\TS;Database=DEMO;User Id=sa;Password=ts@123;TrustServerCertificate=True;";
        string supabaseUrl = "https://ueipplbjrzjstlwrblyb.supabase.co";
        string supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InVlaXBwbGJqcnpqc3Rsd3JibHliIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc2NTI5Mjc4MiwiZXhwIjoyMDgwODY4NzgyfQ.81uegpe-sj4Rb55mVeGh8PseBhj_qZ6a5aGKr01gRYs";

        services.AddDbContext<AccountingDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped(provider =>
            new Client(supabaseUrl, supabaseKey, new Supabase.SupabaseOptions { AutoRefreshToken = true, AutoConnectRealtime = true }));

        services.AddScoped<TransactionSyncService>();
        services.AddLogging(configure => {
            configure.AddConsole();
            configure.SetMinimumLevel(LogLevel.Information);
        });

        var provider = services.BuildServiceProvider();

        // Instantiate TransactionSyncService. 
        // Note: It constructor requires ILogger<TransactionSyncService>, Client, IServiceProvider.
        var syncService = provider.GetRequiredService<TransactionSyncService>();

        try {
            Console.WriteLine("--- Executing SyncReceiptsFromSupabaseAsync ---");
            await syncService.SyncReceiptsFromSupabaseAsync(CancellationToken.None);
            Console.WriteLine("--- Finished SyncReceiptsFromSupabaseAsync ---");
            
            Console.WriteLine("--- Executing SyncVouchersToSupabaseAsync ---");
            await syncService.SyncVouchersToSupabaseAsync(CancellationToken.None);
            Console.WriteLine("--- Finished SyncVouchersToSupabaseAsync ---");
        }
        catch (Exception ex) {
            Console.WriteLine($"CRITICAL HARNESS ERROR: {ex}");
        }

        Console.WriteLine("HARNESS COMPLETE");
    }
}
