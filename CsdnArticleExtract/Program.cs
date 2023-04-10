using System.Text;
using System.Text.RegularExpressions;
using CsdnArticleExtract;
using CsdnArticleExtract.Models;
using CsdnArticleExtract.Utilities;
using LibCsdnArticleExtract;
using Microsoft.Playwright;

internal class Program
{
    static Program()
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private static string AddFrontMatterForMarkdown(string markdown, BlogKind blog, CsdnArticleInfo article, IEnumerable<string> categories, IEnumerable<string> tags)
    {
        StringBuilder sb = new StringBuilder();


        sb.AppendLine("---");
        sb.AppendLine($"title: {article.Title}");

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

        if (categories.Count() > 0)
        {
            sb.AppendLine("categories");
            foreach (var category in categories)
                sb.AppendLine($"  - {category}");
        }

        if (blog != BlogKind.Hexo)
            sb.AppendLine($"description: {article.Description}");
        else
            sb.AppendLine($"excerpt: {article.Description}");

        sb.AppendLine("---");
        sb.AppendLine();
        sb.Append(markdown);

        return sb.ToString();
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Console.WriteLine(e.ExceptionObject);
        ConsoleUtils.PressAnyKeyToContinue();
    }

    private static async Task Main(string[] args)
    {
        Console.WriteLine(
            $"""
            SlimeNull/CsdnArticleExtract v{AppUtils.CurrentVersion}
              Source: https://github.com/SlimeNull/CsdnArticleExtract
            """);

        Console.WriteLine();

        ConsoleUtils.PressAnyKeyToContinue("按任意键开始程序");


        ConsoleUtils.Log("加载 Playwright");
        var playwright =
            await Playwright.CreateAsync();

        List<(string BrowserType, string ExecutablePath)> installedBrowsers = BrowserUtils.GetInstalledBrowsers()
            .Select(executablePath => (BrowserUtils.GetBrowserTypeFromExecutable(executablePath), executablePath))
            .Where(browser => browser.Item1 != null && browser.executablePath != null)
            .Select(browser => (browser.Item1!, browser.executablePath!))
            .ToList();

        string? browserPath = null;
        string? browserTypeStr = null;
        if (installedBrowsers.Count > 0)
        {
            if (ConsoleUtils.YesOrNo("程序在你的电脑上检测到了可用的浏览器, 你要直接使用它们吗?"))
            {
                int selection =
                    ConsoleUtils.Select("使用哪个?", installedBrowsers.Select(browser => browser.ExecutablePath).ToArray());
                if (selection < 0)
                    return;

                browserPath = installedBrowsers[selection].ExecutablePath;
                browserTypeStr = installedBrowsers[selection].BrowserType;
            }
        }

        if (browserPath == null)
        {
            if (ConsoleUtils.YesOrNo("你是否有浏览器现有的可执行文件? (如果没有, 程序会下载一个浏览器, 这会消耗一些时间)"))
                browserPath = ConsoleUtils.InputUtil("输入它的路径", path => File.Exists(path));
            browserTypeStr = BrowserUtils.GetBrowserTypeFromExecutable(browserPath);
        }

        IBrowserType? browserType = null;

        if (browserTypeStr != null)
        {
            if (browserTypeStr.Contains("chromium", StringComparison.CurrentCultureIgnoreCase))
                browserType = playwright.Chromium;
            else if (browserTypeStr.Contains("firefox", StringComparison.CurrentCultureIgnoreCase))
                browserType = playwright.Firefox;
        }
        else
        {
            browserType = ConsoleUtils.Select("你希望使用哪种浏览器进行抓取?", "Chromium", "Firefox", "Webkit") switch
            {
                0 => playwright.Chromium,
                1 => playwright.Firefox,
                2 => playwright.Webkit,
                _ => null
            };
        }

        if (browserType == null)
            return;

        if (browserPath == null)
        {
            int downloadExitCode = Microsoft.Playwright.Program.Main(new string[]
            {
                "install", $"{browserType.Name}",
            });
        }

        ConsoleUtils.Log("加载浏览器");
        await using var browser =
            await browserType.LaunchAsync(new BrowserTypeLaunchOptions()
            {
                Headless = false,
                ExecutablePath = browserPath
            });

        ConsoleUtils.Log("加载 Web 页面");
        var page =
            await browser.NewPageAsync();

        ConsoleUtils.Log("导航");
        await page.GotoAsync("https://www.csdn.net/");

        ConsoleUtils.PressAnyKeyToContinue(
            """
            现在, 请在打开的浏览器中登陆 CSDN 账户, 在登陆完之后按任意键继续.
            """);

        var avatar =
            await page.QuerySelectorAsync("#csdn-toolbar-profile .csdn-profile-avatar");

        if (avatar == null)
        {
            ConsoleUtils.Error("您似乎没有登陆成功, 因为程序没有找到包含个人主页链接的头像");
            ConsoleUtils.Tip("请重启程序并重试");
            ConsoleUtils.PressAnyKeyToContinue();
            return;
        }

        var homeAddr =
            await avatar.GetAttributeAsync("href");

        if (string.IsNullOrWhiteSpace(homeAddr))
        {
            ConsoleUtils.Error("您似乎没有登陆成功, 尽管程序找到了头像节点, 但是目标链接是空的");
            ConsoleUtils.Tip("请重启程序并重试");
            ConsoleUtils.PressAnyKeyToContinue();
            return;
        }

        Uri homeAddrUri =
            new Uri(homeAddr);

        string username =
            CsdnLinkUtils.GetUsernameFromHomeLink(homeAddrUri);

        ConsoleUtils.Log($"获取到用户名: {username}");

        ConsoleUtils.Log("获取文章信息");

        var articles =
            await CsdnArticleExtractApi.GetAllArticles(username);

        if (articles == null)
        {
            ConsoleUtils.Error("无法获取文章信息");
            ConsoleUtils.PressAnyKeyToContinue();
            return;
        }

        ConsoleUtils.Log($"共计: {articles.Count} 篇文章");

        ConsoleUtils.Log("马上就可以爬取文章了");

        Directory.CreateDirectory("output");
        Directory.CreateDirectory("output/assets");

        bool downImages =
            ConsoleUtils.YesOrNo("是否要将文章中的图片转存到本地呢?");
        bool writeHexoFrontMatter =
            ConsoleUtils.YesOrNo("是否在文章首添加 Front-matter?");
        BlogKind frontMatterKind =
            (BlogKind)ConsoleUtils.Select("要使用哪种博客的 Front-matter?", "Hexo", "Hugo", "Jekyll");


        Func<CsdnArticleInfo, string>? articleFileNameGetter =
            ConsoleUtils.Select("你希望使用什么来命名文章?", "文章的 ID", "文章的时间") switch
            {
                0 => article => $"{article.ArticleId}",
                1 => article => $"{article.PostTime}",
                _ => null
            };

        if (articleFileNameGetter == null)
            return;

        int delay = 0;
        if (ConsoleUtils.YesOrNo("是否要在爬取一篇文章之后进行延时呢?"))
        {
            string? delayStr =
                ConsoleUtils.InputUtil("输入延时时间(ms):", input => int.TryParse(input, out _));

            if (delayStr == null)
                return;

            delay = int.Parse(delayStr);
        }

        List<CsdnArticleInfo> succeedArticles =
            new List<CsdnArticleInfo>();
        List<CsdnArticleInfo> failedArticles =
            new List<CsdnArticleInfo>();

        var extraPage =
            await browser.NewPageAsync();

        foreach (var article in articles)
        {
            ConsoleUtils.Log("爬取第一篇文章");


            ConsoleUtils.Log("跳转到文章页面");
            await extraPage.GotoAsync($"https://blog.csdn.net/{username}/article/details/{article.ArticleId}");

            ConsoleUtils.Log("等待内容加载");
            await extraPage.WaitForSelectorAsync(".article-header .article-info-box");

            var categoriesElements =
                await extraPage.QuerySelectorAllAsync(".article-header .article-info-box .blog-tags-box .tags-box .tag-link[href*='category']");
            var tagElements =
                await extraPage.QuerySelectorAllAsync(".article-header .article-info-box .blog-tags-box .tags-box .tag-link[href*='search']");
            List<string> categories =
                new List<string>();
            List<string> tags =
                new List<string>();

            foreach (var categoryElement in categoriesElements)
                categories.Add(await categoryElement.InnerTextAsync());
            foreach (var tagElement in tagElements)
                tags.Add(await tagElement.InnerTextAsync());

            ConsoleUtils.Log($"已获取文章分类: {string.Join(", ", categories)}. 标签: {string.Join(", ", tags)}");


            ConsoleUtils.Log("跳转到编辑器页面");
            await page.GotoAsync($"https://editor.csdn.net/md/?articleId={article.ArticleId}");

            ConsoleUtils.Log("等待内容加载");
            var element =
                await page.WaitForSelectorAsync("div.app div.editor");

            if (element == null)
            {
                ConsoleUtils.Error("抓取失败, 找不到编辑器节点");
                failedArticles.Add(article);
                continue;
            }

            while (string.IsNullOrWhiteSpace(await element.InnerTextAsync()))
                await Task.Delay(10);

            ConsoleUtils.Log("获取内容");
            string markdown =
                await element.InnerTextAsync();

            if (downImages)
            {
                Regex markdownImageRegex =
                    new Regex(@"!\[(?<alt>.*)\]\((?<link>.*?)\)");

                ConsoleUtils.Log("解析图片引用");

                var matches =
                    markdownImageRegex.Matches(markdown);

                ConsoleUtils.Log($"共计: {matches.Count} 个图片引用");
                ConsoleUtils.Log("开始下载图片");
                foreach (Match match in matches)
                {
                    string imageLink =
                        match.Groups["link"].Value;

                    string imageFilename =
                        CsdnLinkUtils.GetFilenameFromImageLink(new Uri(imageLink));

                    string imageFullFilename =
                        $"output/assets/{imageFilename}";

                    if (File.Exists(imageFullFilename))
                        continue;

                    var response =
                        await page.APIRequest.GetAsync(imageLink);

                    var imageBody =
                        await response.BodyAsync();

                    File.WriteAllBytes(imageFullFilename, imageBody);
                }

                ConsoleUtils.Log("修正图片引用");

                markdown = markdownImageRegex.Replace(
                    markdown,
                    match => $"![{match.Groups["alt"].Value}](assets/{CsdnLinkUtils.GetFilenameFromImageLink(new Uri(match.Groups["link"].Value))})");
            }

            if (writeHexoFrontMatter)
            {
                markdown =
                    AddFrontMatterForMarkdown(markdown, frontMatterKind, article, categories, tags);
            }

            ConsoleUtils.Log("保存文章");

            string filename =
                articleFileNameGetter.Invoke(article);

            File.WriteAllText($"output/{filename}.md", markdown);

            succeedArticles.Add(article);

            ConsoleUtils.Log($"成功: {succeedArticles.Count} 个; 失败: {failedArticles.Count} 个");

            await Task.Delay(delay);
        }

        ConsoleUtils.PressAnyKeyToContinue();
    }


}