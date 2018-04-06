using System;
using System.Collections.Generic;
using System.Text;

namespace WordSwapper.Models
{
    public class WordSwap
    {
        public string Word { get; set; }
        public string Replacement { get; set; }
        public bool CanBePlural { get; set; }
        public bool CanBeContraction { get; set; } // TODO: This isn't currently fleshed out and I'm not sure it ever needs to be.
        public List<string> OptionalPrefixes { get; set; } // Not required, but used to simplify the list of words for cases like mother/grandmother
        public List<string> OptionalSuffixes { get; set; } // Not required, but used to simplify the list of words for cases like mother/motherly/motherhood
        public string SpecialCasePOSTag { get; set; } // TODO: Finalize how I'm actually doing this
        public string SpecialCaseAlternative { get; set; } // TODO: Finalize how I'm actually doing this
        public bool IsSpecialCase
        {
            get
            {
                return !string.IsNullOrWhiteSpace(SpecialCasePOSTag);
            }
        }
    }
}
