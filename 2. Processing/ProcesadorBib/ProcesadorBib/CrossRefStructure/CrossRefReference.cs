using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcesadorBib.CrossRefStructure
{
    public class CrossRefReference
    {
        public string key { get; set; }
        
        [JsonProperty(PropertyName = "doi-assented-by")]
        public string doiassentedby { get; set; }

        public string unstructured { get; set; }

        public string DOI { get; set; }
    }
}
