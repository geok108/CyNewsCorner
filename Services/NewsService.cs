﻿using CyNewsCorner.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CyNewsCorner.Services
{
    public class NewsService
    {
        private readonly CyNewsCornerContext Db;

        private static List<Post> newsList { get; set; }

        public NewsService(CyNewsCornerContext context)
        {
            Db = context;
        }

        public List<Post> GetNewsFiltered(string[] selectedSources, int categoryId) {
            try
            {
                var response = new NewsResponse();

                var sources = new List<string>();
                if (selectedSources == null || selectedSources.Length == 0)
                {
                    sources = Db.Sources.Where(q => q.IsActive == true && q.CategoryId == categoryId).Select(q => q.RssUrl).ToList();
                }
                else
                {
                    sources = Db.Sources.Where(q => selectedSources.Contains(q.Name)).Select(q => q.RssUrl).ToList();
                }

                if (newsList == null || newsList.Count == 0) {
                    newsList = ParseXmls(sources);
                }
                
                return newsList;
            }
            catch (Exception ex) {
                throw new Exception("Oops..Exception!", ex);
            }    
        }

        public List<Post> GetAllNews() {
            try
            {
                var response = new NewsResponse();
                var sources = Db.Sources.Select(q => q.RssUrl).ToList();

                if (newsList == null || newsList.Count == 0)
                {
                    newsList = ParseXmls(sources);
                }

                return newsList;
            }
            catch (Exception ex)
            {
                throw new Exception("Oops..Exception!", ex);
            }
        }

        public List<Post> ParseXmls(List<string> sources)
        {
            var news = new List<Post>();

            foreach (var item in sources)
            {
                var startTime = DateTime.UtcNow;
                XDocument doc = XDocument.Load(item);
                var endTime = DateTime.UtcNow;

                foreach (var element in doc.Elements())
                {
                    foreach (var e in element.Element("channel").Elements("item"))
                    {
                        if (e == null) {
                            break;
                        }
                        var post = new Post();
                        post.title = e.Element("title") == null ? "" : e.Element("title").Value;
                        post.category = e.Element("category") == null ? "" : e.Element("category").Value;
                        post.description = e.Element("description") == null ? "" : e.Element("description").Value;
                        post.url = e.Element("link") == null ? "" : e.Element("link").Value;
                        post.image = e.Element("enclosure") == null ? "" : e.Element("enclosure").Attribute("url").Value;
                        post.publishDate = e.Element("pubDate") == null ? "" : e.Element("pubDate").Value;
                        //post.cncCategory = categoryId;
                        news.Add(post);
                    }
                }
            }
            return news;
        }
    }
}
