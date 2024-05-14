using ChatApplication.Models.OtpHistory;
using ChatApplication.Models.Users;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApplication.Data.DataContext
{
    public interface IUserData
    {
        #region Users
        Task<List<User>> GetAllUsers();
        Task<User> GetTrackedUserById(int userId);
        Task<User> GetUserByUserNameAndPassword(string userName, string password);
        Task<dynamic> AddUser(User model);
        Task<dynamic> EditUser(User model);
        Task<bool> IsUserNameExist(string userName);
        Task<bool> IsContactNoExist(string contactNo);
        Task<List<User>> GetPendingUsers();
        Task<List<User>> GetUsersByUserIds(List<int> userIds);
        Task<User> GetUserDetailByContactNumber(string contactNo);
        #endregion

        #region User Groups
        Task<UsersGroup> GetNoTrackedFirstGroupByName(string groupName);
        Task<UsersGroup> GetTrackedGroupByGroupId(int groupId);
        Task<UsersGroup> GetNoTrackedGroupByGroupId(int groupId);
        Task<List<UsersGroup>> GetAllGroups();
        Task<dynamic> AddGroup(UsersGroup model);
        Task<dynamic> EditGroup(UsersGroup model);
        Task<dynamic> DeleteGroup(UsersGroup model);
        #endregion

        #region OTP
        Task<dynamic> DeleteOtpByUserId(int userId);
        Task<OtpHistory> GetOtpDetailByUserIdAndOtp(int userId, int otp);
        Task<dynamic> AddOtpHistory(OtpHistory model);
        #endregion

    }

    public class UserData : IUserData
    {
        private MyDbContext _db;

        public UserData(MyDbContext db)
        {
            _db = db;
        }

        dynamic ReturnValue = "Success";

        #region Public methods

        #region User
        public async Task<List<User>> GetAllUsers()
        {
            try
            {
                var data = await _db.Users.AsNoTracking().ToListAsync();
                return data;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<User> GetTrackedUserById(int userId)
        {
            try
            {
                var data = await _db.Users.FirstOrDefaultAsync(a => a.UserId == userId);
                return data;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<User> GetUserByUserNameAndPassword(string userName, string password)
        {
            try
            {
                var data = await _db.Users.AsNoTracking().FirstOrDefaultAsync(a => a.UserName.Equals(userName) && a.Password.Equals(password));
                return data;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<dynamic> AddUser(User model)
        {
            

            try
            {
                //await _user.InsertAsync(model);

                await _db.Users.AddAsync(model);
                await _db.SaveChangesAsync();

                return ReturnValue;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<dynamic> EditUser(User model)
        {

            try
            {
                //await _user.UpdateAsync(model);

                _db.Entry(model).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                return ReturnValue;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<bool> IsUserNameExist(string userName)
        {
            try
            {
                //var data = await _user.GetFirstNonTrackedAsync(a => a.UserName == userName);
                var data = await _db.Users.AsNoTracking().FirstOrDefaultAsync(a => a.UserName == userName);
                return data != null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<bool> IsContactNoExist(string contactNo)
        {
            try
            {
                //var data = await _user.GetFirstNonTrackedAsync(a => a.ContactNo == contactNo);
                var data = await _db.Users.AsNoTracking().FirstOrDefaultAsync(a => a.ContactNo == contactNo);

                return data != null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<List<User>> GetPendingUsers()
        {
            try
            {
                return await _db.Users.Where(a => a.IsApproved == false && a.IsAdmin == false && a.IsRejected == false).ToListAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<List<User>> GetUsersByUserIds(List<int> userIds)
        {
            try
            {
                return await _db.Users.AsNoTracking().Where(a => userIds.Contains(a.UserId)).ToListAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<User> GetUserDetailByContactNumber(string contactNo)
        {
            try
            {
                return await _db.Users.AsNoTracking().FirstOrDefaultAsync(a => a.ContactNo == contactNo);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region User Groups
        public async Task<UsersGroup> GetNoTrackedGroupByGroupId(int groupId)
        {
            try
            {
                return await _db.UsersGroup.AsNoTracking().FirstOrDefaultAsync(a => a.GroupId == groupId);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<UsersGroup> GetTrackedGroupByGroupId(int groupId)
        {
            try
            {
                return await _db.UsersGroup.FirstOrDefaultAsync(a => a.GroupId == groupId);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<UsersGroup> GetNoTrackedFirstGroupByName(string groupName)
        {
            try
            {
                return await _db.UsersGroup.AsNoTracking().FirstOrDefaultAsync(a => a.GroupName.Equals(groupName));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<List<UsersGroup>> GetAllGroups()
        {
            try
            {
                return await _db.UsersGroup.AsNoTracking().ToListAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<dynamic> AddGroup(UsersGroup model)
        {
            try
            {
                await _db.UsersGroup.AddAsync(model);
                await _db.SaveChangesAsync();

                return ReturnValue;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<dynamic> EditGroup(UsersGroup model)
        {
            try
            {
                _db.Entry(model).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                return ReturnValue;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public async Task<dynamic> DeleteGroup(UsersGroup model)
        {
            try
            {
                _db.UsersGroup.Remove(model);
                await _db.SaveChangesAsync();

                return ReturnValue;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region OTP
        public async Task<dynamic> DeleteOtpByUserId(int userId)
        {
            try
            {
                var otpHistory = await _db.OTPhistory.AsNoTracking().Where(a => a.UserId == userId).ToListAsync();
                if (otpHistory != null && otpHistory.Count > 0)
                {
                    _db.RemoveRange(otpHistory);
                    await _db.SaveChangesAsync();
                }

                return ReturnValue;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<OtpHistory> GetOtpDetailByUserIdAndOtp(int userId, int otp)
        {
            try
            {
                return await _db.OTPhistory.FirstOrDefaultAsync(a => a.UserId == userId && a.OTP == otp);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<dynamic> AddOtpHistory(OtpHistory model)
        {
            try
            {
                await _db.OTPhistory.AddRangeAsync(model);
                await _db.SaveChangesAsync();
                return ReturnValue;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #endregion
    }
}
