using ChatApplication.Common;
using ChatApplication.Data;
using ChatApplication.Hubs;
using ChatApplication.Models;
using CorePush.Apple;
using CorePush.Google;
using CustomMVCClassLibraries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;

namespace ChatApplication
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            LibraryFunctions LF = new LibraryFunctions();

            //GlobalValues.ConnectionString = LF.Decrypt(this.Configuration.GetConnectionString("DefaultConnection"));
            GlobalValues.BaseUrl = (this.Configuration.GetConnectionString("BaseUrl"));
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();

            services.AddControllers();
            services.AddHttpClient();
            services.AddSignalR();
            services.AddHttpClient<FcmSender>();
            services.AddHttpClient<ApnSender>();
            services.Configure<HubOptions>(options =>
            {
                options.MaximumReceiveMessageSize = null;
            });

            #region Configure Db
            string server = Configuration["ConnectionSettings:Server"];
            string database = Configuration["ConnectionSettings:Database"];
            string userId = Configuration["ConnectionSettings:UserId"];
            string password = Configuration["ConnectionSettings:Password"];
            string trustedConnection = Configuration["ConnectionSettings:Trusted_Connection"];
            string multiSets = Configuration["ConnectionSettings:MultipleActiveResultSets"];
            GlobalValues.ConnectionString = $"server={server}; Database={database}; user ID={userId}; Password={password}; Trusted_Connection={trustedConnection}; MultipleActiveResultSets={multiSets}";
            services.AddDbContext<MyDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("DbString")));
                                                                               
            #endregion


            RegisterDependencies(services);
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseDirectoryBrowser(new DirectoryBrowserOptions()
            {
                FileProvider = new PhysicalFileProvider(
                                  Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot", "Images")),
                RequestPath = new PathString("/Images")
            });
            app.UseDirectoryBrowser(new DirectoryBrowserOptions()
            {
                FileProvider = new PhysicalFileProvider(
                                    Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot", "ChatBackupFolder")),
                RequestPath = new PathString("/ChatBackupFolder")
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseCors("CorsPolicy");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<chatviewhub>("/chatviewhub");
            });

            SetGlobalValues(env);
        }

        private void RegisterDependencies(IServiceCollection service)
        {
            //find dependency registrars provided by other assemblies
            var dependencyRegistrars = AppDomainTypeFinder.FindClassesOfType<IDependencyRegistrar>();

            //create and sort instances of dependency registrars
            var instances = dependencyRegistrars
                .Select(dependencyRegistrar => (IDependencyRegistrar)Activator.CreateInstance(dependencyRegistrar))
                .OrderBy(dependencyRegistrar => dependencyRegistrar.Order);

            //register all provided dependencies
            foreach (var dependencyRegistrar in instances)
                dependencyRegistrar.Register(service);
        }

        public void SetGlobalValues(IWebHostEnvironment env)
        {
            GlobalValues.chatUserInfojsonFile = env.ContentRootPath + @"\DataJson\UserInfo.json";
            GlobalValues.chatMessagejsonFile = env.ContentRootPath + @"\DataJson\ChatMessage.json";
            GlobalValues.groupChatjsonFile = env.ContentRootPath + @"\DataJson\GroupMembers.json";
            GlobalValues.unsendMessagesjsonFile = env.ContentRootPath + @"\DataJson\MessagesOfDisconnectedUsers.json";
            GlobalValues.TempChatMessagejsonFile = env.ContentRootPath + @"\DataJson\TempChatMessage.json";
            GlobalValues.BackupFileFolder = env.ContentRootPath + @"\wwwroot\ChatBackupFolder\";
            GlobalValues.BarcodeFolder = env.ContentRootPath + @"\wwwroot\Images\BarcodePhotos\";
            GlobalValues.ChatPhotosFolder = env.ContentRootPath + @"\wwwroot\Images\ChatPhotos\";
            GlobalValues.DocumentFolder = env.ContentRootPath + @"\wwwroot\Document\";
        }
    }
}
