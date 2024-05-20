using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Domain.Entities
{
    /// <summary>
    /// Представляет класс сущности ссылки в очереди
    /// </summary>
    public class AnotherUrl
    {
        /// <summary>
        /// Свойство, содержащее адрес
        /// </summary>
        [Key]
        public string Url { get; set; }
    }
}
