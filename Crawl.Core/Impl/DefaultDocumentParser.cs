using System;
using System.Collections.Generic;
using System.Text;
using Crawl.Core.Model;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Crawl.Core.Impl
{
    public class DefaultDocumentParser : DocumentParser
    {
        protected override string ParserType
        {
            get { return "HtmlAgilityPack"; }
        }

        public DefaultDocumentParser(ILogger<DocumentParser> logger)
            : base(logger)
        {

        }

        public override dynamic GetData(CrawledPage crawledPage)
        {
            return null;
        }

        protected override IEnumerable<string> GetHrefValues(CrawledPage crawledPage)
        {
            List<string> hrefValues = new List<string>();
            if (HasRobotsNoFollow(crawledPage))
                return hrefValues;

            HtmlNodeCollection aTags = crawledPage.HtmlDocument.DocumentNode.SelectNodes("//a[@href]");
            HtmlNodeCollection areaTags = crawledPage.HtmlDocument.DocumentNode.SelectNodes("//area[@href]");
            HtmlNodeCollection canonicals = crawledPage.HtmlDocument.DocumentNode.SelectNodes("//link[@rel='canonical'][@href]");

            hrefValues.AddRange(GetLinks(aTags));
            hrefValues.AddRange(GetLinks(areaTags));
            hrefValues.AddRange(GetLinks(canonicals));

            return hrefValues;
        }

        protected override string GetBaseHrefValue(CrawledPage crawledPage)
        {
            string hrefValue = "";
            HtmlNode node = crawledPage.HtmlDocument.DocumentNode.SelectSingleNode("//base");

            //Must use node.InnerHtml instead of node.InnerText since "aaa<br />bbb" will be returned as "aaabbb"
            if (node != null) hrefValue = node.GetAttributeValue("href", "").Trim();

            return hrefValue;
        }

        protected override string GetMetaRobotsValue(CrawledPage crawledPage)
        {
            string robotsMeta = null;
            HtmlNode robotsNode = crawledPage.HtmlDocument.DocumentNode.SelectSingleNode("//meta[translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')='robots']");
            if (robotsNode != null)
                robotsMeta = robotsNode.GetAttributeValue("content", "");

            return robotsMeta;
        }

        protected virtual List<string> GetLinks(HtmlNodeCollection nodes)
        {
            List<string> hrefs = new List<string>();

            if (nodes == null)
                return hrefs;

            string hrefValue = "";
            foreach (HtmlNode node in nodes)
            {
                if (HasRelNoFollow(node))
                    continue;

                hrefValue = node.Attributes["href"].Value;
                if (!string.IsNullOrWhiteSpace(hrefValue))
                {
                    hrefValue = DeEntitize(hrefValue);
                    hrefs.Add(hrefValue);
                }
            }

            return hrefs;
        }

        protected virtual string DeEntitize(string hrefValue)
        {
            string dentitizedHref = hrefValue;

            try
            {
                dentitizedHref = HtmlEntity.DeEntitize(hrefValue);
            }
            catch
            {
                _logger.LogWarning("Error dentitizing uri: {0} This usually means that it contains unexpected characters", hrefValue);
            }

            return dentitizedHref;
        }

        protected virtual bool HasRelNoFollow(HtmlNode node)
        {
            HtmlAttribute attr = node.Attributes["rel"];
            return (attr != null && attr.Value.ToLower().Trim() == "nofollow");
        }
    }
}
