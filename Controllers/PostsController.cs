using System;
using CyNewsCorner.Requests;
using CyNewsCorner.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore.Update;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;

namespace CyNewsCorner.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly ILogger<PostsController> _logger;
        
        private readonly IDatabase _cacheDb;
        
        private readonly IServer _cacheServer;

        private IConfiguration _config { get; }

        public PostsController(ILogger<PostsController> logger, IConnectionMultiplexer redisConn, IConfiguration configuration)
        {
            _logger = logger;
            _config = configuration;
            _cacheDb = redisConn.GetDatabase();
            _cacheServer = redisConn.GetServer(_config["redisHost"], int.Parse(_config["redisPort"]));
        }

        [HttpGet("status")]
        public string GetStatus() {
            return "Status: UP!";
        }

        [HttpPost("cache/clear")]
        public void FlushCache()
        {
            _logger.LogInformation("Flushing cache...");

            _cacheServer.FlushDatabase(_cacheDb.Database);
            _logger.LogInformation("Flushing cache succeeded.");
        }

        [HttpGet("list")]
        public GetPostsResponse GetNewsList([FromQuery]GetPostListRequest request)
        {
            try
            {
                _logger.LogInformation("Getting filtered news...");

                var validator = new GetPostListRequestValidator();
                var results = validator.Validate(request);
                if (results.Errors.Count > 0)
                {
                    throw new ValidationException(results.Errors);
                }

                var response = new GetPostsResponse();
                var postList = GetAllNews(request.Page, request.PerPage);
                foreach (var post in postList) {
                    post.PublishDatetime = GetRelativeTime(post.Datetime);
                }

                //To be refactored to filter selected sources
                //response.PostList = request.SelectedNewsSources == null ||request.SelectedNewsSources.Length == 0 ? postList : postList.FindAll(q => request.SelectedNewsSources.Contains(q.Source));
                response.PostList = postList;
                _logger.LogInformation("Getting filtered news Succeeded.");

                return response;
            }
            catch (ValidationException ex)
            {
                _logger.LogError(ex.ToString());
                Response.StatusCode = 400;
                var errorRes = new GetPostsResponse();
                errorRes.ErrorMessage = string.Join(", ", ex.Errors) + ".";
                return errorRes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                Response.StatusCode = 400;
                return new GetPostsResponse();
            }
        }

        [HttpGet("{Id}")]
        public GetPostResponse GetPost(string Id)
        {
            try
            {
                _logger.LogInformation("Getting post...");

                //var validator = new GetPostListRequestValidator();
                //var results = validator.Validate(request);
                //if (results.Errors.Count > 0)
                //{
                //    throw new ValidationException(results.Errors);
                //}
                var post = GetAllNews().Find(q => q.Slug == Id);
                var response = new GetPostResponse();
                post.PublishDatetime = GetRelativeTime(post.Datetime);
                response.Post = post;
                //To be refactored to filter selected sources
                //response.PostList = request.SelectedNewsSources == null ||request.SelectedNewsSources.Length == 0 ? postList : postList.FindAll(q => request.SelectedNewsSources.Contains(q.Source));
                _logger.LogInformation("Getting post Succeeded.");

                return response;
            }
            //catch (ValidationException ex)
            //{
            //    _logger.LogError(ex.ToString());
            //    Response.StatusCode = 400;
            //    var errorRes = new GetPostResponse();
            //    errorRes.ErrorMessage = string.Join(", ", ex.Errors) + ".";
            //    return errorRes;
            //}
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                Response.StatusCode = 400;
                return new GetPostResponse();
            }
        }

        [HttpGet("readmorelist/{PostToExclude}")]
        public GetPostsResponse GetReadMoreList(GetReadMoreListRequest request) {
            try
            {
                _logger.LogInformation("Getting News For Read More ...");
                var response = new GetPostsResponse();
                var postList = GetAllNews();
                foreach (var post in postList)
                {
                    post.PublishDatetime = GetRelativeTime(post.Datetime);
                }

                response.PostList = postList;
                _logger.LogInformation("Getting News For Read More Succeeded.");

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                Response.StatusCode = 400;
                return new GetPostsResponse();
            }

        }

        private List<Post> GetAllNews(int page, int perPage)
        {
            var noOfPosts = perPage == null ? 9 : perPage;
            var offset = page == null ? 0 : page * perPage;
            return GetAllNews().Skip(offset).Take(noOfPosts).ToList();
            //return _cacheServer.Keys().Select(key => JsonSerializer.Deserialize<Post>(_cacheDb.StringGet(key))).ToList();
        } 
        
        private List<Post> GetAllNews()
        {
            var posts = _cacheServer.Keys().Select(key => JsonSerializer.Deserialize<Post>(_cacheDb.StringGet(key))).ToList();
            posts.Sort((x, y) => y.Datetime.CompareTo(x.Datetime));

            return posts;
        }

        private string GetRelativeTime(DateTime dateTime) {
            const int SECOND = 1;
            const int MINUTE = 60 * SECOND;
            const int HOUR = 60 * MINUTE;
            const int DAY = 24 * HOUR;
            const int MONTH = 30 * DAY;

            var ts = new TimeSpan(DateTime.UtcNow.Ticks - dateTime.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 1 * MINUTE)
                return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";

            if (delta < 2 * MINUTE)
                return "a minute ago";

            if (delta < 45 * MINUTE)
                return ts.Minutes + " minutes ago";

            if (delta < 90 * MINUTE)
                return "an hour ago";

            if (delta < 24 * HOUR)
                return ts.Hours + " hours ago";

            if (delta < 48 * HOUR)
                return "yesterday";

            if (delta < 30 * DAY)
                return ts.Days + " days ago";

            if (delta < 12 * MONTH)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            else
            {
                int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year ago" : years + " years ago";
            }
        }
    }
}
