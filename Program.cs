using Microsoft.AspNet.SignalR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace ChatApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
            // Make long polling connections wait a maximum of 110 seconds for a
            // response. When that time expires, trigger a timeout command and
            // make the client reconnect.
            GlobalHost.Configuration.ConnectionTimeout = TimeSpan.FromMinutes(6);

            // Wait a maximum of 30 seconds after a transport connection is lost
            // before raising the Disconnected event to terminate the SignalR connection.
            GlobalHost.Configuration.DisconnectTimeout = TimeSpan.FromMinutes(3);

            // For transports other than long polling, send a keepalive packet every
            // 10 seconds. 
            // This value must be no more than 1/3 of the DisconnectTimeout value.
            GlobalHost.Configuration.KeepAlive = TimeSpan.FromMinutes(1);

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
             Host.CreateDefaultBuilder(args)
                 .ConfigureWebHostDefaults(webBuilder =>
                 {

                     webBuilder.UseContentRoot(System.IO.Directory.GetCurrentDirectory());
                     webBuilder.UseIISIntegration();
                     webBuilder.UseStartup<Startup>();

                 }).ConfigureLogging(logging =>
                 {
                     logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Debug);
                     logging.AddFilter("Microsoft.AspNetCore.Http.Connections", LogLevel.Debug);
                 });
    }
}
