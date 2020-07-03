using IaBak.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace IaBak.Client
{
    static class Utils
    {


        internal readonly static HttpClient httpClient = new HttpClient();

        public static Exception GetInnermostException(Exception ex)
        {
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }
            return ex;
        }


        public static void WriteLog(string text)
        {
            Console.Error.WriteLine(text);
        }

        private static string ApiEndpoint = "https://iabak.shaman.io/iabak";

        public static async Task<TResponse> RpcAsync<TResponse>(RequestBase request) where TResponse : ResponseBase
        {
            var method = request.GetType().Name;
            if (!method.EndsWith("Request")) throw new ArgumentException();
            method = method.Substring(0, method.Length - "Request".Length);
            var httpResponse = await Utils.httpClient.PostAsync(ApiEndpoint + "/" + method, new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));
            httpResponse.EnsureSuccessStatusCode();

            var response = JsonConvert.DeserializeObject<TResponse>(await httpResponse.Content.ReadAsStringAsync());
            if (response.Error != null) throw new Exception(response.Error);
            return response;
        }

        public static DriveInfo GetParentDrive(string folder)
        {
            return DriveInfo.GetDrives()
                .OrderByDescending(x => x.RootDirectory.FullName)
                .First(x => folder.StartsWith(x.RootDirectory.FullName));
        }

        public static string GetApplicationPath()
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
    }
}
