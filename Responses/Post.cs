using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CyNewsCorner.Responses
{
    public class Post
    {
        public string title { get; set; }
        public string category { get; set; }
        public string url { get; set; }
        public string description { get; set; }
        public string publishDatetime { get; set; }
        public string image { get; set; }
    }
}
