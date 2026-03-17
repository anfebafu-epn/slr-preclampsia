using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcesadorBib.CrossRefStructure
{
    public class CrossRefMessage
    {
        public CrossRefTimestamp indexed { get; set; }

        [JsonProperty(PropertyName = "publisher-location")]
        public string publisherlocation { get; set; }

        [JsonProperty(PropertyName = "reference-count")]
        public int referencecount { get; set; }

        public string publisher { get; set; }

        public List<CrossRefLicence> license { get; set; }

        [JsonProperty(PropertyName = "content-domain")]
        public CrossRefContentDomain contentdomain { get; set; }

        [JsonProperty(PropertyName = "published-print")]
        public CrossRefTimestamp publishedprint { get; set; }

        public string DOI { get; set; }

        public CrossRefTimestamp created { get; set; }

        public string type { get; set; }

        public string source { get; set; }

        [JsonProperty(PropertyName = "is-referenced-by-count")]
        public int isreferencedbycount { get; set; }

        public List<string> title { get; set; }

        public string prefix { get; set; }

        public List<CrossRefAuthor> author { get; set; }

        public string member { get; set; }

        public List<CrossRefReference> reference { get; set; }

        [JsonProperty(PropertyName = "event")]
        public CrossRefEvent cevent { get;set; }

        [JsonProperty(PropertyName = "container-title")]
        public List<string> containertitle { get; set; }

        [JsonProperty(PropertyName = "original-title")]
        public List<string> originaltitle { get; set; }

        public List<CrossRefLink> link { get; set; }

        public CrossRefTimestamp deposited { get; set; }
        public int score { get; set; }

        public CrossRefResource resource { get; set; }

        public List<string> subtitle { get; set; }

        [JsonProperty(PropertyName = "short-title")]
        public List<string> shorttitle { get; set; }

        public CrossRefTimestamp issued { get; set; }

        public List<string> ISBN { get; set; }
        public List<string> ISSN { get; set; }

        [JsonProperty(PropertyName = "references-count")]
        public string referencescount { get; set; }

        public string URL { get; set; }

        public List<string> subject { get; set; }
        public CrossRefTimestamp published { get; set; }
    }
}
