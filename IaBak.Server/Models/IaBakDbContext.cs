using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
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
        public DbSet<RecentSuggestion> RecentSuggestions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ItemStorage>()
                .HasKey(c => new { c.ItemId, c.UserId });
            modelBuilder.Entity<RecentSuggestion>()
                .HasKey(c => new { c.ItemId, c.UserId });
        }



        public async Task<ItemStorage> TryGetItemStorageAsync(long userId, string itemId)
        {
            return await ItemStorage.FirstOrDefaultAsync(x => x.UserId == userId && x.ItemId == itemId);
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
        public string ItemId { get; set; }
        public DateTime DateNotified { get; set; }
    }

    public class RecentSuggestion
    {
        public long UserId { get; set; }
        public string ItemId { get; set; }
        public DateTime SuggestionDate { get; set; }
    }

    public class ArchiveItem
    {
        [Key] public string Identifier { get; set; }
        public long TotalSize { get; set; }
        public int CurrentRedundancy { get; set; }
        public double Priority { get; set; }
    }
}
