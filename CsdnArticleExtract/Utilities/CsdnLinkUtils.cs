using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsdnArticleExtract.Utilities
{
    internal static class CsdnLinkUtils
    {
        public static string GetUsernameFromHomeLink(Uri uri)
        {
            if (!uri.Host.Equals("blog.csdn.net", StringComparison.CurrentCultureIgnoreCase))
                throw new ArgumentException("Invalid URI", nameof(uri));

            return uri.AbsolutePath.Trim('/');
        }

        public static string GetFilenameFromImageLink(Uri uri)
        {
            return uri.Segments.LastOrDefault() ?? throw new ArgumentException("Invalid URI", nameof(uri));
        }
    }
}
