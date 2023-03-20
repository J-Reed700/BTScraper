using System.Diagnostics;
using AI.Dev.OpenAI.GPT;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;
using Catalyst;
using Mosaik.Core;
using HtmlAgilityPack;
using DataCollector.Extensions;
using CsvHelper;
using System.Globalization;

namespace DataCollector.Extractor.Filters
{
    internal class KBFilter : FilterBase<List<Tuple<string, string, string, int>>>, IFilter
    {

        private string _firstHeader = null;
        private string _header = "";
        private string _headerText = "";
        private string followingDesc = "";
        private bool descendedIntoTroubleshooting = false;
        private Dictionary<string, Tuple<string, string>> _headerContent;
        private Dictionary<string, Dictionary<string, int>> TextShown;
        public KBFilter() : base()
        {
            _headerContent = new Dictionary<string, Tuple<string, string>>();
            TextShown = new Dictionary<string, Dictionary<string, int>>();
        }

        public string Header
        {
            get
            {
                return _header;
            }
            set
            {
                if (_firstHeader is null)
                {
                    _firstHeader = value;
                }
                _header = value;
            }
        }

        public string InitialHeader { get; set; }

        public async Task<bool> ProcessNodeBody(HtmlNode body)
        {
            if (!(body is null))
            {

                followingDesc = Header;

                if (_headerContent.Count() == 0)
                {
                    _headerContent[followingDesc] = new ("", InitialHeader);
                }
                TextShown[InitialHeader] = new Dictionary<string, int>();
                var mainBody = body.Css("lt-article__body");
                foreach (var vbody in mainBody)
                {
                    foreach (var node in vbody.Descendants())
                    {
                        if (string.IsNullOrEmpty(node.InnerText.Trim()) || ShouldIgnoreString(node.InnerText)) { continue; }
                        if (!ShouldIgnoreString(node.InnerText) && string.IsNullOrEmpty(_headerText) && IsHeader(node.EndNode.Name))
                        {
                            _headerText = node.InnerText.Trim();
                        }
                        if (node.EndNode.Name == "h2" || node.EndNode.Name == "h3")
                        {
                            descendedIntoTroubleshooting = false;
                            followingDesc = node.InnerText.Trim();
                            if (!_headerContent.ContainsKey(node.InnerText.Trim()))
                                _headerContent[node.InnerText.Trim()] = new ("", InitialHeader);
                        }
                        else if ((node.EndNode.Name == "p" || node.EndNode.Name == "#text" && !string.IsNullOrEmpty(node.InnerText)) && !TextShownContains(node.InnerText.Trim()))
                        {
                            if (string.Equals("Troubleshooting", followingDesc, StringComparison.OrdinalIgnoreCase) || descendedIntoTroubleshooting)
                            {
                                if (Regex.IsMatch(node.InnerHtml, "^<strong>(.*?)<\\/strong>$"))
                                {
                                    followingDesc = node.InnerText;
                                    descendedIntoTroubleshooting = true;
                                }
                                else
                                {
                                    AddToHeaderContent(node);
                                }
                            }
                            else
                            {
                                AddToHeaderContent(node);
                            }
                            TextShown[InitialHeader][node.InnerText.Trim()] = TextShown[InitialHeader].ContainsKey(node.InnerText.Trim()) ? TextShown[InitialHeader][node.InnerText.Trim()] + 1 : 1;
                        }
                    }
                }
            }
            _header = string.Empty;
            _headerText = string.Empty;
            return false;
        }

        private void AddToHeaderContent(HtmlNode node)
        {
            if (_headerContent.ContainsKey(followingDesc) && !string.IsNullOrEmpty(_headerContent[followingDesc].Item1))
            {
                _headerContent[followingDesc] = new (_headerContent[followingDesc].Item1 + " " + node.InnerText.Trim(), InitialHeader);
            }
            else
            {
                _headerContent[followingDesc] = new (node.InnerText.Trim(), InitialHeader);
            }
        }

        private bool TextShownContains(string value)
        {
            return TextShown.ContainsKey(InitialHeader) && TextShown[InitialHeader] != null && TextShown[InitialHeader].ContainsKey(value);
        }

        // count the number of tokens in a string
        private static int count_tokens(string text)
        {
            return GPT3Tokenizer.Encode(text).Count;
        }

        // 
        //     Reduce a long text to a maximum of `max_len` tokens by potentially cutting at a sentence end
        //     
        private static string reduce_long(string long_text, bool long_text_tokens = false, int max_len = 590)
        {
            int token_count = 0;

            if (!long_text_tokens)
            {
                token_count = count_tokens(long_text);
            }
            if (token_count > max_len)
            {
                var sentences = new Document(long_text.Replace("\n", " "), Language.English).ToList();
                var ntokens = 0;
                foreach (var (i, sentence) in sentences.Select((_p_1, _p_2) => Tuple.Create(_p_2, _p_1)))
                {
                    ntokens += 1 + count_tokens(sentence.Value);
                    if (ntokens > max_len)
                    {
                        var partialSentences = sentences.ToArray().Take(i).Select(x => x.Value);
                        return string.Join("\". \"", partialSentences.Take(partialSentences.Count() - 1)) + ".";
                    }
                }
            }
            return long_text;
        }

        // 
        //     Extract the sections of a Wikipedia page, discarding the references and other low information sections
        //     
        private bool FilterSections(Dictionary<string, Tuple<string,string>> headerContent, int max_len = 1500)
        {
            if (headerContent.Count() == 0)
            {
                return true;
            }
            Debug.Assert(headerContent.Keys.Count == headerContent.Values.Count());
            var cont = headerContent[_firstHeader].Item1.Trim();
            var outputs = new List<Tuple<string, string, string, int>> {
                Tuple.Create(headerContent[_firstHeader].Item2, _firstHeader, cont, count_tokens(cont) + 4)
            };
            headerContent.Remove(_firstHeader);
            // discard the discard categories, accounting for a tree structure
            var max_level = 100;
            var keep_group_level = max_level;
            var remove_group_level = max_level;
            var nheadings = new List<string>();
            var ncontents = new List<string>();
            var headerContentFinal = new Dictionary<string, Tuple<string,string>>();
            foreach (var (heading, content) in headerContent)
            {
                var plain_heading = heading;
                var num_equals = heading.Split(" ")[0].Count();
                if (num_equals <= keep_group_level)
                {
                    keep_group_level = max_level;
                }
                if (num_equals > remove_group_level)
                {
                    if (num_equals <= keep_group_level)
                    {
                        continue;
                    }
                }
                keep_group_level = max_level;
                headerContentFinal[heading.Trim()] = content;
                remove_group_level = max_level;
            }
            // count the tokens of each section
            var ncontent_ntokens = headerContentFinal
                                            .Select(pair =>
                                            {
                                                int tokensInHeading = count_tokens(string.Join(" ", pair.Key.Split(" ").Skip(1).TakeWhile(s => s != "")));
                                                int tokensInContent = count_tokens(pair.Value.Item1);
                                                int adjustment = pair.Value.Item1.Length == 0 ? 1 : 0;
                                                return tokensInContent + 3 + tokensInHeading - adjustment;
                                            }).ToList();
            // Create a tuple of (title, section_name, content, number of tokens)
            outputs.AddRange(from _tup_3 in headerContentFinal.Zip(ncontent_ntokens)
                             let h = _tup_3.Item1
                             let c = _tup_3.Item2
                             select c < max_len ? Tuple.Create(h.Value.Item2, h.Key, h.Value.Item1, c) : Tuple.Create(h.Value.Item2, h.Key, reduce_long(h.Value.Item1, max_len: max_len), count_tokens(reduce_long(h.Value.Item1, max_len: max_len))));
            data.AddRange(outputs);
            return true;
        }

        public bool ShouldIgnoreString(string str)
        {
            switch (str.Trim())
            {
                case null:
                    return true;
                case "":
                    return true;
                case "&nbsp;":
                    return true;
                default:
                    return false;
            }

        }

        public bool IsHeader(string str)
        {
            switch (str)
            {
                case "h":
                    return true;
                case "strong":
                    return true;
                default:
                    return false;
            }
        }

        public bool EvaluateSections()
        {
            return FilterSections(_headerContent);
        }

        public bool SaveToLocalCsvFile(string path)
        {
            try
            {
                using (var writer = new StreamWriter(path))
                {
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecords(data);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool AddContents(List<Tuple<string, string, string, int>> importData)
        {
            if(data == null || data.Count == 0) 
            { 
                data = importData;
            }
            else
            {
                data.AddRange(importData);
            }
            return true;
        }
    }
}