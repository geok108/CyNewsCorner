﻿using CyNewsCorner.Requests;
using CyNewsCorner.Responses;
using CyNewsCorner.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace CyNewsCorner.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NewsController : ControllerBase
    {
        private readonly ILogger<NewsController> _logger;
        private NewsService _news { get; set; }
        public NewsController(ILogger<NewsController> logger, CyNewsCornerContext context)
        {
            _logger = logger;
            _news = new NewsService(context);
        }

        [HttpGet("status")]
        public string GetStatus() {
            return "Status: UP!";
        }

        [HttpGet("list")]
        public NewsResponse GetNewsList(NewsRequest request)
        {
            var response = new NewsResponse();
 
            var newsList = _news.GetNews(request.SelectedNewsSources, request.CategoryId);
            response.PostList = newsList;

            return response;
        }

        private List<Post> PopulateNewsList() {
            var news = new List<Post>();
            //news = _news.GetAllNews();
            return news;
        }

    }

}
