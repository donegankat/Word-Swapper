using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using WordSwapper.Models;
using WordSwapper.Settings;

namespace WordSwapper
{    
    class Program
    {
        private static string _currentDirectory = Directory.GetCurrentDirectory();
        private static string _rootProjectDirectory = _currentDirectory.Remove(_currentDirectory.IndexOf("\\bin"));
        private static AppSettings _settings;
        private static CustomPluralizer _pluralizer;

        private static AppSettings _configureAppSettings()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appSettings.WordsToSwap.json", reloadOnChange: true, optional: true)
            .AddJsonFile("appSettings.PartsOfSpeechTags.json", reloadOnChange: true, optional: true)
            .AddEnvironmentVariables();

            var configuration = builder.Build();

            var settings = new AppSettings();
            configuration.Bind(settings);

            return settings;
        }

        static void Main(string[] args)
        {
            _settings = _configureAppSettings();
            _pluralizer = new CustomPluralizer();

            var runMode = ProgramRunMode.Undefined;
            while (true) // Loop until we get a valid mode selection.
            {
                Console.WriteLine("Select a run option (Default is 1):");
                Console.WriteLine("   1 - Swap words in a local file");
                Console.WriteLine("   2 - Swap words from the text at a URL");

                var userInput = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(userInput) || userInput == "1") // The user input a valid mode selection, so no need to keep looping.
                {
                    Console.Clear();
                    runMode = ProgramRunMode.File;
                    break;
                }
                else if (userInput == "2") // The user input a valid mode selection, so no need to keep looping.
                {
                    Console.Clear();
                    runMode = ProgramRunMode.Web;
                    break;
                }
                else // The user gave an invalid mode selection. Loop and re-prompt them.
                {
                    Console.WriteLine("INVALID MODE SELECTION");
                    Console.WriteLine();
                }
            }

            if (runMode == ProgramRunMode.File)
            {
                _swapLocalFile();
            }
            else if (runMode == ProgramRunMode.Web)
            {
                _swapHtmlFromUrl();
            }

            Console.WriteLine();
            Console.WriteLine("==========================");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        #region Main Swappers

        private static void _swapLocalFile()
        {
            var fileName = "";

            while (true) // Loop until we get a valid file name.
            {
                Console.WriteLine("Enter the file name that you wish to swap:");
                Console.WriteLine($"(File must be in {_rootProjectDirectory}\\{_settings.SourceDirectory})");
                fileName = Console.ReadLine();

                if (File.Exists($"{_rootProjectDirectory}/{_settings.SourceDirectory}/{fileName}")) // The user input a valid file name, so no need to keep looping.
                {
                    Console.Clear();
                    break;
                }
                else // The user gave an invalid file name. Loop and re-prompt them.
                {
                    Console.WriteLine("FILE NOT FOUND");
                    Console.WriteLine();
                }
            }

            var stringToSwap = _loadFile(fileName);
            var extension = Path.GetExtension(fileName);

            if (extension == ".html") // If the user wants to perform a swap on a local .html file, parse the html
            {
                stringToSwap = _getHtmlTextFromFile(stringToSwap);
            }

            Console.WriteLine("ORIGINAL TEXT:");
            Console.WriteLine(stringToSwap);

            stringToSwap = _performSwap(stringToSwap); // Perform the actual swap

            Console.WriteLine();
            Console.WriteLine("==========================");
            Console.WriteLine();
            Console.WriteLine("NEW TEXT:");
            Console.WriteLine(stringToSwap);

            // Save the file as: [original file name]_Swapped.[extension]
            _saveFile(fileName.Replace(extension, $"_Swapped{extension}"), stringToSwap);
        }


        private static void _swapHtmlFromUrl()
        {
            var url = "";

            while (true) // Loop until we get a valid URL.
            {
                Console.WriteLine("Enter the URL for the page you wish to swap:");
                url = Console.ReadLine();

                // TODO: Add some sort of validation to make sure the user gave us a valid URL
                if (!string.IsNullOrWhiteSpace(url)) // The user input a valid URL, so no need to keep looping.
                {
                    if (!url.StartsWith("http://") && !url.StartsWith("https://")) // If the URL we were given doesn't start with http:// or https://, reject it and re-prompt
                    {
                        Console.WriteLine("PLEASE ENTER FULLY-QUALIFIED URL (i.e. beginning with http:// or https://)");
                        Console.WriteLine();
                    }
                    else
                    {
                        Console.Clear();
                        break;
                    }
                }
                else // The user gave an invalid URL. Loop and re-prompt them.
                {
                    Console.WriteLine("INVALID URL");
                    Console.WriteLine();
                }
            }

            var stringToSwap = _getHtmlTextFromUrl(url);

            Console.WriteLine("ORIGINAL TEXT:");
            Console.WriteLine(stringToSwap);

            stringToSwap = _performSwap(stringToSwap); // Perform the actual swap

            Console.WriteLine();
            Console.WriteLine("==========================");
            Console.WriteLine();
            Console.WriteLine("NEW TEXT:");
            Console.WriteLine(stringToSwap);

            // Save the file as: [original file name]_Swapped.[extension]
            var extension = ".txt";
            var stringifiedUrl = Regex.Replace(url, @"https?:\/\/(www\.)?", @"", RegexOptions.IgnoreCase); // Get rid of http://, https://, and www.
            stringifiedUrl = Regex.Replace(stringifiedUrl, @"(\.|\/|\\)", @"_", RegexOptions.IgnoreCase); // Replace ., /, and \ with _
            _saveFile($"{stringifiedUrl}_Swapped{extension}", stringToSwap);
        }


        /// <summary>
        /// Searches the provided string for all words defined in appSettings.WordsToSwap.json and performs a replacement on each one.
        /// This checks singular, plural, and possessive forms of each word according to the settings defined for each word in appSettings.WordsToSwap.json.
        /// 
        /// Every time a word is swapped, a special string called the replacement indicator (defined in appSettings.json) is added to the end to signify
        /// that a word had already been swapped and to not swap it again (e.g. if the swapped form of the word is found later in our list of words to
        /// swap). This replacement indicator is removed after all swaps have been performed.
        /// </summary>
        /// <param name="stringToSwap"></param>
        /// <returns></returns>
        private static string _performSwap(string stringToSwap)
        {
            var beginningWordBoundary = @"\b"; // Ensure we only look at the beginning of the word.
            var negativeLookAhead = $@"(?!{_settings.ReplacementIndicatorRegex})"; // This is a negative look-ahead indicator that we can add to the end of regex searches for each word so we don't replace words that we've already replaced

            foreach (var word in _settings.WordSwap)
            {
                if (word.CanBePlural)
                {
                    string pluralStringToFind = _pluralizer.Pluralize(word.Word);
                    string pluralReplacement = _pluralizer.Pluralize(word.Replacement);

                    var pluralStringToFindRegex = $@"{beginningWordBoundary}({pluralStringToFind}){negativeLookAhead}\b"; // Look for matches that don't already have a replacement indicator
                    var pluralReplacementRegex = $@"{pluralReplacement}{_settings.ReplacementIndicator}";

                    // Perform replacements on any plural variations of the word
                    stringToSwap = _replaceWithCase(stringToSwap, pluralStringToFindRegex, pluralReplacementRegex);
                }

                var stringToFindRegex = $@"{beginningWordBoundary}({word.Word}){negativeLookAhead}\b"; // Look for matches that don't already have a replacement indicator
                var replacementRegex = $@"{word.Replacement}{_settings.ReplacementIndicator}";

                if (word.IsSpecialCase)
                {
                    // TODO: Find sentences containing the special case words.
                    // TODO: Perform _tagPartsOfSpeech on these sentences and figure out if we can identify which word we should replace the target word with.
                    // This will all take some additional thought and consideration about how I want to store these special cases in the .json.
                    // Testing also needs to be performed in order to figure out if the NLP API I'm using will actually work in most cases for the intended purpose.
                }

                // Perform replacements on anything that's left over (i.e. the singular, non-possessive form of the word)
                stringToSwap = _replaceWithCase(stringToSwap, stringToFindRegex, replacementRegex);
            }

            // Get rid of all of the indicators that we inserted to show that a word had already been replaced.
            stringToSwap = Regex.Replace(stringToSwap, @_settings.ReplacementIndicatorRegex, @"");

            return stringToSwap;
        }
        #endregion


        #region Text Functions

        /// <summary>
        /// Replaces all instances of one word in a given string with a new replacement word.
        /// If the original word began with a capital letter, the replacement will also begin with a capital letter.
        /// 
        /// TODO: If I ever feel like it I guess I could check each letter for capitalization. The first letter should be fine for most cases, though.
        /// This TODO actually isn't quite as simple as I originally assumed because I realized that each word may have a different length from its replacement, so I can't just go character-by-character to check upper/lower case.
        /// If I ever do come back to this, MatchEvaluator is probably my best bet. E.g. Regex.Replace(source, stringToFind, [custom MatchEvaluator()], RegexOptions.IgnoreCase);
        /// </summary>
        /// <param name="source"></param>
        /// <param name="stringToFind"></param>
        /// <param name="replacement"></param>
        /// <returns></returns>
        private static string _replaceWithCase(string source, string stringToFind, string replacement)
        {
            return Regex.Replace(source, stringToFind, 
                x => Char.IsUpper(x.Value[0]) ?
                    Char.ToUpper(replacement[0]) + replacement.Substring(1) : // If the first letter in the string we're replacing is upper-case, have the replacement begin with an upper-case letter
                    replacement, // Otherwise just replace with the regular string
                RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
        }


        /// <summary>
        /// Sends a string to a Natural Language Processing API (defined in appSettings.json) and returns the tagged parts of speech from the response.
        /// </summary>
        /// <param name="textToTag"></param>
        /// <returns></returns>
        private static List<PartsOfSpeechResult> _tagPartsOfSpeech(string textToTag)
        {
            HttpClient httpClient = new HttpClient();
            
            var textContent = new StringContent($"text={textToTag}&output=iob", UnicodeEncoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = httpClient.PostAsync(_settings.NlpApiUrl, textContent).Result; // POST to the API and get the response
                if (response.IsSuccessStatusCode)
                {
                    var responseData = response.Content.ReadAsStringAsync().Result;

                    List<PartsOfSpeechResult> posResults = new List<PartsOfSpeechResult>();
                    var deserialized = JsonConvert.DeserializeObject<PartsOfSpeechResponse>(responseData);

                    // The API returns a line for each word with its code for what part of speech it is
                    foreach (var taggedChunk in deserialized.Text.Split("\n")) // Separate each returned line, match it to the PoS tag, and return the words and PoS tags in a list
                    {
                        var splitResult = taggedChunk.Split(' ');
                        posResults.Add(new PartsOfSpeechResult(_settings, splitResult[0], splitResult[1], splitResult[2]));
                    }
                    return posResults;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION: Failed to tag parts of speech - {ex.Message}\n{ex.StackTrace}");
                throw new Exception($"EXCEPTION: Failed to tag parts of speech - {ex.Message}\n{ex.StackTrace}");
            }
        }

        #endregion

        #region HtmlFunctions

        /// <summary>
        /// Fetches the HTML from a URL and extracts the text content, excluding any scripts.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string _getHtmlTextFromUrl(string url)
        {
            try
            {
                var web = new HtmlWeb();
                var html = web.Load(url);
                var htmlDoc = new HtmlParser(html);
                htmlDoc.RemoveScripts();
                return htmlDoc.ExtractText().Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION: Failed to fetch HTML - {ex.Message}\n{ex.StackTrace}");
                throw new Exception($"EXCEPTION: Failed to fetch HTML - {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Reads a local HTML file and extracts the text content, excluding any scripts.
        /// </summary>
        /// <param name="fileContents"></param>
        /// <returns></returns>
        private static string _getHtmlTextFromFile(string fileContents)
        {
            try
            {
                var htmlDoc = new HtmlParser(fileContents);
                htmlDoc.RemoveScripts();
                return htmlDoc.ExtractText().Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION: Failed to parse HTML from file text - {ex.Message}\n{ex.StackTrace}");
                throw new Exception($"EXCEPTION: Failed to parse HTML from file text - {ex.Message}\n{ex.StackTrace}");
            }
        }

        #endregion

        #region File Functions

        private static string _loadFile(string fileName)
        {
            return File.ReadAllText($"{_rootProjectDirectory}/{_settings.SourceDirectory}/{fileName}");
        }

        private static void _saveFile(string fileName, string fileText)
        {            
            File.WriteAllText($"{_rootProjectDirectory}/{_settings.SourceDirectory}/{fileName}", fileText);
        }
        #endregion
    }
}
