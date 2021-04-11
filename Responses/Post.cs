using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CyNewsCorner.Responses
{
    public class Post
    {
        public string Title { get; set; }
        
        public string Category { get; set; }
        
        public string Url { get; set; }
        
        public string Description { get; set; }
        public int Source { get; set; }
        
        public string PublishDatetime { get; set; }
        
        public string Image { get; set; }
    }
}
