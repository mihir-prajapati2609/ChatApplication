using ChatApplication.Common;
using ChatApplication.Data;
using ChatApplication.Data.DataContext;
using ChatApplication.Hubs;
using ChatApplication.Models.Users;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static ChatApplication.Common.Structure;

namespace ChatApplication.Services
{
    public interface IAdminService
    {
        Task<dynamic> GetPendingUsers();

        Task<dynamic> AddGroup(UsersGroup model);

        Task<dynamic> ApproveRejectUser(User model);

        Task<dynamic> GetGroupDetails(UsersGroup usersGroup);

        Task<dynamic> GetAllGroups();

        Task<dynamic> UpdateGroup(UsersGroup usersGroup);

        Task<dynamic> DeleteGroup(int groupId);
    }

    public class AdminService : IAdminService
    {

        private MyDbContext _db;

        private readonly IUserData _user;

        List<User> ConnectedUsers = GlobalValues.ConnectedUsers;

        private readonly IHubContext<chatviewhub> _hubContext;

        public AdminService(MyDbContext db, IUserData user, IHubContext<chatviewhub> hubContext)
        {
            _db = db;
            _user = user;
            _hubContext = hubContext;
        }

        public async Task<dynamic> GetPendingUsers()
        {
            #region DECLARATIONS
            var UserList = new List<User>();
            dynamic ReturnValue = "";
            #endregion

            UserList = await _user.GetPendingUsers();

            ReturnValue = new { Success = true, Data = UserList };

            return ReturnValue;
        }

        public async Task<dynamic> ApproveRejectUser(User model)
        {

            dynamic ReturnValue;

            var user = await _user.GetTrackedUserById(model.UserId);

            if (user is null)
            {
                throw new Exception("User not found!");
            }
               
            try
            {
                user.IsApproved = model.IsApproved;
                user.IsRejected = model.IsRejected;
                user.IsActivated = model.IsActivated;

                await _user.EditUser(user);

                ReturnValue = new { Success = true };
            }
            catch (Exception ex)
            {
                ReturnValue = new { Success = false, Message = ex };
            }

            return ReturnValue;
        }

        public async Task<dynamic> AddGroup(UsersGroup model)
        {
            #region DECLARATIONS
            dynamic ReturnValue;

            var result = new UsersGroup();

            var userGroup = new UsersGroup
            {
                GroupName = model.GroupName.Trim(),
                UserIds = model.UserIds,
                CreatedByUserId = model.CreatedByUserId,
                CreatedDateTime = DateTime.Now,
            };
            #endregion

            try
            {
                var returnValue = await _user.AddGroup(userGroup);

                var data = await _user.GetNoTrackedFirstGroupByName(model.GroupName.Trim());

                if (data is null)
                {
                    return null;
                }

                result.GroupId = data.GroupId;
                result.GroupName = data.GroupName;
                result.UserIds = data.UserIds;
                result.CreatedByUserId = data.CreatedByUserId;
                result.CreatedDateTime = data.CreatedDateTime;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return ReturnValue = new { Success = true, Message = "Registered Successfully" };
        }

        public async Task<dynamic> GetGroupDetails(UsersGroup model)
        {
            #region DECLARATIONS
            var result = new UsersGroup();
            dynamic ReturnValue;
            #endregion

            var userGroup = await _user.GetNoTrackedGroupByGroupId(model.GroupId);
            if (userGroup is null)
                return null;

            if (!string.IsNullOrEmpty(userGroup.UserIds))
            {

                var userIds = userGroup.UserIds.Split(",").Select(a => new { userId = Convert.ToInt32(a) }).Select(a => a.userId).ToList();
                var users = await _user.GetUsersByUserIds(userIds);

                var responseUsers = new List<User>();

                if (users != null && users.Count > 0)
                {
                    responseUsers.AddRange(users.Select(a => new User
                    {
                        UserId = a.UserId,
                        AccessToken = a.AccessToken,
                        Address = a.Address,
                        ClientId = a.ClientId,
                        //ConnectionId = a.ConnectionId,
                        ContactNo = a.ContactNo,
                        DeviceId = a.DeviceId,
                        Email = a.Email,
                        FullName = a.FullName,
                        IsActivated = a.IsActivated,
                        IsAdmin = a.IsAdmin,
                        IsApproved = a.IsApproved,
                        IsRejected = a.IsRejected,
                        Password = a.Password,
                        UserName = a.UserName,
                    }));
                }

                result.ListUsers = responseUsers;
            }

            ReturnValue = new { Success = true, Data = result };

            return ReturnValue;
        }

        public async Task<dynamic> GetAllGroups()
        {

            dynamic ReturnValue;

            var result = new List<UsersGroup>();

            var allUserIds = result.Where(a => !string.IsNullOrEmpty(a.UserIds))
                                            .Select(a => a.UserIds).Select(a => a.Split(","))
                                            .ToList();

            var userIds = new List<int>();

            allUserIds.ForEach(a =>
            {
                userIds.AddRange(a.Select(a => Convert.ToInt32(a)));
            });

            //NOTE :: Apply distinct on user Id 
            userIds = userIds.GroupBy(o => o).Select(g => g.First()).ToList();

            var allUsers = await _user.GetUsersByUserIds(userIds);

            foreach (var userGroup in result)
            {
                var responseUsers = new List<User>();

                var ids = userGroup.UserIds.Split(",").Select(a => Convert.ToInt32(a)).ToList();

                responseUsers.AddRange(allUsers.Where(a => ids.Contains(a.UserId)).Select(a => new User
                {
                    UserId = a.UserId,
                    AccessToken = a.AccessToken,
                    Address = a.Address,
                    ClientId = a.ClientId,
                    //ConnectionId = a.ConnectionId,
                    ContactNo = a.ContactNo,
                    DeviceId = a.DeviceId,
                    Email = a.Email,
                    FullName = a.FullName,
                    IsActivated = a.IsActivated,
                    IsAdmin = a.IsAdmin,
                    IsApproved = a.IsApproved,
                    IsRejected = a.IsRejected,
                    Password = a.Password,
                    UserName = a.UserName
                }));

                userGroup.ListUsers = responseUsers;
            }

            return ReturnValue = new { Success = true, Data = result };
        }

        public async Task<dynamic> UpdateGroup(UsersGroup model)
        {
            #region DECLARATIONS
            dynamic ReturnValue;
            User U = new User();
            UsersGroup UG = new UsersGroup();
            #endregion

            try
            {
                var existing = await _user.GetTrackedGroupByGroupId(model.GroupId);

                if (existing is null)
                    throw new Exception("Group not found!");

                existing.GroupName = model.GroupName;
                existing.UserIds = model.UserIds;

                await _user.EditGroup(existing);

                UG = await _user.GetNoTrackedGroupByGroupId(model.GroupId);

                ReturnValue = new { Success = true };

                await DeletegroupFromJson(model.GroupId);

                await SaveGroupIntoJson(UG);
            }
            catch (Exception ex)
            {
                ReturnValue = new { Success = false, Message = ex };
            }

            return ReturnValue;
        }

        public async Task<dynamic> DeleteGroup(int groupId)
        {

            dynamic ReturnValue;

            try
            {
                var existing = await _user.GetTrackedGroupByGroupId(groupId);
                if (existing is null)
                    throw new Exception("Group not found!");

                await _user.DeleteGroup(existing);

                await DeletegroupFromJson(groupId);

                ReturnValue = new { Success = true };

            }
            catch (Exception ex)
            {
                ReturnValue = new { Success = false, Message = ex };
            }

            return ReturnValue;
        }


        #region Private Methods
        public async Task SaveGroupIntoJson(UsersGroup M)
        {
            #region DECLARATIONS

            var userIds = M.UserIds.Split(',');

            var Userjson = GlobalFunctions.GetJsonObject(GlobalValues.chatUserInfojsonFile);
            var UserjsonObj = JObject.Parse(Userjson);
            var Users = (JArray)UserjsonObj[DataJson.USERS];

            var GroupUserjson = GlobalFunctions.GetJsonObject(GlobalValues.groupChatjsonFile);
            var GroupUserjsonObj = JObject.Parse(GroupUserjson);
            var Groups = (JArray)GroupUserjsonObj[DataJson.GROUPS];

            #endregion
            try
            {
                var UserChatId = "G" + String.Format("{0:D4}", M.GroupId);

                var ISGroupExist = false;
                foreach (var user in Groups.Where(obj => obj[DataJson.USER_CHAT_ID].Value<string>() == UserChatId))
                {
                    ISGroupExist = true;
                    var users = user["Members"];
                }
                if (ISGroupExist == false)
                {
                    var newGroup = "{'UserChatId': '" + UserChatId + "','GroupId': " + M.GroupId + ",'ToName': '" + M.GroupName + "','Members': [" + M.UserIds + "]}";
                    var newGroupObj = JObject.Parse(newGroup);
                    Groups.Add(newGroupObj);
                }
                GroupUserjsonObj[DataJson.GROUPS] = Groups;

                string GroupsJsonResult = JsonConvert.SerializeObject(GroupUserjsonObj,
                                          Formatting.Indented);

                GlobalFunctions.WriteInJson(GlobalValues.groupChatjsonFile, GroupsJsonResult);

                var TempArray = new JArray();

                foreach (var id in userIds)
                {
                    if (int.Parse(id) > 0)
                    {
                        var IsUserExist = false;
                        foreach (var user in Users.Where(obj => obj[DataJson.USER_ID].Value<string>() == id))
                        {
                            IsUserExist = true;
                            var UserChat = user[DataJson.USER_CHAT_ID];
                            var chat = UserChatId;
                            ((JArray)UserChat).Add(chat);
                        }

                        if (IsUserExist == false)
                        {
                            var newUser = "{'UserId': " + id + ",'UserChatId': ['" + UserChatId + "'], 'GroupIds': [],'BlockedUserChatId': []}";
                            var newUserObj = JObject.Parse((string)newUser);
                            TempArray.Add(newUserObj);
                        }

                        if (ConnectedUsers.Any(m => m.UserId == int.Parse(id)) == true)
                        {
                            var toUser = ConnectedUsers.Where(m => m.UserId == int.Parse(id)).FirstOrDefault();
                            await _hubContext.Clients.Client(toUser.ConnectionId).SendAsync("NewMessage", "GetChatList");
                        }

                        //using (var client = new HttpClient())
                        //{
                        //    client.BaseAddress = new Uri(GlobalValues.BaseUrl);
                        //    HttpResponseMessage Res = await client.GetAsync("Home/SendUserChatList?UserId=" + x);
                        //}
                    }


                }
                foreach (var item in TempArray)
                {
                    Users.Add(item);
                }
                UserjsonObj[DataJson.USERS] = Users;

                string userJsonResult = JsonConvert.SerializeObject(UserjsonObj,
                                        Formatting.Indented);

                GlobalFunctions.WriteInJson(GlobalValues.chatUserInfojsonFile, userJsonResult);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task DeletegroupFromJson(int GroupId)
        {
            #region DECLARATIONS
            var Userjson = GlobalFunctions.GetJsonObject(GlobalValues.chatUserInfojsonFile);
            var UserjsonObj = JObject.Parse(Userjson);
            var Users = (JArray)UserjsonObj[DataJson.USERS];

            var GroupUserjson = GlobalFunctions.GetJsonObject(GlobalValues.groupChatjsonFile);
            var GroupUserjsonObj = JObject.Parse(GroupUserjson);
            var Groups = (JArray)GroupUserjsonObj[DataJson.GROUPS];
            #endregion

            try
            {
                //delete Group from UserJson and Group Json
                var groupindex = -1;
                var GroupUserChatId = string.Empty;

                foreach (var group in Groups.Where(obj => obj[DataJson.GROUP_ID].Value<int>() == GroupId))
                {
                    var UserChatId = group[DataJson.USER_CHAT_ID];
                    GroupUserChatId = (string)UserChatId;
                    var groupUserList = group["Members"];
                    foreach (var groupUser in groupUserList)
                    {
                        int id = (int)groupUser;
                        var userindex = -1;

                        foreach (var user in Users.Where(obj => obj[DataJson.USER_ID].Value<int>() == id))
                        {
                            var userChatId = (JArray)user[DataJson.USER_CHAT_ID];
                            foreach (var chatIdValue in userChatId)
                            {
                                if (chatIdValue.Value<string>() == UserChatId.Value<string>())
                                {
                                    userindex = userChatId.IndexOf(chatIdValue);
                                }
                            }
                            userChatId.RemoveAt(userindex);
                            user[DataJson.USER_CHAT_ID] = userChatId;
                        }

                        if (ConnectedUsers.Any(m => m.UserId == id) == true)
                        {
                            var toUser = ConnectedUsers.Where(m => m.UserId == id).FirstOrDefault();
                            await _hubContext.Clients.Client(toUser.ConnectionId).SendAsync("NewMessage", "GetChatList");
                        }

                        //using (var client = new HttpClient())
                        //{
                        //    client.BaseAddress = new Uri(GlobalValues.BaseUrl);
                        //    //HttpResponseMessage Res = await client.GetAsync("Home/SendUserChatList?UserId=" + x);
                        //    HttpResponseMessage Res =  client.GetAsync("Home/SendUserChatList?UserId=" + x).Result;
                        //}

                    }
                    groupindex = Groups.IndexOf(group);


                    UserjsonObj[DataJson.USERS] = Users;
                    string userJsonResult = JsonConvert.SerializeObject(UserjsonObj,
                                            Formatting.Indented);
                    GlobalFunctions.WriteInJson(GlobalValues.chatUserInfojsonFile, userJsonResult);
                }
                Groups.RemoveAt(groupindex);
                GroupUserjsonObj[DataJson.GROUPS] = Groups;

                string GroupsJsonResult = JsonConvert.SerializeObject(GroupUserjsonObj,
                                          Formatting.Indented);

                GlobalFunctions.WriteInJson(GlobalValues.groupChatjsonFile, GroupsJsonResult);

            }
            catch (Exception ex)
            {
                throw;
            }
        }
        #endregion


    }
}
