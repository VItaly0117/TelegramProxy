using System;
using System.Threading;
using System.Threading.Tasks;
using TelegramProxy.Services;

namespace TelegramProxy.Daemon
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== TelegramProxy Docker Daemon ===");

            var settingsManager = new SettingsManager();
            var proxyEngine = new ProxyEngine();
            var cloudflareService = new CloudflareService();

            var currentDc = settingsManager.Current.Datacenters.Find(dc => dc.Name == settingsManager.Current.ActiveDcName) 
                            ?? settingsManager.Current.Datacenters[1];

            proxyEngine.LogMessage += Console.WriteLine;
            cloudflareService.LogMessage += Console.WriteLine;
            cloudflareService.UrlObtained += url => Console.WriteLine($"\n[CLOUDFLARE ACTIVE] -> {url.Replace("https://", "wss://")}\n");

            Console.CancelKeyPress += async delegate(object? sender, ConsoleCancelEventArgs e) {
                e.Cancel = true;
                Console.WriteLine("\nShutting down daemon...");
                await cloudflareService.StopAsync();
                await proxyEngine.StopAsync(TimeSpan.FromSeconds(5));
                Environment.Exit(0);
            };

            proxyEngine.Start(settingsManager.Current.LocalPort, currentDc.Ip, currentDc.Port);

            if (settingsManager.Current.UseCloudflare)
            {
                Console.WriteLine("Starting Cloudflare Tunnel...");
                await cloudflareService.StartAsync(settingsManager.Current.LocalPort, CancellationToken.None);
            }

            Console.WriteLine("Daemon is running purely headless. Press Ctrl+C to exit.");
            
            // Background thread to safely consume and discard traffic telemetry when running headless to prevent channel blocking
            while (true)
            {
                try
                {
                    await foreach (var traffic in proxyEngine.TrafficReader.ReadAllAsync())
                    {
                        // Consuming channel gracefully
                    }
                }
                catch { }
                await Task.Delay(1000);
            }
        }
    }
}
