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
        ConsoleUtils.Log("爬取结果将会保存到 output 目录下");

        Directory.CreateDirectory("output");
        Directory.CreateDirectory("output/assets");

        bool downImages =
            ConsoleUtils.YesOrNo("是否要将文章中的图片转存到本地呢? ");
        bool fixBlogLinks =
            ConsoleUtils.YesOrNo("是否要将文章的博客引用链接修正呢? ");
        string? linkBaseAddress = null;
        if (fixBlogLinks)
            linkBaseAddress = ConsoleUtils.Input("输入修正后链接的基地址: ");
        string downImagesPath =
            ConsoleUtils.YesOrNo("是否要自定义图片文件夹输出路径呢? 默认是 output/assets") ?
            ConsoleUtils.InputUtil("输入它的路径", path =>
            {
                // 无效字符校验
                if (path.Any(chr => FileSystemUtils.InvalidPathChars.Contains(chr)))
                    return false;

                return true;
            }).TrimEndingPathSeperator() : "images";
        bool writeHexoFrontMatter =
            ConsoleUtils.YesOrNo("是否在文章首添加 Front-matter?");
        BlogKind frontMatterKind =
            ConsoleUtils.Select<BlogKind>("要使用哪种博客的 Front-matter?");


        Func<CsdnArticleInfo, Task<string>>? articleFileNameGetter =
            ConsoleUtils.Select("你希望使用什么来命名文章?", "文章的 ID", "文章的时间", "文章标题", "文章标题的英文") switch
            {
                0 => article => Task.FromResult($"{article.ArticleId}"),
                1 => article => Task.FromResult($"{article.PostTime}"),
                2 => article =>
                {
                    StringBuilder sb = new StringBuilder(article.Title);
                    sb.Replace('\\', '_');
                    sb.Replace('/', '_');
                    sb.Replace(':', ' ');
                    sb.Replace('*', ' ');
                    sb.Replace('?', ',');
                    sb.Replace('"', '\'');
                    sb.Replace('<', '(');
                    sb.Replace('>', ')');
                    sb.Replace('|', ' ');

                    return Task.FromResult(sb.ToString());
                }
                ,
                3 => async (CsdnArticleInfo article) =>
                {
                    string? englishTitle = await YoudaoTranslate.TranslateToEnglish(article.Title);
                    if (englishTitle==null)
                        return $"{article.ArticleId}";

                    return BlogUtils.GenerateBlogFilename(englishTitle);
                }
                ,
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

        ConsoleUtils.Log($"开始爬取");
        foreach (var article in articles)
        {
            ConsoleUtils.Log($"正在爬取: {article.Title}");

            string newArticleTitle =
                await articleFileNameGetter.Invoke(article);

            ConsoleUtils.Log("跳转到文章和编辑器页面");
            var pageGoOptions = new PageGotoOptions() { Timeout = 180000 };
            var nav1 = extraPage.GotoAsync($"https://blog.csdn.net/{username}/article/details/{article.ArticleId}", pageGoOptions);
            var nav2 = page.GotoAsync($"https://editor.csdn.net/md/?articleId={article.ArticleId}", pageGoOptions);
            await Task.WhenAny(Task.WhenAll(nav1, nav2), Task.Delay(200));

            ConsoleUtils.Log("等待文章加载");
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

            ConsoleUtils.Log("等待编辑器加载");
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

            ConsoleUtils.Log("获取文章内容");
            string markdown =
                await element.InnerTextAsync();

            if (downImages)
            {
                Regex markdownImageRegex =
                    new Regex(@"!\[(?<alt>.*)\]\((?<link>.*?)\)");

                var matches =
                    markdownImageRegex.Matches(markdown);

                if (matches.Count > 0)
                {
                    ConsoleUtils.Log($"解析到 {matches.Count} 个图片引用");
                    ConsoleUtils.Log("开始下载图片");
                    foreach (Match match in matches)
                    {
                        string imageLink =
                        match.Groups["link"].Value;

                        if (Uri.TryCreate(imageLink, UriKind.Absolute, out var imageUri))
                        {
                            if (imageUri.Host.Equals("img-blog.csdnimg.cn", StringComparison.OrdinalIgnoreCase))
                            {
                                if (imageLink.IndexOf('?') is int end && end > 0)
                                    imageLink = imageLink.Substring(0, end);
                            }
                        }

                        await ProgramHelpers.DownloadImageToLocal(page, imageLink, downImagesPath);
                        Console.WriteLine($"图片 {imageLink} 下载完毕");
                    }

                    ConsoleUtils.Log("修正图片引用");

                    markdown = markdownImageRegex.Replace(
                        markdown,
                        match => $"![{match.Groups["alt"].Value}]({ProgramHelpers.ResolveImageToLocal(match.Groups["link"].Value, downImagesPath)})");
                }
            }

            if (fixBlogLinks)
            {
                int replaced = 0;
                MatchUtils.UriRegex.Replace(markdown, match =>
                {
                    if (!MatchUtils.IsCsdnBlogLink(match.Value, out var userName, out var blogId))
                        return match.Value;

                    replaced++;
                    return Path.Combine(linkBaseAddress!, newArticleTitle).Replace('\\', '/');
                });

                if (replaced > 0)
                    ConsoleUtils.Log($"修正了 {replaced} 个博客链接");
            }

            if (writeHexoFrontMatter)
            {
                string? pic = article.PicList.FirstOrDefault();
                string? localPic = null;
                if (pic != null)
                {
                    await ProgramHelpers.DownloadImageToLocal(page, pic, downImagesPath);
                    localPic = ProgramHelpers.ResolveImageToLocal(pic, downImagesPath);
                }

                markdown =
                    ProgramHelpers.GetFrontMatterForMarkdown(markdown, localPic, article.Top, frontMatterKind, article, categories, tags);
            }

            File.WriteAllText($"output/{newArticleTitle}.md", markdown);

            succeedArticles.Add(article);

            ConsoleUtils.Log($"成功: {succeedArticles.Count} 个; 失败: {failedArticles.Count} 个");

            await Task.Delay(delay);
        }

        ConsoleUtils.PressAnyKeyToContinue();
    }


    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Console.WriteLine(e.ExceptionObject);
        ConsoleUtils.PressAnyKeyToContinue();
    }

}