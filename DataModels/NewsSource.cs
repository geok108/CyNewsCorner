using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace CyNewsCorner.DataModels
{
    public class NewsSource
    {
        public int Id { get; set; }
        public string Name { get; set; } 

        public string ImageUrl { get; set; }
        public string RssUrl { get; set; }
        public string Url { get; set; }
        public bool IsActive { get; set; }
    }
}
