using System;

namespace Crawl.Core.Model
{
    [Serializable]
    public class PageLinksCrawlDisallowedArgs : PageCrawlCompletedArgs
    {
        public string DisallowedReason { get; private set; }

        public PageLinksCrawlDisallowedArgs(CrawlContext crawlContext, CrawledPage crawledPage, string disallowedReason)
            : base(crawlContext, crawledPage)
        {
            if (string.IsNullOrWhiteSpace(disallowedReason))
                throw new ArgumentNullException("disallowedReason");

            DisallowedReason = disallowedReason;
        }
    }
}
