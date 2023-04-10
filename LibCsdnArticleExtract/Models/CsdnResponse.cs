using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsdnArticleExtract.Models
{
    public class CsdnResponse<T>
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public Guid TraceId { get; set; }
        public T? Data { get; set; }
    }
}
