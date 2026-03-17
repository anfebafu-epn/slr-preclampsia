using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcesadorBib.CrossRefStructure
{
    public class CrossRefTimestamp
    {
        [JsonProperty(PropertyName = "date-time")]
        public string datetime { get; set; }

        public long timestamp { get; set; }

        [JsonProperty(PropertyName = "date-parts")]
        //public List<CrossRefDateParts> dateparts { get; set; }
        public int[][] dateparts { get; set; }
    }
}
