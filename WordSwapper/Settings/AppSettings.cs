using System;
using System.Collections.Generic;
using System.Text;

namespace WordSwapper.Settings
{
    public class AppSettings
    {
        public string ReplacementIndicator { get; set; }
        public string ReplacementIndicatorRegex { get; set; }
        public List<WordSwap> WordSwap { get; set; }
    }
}
