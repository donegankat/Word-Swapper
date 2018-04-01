using System;
using System.Collections.Generic;
using System.Text;

namespace WordSwapper
{
    public class WordSwap
    {
        public string Word { get; set; }
        public string Replacement { get; set; }
        public bool CanBePlural { get; set; }
        public bool CanBePossessive { get; set; }
        public bool CanBeContraction { get; set; } // TODO: This isn't currently fleshed out and I'm not sure it ever needs to be.
    }
}
