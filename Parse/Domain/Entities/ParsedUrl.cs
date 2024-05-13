using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Domain.Entities
{
    public class ParsedUrl
    {
        [Key]
        public string URL { get; set; }

        public string? Title { get; set; }

        public string Tags_ { get; set; }

        [Required]
        public string? Text { get; set; }

        public string? Description {get; set; }

        public List<string>? Links { get; set; }

        public DateTime? DateAdded { get; set; }

        public DateTime? DateUpdated { get; set; }

        public string Meta { get; set; }

        public ParsedUrl()
        {
            DateAdded = DateTime.UtcNow;
        }
    }
}
