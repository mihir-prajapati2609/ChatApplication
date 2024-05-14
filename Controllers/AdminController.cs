using ChatApplication.Models.Notification;
using ChatApplication.Models.Users;
using ChatApplication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApplication.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _admin;

        public AdminController(AdminService admin)
        {
            _admin = admin;
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<dynamic> GetPendingUsers()
        {
            try
            {
                return await _admin.GetPendingUsers();
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> AddGroup(UsersGroup model)
        {
            try
            {
                return await _admin.AddGroup(model);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> ApproveRejectUser(User model)
        {
            try
            {
                return await _admin.ApproveRejectUser(model);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> GetGroupDetails(UsersGroup model)
        {
            try
            {
                return await _admin.GetGroupDetails(model);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<dynamic> GetAllGroups()
        {
            try
            {
                return await _admin.GetAllGroups();
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> UpdateGroup(UsersGroup model)
        {
            try
            {
                return await _admin.UpdateGroup(model);
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<dynamic> DeleteGroup(UsersGroup model)
        {
            try
            {
                return await _admin.DeleteGroup(model.GroupId);
            }
            catch (Exception ex)
            {

                throw;
            }
        }


    }
}
