using CyNewsCorner.DataModels;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using AngleSharp.Html.Parser;
using AngleSharp.Html;

namespace CyNewsCorner
{
    public class BackgroundService: IHostedService, IDisposable
    {
        private readonly ILogger<BackgroundService> _logger;

        private Timer _saveTimer;

        private Timer _flushTimer;
        
        private int _number;
        
        private IDatabase RedisDb{get;set;}
        
        private IServer RedisServer { get; set; }
        
        private List<NewsSource> Sources { get; set; }

        private IConfiguration _config { get; }
        
        public BackgroundService(ILogger<BackgroundService> logger, IConfiguration configuration) {
            _logger = logger;
            _config = configuration;
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(string.Format("{0}:{1},{2}", _config["redisHost"], _config["redisPort"], "allowAdmin=true"));
            IDatabase db = redis.GetDatabase();
            RedisDb = db;
            int redisPort = int.Parse(_config["redisPort"]);
            RedisServer = redis.GetServer(_config["redisHost"], redisPort);

            _logger.LogInformation("Parsing sources json...");
            using (var reader = new StreamReader(@"news_sources.json"))
            {
                //read sources from csv
                // while (!reader.EndOfStream)
                // {
                //     var line = reader.ReadLine();
                //     var values = line.Split(",").ToList();
                //     Sources = values;
                // }

                //read sources from json
                string json = reader.ReadToEnd();
                var resultObjects = AllChildren(JObject.Parse(json))
                    .First(c => c.Type == JTokenType.Array && c.Path.Contains("sources"))
                    .Children<JObject>();

                var sources = new List<NewsSource>();

                foreach (var source in resultObjects)
                {
                    var newsSource = new NewsSource();
                    foreach (JProperty property in source.Properties())
                    {
                        switch (property.Name)
                        {
                            case "id":
                                newsSource.Id = int.Parse(property.Value.ToString());
                                break;
                            case "name":
                                newsSource.Name = property.Value.ToString();
                                break; 
                            case "imageUrl":
                                newsSource.ImageUrl = property.Value.ToString();
                                break; 
                            case "rssUrl":
                                newsSource.RssUrl = property.Value.ToString();
                                break;
                            case "url":
                                newsSource.Url = property.Value.ToString();
                                break; 
                            case "isActive":
                                newsSource.IsActive = bool.Parse(property.Value.ToString());
                                break;
                        }
                    }
                    sources.Add(newsSource);
                }

                Sources = sources.Where(q => q.IsActive).ToList();
                _logger.LogInformation("Parsing sources json Succeeded.");
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _flushTimer = new Timer(o => {
                    Interlocked.Increment(ref _number);
                    Console.Out.WriteLineAsync("Starting flushing cache..");
                    DeleteOldPosts();
                },
                null,
                TimeSpan.Zero,
                TimeSpan.FromDays(1));
            
            _saveTimer = new Timer(o => {
                Interlocked.Increment(ref _number);
                Console.Out.WriteLineAsync("Starting saving in cache..");
                SavePosts();
            },
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(1));
         
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _saveTimer?.Dispose();
            _flushTimer?.Dispose();
        }
        
        #region Private
        
        private List<Post> ParseXmls()
        {
            _logger.LogInformation("Parsing news xmls...");

            try
            {
                var news = new List<Post>();
                foreach (var source in Sources)
                {
                    var startTime = DateTime.UtcNow;
                    // if (!isValidResponse(source))
                    // {
                    //     Console.Out.WriteLineAsync(string.Format("Source {0} is invalid.", source));
                    //     continue;
                    // }

                    XDocument doc = XDocument.Load(source.RssUrl);
                    var endTime = DateTime.UtcNow;
                    XNamespace Snmp = "http://www.w3.org/2005/Atom";

                    foreach (var element in doc.Elements())
                    {
                        var elements = element.Element("channel") == null ? element.Descendants(Snmp+"entry") : element.Element("channel").Elements("item");
                        foreach (var e in elements)
                        {
                            var post = new Post();

                            if (e.Name.LocalName == "entry")
                            {
                                post.Title = e.Element(Snmp + "title") == null ? "" : e.Element(Snmp + "title").Value;
                                post.Category = e.Element(Snmp + "category") == null ? "" : e.Element(Snmp + "category").Value;
                                post.ExternalUrl = e.Element(Snmp + "id") == null ? "" : e.Element(Snmp + "id").Value;
                                var content = e.Element(Snmp + "content") == null ? "" : e.Element(Snmp + "content").Value;
                                post.Description = ContentPreprocessing(content, post.ExternalUrl); 
                                post.Source = source.Name;
                                post.SourceUrl = GetSourceUrl(source.Name);
                                post.SourceLogo = GetSourceLogoPath(source.Name);
                                var slug = post.Title.Replace(" ", "-");
                                slug = Regex.Replace(slug, "[^0-9a-zA-Z-,]+", "").ToLower();
                                post.Slug = slug;
                                //post.Image = e.Element("enclosure") == null
                                //    ? ""
                                //    : e.Element("enclosure").Attribute("url").Value;
                                post.Image = GetThumbnail(post.Description, "entry");
                                post.PublishDatetime = e.Element(Snmp + "published") == null ? "" : Convert.ToDateTime(e.Element(Snmp + "published").Value, CultureInfo.CurrentCulture).ToString();
                            }
                            else
                            {
                                post.Title = e.Element("title") == null ? "" : e.Element("title").Value;
                                post.Category = e.Element("category") == null ? "" : e.Element("category").Value;
                                post.ExternalUrl = e.Element("link") == null ? "" : e.Element("link").Value;
                                var content = e.Element("description") == null ? "" : e.Element("description").Value;
                                post.Description = ContentPreprocessing(content, post.ExternalUrl);
                                post.Source = source.Name;
                                post.SourceUrl = GetSourceUrl(source.Name);
                                post.SourceLogo = GetSourceLogoPath(source.Name);
                                var slug = post.Title.Replace(" ", "-");
                                slug = Regex.Replace(slug, "[^0-9a-zA-Z-,]+", "").ToLower();
                                post.Slug = slug;
                                //post.Image = e.Element("enclosure") == null
                                //    ? ""
                                //    : e.Element("enclosure").Attribute("url").Value;
                                post.Image = GetThumbnail(e.ToString());
                                post.PublishDatetime = e.Element("pubDate") == null ? "" : Convert.ToDateTime(e.Element("pubDate").Value, CultureInfo.CurrentCulture).ToString();
                            }
                            post.AddedOn = DateTime.UtcNow;
                            post.Datetime = Convert.ToDateTime(post.PublishDatetime).ToUniversalTime();
                            news.Add(post);
                        }
                    }
                }
                _logger.LogInformation("Parsing news xmls Succeeded.");

                return news;
            }
            catch (Exception ex)
            {
                _logger.LogError("Oops..Exception!", ex);
                throw ex;
            }
        }

        private bool isValidResponse(string url) {
            var request = HttpWebRequest.Create(url) as HttpWebRequest;
            var res = false;
            if (request == null)
                res = false;

            var response = request.GetResponse() as HttpWebResponse;
            if (response == null)
                res = false;

            // var acceptedContentTypes = Db.AcceptedContentTypes.Select(q => q.ContentType).ToList();
            // if (acceptedContentTypes.Any(s => s.Equals(response.ContentType, StringComparison.OrdinalIgnoreCase)))
            //     res = true;

            if(!res)
                Console.Out.WriteLineAsync(string.Format("Missed source content type: {0}", response.ContentType));

            return res;
        }
        
        // recursively yield all children of json
        private static IEnumerable<JToken> AllChildren(JToken json)
        {
            foreach (var c in json.Children()) {
                yield return c;
                foreach (var cc in AllChildren(c)) {
                    yield return cc;
                }
            }
        }
        
        private void SavePosts() {
            try
            {
                _logger.LogInformation("Saving posts...");

                var postsList = ParseXmls();
                var keys = RedisServer.Keys()
                    .Select(key => (string) key).ToArray();

                var posts = new Dictionary<string, string>();
                foreach (var key in keys)
                {
                    posts.Add(key, RedisDb.StringGet(key));
                }

                foreach (var n in postsList)
                {
                    //check first if post exists
                    if (posts.Select(q => q.Key).Any(q => q.Contains(n.ExternalUrl)))
                        continue;

                    //var dt = Convert.ToDateTime(n.PublishDatetime).ToString("dd/MM/yyyy");
                    //var currDate = DateTime.UtcNow.ToString("dd/MM/yyyy");
                    var dt = Convert.ToDateTime(n.PublishDatetime).ToUniversalTime();
                    var currDate = DateTime.UtcNow;
                    if (dt >= currDate.AddDays(-1))
                    {
                        var jsonString = JsonSerializer.Serialize(n);
                        RedisDb.StringSet(n.ExternalUrl, jsonString);
                    }
                }

                Console.Out.WriteLineAsync("Saving posts process completed.");
                _logger.LogInformation("Saving posts Succeeded.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw ex;
            }
        }

        private void DeleteOldPosts()
        {
            var cachedPosts = RedisServer.Keys().Select(key => JsonSerializer
                .Deserialize<Post>(RedisDb.StringGet(key))).ToList();
            
            foreach (var post in cachedPosts)
            {
                var dt = Convert.ToDateTime(post.PublishDatetime).Date;
                var currDate = Convert.ToDateTime(DateTime.UtcNow).Date;
                if (dt < currDate.AddDays(-2))
                {
                    RedisDb.KeyDelete(post.ExternalUrl);
                }
            }
            RedisServer.FlushDatabase(RedisDb.Database);
            _logger.LogInformation("Old posts flushed from cache.");
        }

        private string GetThumbnail(string item) {

            var hasImg = item.Contains("<media:thumbnail url=\"");
            var imgUrl = new string[] { };
            if (!hasImg)
            {
                imgUrl = item.Split("<media:content url=\"");
            }
            else {
                imgUrl = item.Split("<media:thumbnail url=\"");

            }

            if (imgUrl.Length < 2) {
                return "";
            }
            return imgUrl[1].Split("\"")[0];
        }  
        
        private string GetThumbnail(string item, string elName) {

            if(elName != "entry")
            {
                return "";
            }
        
            var imgUrl = item.Split("src=\"");

            if (imgUrl.Length < 2) {
                return "";
            }
            return imgUrl[1].Split("\"")[0];
        }

        private string GetSourceLogoPath(string source) {
            return Sources.Where(q => q.Name == source).Single().ImageUrl;
        } 
        
        private string GetSourceUrl(string source) {
            return Sources.Where(q => q.Name == source).Single().Url;
        }

        private string ContentPreprocessing(string content, string url) {
            if (content.Contains("<img"))
            {
                var insIndex = content.IndexOf("<img") + "<img".Length + 1;
                content = content.Insert(insIndex, " style='width:100%;height:100%;' ");
            }

            if (content.Length > 1500)
            {
                var prs = new HtmlParser();
                var sw = new StringWriter();
                content = content.Substring(0, 1500);
                content = content.Insert(content.Length, "...</p>");
                var cont = prs.ParseDocument(content);
                cont.ToHtml(sw, new PrettyMarkupFormatter());
                content = cont.Body.InnerHtml;
            }

            content = content.Insert(content.Length, "</br><a href=" + url + "><div class=" + "btn btn-success" + ">Read More...</div></a>");

            return content;
        }

        #endregion Private
        }

}
