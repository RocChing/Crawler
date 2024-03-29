﻿using System;

namespace Crawl.Core.Model
{
    [Serializable]
    public class CrawlArgs : EventArgs
    {
        public CrawlContext CrawlContext { get; set; }

        public CrawlArgs(CrawlContext crawlContext)
        {
            CrawlContext = crawlContext ?? throw new ArgumentNullException("crawlContext");
        }
    }
}
