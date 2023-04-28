using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static CsdnArticleExtract.Utilities.YoudaoTranslate;

namespace CsdnArticleExtract.Utilities
{
    internal static class YoudaoTranslate
    {
        private static HttpClient client = new HttpClient();

        public static async Task<string?> TranslateAsync(string src, string fromLang, string toLang)
        {
            var resp = await client.PostAsync("https://aidemo.youdao.com/trans", new FormUrlEncodedContent(
                new Dictionary<string, string>()
                {
                    { "q", src },
                    { "from", fromLang },
                    { "to", toLang }
                }));

            string json = await resp.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<TransResult>(json)?.QueryResult;
        }

        public static Task<string?> TranslateToEnglish(string src)
        {
            return TranslateAsync(src, "auto", "en");
        }

        class NetPackage
        {
            public NetPackage(string errorCode)
            {
                this.errorCode = errorCode;
            }

            public string errorCode { get; set; } = string.Empty;
        }
        class TransResult : NetPackage
        {
            public TransResult(string[] returnPhrase, string query, string l, string tSpeakUrl, Web[] web, string requestId, string[] translation, Dict dict, Webdict webdict, Basic basic, bool isWord, string speakUrl, string errorCode) : base(errorCode)
            {
                this.returnPhrase = returnPhrase;
                this.query = query;
                this.l = l;
                this.tSpeakUrl = tSpeakUrl;
                this.web = web;
                this.requestId = requestId;
                this.translation = translation;
                this.dict = dict;
                this.webdict = webdict;
                this.basic = basic;
                this.isWord = isWord;
                this.speakUrl = speakUrl;
            }

            public string[] returnPhrase { get; set; }
            public string query { get; set; }
            public string l { get; set; }
            public string tSpeakUrl { get; set; } 
            public Web[] web { get; set; }
            public string requestId { get; set; }
            public string[] translation { get; set; } 
            public Dict dict { get; set; }
            public Webdict webdict { get; set; }
            public Basic basic { get; set; }
            public bool isWord { get; set; }
            public string speakUrl { get; set; }

            public string QueryText { get => query != null ? query : string.Empty; }
            public string QueryResult => translation?.FirstOrDefault() ?? string.Empty;

            public string LangFrom => l?.Split('2')?.FirstOrDefault() ?? string.Empty;
            public string LangTo => l?.Split('2')?.LastOrDefault() ?? string.Empty;
        }

        class Dict
        {
            public Dict(string url)
            {
                this.url = url;
            }

            public string url { get; set; }
        }

        class Webdict
        {
            public Webdict(string url)
            {
                this.url = url;
            }

            public string url { get; set; }
        }

        class Basic
        {
            public Basic(string[] exam_type, string usphonetic, string phonetic, string ukphonetic, Wf[] wfs, string ukspeech, string[] explains, string usspeech)
            {
                this.exam_type = exam_type;
                this.usphonetic = usphonetic;
                this.phonetic = phonetic;
                this.ukphonetic = ukphonetic;
                this.wfs = wfs;
                this.ukspeech = ukspeech;
                this.explains = explains;
                this.usspeech = usspeech;
            }

            public string[] exam_type { get; set; }
            public string usphonetic { get; set; }
            public string phonetic { get; set; }
            public string ukphonetic { get; set; }
            public Wf[] wfs { get; set; }
            public string ukspeech { get; set; }
            public string[] explains { get; set; }
            public string usspeech { get; set; }
        }

        class Wf
        {
            public Wf(Wf1 wf)
            {
                this.wf = wf;
            }

            public Wf1 wf { get; set; }
        }

        class Wf1
        {
            public Wf1(string name, string value)
            {
                this.name = name;
                this.value = value;
            }

            public string name { get; set; }
            public string value { get; set; }
        }

        class Web
        {
            public Web(string[] value, string key)
            {
                this.value = value;
                this.key = key;
            }

            public string[] value { get; set; }
            public string key { get; set; }
        }
    }
}
