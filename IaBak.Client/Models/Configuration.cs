using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IaBak.Models
{
    public class Configuration
    {
        //public DateTime LastSync;
        public long UserId;
        public string UserEmail;
        public string UserSecretKey;
        public string Directory;
        public string Nickname;
        public double LeaveFreeGb;
        public DateTime LastUpdateCheck;
        public bool RunOnStartup;

        [JsonIgnore]
        public long LeaveFreeBytes => (long)(LeaveFreeGb * 1024 * 1024 * 1024);
    }
}
