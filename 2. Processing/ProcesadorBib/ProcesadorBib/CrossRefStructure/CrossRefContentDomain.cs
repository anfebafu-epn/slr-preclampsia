using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcesadorBib.CrossRefStructure
{
    public class CrossRefContentDomain
    {
        [JsonProperty(PropertyName = "crossmark-restriction")]
        public bool crossmarkrestriction { get; set; }
    }
}
