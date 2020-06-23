using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IaBak.Models
{

    public class RequestBase
    {
        public long UserId { get; set; }
        public string SecretKey { get; set; }
    }

    public class ResponseBase
    { 
        public string Error { get; set; }
    }

    public class RegistrationRequest : RequestBase
    {
        public string Email { get; set; }
        public string Nickname { get; set; }
    }
    public class RegistrationResponse : ResponseBase
    {
        public long AssignedUserId { get; set; }
    }

    public class SyncRequest : RequestBase
    {
        public List<string> GainedItems { get; set; }
        public List<string> LostItems { get; set; }
    }

    public class SyncResponse : RequestBase 
    {
        public List<ItemDownloadSuggestion> DownloadSuggestions { get; set; }
    }

    public class ItemDownloadSuggestion
    { 
        public string ItemName { get; set; }
    }
}
