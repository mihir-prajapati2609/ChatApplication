using ChatApplication.Common;
using ChatApplication.Data.DataContext;
using ChatApplication.Models.OtpHistory;
using ChatApplication.Models.Users;
using CustomMVCClassLibraries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static ChatApplication.Common.GlobalValues;

namespace ChatApplication.Services
{

    public interface IUserService
    {
        Task<dynamic> GetAllUsers();
        Task<dynamic> CheckAuthentication(User model);
        Task<dynamic> CheckAccessToken(User model);
        Task<dynamic> Logout(User model);
        Task<dynamic> AddUser(User model);
        Task<dynamic> UpdateUser(User model);
        Task<dynamic> updateDeviceIdToken(string deviceId, int userId);
        Task<dynamic> BlockUser(int UserId, string UserChatId);
        Task<dynamic> CheckIsUserBlocked(int logedUserId, int RecieverUserId, string UserChatId);
        Task<dynamic> UnblockUser(int logedUserId, string UserChatId);

        //OTP
        Task<dynamic> AddOtpHistory(int userId, int otp);
    }

    public class UserService : IUserService
    {
        #region Data members
        private readonly IUserData _user;
        #endregion

        #region Ctor
        public UserService(IUserData user)
        {
            _user = user;
        }
        #endregion

        public async Task<dynamic> GetAllUsers()
        {
            dynamic ReturnValue;

            try
            {
                var result = new List<User>();
                var data = await _user.GetAllUsers();

                return ReturnValue = new { Success = true, Data = data };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<dynamic> CheckAuthentication(User model)
        {
            dynamic ReturnValue;

            var LF = new LibraryFunctions();

            var user = new User();

            var passWord = LF.Encrypt(model.Password);

            dynamic Data;

            bool Success;

            string message = "";

            try
            {
                user = await _user.GetUserByUserNameAndPassword(model.UserName, passWord);

                if (user == null)
                {
                    Success = false;
                    Data = new { UserId = (string)null, IsAdmin = false };
                    message = "Invalid Credentials";

                    return ReturnValue = new { Success = true, Data = user, message };
                }
                else
                {
                    Success = true;

                    if (user.IsActivated == false && user.IsApproved == true)
                    {
                        Success = false;
                        message = "Your account is blocked temporarily";
                    }
                    if (user.IsApproved == false && user.IsActivated == false)
                    {
                        Success = false;
                        message = "Your Account is not activated yet";
                    }
                    if (user.IsRejected == true)
                    {
                        Success = false;
                        message = "Your Request for Account is Rejected please Contact Admin";
                    }
                    if (ConnectedUsers.Any(m => m.UserId == user.UserId) == true)
                    {
                        Success = false;
                        message = "Already Logged in from another Device";
                    }

                    if (user.DeviceId != null)
                    {
                        user.DeviceId = model.DeviceId;
                        user.AccessToken = Guid.NewGuid().ToString();
                        await _user.EditUser(user);
                    }

                    return ReturnValue = new { Success = Success, Data = user, Message = message };
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<dynamic> CheckAccessToken(User model)
        {
            var data = await _user.GetUserByUserNameAndPassword(model.UserName, model.Password);

            return new { loggedIn = (data.AccessToken != null) };
        }

        public async Task<dynamic> updateDeviceIdToken(string deviceId, int userId)
        {
            dynamic ReturnValue;

            if (deviceId != null)
            {
                var existing = await _user.GetTrackedUserById(userId);
                if (existing is null)
                    throw new Exception("User not found");

                existing.DeviceId = deviceId;
                await _user.EditUser(existing);
            }

            return ReturnValue = new { Success = true };
        }

        public async Task<dynamic> Logout(User model)
        {

            try
            {

                var user = await _user.GetTrackedUserById(model.UserId);

                if (user is null)
                    throw new Exception("User not found");

                user.DeviceId = null;
                await _user.EditUser(user);

                return new { Success = true, Message = "Log out Succesful" };
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<dynamic> AddUser(User model)
        {
            var LF = new LibraryFunctions();

            dynamic ReturnValue;

            var Data = model;

            if (model == null)
            {
                throw new Exception();
            }
            try
            {
                model.ClientId = 1;
                model.IsActivated = false; //change this to false when Apple apllication approved
                model.IsApproved = false; //change this to false when Apple apllication approved
                model.IsAdmin = false;
                model.IsRejected = false;
                model.Password = LF.Encrypt(model.Password);

                bool isUserNameUnique = await _user.IsUserNameExist(model.UserName);

                bool isContactNoExist = await _user.IsContactNoExist(model.ContactNo);

                if (isUserNameUnique)
                {
                    return ReturnValue = new { Success = false, Data = Data, Message = "Username Already Exist" };
                }

                else if (isContactNoExist)
                {
                    return ReturnValue = new { Success = false, Data = Data, Message = "Contact No Already Exist" };
                }

                if (isUserNameUnique && isContactNoExist)
                {
                    if (model.IsActivated == false && model.IsApproved == false)
                    {
                        ReturnValue = new { Success = true, Data = Data, Message = "Contact To Admin For Register" };
                    }

                    await _user.AddUser(model);

                    var user = await _user.GetUserByUserNameAndPassword(model.UserName, model.Password);

                    return ReturnValue = new { Success = true, Data = user };
                }
                else
                {
                    return ReturnValue = new { Success = false, Data = Data, Message = "Please Enter Unique Credentials" };
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        public async Task<dynamic> UpdateUser(User model)
        {
            dynamic ReturnValue;

            try
            {
                var user = _user.GetTrackedUserById(model.UserId);

                if (user != null)
                {
                    var existing = await _user.GetTrackedUserById(model.UserId);
                    if (existing is null)
                        throw new Exception("User not found");

                    existing.UserName = model.UserName;
                    existing.FullName = model.FullName;
                    existing.Address = model.Address;
                    existing.Email = model.Email;
                    existing.ContactNo = model.ContactNo;
                    await _user.EditUser(existing);

                    return ReturnValue = new { Success = true, Message = "User has been Updated" };
                }
                else
                {
                    return ReturnValue = new { Success = false, Message = "" };
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<dynamic> BlockUser(int UserId, string UserChatId)
        {
            var Userjson = GlobalFunctions.GetJsonObject(GlobalValues.chatUserInfojsonFile);
            var UserjsonObj = JObject.Parse(Userjson);
            JArray Users = (JArray)UserjsonObj["Users"];
            bool IsblockedUserChatIDExist = false;
            bool Success = false;
            var Message = "";
            dynamic ReturnValue;

            try
            {
                if (UserId > 0)
                {
                    foreach (var user in Users.Where(obj => obj["UserId"].Value<int>() == UserId))
                    {
                        var BlockedUserChatId = user["BlockedUserChatId"];
                        foreach (var blockchat in BlockedUserChatId)
                        {
                            var obj = blockchat.Value<string>();
                            if (obj == UserChatId)
                            {
                                IsblockedUserChatIDExist = true;
                                Success = false;
                                Message = "User Already Blocked";
                            }
                        }
                        if (IsblockedUserChatIDExist == false)
                        {
                            var chat = UserChatId;
                            Success = false;
                            ((JArray)BlockedUserChatId).Add(chat);
                            Success = true;
                            Message = "User Blocked";
                            var x = GetUserIdFromUserchatId(UserId, UserChatId);
                            await SendBlockUserNotification(x, UserId);
                        }
                    }
                }
                UserjsonObj["Users"] = Users;
                string userJsonResult = JsonConvert.SerializeObject(UserjsonObj,
                                       Formatting.Indented);
                GlobalFunctions.WriteInJson(GlobalValues.chatUserInfojsonFile, userJsonResult);
            }
            catch (Exception ex)
            {
                Success = false;
                Message = ex.ToString();
            }

            return ReturnValue = new { Success = true, Message = Message };
        }

        public async Task<dynamic> CheckIsUserBlocked(int logedUserId, int RecieverUserId, string UserChatId)
        {
            #region
            dynamic ReturnValue;

            var Userjson = GlobalFunctions.GetJsonObject(GlobalValues.chatUserInfojsonFile);
            var UserjsonObj = JObject.Parse(Userjson);
            JArray Users = (JArray)UserjsonObj["Users"];
            bool Blocked = false;
            string Message = "";
            int UserId = 0;
            #endregion
            if (logedUserId > 0 && RecieverUserId > 0)
            {
                foreach (var user in Users.Where(obj => obj["UserId"].Value<int>() == logedUserId))
                {
                    var BlockedUserChatId = user["BlockedUserChatId"];
                    foreach (var blockchat in BlockedUserChatId)
                    {
                        var obj = blockchat.Value<string>();
                        if (obj == UserChatId)
                        {
                            Blocked = true;
                            Message = "You have blocked this User";
                            UserId = logedUserId;
                        }
                    }
                }
                foreach (var user in Users.Where(obj => obj["UserId"].Value<int>() == RecieverUserId))
                {
                    var BlockedUserChatId = user["BlockedUserChatId"];
                    foreach (var blockchat in BlockedUserChatId)
                    {
                        var obj = blockchat.Value<string>();
                        if (obj == UserChatId)
                        {
                            Blocked = true;
                            Message = "This User has blocked you";
                            UserId = RecieverUserId;
                        }
                    }
                }
            }

            ReturnValue = new { Blocked = Blocked, Message = Message, UserId = UserId };
            return ReturnValue;
        }

        public async Task<dynamic> UnblockUser(int logedUserId, string UserChatId)
        {
            #region
            dynamic ReturnValue;

            var Userjson = GlobalFunctions.GetJsonObject(GlobalValues.chatUserInfojsonFile);
            var UserjsonObj = JObject.Parse(Userjson);
            JArray Users = (JArray)UserjsonObj["Users"];
            bool Blocked = false;
            string Message = "";
            #endregion

            if (logedUserId > 0)
            {
                foreach (var user in Users.Where(obj => obj["UserId"].Value<int>() == logedUserId))
                {
                    //var BlockedUserChatId = user["BlockedUserChatId"];
                    var BlockedUserChatId = (JArray)user["BlockedUserChatId"];

                    var userindex = -1;
                    foreach (var blockchat in BlockedUserChatId)
                    {
                        var obj = blockchat.Value<string>();
                        if (obj == UserChatId)
                        {
                            userindex = BlockedUserChatId.IndexOf(blockchat);
                        }
                    }
                    BlockedUserChatId.RemoveAt(userindex);
                    user["BlockedUserChatId"] = BlockedUserChatId;
                }
                var x = GetUserIdFromUserchatId(logedUserId, UserChatId);
                await SendBlockUserNotification(x, logedUserId);
                Blocked = false;
                Message = "you have unblocked this user";
                UserjsonObj["Users"] = Users;
                string userJsonResult = JsonConvert.SerializeObject(UserjsonObj,
                                       Formatting.Indented);
                GlobalFunctions.WriteInJson(GlobalValues.chatUserInfojsonFile, userJsonResult);
            }

            ReturnValue = new { Blocked = Blocked, Message = Message, UserId = logedUserId };
            return ReturnValue;
        }

        #region OTP
        public async Task<dynamic> AddOtpHistory(int userId, int otp)
        {
            try
            {
                var otpHistory = new OtpHistory
                {
                    OTP = otp,
                    RequestOTPTimeStamp = DateTime.Now,
                    UserId = userId,
                    VerfiedStatus = false
                };

                return await _user.AddOtpHistory(otpHistory);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Private Methods
        private async Task SendBlockUserNotification(int UserId, int FromBockedUserId)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(GlobalValues.BaseUrl);
                HttpResponseMessage Res = await client.GetAsync("Home/SendBlockUserNotification?UserId=" + UserId + "&FromBlockedUserId=" + FromBockedUserId);
            }
        }

        private int GetUserIdFromUserchatId(int UserId, string UserChatId)
        {
            int ToUserId;
            var LastFour = UserChatId.Substring(UserChatId.Length - 4, 4);
            var firstFour = UserChatId.Substring(1, UserChatId.Length - 5);

            if (int.Parse(firstFour) == UserId)
            {
                ToUserId = int.Parse(LastFour);
            }
            else
            {
                ToUserId = int.Parse(firstFour);
            }

            return ToUserId;
        }
        #endregion

    }

}

