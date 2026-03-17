using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcesadorBib
{
    public class TagItem
    {
        public Guid ID { get; set; }
        public string tagdata { get; set; }

        public Guid parentID { get; set; }
        public string tagparent { get; set; }
    }
}
