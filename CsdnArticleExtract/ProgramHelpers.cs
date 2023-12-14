using System.Text;
using CsdnArticleExtract;
using CsdnArticleExtract.Models;
using CsdnArticleExtract.Utilities;
using Microsoft.Playwright;

internal static class ProgramHelpers
{
    public static async Task<string> DownloadImageToLocal(IPage page, string imageLink, string localDirectory)
    {
        string imageFilename =
            CsdnLinkUtils.GetFilenameFromImageLink(new Uri(imageLink));

        string imageFullFilename = Path.Combine(
            Path.Combine("output", localDirectory),
            imageFilename);

        var dirFullPath = Path.GetDirectoryName(imageFullFilename);
        if (dirFullPath != null && !Directory.Exists(dirFullPath))
            Directory.CreateDirectory(dirFullPath);

        if (File.Exists(imageFullFilename))
            return imageFilename;

        var response =
            await page.APIRequest.GetAsync(imageLink, new APIRequestContextOptions()
            {
                Timeout = 120000
            });

        var imageBody =
            await response.BodyAsync();


        await File.WriteAllBytesAsync(imageFullFilename, imageBody);

        return imageFilename;
    }

    public static string GetFrontMatterForMarkdown(string markdown, string? featureImagePath, bool isTop, BlogKind blog, CsdnArticleInfo article, IEnumerable<string> categories, IEnumerable<string> tags)
    {
        StringBuilder sb = new StringBuilder();


        sb.AppendLine("---");
        sb.AppendLine($"title: '{article.Title}'");

        DateTime date = DateTime.Parse(article.PostTime);
        sb.AppendLine(blog switch
        {
            BlogKind.Jekyll => $"date: {date.ToString("yyyy-MM-dd HH:mm:ss zzz")}",
            _ => $"date: {date.ToString("yyyy-MM-dd HH:mm:ss")}",
        });

        if (tags.Count() > 0)
        {
            sb.AppendLine("tags:");
            foreach (var tag in tags)
                sb.AppendLine($"  - {tag}");
        }

        if (categories.Count() > 0 && blog != BlogKind.Gridea)
        {
            sb.AppendLine("categories:");
            foreach (var category in categories)
                sb.AppendLine($"  - {category}");
        }

        if (blog != BlogKind.Gridea)
        {
            if (blog != BlogKind.Hexo)
                sb.AppendLine($"description: '{article.Description}'");
            else
                sb.AppendLine($"excerpt: '{article.Description}'");
        }

        if (blog == BlogKind.Gridea)
        {
            sb.AppendLine($"published: true");
            sb.AppendLine($"hideInList: false");
            sb.AppendLine($"feature: {featureImagePath}");
            sb.AppendLine($"isTop: {isTop}");
        }

        sb.AppendLine("---");
        sb.AppendLine();
        sb.Append(markdown);

        return sb.ToString();
    }

    public static string ResolveImageToLocal(string imageLink, string localDirectory)
    {
        return $"{localDirectory}/{CsdnLinkUtils.GetFilenameFromImageLink(new Uri(imageLink))}";
    }
}