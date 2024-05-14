using ChatApplication.Common;
using ChatApplication.Models.Barcodes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using static ChatApplication.Common.Structure;

namespace ChatApplication.Data.DataContext
{
    public interface IBarcodeData
    {
        #region This is Barcode History
        Task<List<BarcodeMediaHistory>> GetBarcodeByHistory(string barcodeNo);
        Task<BarcodeMediaHistory> GetBarcodeHistoryById(int userId);
        Task<dynamic> SaveBarcodeHistories(List<BarcodeMediaHistory> model);
        Task<List<BarcodeMediaHistory>> GetMachineList(int userId, string machine);
        #endregion

        #region This is Barcode Details
        Task<bool> IsBarcodeExist(string barcodeNo);
        Task<dynamic> SaveMediaDetails(List<BarcodeMediaDetail> model);
        Task<BarcodeMediaDetail> GetBarcodeDetailByFileName(string fileName);
        Task<List<BarcodeMediaDetail>> GetBarcodeDetailListByFileName(string fileName);
        Task<dynamic> EditBarcodeMediaDetail(BarcodeMediaDetail model);
        Task<List<BarcodeMediaDetail>> GetBarcodeDetailListByBarcodeNo(string BarcodeNo);
        #endregion
    }

    public class BarcodeData : IBarcodeData
    {
        SqlConnection Connection = new SqlConnection(GlobalValues.ConnectionString);

        private readonly MyDbContext _db;

        public BarcodeData(MyDbContext db)
        {
            _db = db;
        }

        #region This is Barcode History
        public async Task<List<BarcodeMediaHistory>> GetBarcodeByHistory(string barcodeNo)
        {
            try
            {
                var perameters = new DynamicParameters();
                perameters.Add("barcodeNo", barcodeNo);

                var data = await Dapper.Dapper.GetAllAsync<BarcodeMediaHistory>(Sp_Constant.GET_GETBARCODEHISTORY_BY_BARCODE_NUMBER, CommandType.StoredProcedure, perameters);

                return data.ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<BarcodeMediaHistory> GetBarcodeHistoryById(int userId)
        {
            try
            {
                var data = await _db.BarcodeMediaHistories.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId);

                return data;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<dynamic> SaveBarcodeHistories(List<BarcodeMediaHistory> model)
        {
            dynamic ReturnValue;

            try
            {
                await _db.BarcodeMediaHistories.AddRangeAsync(model);
                await _db.SaveChangesAsync();
                return ReturnValue = new { Success = true };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<BarcodeMediaHistory>> GetMachineList(int userId, string machine)
        {
            try
            {
                var data = await _db.BarcodeMediaHistories.AsNoTracking().Where(a => a.UserId == userId && a.BarcodeNo.Contains(machine)).ToListAsync();

                return data; 
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion


        #region This is Barcode Details
        public async Task<bool> IsBarcodeExist(string barcodeNo)
        {
            dynamic U = false;

            try
            {
                var data = await _db.BarcodeMediaDetails.Where(a => a.BarcodeNo == barcodeNo).ToListAsync();

                if (data.Count > 0)
                {
                    U = true;
                }
                else
                {
                    U = false;
                }

                return U;

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<dynamic> SaveMediaDetails(List<BarcodeMediaDetail> model)
        {
            dynamic ReturnValue;

            try
            {
                await _db.BarcodeMediaDetails.AddRangeAsync(model);
                await _db.SaveChangesAsync();
                return ReturnValue = new { Success = true };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<BarcodeMediaDetail> GetBarcodeDetailByFileName(string fileName)
        {
            try
            {
                var data = await _db.BarcodeMediaDetails.AsNoTracking().Where(a => a.FileName == fileName).FirstOrDefaultAsync();

                return data;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<List<BarcodeMediaDetail>> GetBarcodeDetailListByBarcodeNo(string BarcodeNo)
        {
            try
            {
                var data = await _db.BarcodeMediaDetails.AsNoTracking().Where(a => a.BarcodeNo == BarcodeNo).ToListAsync();

                return data;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<List<BarcodeMediaDetail>> GetBarcodeDetailListByFileName(string fileName)
        {
            try
            {
                var data = await _db.BarcodeMediaDetails.AsNoTracking().Where(a => a.FileName == fileName).ToListAsync();

                return data;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<dynamic> EditBarcodeMediaDetail(BarcodeMediaDetail model)
        {
            dynamic ReturnValue;

            try
            {
                _db.Entry(model).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                return ReturnValue = new { Success = true };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion
    }
}
