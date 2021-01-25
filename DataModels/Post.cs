using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace CyNewsCorner.DataModels
{
    [Table("post")]
    public class Post
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("title")]
        public string Title { get; set; }
        [Column("image")]
        public string Image { get; set; }
        [Column("url")]
        public string Url { get; set; }
        [Column("publish_datetime")]
        public string PublishDatetime { get; set; }
        [Column("category")]
        public string Category { get; set; }
        [Column("description")]
        public string Description { get; set; }
    }
  
}
