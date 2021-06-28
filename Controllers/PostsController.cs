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
                var postList = GetAllNews();
                
                response.PostList = request.SelectedNewsSources == null ||request.SelectedNewsSources.Length == 0 ? postList : postList.FindAll(q => request.SelectedNewsSources.Contains(q.Source));
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
        
        private List<Post> GetAllNews()
        {
            return _cacheServer.Keys().Select(key => JsonSerializer.Deserialize<Post>(_cacheDb.StringGet(key))).ToList();
        }
    }
}
