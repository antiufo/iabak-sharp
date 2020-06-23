using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace IaBak.Server
{
    public class IaBakDbContext : DbContext
    {
        public IaBakDbContext(DbContextOptions<IaBakDbContext> options) : base(options) { } 
        public DbSet<User> Users { get; set; }
        public DbSet<ArchiveItem> ArchiveItems { get; set; }
        public DbSet<ItemStorage> ItemStorage { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ItemStorage>()
                .HasKey(c => new { c.ItemId, c.UserId });
        }
    }

    public class User
    {
        [Key] public long UserId { get; set; }
        public string SecretKey { get; set; }
        public string Email { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string RegistrationIp { get; set; }
        public DateTime LastSync { get; set; }
        public string Nickname { get; set; }

    }

    public class ItemStorage
    {
        public long UserId { get; set; }
        public long ItemId { get; set; }
    }

    public class ArchiveItem
    {
        [Key] public long ItemId { get; set; }
        public string Identifier { get; set; }
        public long? TotalSize { get; set; }
    }
}
