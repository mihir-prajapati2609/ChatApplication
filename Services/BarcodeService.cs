using ChatApplication.Common;
using ChatApplication.Data.DataContext;
using ChatApplication.Models.Barcodes;
using ChatApplication.Models.Message;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChatApplication.Services
{
    public interface IBarcodeService
    {
        Task<dynamic> GetBarcodeImages(string BarcodeNo, int UserId);
        Task<dynamic> StoreBarcodePictures(Message model);
        Task<dynamic> CheckBarcodeNo(string BarcodeNo);
        Task<dynamic> GetMachineLists(string machine, int userId);
    }


    public class BarcodeService : IBarcodeService
    {
        private readonly IBarcodeData _BarcodeData;

        private readonly IUserData _userData;

        public BarcodeService(IBarcodeData barcodeData, IUserData userData)
        {
            _BarcodeData = barcodeData;
            _userData = userData;
        }

        public async Task<dynamic> GetBarcodeImages(string BarcodeNo, int UserId)
        {
            dynamic ReturnValue = "";

            var MInfo = new List<MediaInfo>();

            var getBarcodeFromHistory = await _BarcodeData.GetBarcodeByHistory(BarcodeNo);

            var barcodeHistoryModel = await _BarcodeData.GetBarcodeHistoryById(UserId);

            var rootforImage =  GlobalValues.BarcodeFolder + BarcodeNo;

            try
            {
                string[] filesindirectory = Directory.GetDirectories(rootforImage);

                dynamic Files = Directory.GetFiles(rootforImage, "*.*", SearchOption.AllDirectories);

                try
                {
                    if (UserId == barcodeHistoryModel.UserId)
                    {
                        for (int i = 0; i < getBarcodeFromHistory.Count; i++)
                        {
                            barcodeHistoryModel = getBarcodeFromHistory[i];
                            foreach (var filePath in Files)
                            {
                                var RegexPath = Regex.Split(filePath, "wwwroot")[1];
                                var _fileName = Path.GetFileNameWithoutExtension(filePath);
                                var _medialink = GlobalValues.BaseUrl.Trim('/') + Regex.Replace(RegexPath, @"\\+", @"/");

                                if (barcodeHistoryModel.FileName == "Frontside" && _fileName == "Frontside" || barcodeHistoryModel.FileName == "Backside" && _fileName == "Backside"
                                    || barcodeHistoryModel.FileName == "Growth" && _fileName == "Growth" || (barcodeHistoryModel.FileName == "Plotting" && _fileName == "Plotting"))
                                {
                                    MInfo.Add(new MediaInfo()
                                    {
                                        FileName = barcodeHistoryModel.FileName,
                                        MediaLink = _medialink,
                                        Time = barcodeHistoryModel.Date,
                                    });
                                    break;

                                }
                            }
                        }
                    }

                    ReturnValue = new { success = true, Media = MInfo, Message = "" };
                }
                catch (Exception ex)
                {

                    ReturnValue = new { success = true, Message = "Invalid User" };
                }           
            }
            catch (Exception)
            {
                ReturnValue = new { success = false, Message = "Invalid Barcode" };
            }

            return  ReturnValue;
        }

        public async Task<dynamic> StoreBarcodePictures(Message model)
        {
            dynamic ReturnValue = null;

            var rootForBarcode = GlobalValues.BarcodeFolder;

            var mediaInfo = new MediaInfo();

            bool isBarcodeUnique = await _BarcodeData.IsBarcodeExist(model.BarcodeNo);

            if (!isBarcodeUnique)
            {
                for (int data = 0; data < model.MediaInfos.Count; data++)
                {
                    mediaInfo = model.MediaInfos[data];

                    List<BarcodeMediaDetail> barcodeMediaDetails = new List<BarcodeMediaDetail>()
                    {
                        new BarcodeMediaDetail()
                        {
                            BarcodeNo = model.BarcodeNo,
                            FileName = mediaInfo.FileName,
                            EntryByUserId = model.FromUserId,
                            EntryDate = DateTime.Now,
                            UpdateByUserId = 0,
                            UpdateDate = DateTime.Now

                        }
                    };
                    await _BarcodeData.SaveMediaDetails(barcodeMediaDetails);
                }

            }
            else if (isBarcodeUnique)
            {
                for (int data = 0; data < model.MediaInfos.Count; data++)
                {
                    mediaInfo = model.MediaInfos[data];

                    var barcodeDetail = await _BarcodeData.GetBarcodeDetailByFileName(mediaInfo.FileName);

                    if(barcodeDetail != null)
                    {
                        var barcodes = await _BarcodeData.GetBarcodeDetailListByFileName((object)barcodeDetail.FileName);

                        var existing = await _BarcodeData.GetBarcodeByFileName(barcodeDetail.FileName);

                        if (existing is null)
                            throw new Exception("Barcode not found");

                        existing.UpdateByUserId = model.FromUserId;
                        existing.UpdateDate = DateTime.Now;

                        await _BarcodeData.EditBarcodeMediaDetail((object)existing);
                    }
                }
            }

            for (int data = 0; data < model.MediaInfos.Count; data++)
            {
                mediaInfo = model.MediaInfos[data];

                List<BarcodeMediaHistory> KPHistory = new List<BarcodeMediaHistory>()
                {
                    new BarcodeMediaHistory()
                    {
                    BarcodeNo = model.BarcodeNo,
                    Date = DateTime.Now,
                    FileName = mediaInfo.FileName,
                    UserId = model.FromUserId
                    }
                };

                await _BarcodeData.SaveBarcodeHistories(KPHistory);
            }

            //Saving Barocde Media Details in Files
            try
            {
                if ((model.MediaInfos ?? new List<MediaInfo>()).Count > 0)
                {
                    for (int i = 0; i < model.MediaInfos.Count; i++)
                    {
                        var MInfo = model.MediaInfos[i];

                        if (MInfo.Media == null && MInfo.FileName == "")
                        {
                            ReturnValue = new { success = false, message = "Something's Wrong" };
                        }
                        else
                        {

                            var data = GlobalFunctions.SaveFilesInDirectory(rootForBarcode, model.BarcodeNo.ToUpper(), MInfo.FileName, MInfo.Media);

                            if (data == "SUCCESS")
                            {
                                ReturnValue = new { success = true, message = "Succesfully Stored" };
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                ReturnValue = new { success = false, message = "Error!" };
            }

            return ReturnValue;
        }

        public async Task<dynamic> CheckBarcodeNo(string BarcodeNo)
        {
            dynamic data = false;

            var barcodeMedia = await _BarcodeData.GetBarcodeDetailListByBarcodeNo(BarcodeNo);

            if (barcodeMedia.Count > 0)
            {
                data = new { success = false, message = "Barcode Already Added" };
            }
            else
            {
                data = new { success = true, message = "You Can Store" };
            }

            return data;
        }

        public async Task<dynamic> GetMachineLists(string machine, int userId)
        {
            var barcodeHistory = new List<BarcodeMediaHistory>();

            dynamic ReturnValue;

            try
            {
                var result = new List<string>();
                var data = await _BarcodeData.GetMachineList(userId, machine);

                if (data != null && data.Count > 0)
                    result.AddRange(data.Select(a => a.BarcodeNo).Distinct());

                if (result != null)
                {
                    foreach (var addMachine in result)
                    {
                        barcodeHistory.Add(new BarcodeMediaHistory
                        {
                            MachineNo = addMachine
                        });
                    }

                    ReturnValue = new { success = true, Data = barcodeHistory, Message = "" };
                }
                else
                {
                    ReturnValue = new { success = false, Message = "No Data Found", Data = barcodeHistory };
                }

                return ReturnValue;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


    }
}
