using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CsdnArticleExtract.Utilities
{
    internal static class BlogUtils
    {
        public static string GenerateBlogFilename(string englishTitle)
        {
            StringBuilder sb = new StringBuilder(englishTitle.Length);

            bool canAddUnderline = false;
            foreach (char c in englishTitle)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                    canAddUnderline = true;
                }
                else if (canAddUnderline)
                {
                    sb.Append('_');
                    canAddUnderline = false;
                }
            }

            return sb.ToString().TrimEnd('_');
        }
    }

    internal static class MatchUtils
    {
        public static Regex UriRegex { get; } = new Regex(
            // protocol identifier (optional)
            // short syntax // still required
            "(?:(?:(?:https?|ftp):)?\\/\\/)" +
            // user:pass BasicAuth (optional)
            "(?:\\S+(?::\\S*)?@)?" +
            "(?:" +
              // IP address exclusion
              // private & local networks
              "(?!(?:10|127)(?:\\.\\d{1,3}){3})" +
              "(?!(?:169\\.254|192\\.168)(?:\\.\\d{1,3}){2})" +
              "(?!172\\.(?:1[6-9]|2\\d|3[0-1])(?:\\.\\d{1,3}){2})" +
              // IP address dotted notation octets
              // excludes loopback network 0.0.0.0
              // excludes reserved space >= 224.0.0.0
              // excludes network & broadcast addresses
              // (first & last IP address of each class)
              "(?:[1-9]\\d?|1\\d\\d|2[01]\\d|22[0-3])" +
              "(?:\\.(?:1?\\d{1,2}|2[0-4]\\d|25[0-5])){2}" +
              "(?:\\.(?:[1-9]\\d?|1\\d\\d|2[0-4]\\d|25[0-4]))" +
            "|" +
              // host & domain names, may end with dot
              // can be replaced by a shortest alternative
              // (?![-_])(?:[-\\w\\u00a1-\\uffff]{0,63}[^-_]\\.)+
              "(?:" +
                "(?:" +
                  "[a-z0-9\\u00a1-\\uffff]" +
                  "[a-z0-9\\u00a1-\\uffff_-]{0,62}" +
                ")?" +
                "[a-z0-9\\u00a1-\\uffff]\\." +
              ")+" +
              // TLD identifier name, may end with dot
              "(?:[a-z\\u00a1-\\uffff]{2,}\\.?)" +
            ")" +
            // port number (optional)
            "(?::\\d{2,5})?" +
            // resource path (optional)
            "(?:[/?#]\\S*)?"
            );


        static Regex csdnBlogLinkRegex = new Regex(@"^https?://blog\.csdn\.net/(?<userName>\w+)/article/details/(?<blogId>\d+)");
        public static bool IsCsdnBlogLink(string blogLink, out string? userName, out long blogId)
        {
            userName = null;
            blogId = 0;

            if (!Uri.TryCreate(blogLink, UriKind.Absolute, out Uri? uri))
                return false;
            var match = csdnBlogLinkRegex.Match(blogLink);

            if (!match.Success)
                return false;

            userName = match.Groups["userName"].Value;
            blogId = long.Parse(match.Groups["blogId"].Value);

            return true;
        }
    }
}
