using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using CsdnArticleExtract.Models;
using LibCsdnArticleExtract.Utilities;

namespace LibCsdnArticleExtract
{
    public static class CsdnArticleExtractApi
    {
        private static HttpClient HttpClient { get; } = new HttpClient()
        {
            BaseAddress = new Uri("https://blog.csdn.net/")
        };

        public static async Task<List<CsdnArticleInfo>?> GetAllArticles(string? username)
        {
            Guard.ThrowIfNull(username, nameof(username));

            string encodedUsername =
                Uri.EscapeDataString(username);

            List<CsdnArticleInfo> allArticles = new List<CsdnArticleInfo>();

            int page = 1;
            while (true)
            {
                HttpRequestMessage request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"/community/home-api/v1/get-business-list?page={page}&size=100&businessType=blog&orderby=&noMore=false&year=&month=&username={encodedUsername}");

                HttpResponseMessage response =
                    await HttpClient.SendAsync(request);

                CsdnResponse<CsdnGetBusinessListData>? data =
                    await response.Content.ReadFromJsonAsync<CsdnResponse<CsdnGetBusinessListData>>();

                if (data == null)
                    return null;

                if (data.Data == null)
                    return null;

                foreach (var artical in data.Data.List)
                    allArticles.Add(artical);

                if (allArticles.Count == data.Data.Total)
                    break;

                page++;
            }

            return allArticles;
        }
    }
}
