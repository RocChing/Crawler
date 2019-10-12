using Crawl.Core.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Crawl.Core.Impl
{
    public class ThinkWebCrawler : WebCrawler
    {
        protected ThinkCrawlConfiguration _thinkCrawlConfiguration;
        public ThinkWebCrawler(ILogger<WebCrawler> logger, CrawlConfiguration crawlConfiguration, IThreadManager threadManager, ICrawlDecisionMaker crawlDecisionMaker, ICrawlScheduler crawlScheduler, IPageRequester pageRequester, IDocumentParser hyperLinkParser, IRateLimiter rateLimiter, IOptions<ThinkCrawlConfiguration> thinkCrawlOpt) : base(logger, crawlConfiguration, threadManager, crawlDecisionMaker, crawlScheduler, pageRequester, hyperLinkParser, rateLimiter)
        {
            _thinkCrawlConfiguration = thinkCrawlOpt.Value;

            Check();
        }

        public override CrawlResult Crawl(string url = "")
        {
            if (string.IsNullOrEmpty(url)) url = _thinkCrawlConfiguration.StartRule.Url;
            return base.Crawl(url);
        }

        protected override CrawledPage CrawlThePage(PageToCrawl pageToCrawl)
        {
            var crawledPage = base.CrawlThePage(pageToCrawl);
            crawledPage.PageBag.ThinkCrawlConfig = _thinkCrawlConfiguration;
            return crawledPage;
        }

        private void Check()
        {
            var res = _thinkCrawlConfiguration.Check();
            if (!res.Item1)
            {
                _logger.LogError(res.Item2);
                throw new AggregateException(res.Item2);
            }
        }
    }
}
