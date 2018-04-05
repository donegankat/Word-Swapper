using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// List of tags: https://www.ling.upenn.edu/courses/Fall_2003/ling001/penn_treebank_pos.html
/// </summary>
namespace WordSwapper.Models
{
    public class PartsOfSpeechTag
    {
        public string Tag { get; set; }
        public string Description { get; set; }
    }
}
