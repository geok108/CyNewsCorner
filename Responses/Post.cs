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
        public string publishDate { get; set; }
        public string image { get; set; }
        public int cncCategory { get; set; }
    }
}
