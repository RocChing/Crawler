using System;
using System.Collections.Generic;
using System.Text;
using Crawl.Core.Model;

namespace Crawl.Core
{
    public interface ICrawlDecisionMaker
    {
        /// <summary>
        /// Decides whether the page should be crawled
        /// </summary>
        CrawlDecision ShouldCrawlPage(PageToCrawl pageToCrawl, CrawlContext crawlContext);

        /// <summary>
        /// Decides whether the page's links should be crawled
        /// </summary>
        CrawlDecision ShouldCrawlPageLinks(CrawledPage crawledPage, CrawlContext crawlContext);

        /// <summary>
        /// Decides whether the page's content should be dowloaded
        /// </summary>
        CrawlDecision ShouldDownloadPageContent(CrawledPage crawledPage, CrawlContext crawlContext);

        /// <summary>
        /// Decides whether the page should be re-crawled
        /// </summary>
        CrawlDecision ShouldRecrawlPage(CrawledPage crawledPage, CrawlContext crawlContext);
    }
}
