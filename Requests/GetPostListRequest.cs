﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CyNewsCorner.Requests
{
    public class GetPostListRequest
    {
        public int[] SelectedNewsSources { get; set; }
      
        public int Page { get; set; }
        public int PerPage { get; set; }
    }
}
