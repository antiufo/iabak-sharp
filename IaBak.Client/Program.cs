using IaBak.Models;
using Microsoft.Win32;
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
            IaBakVersion = typeof(Program).Assembly.GetName().Version;
            var configDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IaBak-sharp");
            ConfigFilePath = Path.Combine(configDir, "Configuration.json");

            if (args.Contains("--version"))
            {
                Console.WriteLine(IaBakVersion);
                return;
            }
            Utils.WriteLog("IaBak-sharp " + IaBakVersion);

            if (new[] { "--help", "-help", "-h", "/?" }.Any(x => args.Contains(x)))
            {
                Console.WriteLine("https://github.io/antiufo/iabak-sharp");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("   iabak-sharp");
                Console.WriteLine();
                if (File.Exists(ConfigFilePath))
                {
                    Console.WriteLine($"In order to change your settings, modify {ConfigFilePath}");
                }
                else 
                {
                    Console.WriteLine("On the first run, you will be guided through the configuration of iabak-sharp.");
                }
                return;
            }
            Directory.CreateDirectory(configDir);
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

            Directory.CreateDirectory(StagingFolder);
            Directory.CreateDirectory(DataFolder);

            SetupAutostart();

            var rootDrive = Utils.GetParentDrive(DataFolder).RootDirectory.FullName;


            await InternetArchive.ResumeUnfinishedDownloadsAsync();
            await NotifyDownloadedItemsAsync();

            while (true)
            {
                var now = DateTime.UtcNow;
                if (config.LastUpdateCheck > now) config.LastUpdateCheck = default;


                if (now > config.LastUpdateCheck.AddHours(8))
                {
                    await Updates.CheckForUpdatesAsync();
                }



                var avail = new DriveInfo(rootDrive).AvailableFreeSpace;
                if (avail < config.LeaveFreeBytes)
                {
                    Utils.WriteLog(@$"Not syncing any more items, because less than {new FileSize(config.LeaveFreeBytes)} are left on disk {rootDrive}.
To reduce the amount of reserved space, edit {ConfigFilePath} and restart iabak-sharp (Saving to multiple drives is not currently supported). Press CTRL+C to exit.");
                    await Task.Delay(TimeSpan.FromMinutes(60));
                    continue;
                }


                JobRequestResponse response;
                try
                {
                    Utils.WriteLog("Requesting items to retrieve...");
                    response = await Utils.RpcAsync(new JobRequestRequest
                    {
                        AvailableFreeSpace = avail - config.LeaveFreeBytes,
                    });
                }
                catch (Exception ex)
                {
                    Utils.WriteLog("Unable to request items to retrieve. Will retry soon. Error: " + Utils.GetMessageForException(ex));
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    continue;
                }


                if (response.Suggestions != null && response.Suggestions.Any())
                {
                    var first = response.Suggestions.First();
                    await InternetArchive.TryDownloadItemAsync(first.ItemName);
                    await NotifyDownloadedItemsAsync();
                }
                else 
                {
                    Utils.WriteLog("No new items available for download right now.");
                }

                if (response.RetryInSeconds > 0)
                {
                    Utils.WriteLog($"Waiting {response.RetryInSeconds} seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(response.RetryInSeconds));
                }

            }

        }

        private static void SetupAutostart()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var regkey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (UserConfiguration.RunOnStartup)
                {
                    regkey.SetValue("iabak-sharp", "\"" + Utils.GetApplicationPath() + "\"");
                }
                else 
                {
                    regkey.DeleteValue("iabak-sharp", false);
                }
            }
            else 
            { 
                 // not implemented
            }
        }

        private static async Task NotifyDownloadedItemsAsync()
        {
            var syncStatusFile = Path.Combine(RootFolder, ".lastsyncstatus-" + UserConfiguration.UserId);
            var lastSyncItems = File.Exists(syncStatusFile) ? File.ReadAllLines(syncStatusFile, Encoding.UTF8).Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)) : new string[] { };

            var currentItems = Directory.EnumerateDirectories(DataFolder).Select(x => Path.GetFileName(x)).ToList();
            var lostItems = lastSyncItems.Except(currentItems).ToList();
            var gainedItems = currentItems.Except(lastSyncItems).ToList();
            if (lostItems.Any() || gainedItems.Any())
            {
                Utils.WriteLog($"Notifying server of {gainedItems.Count} newly obtained item(s) and {lostItems.Count} removed one(s).");
                try
                {
                    await Utils.RpcAsync(new SyncRequest { GainedItems = gainedItems, LostItems = lostItems });
                    File.WriteAllLines(syncStatusFile, currentItems, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Utils.WriteLog("Unable to notify the server, will retry later. Error: " + Utils.GetMessageForException(ex));
                }
            }
        }

        public static void SaveConfig()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
            File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(UserConfiguration, Formatting.Indented));
        }


        public static string RootFolder => UserConfiguration?.Directory;
        public static string StagingFolder => Path.Combine(RootFolder, "staging");
        public static string DataFolder => Path.Combine(RootFolder, "data");
        public static string ConfigFilePath;
        public static Version IaBakVersion;



      
    }
}
