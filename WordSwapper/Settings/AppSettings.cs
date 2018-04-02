using System;
using System.Collections.Generic;
using System.Text;
using WordSwapper.Models;

namespace WordSwapper.Settings
{
    public class AppSettings
    {
        public string ReplacementIndicator { get; set; }
        public string ReplacementIndicatorRegex { get; set; }
        public string SourceDirectory { get; set; }
        public string NlpApiUrl { get; set; } // The URL for the Natural Language Processing API
        public List<WordSwap> WordSwap { get; set; }
        public List<PartsOfSpeechTag> PartsOfSpeechTag { get; set; }
    }
}
