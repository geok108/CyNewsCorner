using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace CyNewsCorner.DataModels
{
    [Table("accepted_content_type")]
    public class AcceptedContentType
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("content_type")]
        public string ContentType { get; set; }
    }
}
