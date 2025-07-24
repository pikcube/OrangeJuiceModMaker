using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrangeJuiceModMaker.Data
{
    public class MusicRef
    {
        public string? UnitId { get; set; }
        public string? Event { get; set; }
        public required string Description { get; set; }
    }
}
