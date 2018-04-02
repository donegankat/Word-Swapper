using System;
using System.Collections.Generic;
using System.Text;
using WordSwapper.Settings;

namespace WordSwapper.Models
{
    public class PartsOfSpeechResult
    {
        public string Chunk { get; set; }
        public PartsOfSpeechTag Tag { get; set; }
        public string IOB { get; set; } // A flag equal to I, O, or B representing Inside, Outside, or Begin

        public PartsOfSpeechResult(AppSettings settings, string chunk, string tag, string iob)
        {
            Chunk = chunk;
            Tag = settings.PartsOfSpeechTag.Find(x => x.Tag == tag);
            IOB = iob;
        }
    }
}
