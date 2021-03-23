using System;
using CyNewsCorner.Requests;
using CyNewsCorner.Responses;
using CyNewsCorner.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;

namespace CyNewsCorner.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly ILogger<PostsController> _logger;
        private readonly IDatabase _cacheDb;
        private readonly IServer _cacheServer;
        public PostsController(ILogger<PostsController> logger, IConnectionMultiplexer redisConn)
        {
            _logger = logger;
            _cacheDb = redisConn.GetDatabase();
            _cacheServer = redisConn.GetServer("localhost", 6379);
        }

        [HttpGet("status")]
        public string GetStatus() {
            return "Status: UP!";
        }

        [HttpGet("list")]
        public GetPostsResponse GetAllNewsList()
        {
            var response = new GetPostsResponse();
            var newsList = GetAllNews();
            response.PostList = newsList;
            return response;
        }

        private List<Post> GetAllNews() {
            try
            {
                var postsList = new List<Post>();
                foreach (var key in _cacheServer.Keys())
                {
                    var deserializedJsonString = JsonSerializer.Deserialize<Post>(_cacheDb.StringGet(key));
                    postsList.Add(deserializedJsonString);
                }

                return postsList;
            }
            catch (Exception ex)
            {
                throw new Exception("Oops..Exception!", ex);
            }
        }
        [HttpGet("list/filtered")]
        public GetPostsResponse GetNewsList(GetPostsRequest request)
        {
            var response = new GetPostsResponse();
            var postList = GetAllNews();
            response.PostList = postList.FindAll(q => request.SelectedNewsSources.Contains(q.Url));

            return response;
        }
    }
}
