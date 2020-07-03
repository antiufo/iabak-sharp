using IaBak.Models;
using Shaman.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IaBak.Client
{
    static class InternetArchive
    {

        public static async Task DownloadItemAsync(string identifier)
        {
            var itemStagingDir = Path.Combine(Program.StagingFolder, identifier);
            var itemDoneDir = Path.Combine(Program.DataFolder, identifier);
            if (Directory.Exists(itemDoneDir)) return;
            Directory.CreateDirectory(itemStagingDir);


            var filesXml = identifier + "_files.xml";
            await DownloadFileToStagingAsync(identifier, filesXml, null);

            var files = ReadFilesXml(Path.Combine(itemStagingDir, filesXml));
            if (files.files.Any(x => x.@private == true))
                throw new Exception($"Unable to download the item, because one or more files in it are not public.");
            var nonDerivative = files.files.Where(x => x.source != "derivative").ToList();
            Utils.WriteLog($"Size of {identifier}: {new FileSize(nonDerivative.Sum(x => x.size ?? 0))}");
            foreach (var item in nonDerivative)
            {
                await RetryDownloadFileToStagingAsync(identifier, item.name, item);
            }
            foreach (var item in nonDerivative)
            {
                // So that we detect if the user manually deletes the staging folder while we continue to download the rest of the item.
                if (!File.Exists(Path.Combine(itemStagingDir, item.name)))
                    throw new Exception("One or more of the previously downloaded files was subsequentially found to be missing. Aborting download of the current item.");
            }
            Directory.Move(itemStagingDir, itemDoneDir);
            Utils.WriteLog($"Download of {identifier} completed.");
        }

        public static FilesXml ReadFilesXml(string path)
        {
            var ser = new System.Xml.Serialization.XmlSerializer(typeof(FilesXml));
            using var stream = File.Open(path, FileMode.Open);
            return (FilesXml)ser.Deserialize(stream);
        }

        public static async Task RetryDownloadFileToStagingAsync(string archiveItem, string relativePath, FileXml metadata)
        {
            var delay = 10;
            var attempts = 0;
            while (true)
            {
                attempts++;
                try
                {
                    await DownloadFileToStagingAsync(archiveItem, relativePath, metadata);
                    return;
                }
                catch (Exception ex)
                {
                    Utils.WriteLog($"Download failed for {archiveItem}/{relativePath}: " + Utils.GetInnermostException(ex));

                    if (attempts == 10) throw;

                    Utils.WriteLog($"Retrying in {delay} seconds.");
                    delay *= 2;
                }
            }
        }


        public static async Task DownloadFileToStagingAsync(string archiveItem, string relativePath, FileXml metadata)
        {
            var expectedSize = metadata?.size;
            var outputPath = Path.Combine(Program.StagingFolder, archiveItem, relativePath);
            if (File.Exists(outputPath)) return;
            var tempPath = outputPath + ".tmp";
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            Utils.WriteLog($"Retrieving {archiveItem}/{relativePath}" + (expectedSize != null ? $" ({new FileSize(expectedSize.Value)})" : null));
            using var response = await Utils.httpClient.GetAsync($"https://archive.org/download/{archiveItem}/{relativePath}", HttpCompletionOption.ResponseHeadersRead);
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
                        Utils.WriteLog("  " + new FileSize(totalRead) + " of " + (expectedSize != null ? new FileSize(expectedSize.Value).ToString() : "unknown"));
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
            Utils.WriteLog($"Warning! No hash information is available for ${metadata.name}.");
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

        public static async Task TryDownloadItemAsync(string identifier)
        {

            try
            {
                await DownloadItemAsync(identifier);
            }
            catch (Exception ex)
            {
                Utils.WriteLog($"Failed download for {identifier}: " + Utils.GetInnermostException(ex));
            }
        }


        public static async Task ResumeUnfinishedDownloadsAsync()
        {
            foreach (var dir in Directory.EnumerateDirectories(Program.StagingFolder))
            {
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    try
                    {
                        Directory.Delete(dir);
                    }
                    catch
                    {
                    }
                    continue;
                }
                Utils.WriteLog("Found partially downloaded item, resuming: " + dir);
                await TryDownloadItemAsync(Path.GetFileName(dir));
            }
        }

    }
}
