using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WordSwapper
{
    /// <summary>
    /// Provide functionality to manipulate html strings. More or less a wrapper for the third party library 'HtmlAgility'
    /// </summary>
    // HtmlAgility Documentation: http://html-agility-pack.net/api
    public class HtmlParser
    {
        private HtmlDocument _html;

        public HtmlParser(string htmlString)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlString);

            _html = doc;
        }

        public HtmlParser(HtmlDocument doc)
        {
            _html = doc;
        }

        /// <summary>
        /// Returns the html string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _html.DocumentNode.OuterHtml;
        }

        public void Sanitize()
        {
            RemoveHeadSection();
            RemoveScripts();
        }

        /// <summary>
        /// Removes scripts from the object's HTML string.
        /// </summary>
        public void RemoveScripts()
        {
            _html.DocumentNode.Descendants()
                .Where(node => node.Name == "script")
                .ToList()
                .ForEach(node => node.Remove());
        }

        /// <summary>
        /// Removes the <head></head> section and everything in it.
        /// </summary>
        public void RemoveHeadSection()
        {
            _html.DocumentNode
                .Descendants()
                .Where(node => node.Name == "head")
                .FirstOrDefault()
                .RemoveAll();
        }

        /// <summary>
        /// Extracts the text from the objects provided HTML string. Basic formatting is kept intact (i.e. new lines).
        /// </summary>
        /// <returns></returns>
        public string ExtractText()
        {
            // Convert methods from html agility examples here:
            // http://htmlagilitypack.codeplex.com/SourceControl/changeset/view/94773#1336937
            using (var sw = new StringWriter())
            {
                var articleNode = _html.DocumentNode.SelectSingleNode("//article");
                if (articleNode != null) // If the document has an <article> tag, that's probably the text we want to extract.
                {
                    _convertNode(articleNode, sw);
                }
                else // If the document doesn't have an <article> tag, just extract everything.
                {
                    _convertNode(_html.DocumentNode, sw);
                }
                sw.Flush();

                return sw.ToString();
            }
        }

        private void _convertNode(HtmlNode node, TextWriter outText)
        {
            string htmlString;
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    // Don't output comments.
                    break;

                case HtmlNodeType.Document:
                    _convertChildrenNodes(node, outText);
                    break;

                case HtmlNodeType.Text:
                    // Script and style must not be output.
                    // NOTE: For some reason this doesn't catch all of the <script> nodes, so we also use a custom RemoveScripts() method above.
                    var parentName = node.ParentNode.Name;
                    if ((parentName == "script") || (parentName == "style"))
                        break;

                    // Get text
                    htmlString = ((HtmlTextNode)node).Text;

                    // Is it in fact a special closing node output as text?
                    if (HtmlNode.IsOverlappedClosingElement(htmlString))
                        break;

                    // Check the text is meaningful and not a bunch of whitespaces.
                    if (htmlString.Trim().Length > 0)
                    {
                        outText.Write(HtmlEntity.DeEntitize(htmlString));
                    }
                    break;

                case HtmlNodeType.Element:
                    switch (node.Name)
                    {
                        // Treat paragraphs, headers, and other line-breaking elements as CRLF.
                        case "h1":
                        case "h2":
                        case "h3":
                        case "h4":
                        case "p":
                        case "label":
                            outText.Write("\r\n");
                            break;
                    }

                    if (node.HasChildNodes)
                    {
                        _convertChildrenNodes(node, outText);
                    }
                    break;
            }
        }

        private void _convertChildrenNodes(HtmlNode node, TextWriter outText)
        {
            foreach (var subnode in node.ChildNodes)
            {
                _convertNode(subnode, outText);
            }
        }
    }
}
