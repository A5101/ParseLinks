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
        public string URL { get; set; }

        public string? Title { get; set; }

        public int? Cluster { get; set; }

        public double[] Vector { get; set; }

        public string? Text { get; set; }

        public List<string>? Links { get; set; }

        public DateTime? DatePublished { get; set; }

        public DateTime? DateAdded { get; set; }

        public DateTime? DateUpdated { get; set; }

        public string? Lang { get; set; }

        public List<Image>? Images { get; set; }

        public string? Icon { get; set; }

        public ParsedUrl()
        {
            DateAdded = DateTime.UtcNow;
        }
    }
}
