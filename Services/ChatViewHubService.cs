using ChatApplication.Common;
using ChatApplication.Hubs;
using ChatApplication.Models.Message;
using ChatApplication.Models.Notification;
using ChatApplication.Models.Users;
using CorePush.Google;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static ChatApplication.Common.Structure;
using static ChatApplication.Models.Notification.GoogleNotification;

namespace ChatApplication.Services
{
    public interface IChatViewHubService
    {
        Task<Notification> SendNotifcation(Notification model);
        Task RemoveUserfromConnectedUser(int UserId);
        Task SaveUserchatIdInUserInfo(int FromUserId, int ToUserId, string UserChatId);
        void SaveTmpMessage(Message M);
        void SaveMessageInJson(Message M);
        void MessageAckfromServerToReciever(int ToUserId, int FromUserId, string MessageId, string UserChatId);
        void SaveMediaLink(Message model);
        void SaveUnsendChatAckNotification(int FromUserId, string MessageId);
        string GetUserChatIdFromGroupId(int GroupId);
        DateTime GetLastMessageIdDateTime(string UserChatId);
        int GetUserIdFromUserchatId(int UserId, string UserChatId);
    }

    public class ChatViewHubService : IChatViewHubService
    {
        List<User> ConnectedUsers = GlobalValues.ConnectedUsers;

        string rootforImage = GlobalValues.BaseUrl + "images/ChatPhotos/";
        string rootforDoc = GlobalValues.BaseUrl + "Document/";
        private readonly IHubContext<chatviewhub> _hubContext;

        public ChatViewHubService(IHubContext<chatviewhub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task<Notification> SendNotifcation(Notification model)
        {
            try
            {
                if (model.IsAndoridDevice)
                {
                    FcmSettings settings = new FcmSettings()
                    {
                        SenderId = "YOUR_ID",
                        ServerKey = "YOUR_SERVER_KEY"
                    };

                    var authorizationKey = string.Format("keyy={0}", settings.ServerKey);

                    var httpClient = new HttpClient();

                    var deviceToken = model.DeviceId;

                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authorizationKey);

                    httpClient.DefaultRequestHeaders.Accept
                        .Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var dataPayload = new DataPayload();
                    dataPayload.Title = model.Title;
                    dataPayload.Body = model.Body;
                    dataPayload.UserChatId = model.UserChatId;

                    var notification = new GoogleNotification();
                    notification.Data = dataPayload;
                    notification.Notification = dataPayload;

                    var fcm = new FcmSender(settings, httpClient);
                    var fcmSendResponse = await fcm.SendAsync(deviceToken, notification);

                    if (fcmSendResponse.IsSuccess())
                    {
                        return model;
                    }
                    else
                    {
                        return model;
                    }
                }

                return model;
            }
            catch (Exception ex)
            {
                return model;
            }

        }

        public async Task RemoveUserfromConnectedUser(int UserId)
        {
            var user = new User();

            if (ConnectedUsers.Any(m => m.UserId == UserId) == true)
            {
                user = ConnectedUsers.Where(m => m.UserId == UserId).FirstOrDefault();
                bool v = ConnectedUsers.Remove(user);
            }
        }

        public string GetUserChatIdFromGroupId(int GroupId)
        {
            #region Declaration
            //Group Json Save
            var GroupUserjson = GlobalFunctions.GetJsonObject(GlobalValues.groupChatjsonFile);
            var GroupUserjsonObj = JObject.Parse(GroupUserjson);
            var Groups = (JArray)GroupUserjsonObj[DataJson.GROUPS];
            string ReturnValue = null;
            #endregion

            if (GroupId != 0)
            {
                foreach (var group in Groups.Where(obj => obj[DataJson.GROUP_ID].Value<int>() == GroupId))
                {
                    var UserChatId = group[DataJson.USER_CHAT_ID];
                    ReturnValue = (string)UserChatId;
                }
            }
            return ReturnValue;
        }

        //Get Last Message Date from json
        public DateTime GetLastMessageIdDateTime(string UserChatId)
        {
            #region Declarations
            var Messagejson = GlobalFunctions.GetJsonObject(GlobalValues.chatMessagejsonFile);
            var MessagejsonObj = JObject.Parse(Messagejson);
            var Messages = (JArray)MessagejsonObj[DataJson.CHAT_MESSAGES];
            var LastMessageDateTime = DateTime.Now;
            #endregion
            try
            {
                var x = Messages.Where(obj => obj[DataJson.USER_CHAT_ID].Value<string>() == UserChatId);
                foreach (var message in x)
                {
                    var LastMessage = message[DataJson.MESSAGES];
                    var lastMessage = LastMessage.Last;
                    if (lastMessage != null)
                    {
                        var lastMsgDateTime = lastMessage["ServerDateTime"];
                        LastMessageDateTime = DateTime.Parse((string)lastMsgDateTime);
                    }
                }

                return LastMessageDateTime;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public int GetUserIdFromUserchatId(int UserId, string UserChatId)
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

        public void SaveMediaLink(Message model)
        {
            if ((model.MediaInfos ?? new List<MediaInfo>()).Count > 0)
            {
                for (int i = 0; i < model.MediaInfos.Count; i++)
                {
                    var MInfo = model.MediaInfos[i];

                    var DocName = "";

                    if (MInfo.DocumentType == FileTypes.IMAGE)
                    {
                        string fileName = MInfo.FileName + model.Time + ".jpg";

                        MInfo.MediaLink = rootforImage + fileName;
                    }

                    else if (MInfo.DocumentType == FileTypes.PDF)
                    {
                        var name = model.Id.ToString() + "_" + (i + 1).ToString();
                        DocName = name + ".pdf";
                        MInfo.FileName = DocName;
                        MInfo.MediaLink = rootforDoc + DocName;
                    }

                    else if (MInfo.DocumentType == FileTypes.DOCX)
                    {
                        DocName = DocName + ".docx";
                        MInfo.FileName = DocName;
                        MInfo.MediaLink = rootforDoc + DocName;
                    }
                }
            }
        }

        public void SaveTmpMessage(Message model)
        {
            #region Declaration

            var TempChatJSON = GlobalFunctions.GetJsonObject(GlobalValues.TempChatMessagejsonFile);
            var TempChatOBJ = JObject.Parse(TempChatJSON);
            var TempChatMessages = (JArray)TempChatOBJ[DataJson.PERSONAL_TEMP_CHAT_MESSAGES];


            var GroupUserjson = GlobalFunctions.GetJsonObject(GlobalValues.groupChatjsonFile);
            var GroupUserjsonObj = JObject.Parse(GroupUserjson);
            var groups = (JArray)GroupUserjsonObj[DataJson.GROUPS];

            var usersGroup = new UsersGroup();
            #endregion

            //Single one to one chat message save
            //Group Message save

            try
            {
                if (model.GroupId == 0)
                {
                    if (model.UserChatId != null)
                    {
                        //userList Json
                        SaveUserchatIdInUserInfo(model.FromUserId, model.ToUserId, model.UserChatId);

                        //User Messages Json

                        var IsUserChatIDExist = false;
                        foreach (var user in TempChatMessages.Where(obj => obj[DataJson.USER_CHAT_ID].Value<string>() == model.UserChatId)
                            .Where(obj => obj[DataJson.USER_ID].Value<int>() == model.ToUserId))
                        {
                            IsUserChatIDExist = true;
                            var MessagesList = user[DataJson.MESSAGES];

                            var MediaInfo = JsonConvert.SerializeObject(model.MediaInfos);

                            var message = "{'UserChatId': '" + model.UserChatId + "','MessageId': '" + model.Id + "','SendbyUserId':" + model.FromUserId + ",'ServerDateTime': '" + model.ServerDateTime + "','Time': '" + model.Time + "','FromUserName': '" + model.FromUserName + "','ToUserName': '" + model.ToUserName + "','MessageText':'" + model.Text + "','MessageType':'" + model.MessageType + "','BarcodeNo':'" + model.BarcodeNo + "', 'MediaInfos': '" + MediaInfo + "' }";

                            var newChatObj = JObject.Parse(message);
                            ((JArray)MessagesList).Add(newChatObj);
                        }
                        if (IsUserChatIDExist == false)
                        {
                            var MediaInfo = JsonConvert.SerializeObject(model.MediaInfos);

                            var newMessage = "{'UserId':" + model.ToUserId + ",'UserChatId': '" + model.UserChatId + "','Messages': [{'UserChatId': '" + model.UserChatId + "','MessageId': '" + model.Id + "','SendbyUserId':" + model.FromUserId + ",'ServerDateTime': '" + model.ServerDateTime + "','Time': '" + model.Time + "','FromUserName': '" + model.FromUserName + "','ToUserName': '" + model.ToUserName + "','MessageText':'" + model.Text + "','MessageType':'" + model.MessageType + "','KapanNo':'" + model.KapanNo + "','MediaInfos': '" + MediaInfo + "'}]}";

                            var newMessageObj = JObject.Parse(newMessage);
                            TempChatMessages.Add(newMessageObj);
                        }

                    }
                    TempChatOBJ[DataJson.PERSONAL_TEMP_CHAT_MESSAGES] = TempChatMessages;
                }
                else
                {
                    if (model.UserChatId != null)
                    {
                        foreach (var group in groups.Where(obj => obj[DataJson.GROUP_ID].Value<int>() == model.GroupId))
                        {
                            var groupUsersList = (JArray)group["Members"];

                            var groupName = group["ToName"];

                            foreach (var groupuser in groupUsersList)
                            {
                                var x = (int)groupuser;
                                var IsUserChatIDExist = false;
                                if (x != model.FromUserId)
                                {
                                    foreach (var user in TempChatMessages.Where(obj => obj[DataJson.USER_CHAT_ID].Value<string>() == model.UserChatId)
                                          .Where(obj => obj[DataJson.USER_ID].Value<int>() == x))
                                    {
                                        IsUserChatIDExist = true;
                                        var MessagesList = user[DataJson.MESSAGES];

                                        var MediaInfo = JsonConvert.SerializeObject(model.MediaInfos);

                                        var message = "{'UserChatId': '" + model.UserChatId + "','MessageId': '" + model.Id + "','SendbyUserId':" + model.FromUserId + ",'ServerDateTime': '" + model.ServerDateTime + "','Time': '" + model.Time + "','FromUserName': '" + model.FromUserName + "','ToUserName': '" + groupName + "','MessageText':'" + model.Text + "','MessageType':'" + model.MessageType + "','BarcodeNo':'" + model.BarcodeNo + "', 'MediaInfos': '" + MediaInfo + "' }";

                                        var newChatObj = JObject.Parse(message);
                                        ((JArray)MessagesList).Add(newChatObj);
                                    }
                                    if (IsUserChatIDExist == false)
                                    {
                                        var MediaInfo = JsonConvert.SerializeObject(model.MediaInfos);

                                        var newMessage = "{'UserId':" + x + ",'UserChatId': '" + model.UserChatId + "','Messages': [{'UserChatId': '" + model.UserChatId + "','MessageId': '" + model.Id + "','SendbyUserId':" + model.FromUserId + ",'ServerDateTime': '" + model.ServerDateTime + "','Time': '" + model.Time + "','FromUserName': '" + model.FromUserName + "','ToUserName': '" + groupName + "','MessageText':'" + model.Text + "','MessageType':'" + model.MessageType + "','BarcodeNo':'" + model.BarcodeNo + "','MediaInfos': '" + MediaInfo + "'}]}";

                                        var newMessageObj = JObject.Parse(newMessage);
                                        TempChatMessages.Add(newMessageObj);
                                    }
                                }
                                SaveUserchatIdInUserInfo(model.FromUserId, model.ToUserId, model.UserChatId);
                            }
                        }
                    }
                }


                string TmpChatMsgJsonResult = JsonConvert.SerializeObject(TempChatOBJ,
                                        Formatting.Indented);

                GlobalFunctions.WriteInJson(GlobalValues.TempChatMessagejsonFile, TmpChatMsgJsonResult);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void SaveMessageInJson(Message model)
        {
            #region Declaration
            var Messagejson = GlobalFunctions.GetJsonObject(GlobalValues.chatMessagejsonFile);
            var MessagejsonObj = JObject.Parse(Messagejson);
            var Messages = (JArray)MessagejsonObj[DataJson.CHAT_MESSAGES];
            bool IsUserChatIDExist = false;
            #endregion
            try
            {
                foreach (var user in Messages.Where(obj => obj[DataJson.USER_CHAT_ID].Value<string>() == model.UserChatId))
                {
                    IsUserChatIDExist = true;
                    var MessagesList = user[DataJson.MESSAGES];
                    var mediaInfo = JsonConvert.SerializeObject(model.MediaInfos);

                    var message = "{'UserChatId': '" + model.UserChatId + "','MessageId': '" + model.Id + "','SendbyUserId':" + model.FromUserId + "" +
                                 ",'ServerDateTime': '" + model.ServerDateTime + "','Time': '" + model.Time + "'," +
                                 "'FromUserName': '" + model.FromUserName + "','ToUserName': '" + model.ToUserName + "'," +
                                 "'MessageText':'" + model.Text + "','MessageType':'" + model.MessageType + "'" + "," +
                                 "'BarcodeNo':'" + model.BarcodeNo + "', 'MediaInfos': '" + mediaInfo + "' }";

                    var newChatObj = JObject.Parse(message);
                    ((JArray)MessagesList).Add(newChatObj);
                }
                if (IsUserChatIDExist == false)
                {
                    var MediaInfo = JsonConvert.SerializeObject(model.MediaInfos);
                    var newMessage = "{'UserId':" + model.ToUserId + ",'UserChatId': '" + model.UserChatId + "','Messages': [{'UserChatId': '" + model.UserChatId + "','MessageId': '" + model.Id + "','SendbyUserId':" + model.FromUserId + "" +
                                     ",'ServerDateTime': '" + model.ServerDateTime + "','Time': '" + model.Time + "'," +
                                     "'FromUserName': '" + model.FromUserName + "','ToUserName': '" + model.ToUserName + "'," +
                                     "'MessageText':'" + model.Text + "','MessageType':'" + model.MessageType + "'" + "," +
                                     "'BarcodeNo':'" + model.BarcodeNo + "', 'MediaInfos': '" + MediaInfo + "' }]}";
                    var newMessageObj = JObject.Parse(newMessage);
                    Messages.Add(newMessageObj);
                }

                MessagejsonObj[DataJson.CHAT_MESSAGES] = Messages;

                string MessageJsonResult = JsonConvert.SerializeObject(MessagejsonObj,
                                           Formatting.Indented);

                GlobalFunctions.WriteInJson(GlobalValues.chatMessagejsonFile, MessageJsonResult);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void MessageAckfromServerToReciever(int ToUserId, int FromUserId, string MessageId, string UserChatId)
        {
            #region Declaration
            var TempChatJSON = GlobalFunctions.GetJsonObject(GlobalValues.TempChatMessagejsonFile);
            var TempChatOBJ = JObject.Parse(TempChatJSON);
            var TempChatMessages = (JArray)TempChatOBJ[DataJson.PERSONAL_TEMP_CHAT_MESSAGES];
            #endregion
            try
            {
                if (ToUserId != 0 && MessageId != null)
                {
                    foreach (var user in TempChatMessages.Where(obj => obj[DataJson.USER_CHAT_ID].Value<string>() == UserChatId)
                                                         .Where(obj => obj[DataJson.USER_ID].Value<int>() == ToUserId))
                    {
                        var MessagesList = (JArray)user[DataJson.MESSAGES];
                        int index = -1;
                        foreach (var message in MessagesList.Where(obj => obj[DataJson.MESSAGE_ID].Value<string>() == MessageId))
                        {
                            var X = message;
                            index = MessagesList.IndexOf(X);
                        }
                        if (index >= 0)
                        {
                            MessagesList.RemoveAt(index);
                        }
                    }
                    TempChatOBJ[DataJson.PERSONAL_TEMP_CHAT_MESSAGES] = TempChatMessages;

                    string TmpChatMsgJsonResult = JsonConvert.SerializeObject(TempChatOBJ,
                                                  Formatting.Indented);

                    GlobalFunctions.WriteInJson(GlobalValues.TempChatMessagejsonFile, TmpChatMsgJsonResult);
                }
            }
            catch (Exception Ex)
            {
                throw Ex;
            }
        }

        public void SaveUnsendChatAckNotification(int FromUserId, string MessageId)
        {
            #region Declaration
            var unsendMessagesJson = GlobalFunctions.GetJsonObject(GlobalValues.unsendMessagesjsonFile);
            var unsendMessageJsonObj = JObject.Parse(unsendMessagesJson);
            var UnsendChatAckNotification = (JArray)unsendMessageJsonObj[DataJson.UNSEND_CHAT_ACK_NOTIFICATION];
            #endregion

            var IsUserExist = false;
            foreach (var user in UnsendChatAckNotification.Where(obj => obj[DataJson.USER_ID].Value<int>() == FromUserId))
            {
                IsUserExist = true;
                var MessageIdsList = user["MessageIds"];
                ((JArray)MessageIdsList).Add(MessageId);
            }
            if (IsUserExist == false)
            {
                var newMessageAck = "{'UserId':" + FromUserId + ",'MessageIds': [ '" + MessageId + "' ]}";
                var newMessageAckObj = JObject.Parse(newMessageAck);
                UnsendChatAckNotification.Add(newMessageAckObj);
            }

            unsendMessageJsonObj[DataJson.UNSEND_CHAT_ACK_NOTIFICATION] = UnsendChatAckNotification;

            string UnsendMessageJsonResult = JsonConvert.SerializeObject(unsendMessageJsonObj,
                                             Formatting.Indented);

            GlobalFunctions.WriteInJson(GlobalValues.unsendMessagesjsonFile, UnsendMessageJsonResult);

        }

        #region Private Methods
        public async Task SaveUserchatIdInUserInfo(int FromUserId, int ToUserId, string UserChatId)
        {
            #region Declaration
            var Userjson = GlobalFunctions.GetJsonObject(GlobalValues.chatUserInfojsonFile);
            var UserjsonObj = JObject.Parse(Userjson);
            var Users = (JArray)UserjsonObj[DataJson.USERS];

            bool IsFromUserExist = false;
            bool IsToUserExist = false;

            #endregion

            try
            {
                if (FromUserId > 0)
                {
                    foreach (var user in Users.Where(obj => obj[DataJson.USER_ID].Value<int>() == FromUserId))
                    {
                        IsFromUserExist = true;
                        var ObjuserChatId = user[DataJson.USER_CHAT_ID];
                        var ISUserChatIDExist = false;
                        foreach (var chat1 in ObjuserChatId)
                        {
                            var obj = chat1.Value<string>();
                            if (obj == UserChatId)
                            {
                                ISUserChatIDExist = true;
                            }
                        }
                        if (!ISUserChatIDExist)
                        {
                            var chat = UserChatId;
                            ((JArray)ObjuserChatId).Add(chat);

                            if (ConnectedUsers.Any(m => m.UserId == ToUserId) == true)
                            {
                                var toUser = ConnectedUsers.Where(m => m.UserId == ToUserId).FirstOrDefault();
                                await _hubContext.Clients.Client(toUser.ConnectionId).SendAsync("NewMessage", "GetChatList");
                            }
                        }
                    }

                    if (!IsFromUserExist)
                    {
                        var newUser = "{'UserId': " + FromUserId + ",'UserChatId': ['" + UserChatId + "'], 'GroupIds': [],'BlockedUserChatId': []}";
                        var newUserObj = JObject.Parse(newUser);
                        Users.Add(newUserObj);
                        if (ConnectedUsers.Any(m => m.UserId == ToUserId) == true)
                        {
                            var toUser = ConnectedUsers.Where(m => m.UserId == ToUserId).FirstOrDefault();
                            await _hubContext.Clients.Client(toUser.ConnectionId).SendAsync("NewMessage", "GetChatList");
                        }
                    }

                }
                if (ToUserId > 0)
                {
                    foreach (var user in Users.Where(obj => obj[DataJson.USER_ID].Value<int>() == ToUserId))
                    {
                        IsToUserExist = true;
                        var ObjuserChatId = user[DataJson.USER_CHAT_ID];
                        var ISUserChatIDExist = false;
                        foreach (var chat1 in ObjuserChatId)
                        {
                            var obj = chat1.Value<string>();
                            if (obj == UserChatId)
                            {
                                ISUserChatIDExist = true;
                            }
                        }
                        if (!ISUserChatIDExist)
                        {
                            var chat = UserChatId;
                            ((JArray)ObjuserChatId).Add(chat);
                        }
                    }

                    if (!IsToUserExist)
                    {
                        var newUser = "{'UserId': " + ToUserId + ",'UserChatId': ['" + UserChatId + "'], 'GroupIds': [],'BlockedUserChatId': []}";
                        var newUserObj = JObject.Parse(newUser);
                        Users.Add(newUserObj);
                    }
                }
                UserjsonObj[DataJson.USERS] = Users;

                string userJsonResult = JsonConvert.SerializeObject(UserjsonObj,
                                       Formatting.Indented);

                GlobalFunctions.WriteInJson(GlobalValues.chatUserInfojsonFile, userJsonResult);

            }
            catch (Exception ex)
            {
                throw;
            }

        }
        #endregion
    }
}
