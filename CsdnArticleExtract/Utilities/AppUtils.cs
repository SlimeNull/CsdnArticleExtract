using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CsdnArticleExtract.Utilities
{
    internal static class AppUtils
    {
        public static Version CurrentVersion { get; } =
            Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
    }
}
