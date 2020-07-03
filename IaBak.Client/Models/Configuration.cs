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
    }
}
