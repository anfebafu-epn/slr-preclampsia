using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcesadorBib.CrossRefStructure
{
    public class CrossRefLink
    {
        public string URL { get; set; }

        [JsonProperty(PropertyName = "content-type")]
        public string contenttype{get;set;}

        [JsonProperty(PropertyName = "content-version")]
        public string contentversion { get; set; }

        [JsonProperty(PropertyName = "similarity-checking")]
        public string intendedapplication { get; set; }
    }
}
