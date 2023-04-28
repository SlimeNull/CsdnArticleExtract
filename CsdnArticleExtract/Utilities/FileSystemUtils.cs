using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsdnArticleExtract.Utilities
{
    internal static class FileSystemUtils
    {
        private static Lazy<char[]> laziedInvalidPathChars = 
            new Lazy<char[]>(() => Path.GetInvalidPathChars());

        public static char[] InvalidPathChars => laziedInvalidPathChars.Value;

        public static string TrimEndingPathSeperator(this string origin)
        {
            return origin.TrimEnd('/', '\\');
        }
    }
}
