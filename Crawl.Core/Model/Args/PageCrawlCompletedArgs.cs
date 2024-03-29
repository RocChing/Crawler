﻿using System;

namespace Crawl.Core.Model
{
    [Serializable]
    public class PageCrawlCompletedArgs : CrawlArgs
    {
        public CrawledPage CrawledPage { get; private set; }

        public PageCrawlCompletedArgs(CrawlContext crawlContext, CrawledPage crawledPage)
            : base(crawlContext)
        {
            CrawledPage = crawledPage ?? throw new ArgumentNullException("crawledPage");
        }
    }
}
