namespace CsdnArticleExtract.Models
{
    public class CsdnArticleInfo
    {
        public int ArticleId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public int Type { get; set; }
        public bool Top { get; set; }
        public bool ForcePlan { get; set; }
        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
        public string EditUrl { get; set; } = string.Empty;
        public string PostTime { get; set; } = string.Empty;
        public int DiggCount { get; set; }
        public string FormatTime { get; set; } = string.Empty;
        public List<string> PicList { get; set; } =
            new List<string>(0);
    }
}
