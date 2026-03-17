using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcesadorBib.CrossRefStructure
{
    public class CrossRefEvent
    {
        public string name { get; set; }
        public string location { get; set; }
        public string acronym { get; set; }
        public string number { get; set; }

        public List<string> sponsor { get; set; }

        public CrossRefTimestamp start { get; set; }

        public CrossRefTimestamp end { get; set; }
    }
}
