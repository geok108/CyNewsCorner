using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CyNewsCorner.Requests
{
    public class NewsRequest
    {
        public string[] SelectedNewsSources { get; set; }
        public int CategoryId { get; set; }
    }
}
