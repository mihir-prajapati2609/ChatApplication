using ChatApplication.Common;
using ChatApplication.Data;
using ChatApplication.Data.DataContext;
using ChatApplication.Models.Message;
using ChatApplication.Models.Notification;
using ChatApplication.Models.Users;
using ChatApplication.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static ChatApplication.Common.Structure;

namespace ChatApplication.Hubs
{
    public class chatviewhub : Hub
    {
        #region DECLARATIONS
        private readonly IChatViewHubService _hubService;
        List<User> ConnectedUsers = GlobalValues.ConnectedUsers;

        private IHubContext<chatviewhub> HubContext;
        private IUserData _user;
        private MyDbContext _db;
        #endregion

        public chatviewhub(IChatViewHubService hubService, IHubContext<chatviewhub> hubContext, IUserData user, MyDbContext db)
        {
            _hubService = hubService;
            HubContext = hubContext;
            _user = user;
            _db = db;
        }


        #region CONNECTION METHODS
        //on connection of SignalR
        public override async Task OnConnectedAsync()
        {
            try
            {
                var accessToken = Context.GetHttpContext().Request.Query["access_token"].ToString();

                var user = _db.Users.AsNoTracking().Where(m => m.AccessToken == accessToken).FirstOrDefault();

                if (user != null)
                {
                    await RegisterUserOnSignalR(user.UserId);
                }
            }
            catch (Exception ex)
            {
                GlobalFunctions.saveErrorMessage(ex, "OnConnectedAsync");
                throw;
            }

            await base.OnConnectedAsync();

        }

        //on deconnection of SignalR
        public override async Task OnDisconnectedAsync(Exception ex)
        {
            //RemoveUserfromConnectedUser

            var user = new User();
            user = ConnectedUsers.Find(u => u.ConnectionId == Context.ConnectionId);
            if (user != null)
            {
                await _hubService.RemoveUserfromConnectedUser(user.UserId);
            }

            await base.OnDisconnectedAsync(ex);
        }

        //Register User to get Message
        public async Task RegisterUserOnSignalR(int UserId)
        {
            var user = new User();

            if (ConnectedUsers.Any(m => m.UserId == UserId) == false)
            {
                user = await _user.GetTrackedUserById(UserId);

                user.ConnectionId = Context.ConnectionId;
                ConnectedUsers.Add(user);
                await RegisterGroup(user.UserId);
                await GetChatList(user.UserId);
                await getTempMessage(user.UserId);
            }
            else
            {
                ConnectedUsers.Find(u => u.UserId == UserId).ConnectionId = Context.ConnectionId;
                user = ConnectedUsers.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
                await RegisterGroup(user.UserId);
                await GetChatList(user.UserId);
                await getTempMessage(user.UserId);
            }
        }

        // Register User in SignalR Group
        public async Task RegisterGroup(int UserId)
        {
            #region Declarations
            var GroupUserjson = GlobalFunctions.GetJsonObject(GlobalValues.groupChatjsonFile);
            var GroupUserjsonObj = JObject.Parse(GroupUserjson);
            var groups = (JArray)GroupUserjsonObj[DataJson.GROUPS];
            UsersGroup UG = new UsersGroup();
            #endregion

            try
            {
                if (UserId > 0)
                {
                    var GroupUser = ConnectedUsers.FirstOrDefault(x => x.UserId == UserId);
                    foreach (var group in groups)
                    {
                        var groupUsersList = (JArray)group["Members"];
                        int groupId = group[DataJson.GROUP_ID].Value<int>();
                        foreach (var user in groupUsersList/*.Where(obj => obj.Value<int>() == UserId)*/)
                        {
                            UG = await _user.GetNoTrackedGroupByGroupId(groupId);
                            if (GroupUser != null)
                            {
                                AddOrRemoveGroups(GroupUser.ConnectionId, UG.GroupId.ToString(), false);
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //Add or remove User to group   
        public async Task AddOrRemoveGroups(string ConnectionId, string GroupId, bool IsRemove = false)
        {
            try
            {
                if (IsRemove == false)
                {
                    await Groups.AddToGroupAsync(ConnectionId, GroupId);
                }
                else
                {
                    await Groups.RemoveFromGroupAsync(ConnectionId, GroupId);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //Get Ping From Client Side for Connection Checking 
        public dynamic GetPing(int UserId)
        {

            dynamic ReturnValue;

            try
            {
                var connectedUser = ConnectedUsers.FirstOrDefault(x => x.UserId == UserId);

                if (connectedUser != null)
                {
                    return ReturnValue = new { Success = true };
                }
                else
                {
                    return ReturnValue = new { Success = false };
                }
            }
            catch (Exception ex)
            {
                GlobalFunctions.saveErrorMessage(ex, "GetPing");
                throw;
            }
        }
        #endregion

        #region MESSAGING METHODS
        //Send Message for one to one
        public async Task SendPrivateMessage(Message model)
        {
            var user = await _user.GetTrackedUserById(model.ToUserId);

            var FromUser = await _user.GetTrackedUserById(model.FromUserId);

            Notification notification  = new Notification()
            {
                IsAndoridDevice = true,
                DeviceId = user.DeviceId,
                Body = model.Text,
                Title = FromUser.FullName,
                UserChatId = model.UserChatId
            };

            try
            {
                var toUser = ConnectedUsers.FirstOrDefault(x => x.UserId == model.ToUserId);
                var fromUser = ConnectedUsers.FirstOrDefault(x => x.UserId == model.FromUserId);
                if (fromUser == null)
                {
                    await RegisterUserOnSignalR(model.FromUserId);
                    fromUser = ConnectedUsers.FirstOrDefault(x => x.UserId == model.FromUserId);
                }
                model.ServerDateTime = DateTime.Now;
                model.SentDateTime = DateTime.Now;
                model.Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                model.FromUserName = fromUser.FullName;
                model.ToUserName = user.FullName;
                var FromUserId = string.Format("{0:D4}", model.FromUserId);
                var ToUserId = string.Format("{0:D4}", model.ToUserId);
                try
                {
                    _hubService.SaveMediaLink(model);
                }
                catch (Exception ex)
                {                
                    throw;
                }

                _hubService.SaveTmpMessage(model);

                SaveMessagetoJson(model);

                //GlobalValues.printMessage("Save Completed", M);

                if (toUser != null)
                {
                    if (user.DeviceId != null)
                    {
                        await _hubService.SendNotifcation(notification);
                    }

                    await Clients.Client(toUser.ConnectionId).SendAsync("receivemessage", model);
                    await Clients.Client(fromUser.ConnectionId).SendAsync("receivemessage", model);

                }
                else
                {
                    await Clients.Client(fromUser.ConnectionId).SendAsync("receivemessage", model);
                    await SaveMessageOfDisconnectedUsers(model);
                }
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        //Send Message to group
        public async Task SendMessageToGroup(Message model)
        {
            var userGroup = await _user.GetNoTrackedGroupByGroupId(model.GroupId);
            User user = await _user.GetTrackedUserById(model.FromUserId);

            model.ServerDateTime = DateTime.Now;
            model.SentDateTime = DateTime.Now;
            model.FromUserName = user.FullName;
            model.ToUserName = userGroup.GroupName;

            var Noti = new Notification();

            var UserUds = userGroup.UserIds;
            var UserIdLists = UserUds.Split(',');
            try
            {
                _hubService.SaveMediaLink(model);
            }
            catch (Exception ex)
            {
                throw;
            }

            SaveMessagetoJson(model);

            foreach (var y in UserIdLists)
            {
                var Id = int.Parse(y);

                var toUser = ConnectedUsers.FirstOrDefault(x => x.UserId == Id);

                if (model.FromUserId == Id)
                {
                    continue;
                }

                else if (toUser != null)
                {
                    Noti.IsAndoridDevice = true;
                    Noti.DeviceId = toUser.DeviceId;
                    Noti.Body = model.Text;
                    Noti.Title = user.FullName;
                    Noti.UserChatId = model.UserChatId;

                    if (user.DeviceId != null)
                    {
                        await _hubService.SendNotifcation(Noti);
                    }
                }
            }

            _hubService.SaveTmpMessage(model);

            await Clients.Group(model.GroupId.ToString()).SendAsync("receivemessagefromgroup", model);
        }
        #endregion

        #region PRIVATE METHODS
        //Get User wise Chat List
        public async Task GetChatList(int UserID)
        {
            try
            {
                #region Declarations
                ////var Userjson = GlobalValues.GetJsonObject(GlobalValues.chatUserInfojsonFile);
                var Userjson = GlobalFunctions.GetJsonObject(GlobalValues.chatUserInfojsonFile);
                var UserjsonObj = JObject.Parse(Userjson);
                var Users = (JArray)UserjsonObj[DataJson.USERS];

                var GroupUserjson = GlobalFunctions.GetJsonObject(GlobalValues.groupChatjsonFile);
                var GroupUserjsonObj = JObject.Parse(GroupUserjson);
                var Groups = (JArray)GroupUserjsonObj[DataJson.GROUPS];

                string ReturnValue = "";
                var mainUser = new User();
                var userGroup = new UsersGroup();

                #endregion
                var toUser = ConnectedUsers.FirstOrDefault(x => x.UserId == UserID);
                var UserList = new List<dynamic>();

                foreach (var user in Users.Where(obj => obj[DataJson.USER_ID].Value<int>() == UserID))
                {
                    var usersList = (JArray)user[DataJson.USER_CHAT_ID];

                    foreach (var chatuser in usersList)
                    {
                        var lastmsgdatetime = _hubService.GetLastMessageIdDateTime(chatuser.Value<string>());

                        long yourDateTimeMilliseconds = new DateTimeOffset(lastmsgdatetime).ToUnixTimeMilliseconds();

                        if (chatuser.Value<string>().StartsWith('P'))
                        {
                            //var lastmsgdatetime = DateTime.Now;
                            var FrommessageUserId = _hubService.GetUserIdFromUserchatId(UserID, chatuser.Value<string>());

                            mainUser = await _user.GetTrackedUserById(FrommessageUserId);

                            var userName = mainUser.FullName;

                            var anonymousObject = new
                            {
                                Id = FrommessageUserId,
                                Name = userName,
                                LastMessageDatetime = yourDateTimeMilliseconds,
                                Type = "Private Chat",
                                UserChatId = chatuser.Value<string>()
                            };
                            UserList.Add(anonymousObject);
                            UserList = UserList.OrderBy(anonymousObject => anonymousObject.LastMessageDatetime).ToList();
                        }
                        else
                        {
                            await RegisterGroup(UserID);
                            foreach (var group in Groups.Where(obj => obj[DataJson.USER_CHAT_ID].Value<string>() == chatuser.Value<string>()))
                            {
                                int GroupId = group[DataJson.GROUP_ID].Value<int>();

                                userGroup = await _user.GetNoTrackedGroupByGroupId(GroupId);

                                var anonymousObject = new
                                {
                                    Id = userGroup.GroupId,
                                    Name = userGroup.GroupName,
                                    LastMessageDatetime = yourDateTimeMilliseconds,
                                    Type = "Group Chat",
                                    UserChatId = chatuser.Value<string>()
                                };
                                UserList.Add(anonymousObject);
                                UserList = UserList.OrderBy(anonymousObject => anonymousObject.LastMessageDatetime).ToList();
                            }
                        }
                    }
                    UserList.OrderBy(x => x.LastMessageDatetime);
                }

                if (toUser != null)
                {
                    UserList.Reverse();
                    ReturnValue = JsonConvert.SerializeObject(UserList);
                    await Clients.Client(toUser.ConnectionId).SendAsync("chatList", ReturnValue);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //Send Temp Message While Reciver Turns Online From Offline
        public async Task getTempMessage(int UserId)
        {
            var mainUser = ConnectedUsers.FirstOrDefault(x => x.UserId == UserId);

            if (mainUser != null)
            {
                if (GlobalValues.TempChatMessagejsonFile != null)
                {
                    #region Declaration
                    var TempChatJSON = GlobalFunctions.GetJsonObject(GlobalValues.TempChatMessagejsonFile);
                    var TempChatOBJ = JObject.Parse(TempChatJSON);
                    var TempChatMessages = (JArray)TempChatOBJ[DataJson.PERSONAL_TEMP_CHAT_MESSAGES];

                    var GroupUserjson = GlobalFunctions.GetJsonObject(GlobalValues.groupChatjsonFile);
                    var GroupUserjsonObj = JObject.Parse(GroupUserjson);
                    var Groups = (JArray)GroupUserjsonObj[DataJson.GROUPS];

                    var messageModel = new Message();
                    var userGroup = new UsersGroup();
                    var U = new User();

                    #endregion

                    foreach (var user in TempChatMessages.Where(obj => obj[DataJson.USER_ID].Value<int>() == UserId))
                    {

                        var MessagesList = (JArray)user[DataJson.MESSAGES];
                        var UserChatId = user[DataJson.USER_CHAT_ID];

                        foreach (var msg in MessagesList)
                        {
                            messageModel.Id = (string)msg["MessageId"];
                            messageModel.Text = (string)msg["MessageText"];
                            messageModel.MessageType = (string)msg["MessageType"];
                            messageModel.ServerDateTime = DateTime.Parse((string)msg["ServerDateTime"]);
                            messageModel.Time = (long)msg["Time"];
                            messageModel.FromUserId = (int)msg["SendbyUserId"];
                            messageModel.ToUserId = UserId;
                            messageModel.UserChatId = (string)UserChatId;
                            messageModel.ToUserName = (string)msg["ToUserName"];
                            messageModel.FromUserName = (string)msg["FromUserName"];
                            messageModel.MediaInfos = new List<MediaInfo>();
                            var MediaInfoList = (string)msg["MediaInfos"];

                            var finalMedia = JArray.Parse(MediaInfoList);

                            if (finalMedia != null)
                            {
                                MediaInfo media = new MediaInfo()
                                {
                                    DocumentType = (string)finalMedia[0]["DocumentType"],
                                    FileName = (string)finalMedia[0]["FileName"],
                                    MediaLink = (string)finalMedia[0]["MediaLink"],
                                    MediaText = (string)finalMedia[0]["MediaText"]
                                };

                                messageModel.MediaInfos.Add(media);
                            }

                            messageModel.MessageType = (string)msg["MessageType"];

                            if (messageModel.Id.Substring(0, 1) == "P")
                            {
                                await Clients.Client(mainUser.ConnectionId).SendAsync("receivemessage", messageModel);
                            }
                            if (messageModel.Id.Substring(0, 1) == "G")
                            {
                                foreach (var group in Groups.Where(obj => obj["UserChatId"].Value<string>() == (string)UserChatId))
                                {
                                    messageModel.GroupId = (int)group["GroupId"];
                                }

                                U = await _user.GetTrackedUserById(messageModel.FromUserId);

                                messageModel.FromUserName = U.FullName;
                                await Clients.Client(mainUser.ConnectionId).SendAsync("receivemessagefromgroup", messageModel);
                            }
                        }
                    }
                }
            }
            else
            {
                await getTempMessage(UserId);
            }


        }

        //This is For Delete User Chat List
        public string DeleteChatId(int UserId, string UserChatId)
        {
            var Userjson = GlobalFunctions.GetJsonObject(GlobalValues.chatUserInfojsonFile);
            var UserjsonObj = JObject.Parse(Userjson);
            var Users = (JArray)UserjsonObj[DataJson.USERS];

            dynamic ReturnValue = string.Empty;

            try
            {
                if (UserId != 0)
                {
                    var isUserAvailable = Users.Where(obj => obj[DataJson.USER_ID].Value<int>() == UserId);

                    foreach (var user in isUserAvailable.Where(obj => obj[DataJson.USER_CHAT_ID].Value<string>() == UserChatId))
                    {
                        var chatList = (JArray)user[DataJson.USER_CHAT_ID];
                        int index = -1;
                        foreach (var chatId in chatList.Where(obj => obj[DataJson.USER_CHAT_ID].Value<string>() == UserChatId))
                        {
                            var X = chatId;
                            index = chatList.IndexOf(X);
                        }
                        if (index >= 0)
                        {
                            chatList.RemoveAt(index);
                        }
                    }
                    UserjsonObj["Users"] = Users;
                    string userChatJsonResult = JsonConvert.SerializeObject(UserjsonObj,
                                            Formatting.Indented);

                    GlobalFunctions.WriteInJson(GlobalValues.chatUserInfojsonFile, userChatJsonResult);

                    return ReturnValue = new { Success = true };

                }
                else
                {
                    return ReturnValue = new { Success = false };
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }

        }

        //Send Typing Status to Client
        public async Task DirectTyping(int toUserId, string UserChatId, bool IsTyping)
        {
            try
            {
                var user = ConnectedUsers.FirstOrDefault(x => x.UserId == toUserId);

                if (user != null)
                {
                    await Clients.Client(user.ConnectionId).SendAsync("receiveTyping", UserChatId, IsTyping);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region Acknowledgement Methods
        //send message acknowledgement from reciever to sender
        public async Task MessageAck(Message model)
        {
            try
            {
                if (model.GroupId != 0)
                {
                    _hubService.MessageAckfromServerToReciever(model.ToUserId, model.FromUserId, model.Id, model.UserChatId);
                }
                else
                {
                    _hubService.MessageAckfromServerToReciever(model.ToUserId, model.FromUserId, model.Id, model.UserChatId);
                }
                var fromUser = ConnectedUsers.FirstOrDefault(x => x.UserId == model.FromUserId);
                var touser = ConnectedUsers.FirstOrDefault(x => x.UserId == model.ToUserId);
                if (model.Id.Substring(0, 1) == "P")
                {
                    if (fromUser != null)
                    {
                        await Clients.Client(fromUser.ConnectionId).SendAsync("messagereceived", model);
                    }
                    else
                    {
                        _hubService.SaveUnsendChatAckNotification(model.FromUserId, model.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                GlobalFunctions.saveErrorMessage(ex, "MessageAck");
                throw ex;
            }

        }

        public dynamic GetUnsendAck(int UserId, string MessageId)
        {
            #region Declaration
            var TempChatJSON = GlobalFunctions.GetJsonObject(GlobalValues.TempChatMessagejsonFile);
            var TempChatOBJ = JObject.Parse(TempChatJSON);
            var TempChatMessages = (JArray)TempChatOBJ[DataJson.PERSONAL_TEMP_CHAT_MESSAGES];

            dynamic ReturnValue = null;
            #endregion
            try
            {
                var fromUser = ConnectedUsers.FirstOrDefault(x => x.UserId == UserId);
                foreach (var MessageAck in TempChatMessages.Where(obj => obj[DataJson.USER_ID].Value<int>() == UserId))
                {
                    var Msg = (JArray)MessageAck[DataJson.MESSAGES];

                    if (Msg.Count > 0)
                    {
                        if (Msg.Any(a => a[DataJson.MESSAGE_ID].Value<string>() == MessageId))
                        {
                            return ReturnValue = new { messageId = MessageId, isAckMsgAvailable = true };
                        }

                        return ReturnValue = new { messageId = MessageId, isAckMsgAvailable = false };
                    }
                    else
                    {
                        return ReturnValue = new { messageId = MessageId, isAckMsgAvailable = false };
                    }
                }

                ReturnValue = new { messageId = MessageId, isAckMsgAvailable = false };

            }
            catch (Exception ex)
            {  
                throw ex;
            }

            return ReturnValue;
        }
        #endregion

        #region Saving Methods
        //save Message to Json
        public void SaveMessagetoJson(Message model)
        {
            try
            {
                //Single one to one chat message save
                //Group Message save

                if (model.GroupId != 0)
                {
                    model.UserChatId = _hubService.GetUserChatIdFromGroupId(model.GroupId);
                }

                if (model.UserChatId != null)
                {
                    _hubService.SaveMessageInJson(model);
                }

                if (model.MediaInfos != null)
                {
                    SaveMedia(model);
                }

            }
            catch (Exception ex)
            {            
                throw ex;
            }
        }

        //Create Log File for One to One chat
        public void SaveMedia(Message model)
        {
            #region DECLARATIONS

            string rootforDocument = GlobalValues.DocumentFolder;

            string rootforImages = GlobalValues.ChatPhotosFolder;

            #endregion

            try
            {
                if ((model.MediaInfos ?? new List<MediaInfo>()).Count > 0)
                {
                    for (int i = 0; i < model.MediaInfos.Count; i++)
                    {
                        var MInfo = model.MediaInfos[i];

                        if (MInfo.Media != null)
                        {
                            string subDirectory = rootforImages + "\\" + model.BarcodeNo + "\\" + MInfo.FileName;

                            byte[] fileBytes = Convert.FromBase64String(MInfo.Media);

                            if (fileBytes.Count() > 0)
                            {
                                string FUfilePath = "";

                                if (MInfo.DocumentType == FileTypes.IMAGE)
                                {
                                    FUfilePath = rootforImages + "\\" + MInfo.FileName + model.Time + ".jpg";

                                    MInfo.Media = null;
                                }

                                else if (MInfo.DocumentType == FileTypes.PDF || MInfo.DocumentType == FileTypes.DOCX)
                                {
                                    if (Directory.Exists(rootforDocument))
                                    {
                                        string fileName;
                                        if (MInfo.DocumentType == FileTypes.PDF)
                                        {
                                            fileName = model.Id.ToString() + "_" + (i + 1).ToString() + ".pdf";
                                        }
                                        else
                                        {
                                            fileName = model.Id.ToString() + "_" + (i + 1).ToString() + ".docx";
                                        }

                                        FUfilePath = rootforDocument + "\\" + fileName;
                                    }
                                }

                                File.WriteAllBytes(FUfilePath, fileBytes);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        //save message of offline Users
        public async Task SaveMessageOfDisconnectedUsers(Message model)
        {
            #region Declarations
            var user = await _user.GetTrackedUserById(model.ToUserId);

            var FromUser = await _user.GetTrackedUserById(model.FromUserId);


            Notification N = new Notification()
            {
                IsAndoridDevice = true,
                DeviceId = user.DeviceId,
                Body = model.Text,
                Title = FromUser.FullName,
                UserChatId = model.UserChatId
            };

            #endregion
            try
            {
                //Single one to one chat message save
                //Group Message save

                if (model.GroupId != 0)
                {
                    model.UserChatId = _hubService.GetUserChatIdFromGroupId(model.GroupId);
                }
                if (model.UserChatId != null)
                {
                    //userList Json
                    await _hubService.SaveUserchatIdInUserInfo(model.FromUserId, model.ToUserId, model.UserChatId);

                    if (model.MediaInfos != null)
                    {
                        SaveMedia(model);
                    }

                    if (user.DeviceId != null)
                    {
                        await _hubService.SendNotifcation(N);
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        #endregion

    }
}
