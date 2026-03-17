using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcesadorBib.CrossRefStructure
{
    public class CrossRefStructure
    {
        public string status { get; set; }

        [JsonProperty(PropertyName = "message-type")]
        public string mesagetype {get;set;}

        [JsonProperty(PropertyName = "message-version")]
        public string mesageversion { get; set; }

        public CrossRefMessage message { get; set; }
    }
}
