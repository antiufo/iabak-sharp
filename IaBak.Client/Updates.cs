using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IaBak.Client
{
    class Updates
    {
        public async static Task CheckForUpdatesAsync()
        {
            Utils.WriteLog("Checking for updates...");
            try
            {
                if (Program.UserConfiguration != null)
                {
                    Program.UserConfiguration.LastUpdateCheck = DateTime.UtcNow;
                    Program.SaveConfig();
                }
                var currentVersion = Program.IaBakVersion;
                var latestVersion = JsonConvert.DeserializeObject<UpdateCheckInfo>(await Utils.httpClient.GetStringAsync("https://iabak.shaman.io/latest-version.json"));
                if (Version.Parse(latestVersion.LatestVersion) <= currentVersion)
                {
                    Utils.WriteLog("No updates found.");
                    return;
                }
                Utils.WriteLog("Downloading update...");
                var url = Environment.OSVersion.Platform switch
                {
                    PlatformID.Win32NT => latestVersion.LatestVersionUrlWindowsX64,
                    PlatformID.Unix => latestVersion.LatestVersionUrlLinuxX64,
                    _ => throw new Exception("OS not supported: " + Environment.OSVersion.Platform)
                };

                var location = Process.GetCurrentProcess().MainModule.FileName;
                var tempPath = Path.Combine(Path.GetTempPath(), "update-" + Path.GetFileName(location));
                using var ms = new MemoryStream();
                using (var stream = await Utils.httpClient.GetStreamAsync(url))
                {
                    await stream.CopyToAsync(ms);
                }
                ms.Seek(0, SeekOrigin.Begin);
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Read))
                {
                    using var entry = zip.Entries.OrderByDescending(x => x.Length).First().Open();
                    using var temp = File.Create(tempPath);
                    await entry.CopyToAsync(temp);
                }

                Utils.WriteLog("Replacing old executable...");
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {

                    File.Move(location, location + "." + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".old");
                    File.Move(tempPath, location);
                }
                else
                {
                    var chmod = new Mono.Unix.UnixFileInfo(location).FileAccessPermissions;
                    new Mono.Unix.UnixFileInfo(tempPath).FileAccessPermissions = chmod;
                    File.Move(tempPath, location, true);
                }

                Program.singleInstanceLock.Close();

                var psi = new ProcessStartInfo(location);
                foreach (var item in Environment.GetCommandLineArgs().Skip(1))
                {
                    psi.ArgumentList.Add(item);
                }
                var ps = Process.Start(psi);
                ps.WaitForExit();
                Environment.Exit(ps.ExitCode);
            }
            catch (Exception ex)
            {
                Utils.WriteLog("An error occured while checking for updates: " + Utils.GetInnermostException(ex));
            }
        }

    }
}
