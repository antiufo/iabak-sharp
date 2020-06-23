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
    class Program
    {

        public Configuration UserConfiguration;

        static async Task Main(string[] args)
        {
            ApplicationDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            ConfigFilePath = Path.Combine(ApplicationDirectory, "Configuration.json");
            if (!File.Exists(ConfigFilePath))
            {
                await UserRegistrationAsync();
            }

            var config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(ConfigFilePath));

            StagingFolder = Path.Combine(config.Directory, "staging");
            DataFolder = Path.Combine(config.Directory, "data");

            Directory.CreateDirectory(StagingFolder);
            Directory.CreateDirectory(DataFolder);



            await DownloadItemAsync(
                "Topolino1412"
                //"archiveteam_archivebot_go_20190313130002"
                //"archiveteam_archivebot_go_20200227040015"
                );
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
                UserId = Guid.NewGuid().ToString(),
                UserSecretKey = GenerateSecretKey(),
                Directory = customDir,
                LeaveFreeGb = customLeaveFree
            };
            Directory.CreateDirectory(config.Directory);
            File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));

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
