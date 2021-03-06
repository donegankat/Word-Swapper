# Word-Swapper
Swaps a list of pre-defined words with replacements.
The list of words to swap and their replacements are defined in [appSettings.WordsToSwap.json](WordSwapper/appSettings.WordsToSwap.json).

## Example
The following example will find any instance of the word `man` and replace it with the word `woman`:
```json
{
  "Word": "man",
  "Replacement": "woman",
  "CanBePlural": true
}
```
- If `CanBePlural` is set to `true`, the program will use Entity Framework to pluralize the word and its replacement and perform swaps on those plural forms.
  - In this example, the program would find any instances of the word `men` and replace it with `women`.
- **Note:** The regular expressions for finding and replacing words (both plural and singular) will also find and replace any possessive (i.e. ending in `'` or `'s`) forms of the words.

### Optional Prefixes and Suffixes
To simplify the list of words, you can optionally specify a list of  `OptionalPrefixes` and/or `OptionalSuffixes`:
```json
{
  "Word": "father",
  "Replacement": "mother",
  "CanBePlural": true,
  "OptionalPrefixes": [ "step", "grand" ],
  "OptionalSuffixes": [ "ly", "hood" ]
}
```
This example will replace the following words (as well as the plural forms of each):
- `father` -> `mother`
- `stepfather` -> `stepmother`
- `grandfather` -> `grandmother`
- `fatherly` -> `motherly`
- `stepfatherly` -> `stepmotherly`
- `grandfatherly` -> `grandmotherly`
- `fatherhood` -> `motherhood`
- `stepfatherhood` -> `stepmotherhood`
- `grandfatherhood` -> `grandmotherhood`

# Notes
- **Update 04/04/2018:** This program now has 2 modes of running: read & swap from a local file or fetch & swap from a URL. The user is prompted at startup which mode they would like to run in and then prompted for the appropriate file name or URL.
- Place any files you want to read from and perform the swapping on into the [Sources](WordSwapper/Sources) folder.
  - Any files that you run this program on will have a copy created in the Sources folder. This new file will contain the results of the swap and follow the naming convention:
    `[file name]_Swapped.[extension]`
  - If the swap was perfomed on the HTML from a URL, the resulting file placed in the Sources folder will be called:
    `[URL with illegal punctuation removed]_Swapped.txt`
- Define any words you want to find and swap in [appSettings.WordsToSwap.json](WordSwapper/appSettings.WordsToSwap.json).
  - Pluralizations are automatically performed if the `CanBePlural` flag is set to `true`.

# To Do
### Natural Language Processing and Parts of Speech
Improve accuracy for special words/cases by adding Natural Language Processing to identify parts of speech for sentences.
- **Example:** `his` can sometimes be swapped for `her`, but in some cases it should be swapped for `hers`
  - `He broke his glasses` -> `She broke her glasses`
  - `The pencil is his` -> `The pencil is hers`
- **Update 04/04/2018:** I've added a method to POST to an API that uses NLP to identify the parts of speech in a sentence. More testing needs to be done in order to determine if this API will be suitable enough to accomplish the objective above.
  - **Note:** This API call is **NOT** currently implemented, but it does work.
  - **Issues:** (as noted in appSettings.WordsToSwap.json)
    - "The pencil is his" comes back as PRP$ (Possessive Pronoun), but "The pencil is hers" comes back as NNS (Noun, plural).
    - "I'm looking at her" comes back as PRP$ (Possessive Pronoun), but "I'm looking at him" comes back as PRP (Personal Pronoun)
  - Maybe tagging the parts of speech, performing a "mock swap" of the tricky word with both potential replacements, and then re-tagging the string would yield some sort of insight?
  
### Misc.
- Separate some of the functions in Program.cs into their own classes where appropriate.
