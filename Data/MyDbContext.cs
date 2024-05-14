using ChatApplication.Models.Barcodes;
using ChatApplication.Models.Kapans;
using ChatApplication.Models.OtpHistory;
using ChatApplication.Models.Users;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApplication.Data
{
    public class MyDbContext : DbContext
    {
        #region Ctor
        public MyDbContext(DbContextOptions<MyDbContext> options)
          : base(options)
        {

        }
        #endregion

        public virtual DbSet<User> Users { get; set; }

        public virtual DbSet<UsersGroup> UsersGroup { get; set; }

        public virtual DbSet<BarcodeMediaDetail> BarcodeMediaDetails { get; set; }

        public virtual DbSet<BarcodeMediaHistory> BarcodeMediaHistories { get; set; }

        public virtual DbSet<OtpHistory> OTPhistory { get; set; }
    }
}
