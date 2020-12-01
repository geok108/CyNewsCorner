using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace CyNewsCorner.DataModels
{
    [Table("news_source")]
    public class NewsSource
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("name")]
        public string Name { get; set; } 
        [Column("rss_url")]
        public string RssUrl { get; set; }
        [Column("is_active")]
        public bool IsActive { get; set; }
        [Column("category_id")]
        public int CategoryId { get; set; }
    }
}
