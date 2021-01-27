using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CyNewsCorner
{
    public class ParseXmlsJob: IHostedService, IDisposable
    {
        private readonly CyNewsCornerContext Db;
        private Timer timer;
        private int number;
        public ParseXmlsJob() {
            var contextOptions = new DbContextOptionsBuilder<CyNewsCornerContext>()
            .Options;

            Db = new CyNewsCornerContext(contextOptions);
        }

        public List<DataModels.Post> ParseXmls(List<string> sources)
        {
            var news = new List<DataModels.Post>();

            foreach (var source in sources)
            {
                var startTime = DateTime.UtcNow;
                if (!isValidResponse(source)) {
                    Console.Out.WriteLineAsync(string.Format("Source {0} is invalid.", source));
                    continue;
                }
                
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
                        news.Add(post);
                    }
                }
            }
            return news;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            timer = new Timer(o => {
                Interlocked.Increment(ref number);
                Console.Out.WriteLineAsync("Greetings from HelloJob!");
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
            var sources = Db.Sources.Select(q => q.RssUrl).ToList();
            var postsList = ParseXmls(sources);
            foreach (var n in postsList)
            {
                //check first if post exists
                Db.Posts.Add(n);
            }
            Db.SaveChanges();
            Console.Out.WriteLineAsync("Saving posts process completed.");
        }

        private bool isValidResponse(string url) {
            var request = HttpWebRequest.Create(url) as HttpWebRequest;
            var res = false;
            if (request == null)
                res = false;

            var response = request.GetResponse() as HttpWebResponse;
            if (response == null)
                res = false;

            var acceptedContentTypes = Db.AcceptedContentTypes.Select(q => q.ContentType).ToList();
            if (acceptedContentTypes.Any(s => s.Equals(response.ContentType, StringComparison.OrdinalIgnoreCase)))
                res = true;

            if(!res)
                Console.Out.WriteLineAsync(string.Format("Missed source content type: {0}", response.ContentType));

            return res;
        }
    }
}
