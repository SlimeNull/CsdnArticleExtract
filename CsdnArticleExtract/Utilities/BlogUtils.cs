using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
}
