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
            Console.WriteLine($"Thank you. If you want to modify these settings in the future, edit '{Program.ConfigFilePath}'.");
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
