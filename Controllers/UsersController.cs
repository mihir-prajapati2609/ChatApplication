using ChatApplication.Common;
using ChatApplication.Models.Users;
using ChatApplication.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using ChatApplication.Models.Message;

namespace ChatApplication.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _user;

        private readonly IBarcodeService _barcode;

        public UsersController(IUserService user, IBarcodeService kapan)
        {
            _user = user;
            _barcode = kapan;
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<dynamic> GetUsers()
        {
            try
            {
                return await _user.GetAllUsers();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> AddUser(User model)
        {
            try
            {
                return await _user.AddUser(model); 
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> UpdateDeviceIdToken(string deviceId, int userId)
        {
            try
            {
                return await _user.updateDeviceIdToken(deviceId, userId);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> Logout(User user)
        {
            try
            {
                return await _user.Logout(user);
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> UpdateUser(User user)
        {
            try
            {
                return await _user.UpdateUser(user);
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> CheckAuthentication(User user)
        {
            try
            {
                return await _user.CheckAuthentication(user);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> CheckAccessToken(User user)
        {
            try
            {
                return await _user.CheckAccessToken(user);
            }
            catch (Exception)
            {

                throw;
            }
        }

        //User Wants to Block another User 
        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> BlockUser(int UserId, string UserChatId)
        {
            try
            {
                return await _user.BlockUser(UserId, UserChatId);
            }
            catch (Exception ex)
            {

                throw ex;
            }
             
        }


        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> CheckIsUserBlocked(int logedUserId, int RecieverUserId, string UserChatId)
        {
            try
            {
                return await _user.CheckIsUserBlocked(logedUserId, RecieverUserId, UserChatId);
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> UnblockUser(int logedUserId, string UserChatId)
        {
            try
            {
                return await _user.UnblockUser(logedUserId, UserChatId);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task SendBlockUserNotification(int UserId, int FromBockedUserId)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(GlobalValues.BaseUrl);
                HttpResponseMessage Res = await client.GetAsync("Home/SendBlockUserNotification?UserId=" + UserId + "&FromBlockedUserId=" + FromBockedUserId);
            }
        }


        #region THIS IS FOR KAPAN MODULE
        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> GetKapanImages(string KapanNo, int UserId)
        {
            try
            {
                return await _barcode.GetBarcodeImages(KapanNo, UserId);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> StoreKapanPictures(Message model)
        {
            try
            {
                return await _barcode.StoreBarcodePictures(model);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<dynamic> CheckKapanNo(string KapanNo)
        {
            try
            {
                return await _barcode.CheckBarcodeNo(KapanNo);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<dynamic> GetMachineLists(string Machine, int UserId)
        {
            try
            {
                return await _barcode.GetMachineLists(Machine, UserId);
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        #endregion


    }
}
