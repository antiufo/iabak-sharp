using IaBak.Models;
using Newtonsoft.Json;
using Shaman.Types;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IaBak.Client
{
    static class Program
    {

        public static Configuration UserConfiguration;
        internal static Stream singleInstanceLock;
        static async Task Main(string[] args)
        {
            ApplicationDirectory = Utils.GetApplicationPath();
            IaBakVersion = typeof(Program).Assembly.GetName().Version;

            if (args.Contains("--version"))
            {
                Console.WriteLine(IaBakVersion);
                return;
            }
            if (args.Contains("--help"))
            {
                Console.WriteLine("For help, see https://github.io/antiufo/iabak-sharp.");
                return;
            }
            Utils.WriteLog("IaBak-sharp " + IaBakVersion);
            var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IaBak-sharp");
            Directory.CreateDirectory(configDir);
            ConfigFilePath = Path.Combine(configDir, "Configuration.json");
            try
            {
                singleInstanceLock = new FileStream(Path.Combine(configDir, "lock"), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024, FileOptions.DeleteOnClose);
            }
            catch 
            {
                Utils.WriteLog("Another instance is already running.");
                Environment.Exit(1);
            }
            if (!File.Exists(ConfigFilePath))
            {
                await Updates.CheckForUpdatesAsync();
                await Registration.UserRegistrationAsync();
            }

            var config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(ConfigFilePath));
            UserConfiguration = config;

            StagingFolder = Path.Combine(config.Directory, "staging");
            DataFolder = Path.Combine(config.Directory, "data");

            Directory.CreateDirectory(StagingFolder);
            Directory.CreateDirectory(DataFolder);
            var rootDrive = Utils.GetParentDrive(DataFolder).RootDirectory.FullName;
            while (true)
            {
                var now = DateTime.UtcNow;
                if (config.LastSync > now) config.LastSync = default;
                if (config.LastUpdateCheck > now) config.LastUpdateCheck = default;


                if (now > config.LastUpdateCheck.AddHours(8))
                {
                    await Updates.CheckForUpdatesAsync();
                }

                await InternetArchive.ResumeUnfinishedDownloadsAsync();

                var nextSync = config.LastSync.AddMinutes(10);
                if (nextSync > now)
                {
                    var delay = nextSync - now;
                    Utils.WriteLog($"Next sync in {delay.TotalMinutes:0} minutes.");
                    await Task.Delay(delay);
                }


                

                Utils.WriteLog("Syncing...");



                var avail = new DriveInfo(rootDrive).AvailableFreeSpace;
                var thresholdBytes = (long)(config.LeaveFreeGb * 1024 * 1024 * 1024);
                if (avail < thresholdBytes)
                {
                    Utils.WriteLog(@$"Not syncing any other items, because less than {new FileSize(thresholdBytes)} are left on disk {rootDrive}.
To reduce the amount of reserved space, edit Configuration.json.
Saving to multiple drives is not currently supported.");
                    config.LastSync = now;
                    SaveConfig();
                    continue;
                }



                //var respose = await RpcAsync<SyncResponse>(new SyncRequest
                //{
                //    UserId = config.UserId,
                //    SecretKey = config.UserSecretKey,
                //    GainedItems = new List<string> { "gatto", Guid.NewGuid().ToString() },
                //    LostItems = new List<string> { "topo" },
                //    AvailableFreeSpace = avail - thresholdBytes

                //});

                //RpcAsync(new SyncRequest {  })

                SaveConfig();
            }

        }


        public static void SaveConfig()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
            File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(UserConfiguration, Formatting.Indented));
        }


        public static string RootFolder = @"E:\IaBak";
        public static string StagingFolder = Path.Combine(RootFolder, "staging");
        public static string DataFolder = Path.Combine(RootFolder, "data");
        public static string ConfigFilePath;
        public static string ApplicationDirectory;
        public static Version IaBakVersion;



      
    }
}
