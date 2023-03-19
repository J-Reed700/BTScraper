using System.Diagnostics;
using AI.Dev.OpenAI.GPT;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;
using Catalyst;
using Mosaik.Core;
using HtmlAgilityPack;
using DataCollector.Extensions;

namespace DataCollector
{
    public class Filter
    {

        private string _firstHeader = null;
        private string _header = "";
        private string _headerText = "";
        private string followingDesc = "";
        private Dictionary<string, string> _headerContent;
        private List<Tuple<string, string, string, int>> HeaderContents;
        private Dictionary<string, int> TextShown; 
        public Filter()
        {
            _headerContent= new Dictionary<string, string>();
            HeaderContents = new List<Tuple<string, string, string, int>>();
            TextShown = new Dictionary<string, int>();
        }
        
        public string GetHeader()
        {
            return _header;
        }

        public void SetHeader(string header)
        {
            if(_firstHeader is null)
            {
                _firstHeader = header;
            }
            _header = header;
        }

        public async Task<bool> ProcessBody(HtmlNode body)
        {
            if(!(body is null))
            {

                followingDesc = _header;
                
                if(_headerContent.Count() == 0)
                {
                    _headerContent[followingDesc] = "";
                }
                var mainBody = body.Css("lt-article__body");
                foreach(var vbody in mainBody)
                {
                    foreach(var node in vbody.Descendants())
                    {
                        if(String.IsNullOrEmpty(node.InnerText.Trim()) || ShouldIgnoreString(node.InnerText)) { continue; }
                        if(!ShouldIgnoreString(node.InnerText) && String.IsNullOrEmpty(_headerText) && IsHeader(node.EndNode.Name)) 
                        {
                            _headerText = node.InnerText.Trim(); 
                        }
                        if(node.EndNode.Name == "h2" || node.EndNode.Name == "h3") 
                        {
                            followingDesc = node.InnerText.Trim();
                            _headerContent[node.InnerText.Trim()] = "";
                        }
                        if(node.EndNode.Name == "p" && !TextShown.ContainsKey(node.InnerText.Trim()))                        
                        {
                            if (_headerContent.ContainsKey(followingDesc) && !String.IsNullOrEmpty(_headerContent[followingDesc]))
                            {
                                _headerContent[followingDesc] += " " + node.InnerText.Trim();
                            }
                            else
                            {
                                _headerContent[followingDesc] = node.InnerText.Trim();
                            }
                            TextShown[node.InnerText.Trim()] = TextShown.ContainsKey(node.InnerText.Trim()) ? TextShown[node.InnerText.Trim()] + 1 : 1;
                        }
                    }
                }
            }
            _header = String.Empty;
            _headerText= String.Empty;
            return false;
        }

        public void test()
        {
            var doc = new Document("The quick brown fox jumps over the lazy dog", Language.English);
        }

        // count the number of tokens in a string
        public static int count_tokens(string text)
        {
            return GPT3Tokenizer.Encode(text).Count;
        }

        // 
        //     Reduce a long text to a maximum of `max_len` tokens by potentially cutting at a sentence end
        //     
        public static string reduce_long(string long_text, bool long_text_tokens = false, int max_len = 590)
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
                        return String.Join("\". \"", partialSentences.Take(partialSentences.Count() - 1)) + ".";
                    }
                }
            }
            return long_text;
        }

        public static List<string> discard_categories = new List<string> {
        "See also",
        "References",
        "External links",
        "Further reading",
        "Footnotes",
        "Bibliography",
        "Sources",
        "Citations",
        "Literature",
        "Footnotes",
        "Notes and references",
        "Photo gallery",
        "Works cited",
        "Photos",
        "Gallery",
        "Notes",
        "References and sources",
        "References and notes"
    };

        // 
        //     Extract the sections of a Wikipedia page, discarding the references and other low information sections
        //     
        public void extract_sections(Dictionary<string,string> headerContent, string title, int max_len = 1500, object discard_categories = null)
        {
            if (headerContent.Count() == 0)
            {
                return;
            }
            Debug.Assert(headerContent.Keys.Count == headerContent.Values.Count() );
            var cont = headerContent[_firstHeader].Trim();
            var outputs = new List<Tuple<string, string, string, int>> {
                Tuple.Create(title, _header, cont, count_tokens(cont) + 4)
            };
            headerContent.Remove(_header);
            // discard the discard categories, accounting for a tree structure
            var max_level = 100;
            var keep_group_level = max_level;
            var remove_group_level = max_level;
            var nheadings = new List<string>();
            var ncontents = new List<string>();
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
                nheadings.Add(heading.Trim());
                ncontents.Add(heading.Trim());
                remove_group_level = max_level;
            }
            // count the tokens of each section
            var ncontent_ntokens = nheadings.Zip(ncontents, (h, c) => new { Heading = h, Content = c })
                                            .Select(pair =>
                                            {
                                                int tokensInHeading = count_tokens(string.Join(" ", pair.Heading.Split(" ").Skip(1).TakeWhile(s => s != "")));
                                                int tokensInContent = count_tokens(pair.Content);
                                                int adjustment = pair.Content.Length == 0 ? 1 : 0;
                                                return tokensInContent + 3 + tokensInHeading - adjustment;
                                            }).ToList();
            // Create a tuple of (title, section_name, content, number of tokens)
            outputs.AddRange(from _tup_3 in Enumerable.Zip(nheadings, ncontents, ncontent_ntokens)
                        let h = _tup_3.Item1
                        let c = _tup_3.Item2
                        let t = _tup_3.Item3
                        select t < max_len ? Tuple.Create(title, h, c, t) :Tuple.Create(title, h, reduce_long(c, max_len: max_len), count_tokens(reduce_long(c, max_len: max_len))));
            HeaderContents.AddRange(outputs);
        }

        public bool ShouldIgnoreString(string str)
        {
            switch (str.Trim()) {
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
            switch(str)
            {
                case "h":
                    return true;
                case "strong":
                    return true;
                default:
                    return false;
            }
        }

        public void EvaluateSections()
        {
            extract_sections(_headerContent, "Sections");
        }
    }
}