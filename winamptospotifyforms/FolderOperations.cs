using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace winamptospotifyforms
{
    public class FolderOperations
    {
        private readonly ILogger logger = new LoggerConfiguration().WriteTo.File("log-file-operations.txt", rollingInterval: RollingInterval.Day).CreateLogger();

        /// <summary>Gets track names from selected path.</summary>
        /// <param name="path">Selected folder path that contains mp3s</param>
        /// <param name="artist">This value get from folder get more successful results</param>
        /// <returns>List of mp3 file names</returns>
        public List<string> GetMp3FileNames(string path, string artist)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException($"{nameof(path)} is empty");
            if (string.IsNullOrWhiteSpace(artist)) throw new ArgumentNullException($"{nameof(artist)} is empty");

            FileInfo[] filesInfoArray = new DirectoryInfo(path).GetFiles();
            List<string> fileNames = new List<string>();

            if (filesInfoArray.Length > 0)
            {
                foreach (var file in filesInfoArray)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file.Name);

                    Regex reg = new Regex(@"[^\p{L}\p{N} ]");
                    fileName = reg.Replace(fileName, String.Empty);
                    fileName = Regex.Replace(fileName, @"[0-9]+", "");
                    fileName = fileName.ToLower().Replace(artist.ToLower(), "", StringComparison.InvariantCultureIgnoreCase);
                    fileName = fileName.TrimStart();
                    fileName = fileName.TrimEnd();
                    fileNames.Add(fileName);
                }
            }
            else
            {
                logger.Error($"Cannot find any file in {path}");
                throw new Exception($"Cannot find any file in {path}");
            }
            return fileNames;
        }
    }
}
