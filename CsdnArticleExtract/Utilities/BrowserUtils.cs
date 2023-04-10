using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace CsdnArticleExtract.Utilities
{
    public static class BrowserUtils
    {
        public static List<string> GetInstalledBrowsers()
        {
            List<string> possiblePaths = new List<string>();

            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            // Check for Edge and Chrome
            possiblePaths.Add(Path.Combine(programFiles, "Microsoft", "Edge", "Application", "msedge.exe"));
            possiblePaths.Add(Path.Combine(programFilesX86, "Microsoft", "Edge", "Application", "msedge.exe"));
            possiblePaths.Add(Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe"));
            possiblePaths.Add(Path.Combine(programFilesX86, "Google", "Chrome", "Application", "chrome.exe"));

            // Check for Firefox
            possiblePaths.Add(Path.Combine(programFiles, "Mozilla Firefox", "firefox.exe"));
            possiblePaths.Add(Path.Combine(programFilesX86, "Mozilla Firefox", "firefox.exe"));

            List<string> result = new List<string>();
            foreach (var path in possiblePaths)
                if (File.Exists(path))
                    result.Add(path);

            return result;
        }


        /// <summary>
        /// Get the browser type of executable.
        /// Possible values: chromium, firefox, webkit
        /// </summary>
        /// <param name="executable"></param>
        /// <returns></returns>
        public static string? GetBrowserTypeFromExecutable(string? executable)
        {
            if (executable == null)
                return null;
            if (!File.Exists(executable))
                return null;

            FileVersionInfo fileInfo = FileVersionInfo.GetVersionInfo(executable);
            string? productName = fileInfo.ProductName;
            string? company = fileInfo.CompanyName;

            if (productName == null ||
                company == null)
                return null;

            if (productName.Contains("Microsoft Edge"))
                return "chromium";

            if (productName.Contains("Google Chrome") || company.Contains("Google Inc."))
                return "chromium";

            if (productName.Contains("Firefox") || company.Contains("Mozilla"))
                return "firefox";

            return null;
        }
    }
}
