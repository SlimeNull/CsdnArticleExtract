namespace CsdnArticleExtract.Models
{
    public class CsdnGetBusinessListData
    {
        public List<CsdnArticleInfo> List { get; set; } = 
            new List<CsdnArticleInfo>(0);

        public int? Total { get; set; }
    }
}
