using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Domain.Entities
{
    public class Image
    {
        public string? SourceUrl {  get; set; }
        public string? Url { get; set; }
        public string? Title { get; set; }
        public string? Alt { get; set; }
    }
}
