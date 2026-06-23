using System;
using System.IO;

namespace BedwarsOverlay.Core.LogWatching
{
    public static class LogPathDetector
    {
        /// <summary>
        /// Attempts to locate the Minecraft latest.log file.
        /// Priority: Badlion Client -> Vanilla fallback.
        /// </summary>
        public static string GetDefaultLogPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            
            // 1. Badlion Client Path: Roaming\.minecraft\logs\blclient\minecraft\latest.log
            string badlionPath = Path.Combine(appData, ".minecraft", "logs", "blclient", "minecraft", "latest.log");
            if (File.Exists(badlionPath))
            {
                return badlionPath;
            }

            // 2. Vanilla Minecraft Path: Roaming\.minecraft\logs\latest.log
            string vanillaPath = Path.Combine(appData, ".minecraft", "logs", "latest.log");
            return vanillaPath; // Return vanilla path as fallback
        }
    }
}
