using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WordSwapper
{
    public class CustomPluralizer : IPluralizer
    {
        public string Pluralize(string name)
        {
            return Inflector.Inflector.Pluralize(name) ?? name;
        }
        public string Singularize(string name)
        {
            return Inflector.Inflector.Singularize(name) ?? name;
        }
    }
}
