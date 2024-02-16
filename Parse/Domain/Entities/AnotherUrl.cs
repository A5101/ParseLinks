using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Domain.Entities
{
    public class AnotherUrl
    {
        [Key]
        public string Url { get; set; }
    }
}
