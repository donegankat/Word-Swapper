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
            CustomPluralizer pluralizer = new CustomPluralizer();

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

            Console.WriteLine("ORIGINAL TEXT:");
            Console.WriteLine(stringToSwap);

            var negativeLookAhead = $@"(?!{_settings.ReplacementIndicatorRegex})"; // This is a negative look-ahead indicator that we can add to the end of regex searches for each word so we don't replace words that we've already replaced

            foreach (var word in _settings.WordSwap)
            {
                if (word.CanBePlural)
                {
                    string pluralStringToFind = pluralizer.Pluralize(word.Word);
                    string pluralReplacement = pluralizer.Pluralize(word.Replacement);

                    var pluralStringToFindRegex = $@"\b({pluralStringToFind}){negativeLookAhead}\b";
                    var pluralReplacementRegex = $@"{pluralReplacement}{_settings.ReplacementIndicator}";

                    // Perform replacements on any plural variations of the word
                    stringToSwap = _replaceWithCase(stringToSwap, pluralStringToFindRegex, pluralReplacementRegex);
                }                

                if (word.CanBePossessive)
                {
                    var possessiveStringToFind = $@"\b({word.Word})('s){negativeLookAhead}\b";
                    var possessiveReplacement = $@"{word.Replacement}'s{_settings.ReplacementIndicator}";

                    // Perform replacements on any possessive variations of the word
                    stringToSwap = _replaceWithCase(stringToSwap, possessiveStringToFind, possessiveReplacement);
                }

                var stringToFindRegex = $@"\b({word.Word}){negativeLookAhead}\b";
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

            Console.WriteLine();
            Console.WriteLine("==========================");
            Console.WriteLine();
            Console.WriteLine("NEW TEXT:");
            Console.WriteLine(stringToSwap);

            // Save the file as: [original file name]_Swapped.[extension]
            var extension = Path.GetExtension(fileName);
            _saveFile(fileName.Replace(extension, $"_Swapped{extension}"), stringToSwap);

            Console.WriteLine();
            Console.WriteLine("==========================");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

        private static string _replaceWithCase(string source, string stringToFind, string replacement)
        {
            return Regex.Replace(source, stringToFind,
                x => Char.IsUpper(x.Value[0]) ?
                    Char.ToUpper(replacement[0]) + replacement.Substring(1) : // If the first letter in the string we're replacing is upper-case, have the replacement begin with an upper-case letter
                    replacement, // Otherwise just replace with the regular string
                RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
        }

        private static List<PartsOfSpeechResult> _tagPartsOfSpeech(string textToTag)
        {
            HttpClient httpClient = new HttpClient();
            var textContent = new StringContent($"text={textToTag}&output=iob", UnicodeEncoding.UTF8, "application/json");
            HttpResponseMessage response = httpClient.PostAsync(_settings.NlpApiUrl, textContent).Result;
            if (response.IsSuccessStatusCode)
            {
                var responseData = response.Content.ReadAsStringAsync().Result;

                List<PartsOfSpeechResult> posResults = new List<PartsOfSpeechResult>();
                var deserialized = JsonConvert.DeserializeObject<PartsOfSpeechResponse>(responseData);
                //deserialized.Text = deserialized.Text.
                //var y = deserialized.
                foreach (var taggedChunk in deserialized.Text.Split("\n"))
                {
                    var splitResult = taggedChunk.Split(' ');
                    posResults.Add(new PartsOfSpeechResult(_settings, splitResult[0], splitResult[1], splitResult[2]));
                }
                return posResults;
            }
            return null;
        }

        private static string _loadFile(string fileName)
        {
            return File.ReadAllText($"{_rootProjectDirectory}/{_settings.SourceDirectory}/{fileName}");
        }

        private static void _saveFile(string fileName, string fileText)
        {            
            File.WriteAllText($"{_rootProjectDirectory}/{_settings.SourceDirectory}/{fileName}", fileText);
        }
    }
}
