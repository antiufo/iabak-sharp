using IaBak.Models;
using Newtonsoft.Json;
using Shaman.Types;
using System;
using System.Diagnostics;
using System.IO;
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

        static async Task Main(string[] args)
        {
            ApplicationDirectory = GetApplicationPath();

            ConfigFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IaBak-sharp", "Configuration.json");
            if (!File.Exists(ConfigFilePath))
            {
                await UserRegistrationAsync();
            }

            var config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(ConfigFilePath));
            UserConfiguration = config;

            StagingFolder = Path.Combine(config.Directory, "staging");
            DataFolder = Path.Combine(config.Directory, "data");

            Directory.CreateDirectory(StagingFolder);
            Directory.CreateDirectory(DataFolder);
            var rootDrive = GetParentDrive(DataFolder).RootDirectory.FullName;
            while (true)
            {
                var now = DateTime.UtcNow;
                if (config.LastSync > now) config.LastSync = default;
                var nextSync = config.LastSync.AddMinutes(10);
                if (nextSync > now)
                {
                    var delay = nextSync - now;
                    WriteLog($"Next sync in {delay.TotalMinutes:0} minutes.");
                    await Task.Delay(delay);
                }
                WriteLog("Syncing...");

                var avail = new DriveInfo(rootDrive).AvailableFreeSpace;
                var thresholdBytes = (long)(config.LeaveFreeGb * 1024 * 1024 * 1024);
                if (avail < thresholdBytes)
                {
                    WriteLog(@$"Not syncing any other items, because less than {new FileSize(thresholdBytes)} are left on disk {rootDrive}.
To reduce the amount of reserved space, edit Configuration.json.
Saving to multiple drives is not currently supported.");
                    config.LastSync = now;
                    SaveConfig();
                    continue;
                }


                //RpcAsync(new SyncRequest {  })

                SaveConfig();
            }

            await DownloadItemAsync(
                "Topolino1412"
                //"archiveteam_archivebot_go_20190313130002"
                //"archiveteam_archivebot_go_20200227040015"
                );
        }

        private static void SaveConfig()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
            File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(UserConfiguration, Formatting.Indented));
        }

        private static DriveInfo GetParentDrive(string folder)
        {
            return DriveInfo.GetDrives()
                .OrderByDescending(x => x.RootDirectory.FullName)
                .First(x => folder.StartsWith(x.RootDirectory.FullName));
        }

        private static string GetApplicationPath()
        {
            var path = Path.GetDirectoryName(typeof(Program).Assembly.Location);

            string currentDir = path;
            while (true)
            {
                currentDir = Path.GetDirectoryName(currentDir);
                if (currentDir == null) break;
                if (Directory.Exists(Path.Combine(currentDir, ".git"))) return currentDir;
            }

            return path;
        }

        private async static Task UserRegistrationAsync()
        {
            Console.WriteLine("Welcome to IaBak-sharp.");
            Console.WriteLine();
            Console.Write("Enter your email address (optional, will *not* be publicly visible): ");
            var email = NormalizeString(Console.ReadLine());
            Console.Write("Enter a nickname (optional, *will* be publicly visible): ");
            var nickname = NormalizeString(Console.ReadLine());
            var defaultDir = Environment.OSVersion.Platform == PlatformID.Win32NT ? @"C:\IaBak" : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/iabak";
            string customDir;
            while (true)
            {
                Console.Write($"Where do you want to store your IA backups? [{defaultDir}]: ");
                customDir = NormalizeString(Console.ReadLine()) ?? defaultDir;
                if (!Path.IsPathRooted(customDir))
                {
                    Console.WriteLine("Please enter an absolute path.");
                    continue;
                }

                break;
            }
            const double defaultLeaveFree = 10;
            double customLeaveFree;
            while (true)
            {
                Console.Write($"How much space to you want to leave free for other uses, in GB? [{defaultLeaveFree}]: ");
                var customLeaveFreeStr = NormalizeString(Console.ReadLine()) ?? defaultLeaveFree.ToString();
                if (!double.TryParse(customLeaveFreeStr, out customLeaveFree))
                {
                    Console.WriteLine("Invalid number.");
                    continue;
                }
                break;
            }



            var config = new Configuration
            {
                UserEmail = email,
                Nickname = nickname,
                UserSecretKey = GenerateSecretKey(),
                Directory = customDir,
                LeaveFreeGb = customLeaveFree
            };

            Directory.CreateDirectory(config.Directory);
            var response = await RpcAsync<RegistrationResponse>(new RegistrationRequest
            {
                Email = config.UserEmail,
                Nickname = config.Nickname,
                SecretKey = config.UserSecretKey,
            });

            config.UserId = response.AssignedUserId;
            UserConfiguration = config;
            SaveConfig();

            Console.WriteLine();
            Console.WriteLine($"Thank you. If you want to modify these settings in the future, edit '{ConfigFilePath}'.");
            Console.WriteLine();

        }

        private static string GenerateSecretKey()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[16];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static string NormalizeString(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return null;
            return str.Trim();
        }
        public static string RootFolder = @"E:\IaBak";
        public static string StagingFolder = Path.Combine(RootFolder, "staging");
        public static string DataFolder = Path.Combine(RootFolder, "data");
        public static string ConfigFilePath;
        public static string ApplicationDirectory;
        private readonly static HttpClient httpClient = new HttpClient();
        private static string ApiEndpoint = "http://localhost:5000/iabak";

        public static async Task<TResponse> RpcAsync<TResponse>(RequestBase request) where TResponse : ResponseBase
        {
            var method = request.GetType().Name;
            if (!method.EndsWith("Request")) throw new ArgumentException();
            method = method.Substring(0, method.Length - "Request".Length);
            var httpResponse = await httpClient.PostAsync(ApiEndpoint + "/" + method, new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));
            httpResponse.EnsureSuccessStatusCode();

            var response = JsonConvert.DeserializeObject<TResponse>(await httpResponse.Content.ReadAsStringAsync());
            if (response.Error != null) throw new Exception(response.Error);
            return response;
        }


        public static async Task DownloadItemAsync(string identifier)
        {
            var itemStagingDir = Path.Combine(StagingFolder, identifier);
            var itemDoneDir = Path.Combine(DataFolder, identifier);
            if (Directory.Exists(itemDoneDir)) return;
            Directory.CreateDirectory(itemStagingDir);


            var filesXml = identifier + "_files.xml";
            await DownloadFileToStagingAsync(identifier, filesXml, null);

            var files = ReadFilesXml(Path.Combine(itemStagingDir, filesXml));
            var nonDerivative = files.files.Where(x => x.source != "derivative").ToList();
            WriteLog($"Size of {identifier}: {new FileSize(nonDerivative.Sum(x => x.size ?? 0))}");
            foreach (var item in nonDerivative)
            {
                await DownloadFileToStagingAsync(identifier, item.name, item);
            }
            foreach (var item in nonDerivative)
            {
                // So that we detect if the user manually deletes (parts of) the staging folder.
                if (!File.Exists(Path.Combine(itemStagingDir, item.name)))
                    throw new Exception($"File '{item.name}' disappeared from '{itemStagingDir}' before the item could be fully downloaded.");
            }
            Directory.Move(itemStagingDir, itemDoneDir);
            WriteLog($"Download of {identifier} completed.");
        }

        public static FilesXml ReadFilesXml(string path)
        {
            var ser = new System.Xml.Serialization.XmlSerializer(typeof(FilesXml));
            using var stream = File.Open(path, FileMode.Open);
            return (FilesXml)ser.Deserialize(stream);
        }

        public static async Task DownloadFileToStagingAsync(string archiveItem, string relativePath, FileXml metadata)
        {
            var expectedSize = metadata?.size;
            var outputPath = Path.Combine(StagingFolder, archiveItem, relativePath);
            if (File.Exists(outputPath)) return;
            var tempPath = outputPath + ".tmp";
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            WriteLog($"Retrieving {archiveItem}/{relativePath}" + (expectedSize != null ? $" ({new FileSize(expectedSize.Value)})" : null));
            using var response = await httpClient.GetAsync($"https://archive.org/download/{archiveItem}/{relativePath}", HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var lastModified = response.Content.Headers.LastModified;
            if (lastModified == null) throw new Exception($"Server did not return a Last-Modified header for '{archiveItem}/{relativePath}'.");
            var lastProgressPrint = Stopwatch.StartNew();
            using var stream = await response.Content.ReadAsStreamAsync();
            using (var tempStream = File.Create(tempPath))
            {
                var buffer = new byte[1 * 1024 * 1024];
                var totalRead = 0L;
                while (true)
                {
                    var read = await stream.ReadAsync(buffer);
                    if (read == 0) break;
                    await tempStream.WriteAsync(buffer, 0, read);
                    totalRead += read;
                    if (lastProgressPrint.ElapsedMilliseconds > 30_000)
                    {
                        WriteLog("  " + new FileSize(totalRead) + " of " + (expectedSize != null ? new FileSize(expectedSize.Value).ToString() : "unknown"));
                        lastProgressPrint.Restart();
                    }
                }
            }
            var actualFileLength = new FileInfo(tempPath).Length;
            if (expectedSize != null && actualFileLength != expectedSize)
                throw new Exception($"Unexpected size for item '{archiveItem}/{relativePath}': {expectedSize} according to metadata, but retrieved {actualFileLength}.");

            if (metadata != null)
            {
                CheckHash(tempPath, metadata);
            }

            File.SetLastWriteTimeUtc(tempPath, lastModified.Value.UtcDateTime);
            File.Move(tempPath, outputPath, overwrite: true);
        }

        private static void CheckHash(string tempPath, FileXml metadata)
        {
            if (CheckHash(tempPath, metadata.sha1, () => SHA1.Create())) return;
            if (CheckHash(tempPath, metadata.md5, () => MD5.Create())) return;
            // TODO: CRC is not built into .net. Are there archive items without neither sha1, nor md5?
            WriteLog($"Warning! No hash information is available for ${metadata.name}.");
        }

        private static bool CheckHash(string tempPath, string expectedHash, Func<HashAlgorithm> createHashAlgo)
        {
            if (expectedHash == null) return false;
            var actualHash = GetFileHashAsString(tempPath, createHashAlgo());
            if (actualHash != expectedHash) throw new Exception($"Hash mismatch for {tempPath}.");
            return true;
        }

        private static string ByteArrayToString(byte[] ba)
        {
            var hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
        private static string GetFileHashAsString(string path, HashAlgorithm algorithm)
        {

            using (algorithm)
            using (var file = File.OpenRead(path))
            {
                var hash = algorithm.ComputeHash(file);
                return ByteArrayToString(hash);
            };
        }

        private static void WriteLog(string text)
        {
            Console.Error.WriteLine(text);
        }
    }
}
