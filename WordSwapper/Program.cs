using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using WordSwapper.Settings;

namespace WordSwapper
{    
    class Program
    {
        private static string _currentDirectory = Directory.GetCurrentDirectory();
        private static string _rootProjectDirectory = _currentDirectory.Remove(_currentDirectory.IndexOf("\\bin"));

        private static AppSettings _configureAppSettings()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appSettings.WordsToSwap.json", reloadOnChange: true, optional: true)
            .AddEnvironmentVariables();

            var configuration = builder.Build();

            var settings = new AppSettings();
            configuration.Bind(settings);

            return settings;
        }

        static void Main(string[] args)
        {
            var settings = _configureAppSettings();
            CustomPluralizer pluralizer = new CustomPluralizer();

            var fileName = "";
            while (true) // Loop until we get a valid file name.
            {
                Console.WriteLine("Enter the file name that you wish to swap:");
                fileName = Console.ReadLine();

                if (File.Exists($"{_rootProjectDirectory}/Sources/{fileName}")) // The user input a valid file name, so no need to keep looping.
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

            Console.WriteLine($"Original string: {stringToSwap}");

            string replacedString = stringToSwap;
            var negativeLookAhead = $@"(?!{settings.ReplacementIndicatorRegex})"; // This is a negative look-ahead indicator that we can add to the end of regex searches for each word so we don't replace words that we've already replaced

            foreach (var word in settings.WordSwap)
            {
                if (word.CanBePlural)
                {
                    string pluralStringToFind = pluralizer.Pluralize(word.Word);
                    string pluralReplacement = pluralizer.Pluralize(word.Replacement);

                    var pluralStringToFindRegex = $@"\b({pluralStringToFind}){negativeLookAhead}\b";
                    var pluralReplacementRegex = $@"{pluralReplacement}{settings.ReplacementIndicator}";

                    // Perform replacements on any plural variations of the word
                    replacedString = _replaceWithCase(replacedString, pluralStringToFindRegex, pluralReplacementRegex);
                }                

                if (word.CanBePossessive)
                {
                    var possessiveStringToFind = $@"\b({word.Word})('s){negativeLookAhead}\b";
                    var possessiveReplacement = $@"{word.Replacement}'s{settings.ReplacementIndicator}";

                    // Perform replacements on any possessive variations of the word
                    replacedString = _replaceWithCase(replacedString, possessiveStringToFind, possessiveReplacement);
                }

                var stringToFindRegex = $@"\b({word.Word}){negativeLookAhead}\b";
                var replacementRegex = $@"{word.Replacement}{settings.ReplacementIndicator}";

                // Perform replacements on anything that's left over (i.e. the singular, non-possessive form of the word)
                replacedString = _replaceWithCase(replacedString, stringToFindRegex, replacementRegex);
            }

            // Make the first word and any words that follow a line break or period begin with an upper case letter.
            // NOTE: This is no longer needed because I now check to see if the word we're replacing began with an uppercase letter. Still I wanted to perform a check-in so I remember this code.
            //replacedString = Regex.Replace(replacedString, @"(^|\n|\.\s)(\w)", x => x.Groups[1].Value + x.Groups[2].Value.ToUpper());

            // Get rid of all of the indicators that we inserted to show that a word had already been replaced.
            replacedString = Regex.Replace(replacedString, @settings.ReplacementIndicatorRegex, @"");

            Console.WriteLine();
            Console.WriteLine("==========================");
            Console.WriteLine();
            Console.WriteLine($"New string: {replacedString}");

            // Save the file as: [original file name]_Swapped.[extension]
            var extension = Path.GetExtension(fileName);
            _saveFile(fileName.Replace(extension, $"_Swapped{extension}"), replacedString);

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

        private static string _loadFile(string fileName)
        {
            return File.ReadAllText($"{_rootProjectDirectory}/Sources/{fileName}");
        }

        private static void _saveFile(string fileName, string fileText)
        {            
            File.WriteAllText($"{_rootProjectDirectory}/Sources/{fileName}", fileText);
        }
    }
}
