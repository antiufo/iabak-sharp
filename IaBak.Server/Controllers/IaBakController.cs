using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IaBak.Models;
using Microsoft.AspNetCore.Mvc;
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



        [HttpGet]
        public IEnumerable<User> Get()
        {
            return _dbContext.Users.ToList();
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
