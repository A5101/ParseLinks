using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Domain.Entities
{
    public class ApiEntity
    {
        public string Url { get; set; }
        public string Text { get; set; }
        public string Title { get; set; }
        public DateTime? DatePublished { get; set; }
    }
}
