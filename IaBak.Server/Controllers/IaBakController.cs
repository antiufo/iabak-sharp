using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IaBak.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IaBak.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IaBakController : ControllerBase
    {


        private readonly ILogger<IaBakController> _logger;
        private readonly IaBakDbContext _dbContext;

        public IaBakController(ILogger<IaBakController> logger, IaBakDbContext dbContext)
        {
            _logger = logger;
            this._dbContext = dbContext;
        }


        [HttpPost("CheckServerStatus")]
        public Task<CheckServerStatusResponse> CheckServerStatus(CheckServerStatusRequest request)
        {
            return Task.FromResult(new CheckServerStatusResponse());
        }


        private async Task<User> GetUserAsync(RequestBase request)
        {
            var user = await _dbContext.Users.FirstAsync(x => x.UserId == request.UserId && x.SecretKey == request.SecretKey);
            if (user == null) throw new Exception("Invalid user.");
            return user;
        }

        private async Task<long> GetUserIdAsync(RequestBase request) => (await GetUserAsync(request)).UserId;

        [HttpPost("Sync")]
        public async Task<SyncResponse> Sync(SyncRequest request)
        {
            var now = DateTime.UtcNow;
            var user = await GetUserAsync(request);

            foreach (var itemId in request.GainedItems)
            {
                var info = await _dbContext.TryGetItemStorageAsync(user.UserId, itemId);

                if (info != null)
                {
                    _dbContext.ItemStorage.Update(info);
                }
                else
                {
                    info = new ItemStorage { ItemId = itemId, UserId = user.UserId, DateNotified = now };
                    _dbContext.ItemStorage.Add(info);
                }
            }

            foreach (var itemId in request.LostItems)
            {
                var info = await _dbContext.TryGetItemStorageAsync(user.UserId, itemId);

                if (info != null)
                    _dbContext.ItemStorage.Remove(info);
            }

            user.LastSync = DateTime.UtcNow;
            
            await _dbContext.SaveChangesAsync();
            return new SyncResponse();

        }

        [HttpPost("JobRequest")]
        public async Task<JobRequestResponse> JobRequest(JobRequestRequest request)
        {
            var suggestion = await _dbContext.ArchiveItems
                .Where(x => x.CurrentRedundancy == 0 && !_dbContext.RecentSuggestions.Any(y => y.ItemId == x.Identifier))
                .FirstOrDefaultAsync();

            var user = await GetUserAsync(request);
            if (suggestion != null)
            {
                _dbContext.RecentSuggestions.Add(new RecentSuggestion { ItemId = suggestion.Identifier, UserId = user.UserId, SuggestionDate = DateTime.UtcNow });
            }

            await _dbContext.SaveChangesAsync();
            return new JobRequestResponse
            {
                RetryInSeconds = suggestion != null ? 10 : 60,
                Suggestions = suggestion != null ? new List<ItemDownloadSuggestion> {
                    new ItemDownloadSuggestion { ItemName = suggestion.Identifier }
                } : null
            };
            
        }

        [HttpPost("Registration")]
        public async Task<RegistrationResponse> Registration(RegistrationRequest request)
        {
            var user = new User
            {
                Email = request.Email,
                RegistrationDate = DateTime.UtcNow,
                RegistrationIp = this.HttpContext.Connection.RemoteIpAddress.ToString(),
                SecretKey = request.SecretKey,
                Nickname = request.Nickname,
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            return new RegistrationResponse { AssignedUserId = user.UserId };
        }
    }
}
