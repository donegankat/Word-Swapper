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
            RemoveScripts();
            //RemoveHrefLineBreaks(_html.DocumentNode);
        }

        private void RemoveHrefLineBreaks(HtmlNode node)
        {
            foreach (var subnode in node.ChildNodes)
            {
                var href = subnode.Attributes["href"].Value;

                if (!string.IsNullOrEmpty(href))
                {
                    href = Regex.Replace(href, @"[\r|\n]", "");
                    subnode.SetAttributeValue("href", href);
                }


                //Recursivly Go Down the tree and check for further hrefs
                if (subnode.HasChildNodes)
                    RemoveHrefLineBreaks(subnode);
            }
        }

        /// <summary>
        /// Removes scripts from the objects html string
        /// </summary>
        public void RemoveScripts()
        {
            _html.DocumentNode.Descendants()
                .Where(node => node.Name == "script")
                .ToList()
                .ForEach(node => node.Remove());
        }

        /// <summary>
        /// Removes the <title></title> tag from the <head></head>
        /// This is needed because Bee fills in a Bee template-related title
        /// </summary>
        public string GetHeadTitle()
        {
            List<string> headTitleTags = new List<string>();

            _html.DocumentNode.Descendants()
                .Where(node => node.Name == "head")
                .FirstOrDefault()
                    .Descendants()
                    .Where(node => node.Name == "title")
                    .ToList()
                    .ForEach(node => headTitleTags.Add(node.InnerText));

            return String.Join(", ", headTitleTags);
        }

        /// <summary>
        /// Extracts the text from the objects provided html String. Basic Fromating is kept in tact (ie new lines)
        /// </summary>
        /// <returns></returns>
        public string ExtractText()
        {
            //convert methods from html agility examples here:
            //http://htmlagilitypack.codeplex.com/SourceControl/changeset/view/94773#1336937
            using (var sw = new StringWriter())
            {
                _convertNode(_html.DocumentNode, sw);
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
                    // don't output comments
                    break;

                case HtmlNodeType.Document:
                    _convertChildrenNodes(node, outText);
                    break;

                case HtmlNodeType.Text:
                    // script and style must not be output
                    var parentName = node.ParentNode.Name;
                    if ((parentName == "script") || (parentName == "style"))
                        break;

                    // get text
                    htmlString = ((HtmlTextNode)node).Text;

                    // is it in fact a special closing node output as text?
                    if (HtmlNode.IsOverlappedClosingElement(htmlString))
                        break;

                    // check the text is meaningful and not a bunch of whitespaces
                    if (htmlString.Trim().Length > 0)
                    {
                        outText.Write(HtmlEntity.DeEntitize(htmlString));
                    }
                    break;

                case HtmlNodeType.Element:
                    switch (node.Name)
                    {
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
