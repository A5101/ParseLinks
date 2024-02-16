using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Domain.Entities
{
    public class UnaccessedUrl
    {
        [Key]
        public string Url { get; set; }
    }
}
