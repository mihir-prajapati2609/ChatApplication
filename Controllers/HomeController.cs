using ChatApplication.Common;
using ChatApplication.Data.DataContext;
using ChatApplication.Hubs;
using ChatApplication.Models.Users;
using ChatApplication.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using static ChatApplication.Common.Structure;

namespace ChatApplication.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IHubContext<chatviewhub> _hubContext;
        List<User> ConnectedUsers = GlobalValues.ConnectedUsers;
        private readonly IUserData _user;
        private readonly IUserService _userService;


        public HomeController(IHubContext<chatviewhub> hubContext, IUserData user, IUserService userService)
        {
            _hubContext = hubContext;
            _user = user;
            _userService = userService;
        }

        [HttpGet]
        [Route("[action]")]
        public async Task SendUserChatList(int UserId)
        {
            User user = new User();
            if (ConnectedUsers.Any(m => m.UserId == UserId) == true)
            {
                user = ConnectedUsers.Where(m => m.UserId == UserId).FirstOrDefault();
                await _hubContext.Clients.Client(user.ConnectionId).SendAsync("NewMessage", "GetChatList");
            }
        }

        [HttpGet]
        [Route("[action]")]
        public async Task SendBlockUserNotification(int UserId, int FromBlockedUserId)
        {
            var user = new User();
            string x = "UserBlocked_" + FromBlockedUserId.ToString();
            if (ConnectedUsers.Any(m => m.UserId == UserId) == true)
            {
                user = ConnectedUsers.Where(m => m.UserId == UserId).FirstOrDefault();

                await _hubContext.Clients.Client(user.ConnectionId).SendAsync("NewMessage", x);
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> GetOTPForForgetPassword(string ContactNo)
        {
            #region Declaration
            var accountSid = "YOUR_AC_SID";
            var authToken = "YOUR_AUTH_TOKEN";
            var user = new User();
            dynamic ReturnValue;
            var random = new Random();
            #endregion

            user = await _user.GetUserDetailByContactNumber(ContactNo);

            if (user != null)
            {
                ContactNo = "+358" + ContactNo;
                TwilioClient.Init(accountSid, authToken);
                var RandomNumber = random.Next(1000, 9999);
                try
                {
                    var message = MessageResource.Create(
                    body: "your OTP verification number is " + RandomNumber,
                    from: new Twilio.Types.PhoneNumber("YOUR PHONE NUMBER"),
                    to: new Twilio.Types.PhoneNumber(ContactNo)
                     );

                }
                catch (Exception ex)
                {

                    throw ex;
                }

                await _userService.AddOtpHistory(user.UserId, RandomNumber);
  
                ReturnValue = new { Success = true, Message = "OTP has been Sent to registered Mobile Number" };

            }
            else
            {
                ReturnValue = new { Success = false, Message = "Invalid Phone Number" };
            }
            return ReturnValue;

        }

        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> VerifyOTP(string MobileNumber, int OTP)
        {
            #region Declaration
            dynamic ReturnValue;
            #endregion

            var user = await _user.GetUserDetailByContactNumber(MobileNumber);

            if (user != null)
            {
                var data = await _user.GetOtpDetailByUserIdAndOtp(user.UserId, OTP);

                var status = data.RequestOTPTimeStamp;
                  
                var diff1 = DateTime.Now.Subtract(status);

                if (diff1.TotalMinutes > 5)
                {
                    ReturnValue = new { Success = false, Message = "Please try again. Exceeded time limit" };
                }
                else
                {
                    ReturnValue = new { Success = true, Message = "OTP Veified" };
                }
            }
            else
            {
                ReturnValue = new { Success = false, Message = "Invalid Phone Number" };
            }
                    
            //delete Otp from Database
            await _user.DeleteOtpByUserId(user.UserId);

            return ReturnValue;
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> UpdatePassword(string MobileNumber, string Password)
        {

            #region Declaration
            dynamic ReturnValue;
            #endregion

            var user = await _user.GetUserDetailByContactNumber(MobileNumber);
            if (user != null)
            {
                user.ContactNo = MobileNumber.ToString();
                user.Password = Password;
                await _user.EditUser(user);

                ReturnValue = new { Success = true, Message = "Password has been Updated" };

                return ReturnValue;
            }
            else
            {
                return ReturnValue = new { Success = false, Message = "Invalid Phone Number" };
            }
        }

        [HttpGet]
        [Route("[action]")]
        public dynamic GetChatBackupFile(int UserId)
        {
            #region Declaration
            dynamic ReturnValue;

            var Userjson = GlobalFunctions.GetJsonObject(GlobalValues.chatUserInfojsonFile);
            var UserjsonObj = JObject.Parse(Userjson);
            JArray Users = (JArray)UserjsonObj["Users"];

            var Messagejson = GlobalFunctions.GetJsonObject(GlobalValues.chatMessagejsonFile);
            var MessagejsonObj = JObject.Parse(Messagejson);
            var Messages = (JArray)MessagejsonObj["ChatMessages"];

            JArray FinalJsonFile = new JArray();
            var FileName = UserId + "_" + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".txt";

            var FilePath = GlobalValues.BackupFileFolder + FileName;
            #endregion

            if (UserId == 0)
            {
                UserId = 1;
            }

            try
            {
                foreach (var user in Users.Where(obj => obj[DataJson.USER_ID].Value<int>() == UserId))
                {
                    var ObjuserChatId = user[DataJson.USER_CHAT_ID];
                    foreach (var chat1 in ObjuserChatId)
                    {
                        string obj1 = chat1.Value<string>();
                        foreach (var message in Messages.Where(obj => obj[DataJson.USER_CHAT_ID].Value<string>() == obj1))
                        {
                            var messages = message[DataJson.MESSAGES];
                            foreach (var messageobj in messages)
                            {
                                FinalJsonFile.Add(messageobj);
                            }
                        }
                    }
                    var PChatMsgTmpJsonResult = JsonConvert.SerializeObject(FinalJsonFile,
                                             Formatting.Indented);

                    var desEncrypted = GlobalFunctions.EncryptStreamDES(PChatMsgTmpJsonResult);

                    GlobalFunctions.WriteInJson(FilePath, desEncrypted);

                    return ReturnValue = new { Success = true, FileName };
                }

                ReturnValue = new { Success = false, FileName };
            }
            catch (Exception ex)
            {
                ReturnValue = new { Success = false, Message = GlobalFunctions.GetErrorMessageFromException(ex) };
            }

            return ReturnValue;
        }


    }
}
