using System.Collections.Generic;

namespace CyNewsCorner.Responses
{
    public class GetPostsResponse
    {
        public List<Post> PostList { get; set; }
        public string ErrorMessage { get; set; }
    }
}
