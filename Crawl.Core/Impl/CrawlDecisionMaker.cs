using Crawl.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Crawl.Core.Impl
{
    public class CrawlDecisionMaker : ICrawlDecisionMaker
    {
        public virtual CrawlDecision ShouldCrawlPage(PageToCrawl pageToCrawl, CrawlContext crawlContext)
        {
            if (pageToCrawl == null) return CrawlDecision.DisallowCrawl("Null crawled page");

            if (crawlContext == null) return CrawlDecision.DisallowCrawl("Null crawl context");

            if (pageToCrawl.CrawlDepth > crawlContext.CrawlConfiguration.MaxCrawlDepth)
                return CrawlDecision.DisallowCrawl("Crawl depth is above max");

            if (!pageToCrawl.Uri.Scheme.StartsWith("http"))
                return CrawlDecision.DisallowCrawl("Scheme does not begin with http");

            //TODO Do we want to ignore redirect chains (ie.. do not treat them as seperate page crawls)?
            if (!pageToCrawl.IsRetry &&
                crawlContext.CrawlConfiguration.MaxPagesToCrawl > 0 &&
                crawlContext.CrawledCount > crawlContext.CrawlConfiguration.MaxPagesToCrawl)
            {
                return CrawlDecision.DisallowCrawl(string.Format("MaxPagesToCrawl limit of [{0}] has been reached", crawlContext.CrawlConfiguration.MaxPagesToCrawl));
            }

            return CrawlDecision.AllowCrawl();
        }

        public virtual CrawlDecision ShouldCrawlPageLinks(CrawledPage crawledPage, CrawlContext crawlContext)
        {
            if (crawledPage == null) return CrawlDecision.DisallowCrawl("Null crawled page");

            if (crawlContext == null) return CrawlDecision.DisallowCrawl("Null crawl context");

            if (string.IsNullOrWhiteSpace(crawledPage.Content.Text)) return CrawlDecision.DisallowCrawl("Page has no content");

            if (crawledPage.CrawlDepth >= crawlContext.CrawlConfiguration.MaxCrawlDepth)
                return CrawlDecision.DisallowCrawl("Crawl depth is above max");

            return CrawlDecision.AllowCrawl();
        }

        public virtual CrawlDecision ShouldDownloadPageContent(CrawledPage crawledPage, CrawlContext crawlContext)
        {
            if (crawledPage == null) return CrawlDecision.DisallowCrawl("Null crawled page");

            if (crawlContext == null) return CrawlDecision.DisallowCrawl("Null crawl context");

            //if (crawledPage.HttpWebResponse == null) return CrawlDecision.DisallowCrawl("Null HttpWebResponse");

            if (crawledPage.StatusCode != HttpStatusCode.OK)
                return CrawlDecision.DisallowCrawl("HttpStatusCode is not 200");

            //string pageContentType = crawledPage.HttpWebResponse.ContentType.ToLower().Trim();
            //bool isDownloadable = false;
            //List<string> cleanDownloadableContentTypes = crawlContext.CrawlConfiguration.DownloadableContentTypes
            //    .Split(',')
            //    .Select(t => t.Trim())
            //    .Where(t => !string.IsNullOrEmpty(t))
            //    .ToList();

            //foreach (string downloadableContentType in cleanDownloadableContentTypes)
            //{
            //    if (pageContentType.Contains(downloadableContentType.ToLower().Trim()))
            //    {
            //        isDownloadable = true;
            //        break;
            //    }
            //}
            //if (!isDownloadable) return CrawlDecision.DisallowCrawl("Content type is not any of the following: " + string.Join(",", cleanDownloadableContentTypes));

            return CrawlDecision.AllowCrawl();
        }

        public virtual CrawlDecision ShouldRecrawlPage(CrawledPage crawledPage, CrawlContext crawlContext)
        {
            if (crawledPage == null)  return CrawlDecision.DisallowCrawl("Null crawled page");

            if (crawlContext == null) return CrawlDecision.DisallowCrawl("Null crawl context");

            if (crawledPage.Exception == null) return CrawlDecision.DisallowCrawl("WebException did not occur");

            if (crawlContext.CrawlConfiguration.MaxRetryCount < 1)
                return CrawlDecision.AllowCrawl("无限次重试");

            if (crawledPage.RetryCount >= crawlContext.CrawlConfiguration.MaxRetryCount)
                return CrawlDecision.DisallowCrawl("MaxRetryCount has been reached");

            return CrawlDecision.AllowCrawl();
        }
    }
}
