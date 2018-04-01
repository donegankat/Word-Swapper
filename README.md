# Word-Swapper
Swaps a list of pre-defined words with replacements.

# Notes
- You must create a directory called Sources at /WordSwapper/Sources.
-- Place any files you want to read from and perform the swapping on into this directory.
-- Any files that you run this program on will have a copy created in this Sources folder called [file name]_Swapped.[extension] which will contain the results of the swap.
- Define any words you want to find and swap in appSettings.WordsToSwap.json.
-- Pluralizations are automatically performed if the CanBePlural flag is set to true.
-- Possessive (i.e. "'s") forms of each word are checked if the CanBePossessive flag is set to true.

# To Do
- Add natural language processing to identify parts of speech for sentences in order to make the word replacements more accurate.
-- Example: "His" can sometimes be swapped for "her", but in some cases it should be swapped for "hers"
--- "He broke his glasses" -> "She broke her glasses"
--- "The pencil is his" -> "The pencil is hers"
