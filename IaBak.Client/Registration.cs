using IaBak.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IaBak.Client
{
    static class Registration
    {

        public async static Task UserRegistrationAsync()
        {
            try
            {
                await Utils.RpcAsync(new CheckServerStatusRequest());
            }
            catch (Exception ex)
            {
                Utils.WriteLog("Unable to contact iabak-sharp servers. Please try again later. Error: " + Utils.GetInnermostException(ex));
                Environment.Exit(1);
            }



            var config = new Configuration
            {
                UserSecretKey = GenerateSecretKey(),
            };

            Console.WriteLine("Welcome to IaBak-sharp.");
            Console.WriteLine();
            Console.Write("Enter your email address (optional, will *not* be publicly visible): ");
            config.UserEmail = NormalizeString(Console.ReadLine());
            Console.Write("Enter a nickname (optional, *will* be publicly visible, eg. in leaderboards): ");
            config.Nickname = NormalizeString(Console.ReadLine());
            var defaultDir = Environment.OSVersion.Platform == PlatformID.Win32NT ? @"C:\IaBak" : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/iabak";
            while (true)
            {
                Console.Write($"Where do you want to store your IA backups? [{defaultDir}]: ");
                config.Directory = NormalizeString(Console.ReadLine()) ?? defaultDir;
                if (!Path.IsPathRooted(config.Directory))
                {
                    Console.WriteLine("Please enter an absolute path.");
                    continue;
                }
                try
                {
                    Directory.CreateDirectory(config.Directory);
                }
                catch 
                {
                    Console.WriteLine("Unable to create the specified directory.");
                    continue;
                }
                break;
            }
            const double defaultLeaveFree = 10;
            double customLeaveFree;
            var drive = Utils.GetParentDrive(config.Directory);
            while (true)
            {
                Console.Write($"How much space to you want to leave free for other uses in {drive.Name}, in GB? [{defaultLeaveFree}]: ");
                var customLeaveFreeStr = NormalizeString(Console.ReadLine()) ?? defaultLeaveFree.ToString();
                if (!double.TryParse(customLeaveFreeStr, out customLeaveFree))
                {
                    Console.WriteLine("Invalid number.");
                    continue;
                }
                config.LeaveFreeGb = customLeaveFree;
                if (drive.AvailableFreeSpace < config.LeaveFreeBytes)
                {
                    Console.WriteLine($"Drive {drive.Name} doesn't have the specified amount of free space, so iabak-sharp would be left with nothing to download.");
                    continue;
                }
                break;
            }

            while (true)
            {
                Console.WriteLine("Do you want iabak-sharp to run automatically on startup? [Y]");
                var autostart = (NormalizeString(Console.ReadLine()) ?? "Y").ToUpper();
                if (autostart == "Y") config.RunOnStartup = true; 
                else if (autostart == "N") config.RunOnStartup = false;
                else
                {
                    continue;
                }
                break;
            }
            if (config.RunOnStartup && Environment.OSVersion.Platform != PlatformID.Win32NT)
                Console.WriteLine("Note that launch at startup is not currently implemented for Linux: https://github.com/antiufo/iabak-sharp/issues/1");
    

            Directory.CreateDirectory(config.Directory);
            var response = await Utils.RpcAsync(new RegistrationRequest
            {
                Email = config.UserEmail,
                Nickname = config.Nickname,
                SecretKey = config.UserSecretKey,
            });

            config.UserId = response.AssignedUserId;
            Program.UserConfiguration = config;
            Program.SaveConfig();

            Console.WriteLine();
            Console.WriteLine($"Thank you. If you want to modify these settings in the future, edit '{Program.ConfigFilePath}' (while the tool is not running).");
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
    }
}
