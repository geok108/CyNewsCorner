﻿using CyNewsCorner.DataModels;
using CyNewsCorner.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CyNewsCorner.Services
{
    public class PostsService
    {
        private readonly CyNewsCornerContext Db;
        public PostsService(CyNewsCornerContext context)
        {
            Db = context;
        }

        public List<DataModels.Post> GetNewsFiltered(string[] selectedSources, int categoryId) {
            try
            {
                var response = new GetPostsResponse();

                var sources = new List<string>();
                if (selectedSources == null || selectedSources.Length == 0)
                {
                    sources = Db.Sources.Where(q => q.IsActive == true && q.CategoryId == categoryId).Select(q => q.RssUrl).ToList();
                }
                else
                {
                    sources = Db.Sources.Where(q => selectedSources.Contains(q.Name)).Select(q => q.RssUrl).ToList();
                }

                var postsList = Db.Posts.ToList();
                
                return postsList;
            }
            catch (Exception ex) {
                throw new Exception("Oops..Exception!", ex);
            }    
        }

        public List<DataModels.Post> GetAllNews() {
            try
            {
                var postsList = Db.Posts.ToList();
                return postsList;
            }
            catch (Exception ex)
            {
                throw new Exception("Oops..Exception!", ex);
            }
        }

        
    }
}
