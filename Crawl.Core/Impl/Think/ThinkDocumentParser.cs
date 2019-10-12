using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using Crawl.Core.Model;
using Crawl.Core.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Crawl.Core.Impl
{
    public class ThinkDocumentParser : IDocumentParser
    {
        private ILogger<ThinkDocumentParser> _logger;
        private ThinkCrawlConfiguration _thinkCrawlConfiguration;
        public ThinkDocumentParser(ILogger<ThinkDocumentParser> logger, IOptions<ThinkCrawlConfiguration> thinkCrawlOpt)
        {
            _logger = logger;
            _thinkCrawlConfiguration = thinkCrawlOpt.Value;
        }

        public IEnumerable<PageToCrawl> GetLinks(CrawledPage crawledPage)
        {
            List<PageToCrawl> pages = new List<PageToCrawl>();
            List<string> rules = GetDynamicData<List<string>>(crawledPage.PageBag, "Rules");
            if (rules == null)
            {
                rules = _thinkCrawlConfiguration.StartRule.GetRules();
            }

            var doc = crawledPage.HtmlDocument;
            foreach (var ruleName in rules)
            {
                var rule = _thinkCrawlConfiguration.GetRule(ruleName);
                if (rule == null) continue;

                string xpath = rule.XPath;
                if (string.IsNullOrEmpty(xpath) || !rule.NeedCrawl) continue;
                #region add link

                bool donotFilter = string.IsNullOrEmpty(rule.FilterText);
                var links = doc.DocumentNode.SelectNodes(xpath);

                if (links == null || links.Count < 1) continue;

                foreach (var a in links)
                {
                    string text = StringUtil.RemoveHTML(a.InnerText);
                    string url = FormatUrl(a.Attributes["href"].Value ?? "", crawledPage.Uri);
                    if (string.IsNullOrEmpty(url)) continue;

                    if (donotFilter || text == rule.FilterText)
                    {
                        _logger.LogInformation("文本:[{0}] , URL: {1}", text, url);

                        var uri = new Uri(url);

                        PageToCrawl page = new PageToCrawl(uri)
                        {
                            ParentUri = crawledPage.Uri,
                            CrawlDepth = crawledPage.CrawlDepth + 1,
                            IsInternal = uri.Authority == crawledPage.Uri.Authority,
                            IsRoot = false
                        };

                        page.PageBag.Rules = rule.GetNextRules();
                        pages.Add(page);
                    }
                }
                #endregion
            }
            return pages;
        }

        public dynamic GetData(CrawledPage crawledPage)
        {
            List<string> rules = GetDynamicData<List<string>>(crawledPage.PageBag, "Rules");
            if (rules == null)
            {
                rules = _thinkCrawlConfiguration.StartRule.GetRules();
            }

            Dictionary<string, dynamic> dic = new Dictionary<string, dynamic>();
            foreach (var ruleName in rules)
            {
                var rule = _thinkCrawlConfiguration.GetRule(ruleName);
                if (rule == null) continue;

                if (rule.HasFields())
                {
                    dynamic ruleData = new ExpandoObject();
                    foreach (var field in rule.Fields)
                    {
                        ExpandFieldData(field, ruleData, crawledPage.HtmlDocument.DocumentNode);
                    }
                    dic.Add(ruleName, ruleData);

                    string info = JsonConvert.SerializeObject(ruleData, new Newtonsoft.Json.Converters.ExpandoObjectConverter());
                    _logger.LogInformation(info);
                }
            }
            return dic;
        }

        private void ExpandFieldData(ThinkCrawlField field, dynamic data, HtmlAgilityPack.HtmlNode root, HtmlAgilityPack.HtmlNode parentNode = null)
        {
            string name = field.Name;
            string fieldXPath = field.XPath;
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(fieldXPath))
            {
                SetDynamicValue(data, name, "");
            }
            if (field.Type == ThinkCrawlFieldType.Single)
            {
                SetDynamicValue(data, name, GetHtmlNodeValue(field.Inherit ? parentNode : root, field));
            }
            else if (field.Type == ThinkCrawlFieldType.Group)
            {
                List<dynamic> childList = new List<dynamic>();
                var nodes = root.SelectNodes(field.XPath);
                if (nodes != null && nodes.Count > 0)
                {
                    foreach (var node in nodes)
                    {
                        dynamic childData = new ExpandoObject();
                        foreach (var childField in field.Children)
                        {
                            ExpandFieldData(childField, childData, root, node);
                        }
                        childList.Add(childData);
                    }
                }
                SetDynamicValue(data, name, childList);
            }
        }

        #region 辅助方法
        private void SetDynamicValue(dynamic data, string key, object value)
        {
            IDictionary<string, object> dic = data as IDictionary<string, object>;
            if (dic != null)
            {
                if (dic.ContainsKey(key)) dic[key] = value;
                else dic.Add(key, value);
            }
        }

        private string GetHtmlNodeValue(HtmlAgilityPack.HtmlNode hn, ThinkCrawlField field)
        {
            string value = "";
            if (hn == null) return value;
            var node = hn.SelectSingleNode(field.XPath);
            if (node != null)
            {
                if (!string.IsNullOrEmpty(field.Attr)) value = GetHtmlNodeAttributeValue(node, field.Attr);
                else if (field.IsHtml) value = node.InnerHtml;
                else value = StringUtil.RemoveHTML(node.InnerText);
            }
            return value;
        }

        private string GetHtmlNodeAttributeValue(HtmlAgilityPack.HtmlNode node, string name)
        {
            if (node.HasAttributes)
            {
                return node.Attributes[name].Value;
            }
            return "";
        }

        private T GetDynamicData<T>(dynamic bag, string key) where T : class
        {
            if (bag != null)
            {
                var dic = bag as IDictionary<string, object>;
                if (dic.Keys.Contains(key))
                {
                    return dic[key] as T;
                }
            }
            return null;
        }

        private string FormatUrl(string url, Uri baseUri)
        {
            if (StringUtil.IsUrl(url)) return url;
            return new Uri(baseUri, url).AbsoluteUri;
        }

        #endregion
    }
}
