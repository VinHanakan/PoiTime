using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReportTimePlayer
{
    public sealed class WikiEnrichmentResult
    {
        public int TextCount { get; set; }
        public bool PortraitDownloaded { get; set; }
        public string PortraitError { get; set; }

        public string Summary => $"已补全 {TextCount} 条报时台词，" +
            (PortraitDownloaded ? "已下载立绘。" :
             PortraitError != null ? "立绘下载失败：" + PortraitError : "未找到可下载的立绘或已有本地立绘。");
    }

    public static class WikiVoicePackService
    {
        private const string WikiApi = "https://zh.kcwiki.cn/api.php";
        private const string FileRedirect = "https://zh.kcwiki.cn/wiki/Special:Redirect/file/";

        public static async Task<WikiEnrichmentResult> EnrichAsync(
            string shipId, string wikiName, string targetFolder, HttpClient client)
        {
            if (string.IsNullOrWhiteSpace(shipId) || string.IsNullOrWhiteSpace(wikiName))
                throw new ArgumentException("需要舰娘 ID 和 Wiki 页面名称。");

            string configPath = Path.Combine(targetFolder, "config.json");
            var config = JsonSerializer.Deserialize<VoiceFolderConfig>(File.ReadAllText(configPath));
            if (config == null) throw new InvalidDataException("语音包配置无效。");

            string uri = WikiApi + "?action=parse&prop=wikitext&format=json&redirects=1&page=" +
                Uri.EscapeDataString(wikiName);
            using (var response = await client.GetAsync(uri))
            {
                response.EnsureSuccessStatusCode();
                using (var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync()))
                {
                    if (!document.RootElement.TryGetProperty("parse", out JsonElement parsed))
                        throw new InvalidOperationException("Wiki 上未找到页面：" + wikiName);

                    string source = parsed.GetProperty("wikitext").GetProperty("*").GetString();
                    var lines = ParseHourlyLines(source);
                    var result = new WikiEnrichmentResult();
                    foreach (var voice in config.voices ?? Enumerable.Empty<VoiceAnnouncement>())
                    {
                        string key = Path.GetFileNameWithoutExtension(voice.fileName ?? "");
                        if (string.IsNullOrWhiteSpace(voice.text) && lines.TryGetValue(key, out string text))
                        {
                            voice.text = text;
                            result.TextCount++;
                        }
                    }

                    string portraitName = string.IsNullOrWhiteSpace(config.portrait)
                        ? "portrait.png" : Path.GetFileName(config.portrait);
                    if (!File.Exists(Path.Combine(targetFolder, portraitName)))
                    {
                        string defaultPortrait = Path.Combine(targetFolder, "portrait.png");
                        if (File.Exists(defaultPortrait))
                        {
                            config.portrait = "portrait.png";
                        }
                        else
                        {
                            string wikiFile = FindPortraitFile(source, shipId);
                            if (wikiFile != null)
                            {
                                try
                                {
                                    await DownloadPortraitAsync(client, wikiFile, defaultPortrait);
                                    config.portrait = "portrait.png";
                                    result.PortraitDownloaded = true;
                                }
                                catch (Exception ex)
                                {
                                    result.PortraitError = ex.Message;
                                }
                            }
                        }
                    }

                    File.WriteAllText(configPath, JsonSerializer.Serialize(config,
                        new JsonSerializerOptions { WriteIndented = true }));
                    return result;
                }
            }
        }

        public static Dictionary<string, string> ParseHourlyLines(string source)
        {
            var lines = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in Regex.Matches(source ?? "",
                @"\{\{台词翻译表(?!/)(?<body>.*?)\}\}", RegexOptions.Singleline))
            {
                string body = match.Groups["body"].Value;
                string file = Field(body, "档名");
                if (!Regex.IsMatch(file, @"^\d+[a-z]?-\d{4}$", RegexOptions.IgnoreCase)) continue;
                string text = Field(body, "中文译文");
                if (string.IsNullOrWhiteSpace(text)) text = Field(body, "日文台词");
                text = CleanWikiText(text);
                if (!string.IsNullOrWhiteSpace(text)) lines[file] = text;
            }
            return lines;
        }

        public static string FindPortraitFile(string source, string shipId)
        {
            var gallery = Regex.Match(source ?? "", @"<gallery[^>]*>(?<body>.*?)</gallery>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!gallery.Success) return null;

            var candidates = Regex.Matches(gallery.Groups["body"].Value,
                @"(?:File:)?(?<file>KanMusu(?<id>\d+[a-z]?)(?:HD)?Illust\.(?:png|jpg|jpeg))\s*\|",
                RegexOptions.IgnoreCase).Cast<Match>().ToList();
            string exact = NormalizeId(shipId);
            string baseId = NormalizeId(Regex.Replace(shipId ?? "", "[a-z]$", "",
                RegexOptions.IgnoreCase));
            var chosen = candidates.FirstOrDefault(m => NormalizeId(m.Groups["id"].Value) == exact)
                ?? candidates.FirstOrDefault(m => NormalizeId(m.Groups["id"].Value) == baseId);
            return chosen?.Groups["file"].Value;
        }

        private static string NormalizeId(string id)
        {
            var match = Regex.Match(id ?? "", @"^(?<number>\d+)(?<suffix>[a-z]?)$",
                RegexOptions.IgnoreCase);
            if (!match.Success) return id ?? "";
            return int.Parse(match.Groups["number"].Value) +
                match.Groups["suffix"].Value.ToLowerInvariant();
        }

        private static string Field(string templateBody, string fieldName)
        {
            var match = Regex.Match(templateBody,
                @"(?m)^\s*\|\s*" + Regex.Escape(fieldName) + @"\s*=\s*(?<value>.*)$");
            return match.Success ? match.Groups["value"].Value.Trim() : "";
        }

        private static string CleanWikiText(string value)
        {
            value = Regex.Replace(value ?? "", @"<br\s*/?>", " ", RegexOptions.IgnoreCase);
            value = Regex.Replace(value, @"\[\[(?:[^|\]]*\|)?([^\]]+)\]\]", "$1");
            value = Regex.Replace(value, @"<[^>]+>", "");
            return Regex.Replace(WebUtility.HtmlDecode(value), @"\s+", " ").Trim();
        }

        private static async Task DownloadPortraitAsync(HttpClient client, string wikiFile, string path)
        {
            string temporaryPath = path + ".download";
            string convertedPath = path + ".new";
            try
            {
                using (var response = await client.GetAsync(FileRedirect + Uri.EscapeDataString(wikiFile),
                    HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    string type = response.Content.Headers.ContentType?.MediaType;
                    if (type == null || !type.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Wiki 返回的不是图片。");
                    using (var source = await response.Content.ReadAsStreamAsync())
                    using (var target = new FileStream(temporaryPath, FileMode.Create))
                        await source.CopyToAsync(target);
                }
                using (var image = Image.FromFile(temporaryPath))
                    image.Save(convertedPath, System.Drawing.Imaging.ImageFormat.Png);
                File.Move(convertedPath, path);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                if (File.Exists(convertedPath)) File.Delete(convertedPath);
            }
        }
    }
}
