using CyNewsCorner.DataModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using StackExchange.Redis;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CyNewsCorner
{
    public class BackgroundService: IHostedService, IDisposable
    {
        private Timer timer;
        private int number;
        private IDatabase RedisDb{get;set;}
        private IServer RedisServer { get; set; }
        private List<string> Sources { get; set; }
        public BackgroundService() {
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379,allowAdmin=true");

            IDatabase db = redis.GetDatabase();
            RedisDb = db;
            RedisServer = redis.GetServer("localhost", 6379);
     
            using (var reader = new StreamReader(@"news_sources.csv"))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(",").ToList();
                    Sources = values;
                }
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            timer = new Timer(o => {
                Interlocked.Increment(ref number);
                Console.Out.WriteLineAsync("Greetings from HelloJob!");
                DeleteOldPosts();
                SavePosts();
            },
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(5));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            timer?.Dispose();
        }

        private void SavePosts() {
            var postsList = ParseXmls();
            var keys = RedisServer.Keys()
                .Select(key => (string)key).ToArray();

            var posts = new Dictionary<string, string>();
            foreach (var key in keys)
            {
                posts.Add(key, RedisDb.StringGet(key));
            }
            foreach (var n in postsList)
            {
                //check first if post exists
                 if (posts.Select(q => q.Key).Any(q => q.Contains(n.Url)))
                     continue;

                var jsonString = JsonSerializer.Serialize(n);
                RedisDb.StringSet(n.Url, jsonString);
                posts.Add(n.Url, jsonString);
            }
            Console.Out.WriteLineAsync("Saving posts process completed.");
        }

        private void DeleteOldPosts() {
            RedisServer.FlushDatabase(RedisDb.Database);
        }

        public List<Post> ParseXmls()
        {
            var news = new List<DataModels.Post>();

            foreach (var source in Sources)
            {
                var startTime = DateTime.UtcNow;
                // if (!isValidResponse(source))
                // {
                //     Console.Out.WriteLineAsync(string.Format("Source {0} is invalid.", source));
                //     continue;
                // }

                XDocument doc = XDocument.Load(source);
                var endTime = DateTime.UtcNow;

                foreach (var element in doc.Elements())
                {
                    foreach (var e in element.Element("channel").Elements("item"))
                    {
                        var post = new DataModels.Post();
                        post.Title = e.Element("title") == null ? "" : e.Element("title").Value;
                        post.Category = e.Element("category") == null ? "" : e.Element("category").Value;
                        post.Description = e.Element("description") == null ? "" : e.Element("description").Value;
                        post.Url = e.Element("link") == null ? "" : e.Element("link").Value;
                        post.Image = e.Element("enclosure") == null ? "" : e.Element("enclosure").Attribute("url").Value;
                        post.PublishDatetime = e.Element("pubDate") == null ? "" : e.Element("pubDate").Value;
                        post.AddedOn = DateTime.UtcNow;
                        news.Add(post);
                    }
                }
            }
            return news;
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
    }
}
