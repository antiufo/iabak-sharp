using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IaBak.Models
{

    public abstract class RequestBase<TResponse> where TResponse : ResponseBase
    {
        public long UserId { get; set; }
        public string SecretKey { get; set; }
        public string Version { get; set; }
    }

    public abstract class ResponseBase
    { 
        public string Error { get; set; }
    }

    public class CheckServerStatusRequest : RequestBase<CheckServerStatusResponse>
    {
        // Empty.
    }
    public class CheckServerStatusResponse : ResponseBase
    {
        // Empty.
    }

    public class RegistrationRequest : RequestBase<RegistrationResponse>
    {
        public string Email { get; set; }
        public string Nickname { get; set; }
    }
    public class RegistrationResponse : ResponseBase
    {
        public long AssignedUserId { get; set; }
    }



    public class SyncRequest : RequestBase<SyncResponse>
    {
        public List<string> GainedItems { get; set; }
        public List<string> LostItems { get; set; }
    }

    public class SyncResponse : ResponseBase
    {
        // empty
    }


    public class JobRequestRequest : RequestBase<JobRequestResponse>
    {
        public long AvailableFreeSpace { get; set; }
    }

    public class JobRequestResponse : ResponseBase
    {
        public List<ItemDownloadSuggestion> Suggestions { get; set; }
        public int RetryInSeconds { get; set; }
    }


    public class ItemDownloadSuggestion
    {
        public string ItemName { get; set; }
    }
}
