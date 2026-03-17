using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcesadorBib.CrossRefStructure
{
    public class CrossRefLicence
    {
        public CrossRefTimestamp start { get; set; }

        [JsonProperty(PropertyName = "content-version")]
        public string contentversion { get; set; }

        [JsonProperty(PropertyName = "delay-in-days")]
        public string delayindays { get; set; }

        public string URL { get; set; }
    }
}
