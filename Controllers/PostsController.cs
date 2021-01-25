using CyNewsCorner.Requests;
using CyNewsCorner.Responses;
using CyNewsCorner.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace CyNewsCorner.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly ILogger<PostsController> _logger;
        private PostsService _news { get; set; }
        public PostsController(ILogger<PostsController> logger, CyNewsCornerContext context)
        {
            _logger = logger;
            _news = new PostsService(context);
        }

        [HttpGet("status")]
        public string GetStatus() {
            return "Status: UP!";
        }

        [HttpGet("list")]
        public GetPostsResponse GetAllNewsList()
        {
            var response = new GetPostsResponse();  
 
            var newsList = _news.GetAllNews();
            response.PostList = PopulatePostsResponse(newsList);

            return response;
        }

        [HttpGet("list/filtered")]
        public GetPostsResponse GetNewsList(GetPostsRequest request)
        {
            var response = new GetPostsResponse();

            var newsList = _news.GetNewsFiltered(request.SelectedNewsSources, request.CategoryId);
            response.PostList = PopulatePostsResponse(newsList);

            return response;
        }

        private List<Post> PopulatePostsResponse(List<DataModels.Post> posts) {
            var res = new List<Post>();

            foreach (var post in posts) {
                var p = new Post();
                p.title = post.Title;
                p.category = post.Category;
                p.description = post.Description;
                p.image = post.Image;
                p.publishDatetime = post.PublishDatetime;
                p.url = post.Url;

                res.Add(p);
            }

            return res;
        }
    }
}
