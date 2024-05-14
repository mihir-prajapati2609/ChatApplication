using ChatApplication.Data.DataContext;
using ChatApplication.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApplication.Services
{
    public partial class DependencyRegistrar : IDependencyRegistrar
    {
        /// <summary>
        /// Register services and interfaces
        /// </summary>
        /// <param name="service">Container builder</param>
        public virtual void Register(IServiceCollection service)
        {
            //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.2#service-lifetimes

            //Scoped (Add Scoped dependencies only when it needs to be called frequently. For e.g. UserProfile)


            #region Custom Factory
            service.AddScoped<IAdminService, AdminService>();
            service.AddScoped<IUserService, UserService>();
            service.AddScoped<IChatViewHubService, ChatViewHubService>();
            service.AddScoped<IKapanService, BarcodeService>();
            service.AddScoped<IBarcodeData, BarcodeData>();
            service.AddScoped<IUserData, UserData>();
            #endregion
        }

        /// <summary>
        /// Order of this dependency registrar implementation
        /// </summary>
        public int Order
        {
            get { return 2; }
        }
    }
}
