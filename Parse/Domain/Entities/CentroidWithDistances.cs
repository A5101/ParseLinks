using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parse.Domain.Entities
{
    public class CentroidWithDistance : Centroid
    {
        public int clusterNum { get; set; }
        public double[] vector { get; set; }
        public double distance { get; set; }
    }
}
