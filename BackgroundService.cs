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
        
        public BackgroundService(ILogger<BackgroundService> logger) {
            _logger = logger;

            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379,allowAdmin=true");

            IDatabase db = redis.GetDatabase();
            RedisDb = db;
            RedisServer = redis.GetServer("localhost", 6379);

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
                                post.Description = e.Element(Snmp + "content") == null ? "" : e.Element(Snmp + "content").Value;
                                post.Source = source.Name;
                                post.SourceUrl = GetSourceUrl(source.Name);
                                post.SourceLogo = GetSourceLogoPath(source.Name);
                                post.ExternalUrl = e.Element(Snmp + "id") == null ? "" : e.Element(Snmp + "id").Value;
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
                                post.Description = e.Element("description") == null ? "" : e.Element("description").Value;
                                post.Source = source.Name;
                                post.SourceUrl = GetSourceUrl(source.Name);
                                post.SourceLogo = GetSourceLogoPath(source.Name);
                                post.ExternalUrl = e.Element("link") == null ? "" : e.Element("link").Value;
                                //post.Image = e.Element("enclosure") == null
                                //    ? ""
                                //    : e.Element("enclosure").Attribute("url").Value;
                                post.Image = GetThumbnail(e.ToString());
                                post.PublishDatetime = e.Element("pubDate") == null ? "" : Convert.ToDateTime(e.Element("pubDate").Value, CultureInfo.CurrentCulture).ToString();
                            }
                            post.AddedOn = DateTime.UtcNow;

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
                throw new Exception("Oops..Exception!");
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
            _logger.LogInformation("Saving posts...");

            try
            {
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
                        n.Url = GeneratePostHtml(n);
                        var jsonString = JsonSerializer.Serialize(n);
                        RedisDb.StringSet(n.ExternalUrl, jsonString);

                        //create wp posts
                        //var wpJsonStr = PopulateWpObj(n);
                        //var client = new HttpClient();
                        //client.DefaultRequestHeaders.Authorization =
                        //    new AuthenticationHeaderValue("Basic", "YWRtaW46dUg3NSBHTVR0IGVFMnkgWGxDYiBRTjdhIGhBUXA=");
                        //client.PostAsync("http://localhost/wordpress/wp-json/wp/v2/posts", new StringContent(wpJsonStr, Encoding.UTF8, "application/json"));

                    }
                }

                Console.Out.WriteLineAsync("Saving posts process completed.");
                _logger.LogInformation("Saving posts Succeeded.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw new Exception("Oops..Exception!");
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

        private string PopulateWpObj(Post post) {
            var wpPost = new WpPost();
            wpPost.title = post.Title;
            wpPost.content = post.Description;
            wpPost.status = "publish";

            return JsonSerializer.Serialize(wpPost);
        }

        private string GeneratePostHtml(Post post)
        {
            var templateFile = File.ReadAllText("C:\\Users\\georg\\DEV\\CyNewsCorner\\postsTemplate.html");
            var postFile = templateFile.Replace("[POSTTITLE]", post.Title);
            var content = post.Description.Length > 500 ? post.Description.Substring(0, 500) + "..." : post.Description;
            postFile = postFile.Replace("[POSTCONTENT]", content);
            postFile = postFile.Replace("[POSTURL]", post.ExternalUrl);

            var slug = post.Title.Replace(" ", "-");
            slug = Regex.Replace(slug, "[^0-9a-zA-Z-,]+", "");
            File.WriteAllText("C:\\Users\\georg\\DEV\\bluecorner\\public\\posts\\" + slug + ".html", postFile);

            return "/posts/"+slug + ".html";
        }

        #endregion Private
        }

    public class WpPost {
        public string title { get; set; }
        public string content { get; set; }
        public string status { get; set; }
    }
}
