using System;
using System.IO;
using System.Windows.Forms;

namespace QobuzDownloaderX.Shared.Tools
{
    internal static class FileTools
    {
        public static void DeleteFilesByAge(string folderPath, int maxAgeInDays)
        {
            if (!Directory.Exists(folderPath))
            {
                return;
            }

            var thresholdDate = DateTime.Now.AddDays(-maxAgeInDays);

            foreach (var file in Directory.GetFiles(folderPath))
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTime < thresholdDate)
                {
                    File.Delete(file);
                }
            }
        }

        public static string GetInitializedLogDir()
        {
            var logDirPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "logs");
            if (!Directory.Exists(logDirPath))
            {
                Directory.CreateDirectory(logDirPath);
            }
			
            DeleteFilesByAge(logDirPath, 1);

            return logDirPath;
        }

        public static string GetInitializedSettingsDir()
        {
            var settingsDirPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "settings");
            if (!Directory.Exists(settingsDirPath))
            {
                Directory.CreateDirectory(settingsDirPath);
            }

            return settingsDirPath;
        }
    }
}