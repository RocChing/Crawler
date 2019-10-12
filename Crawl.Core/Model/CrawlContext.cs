using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Crawl.Core.Model
{
    public class CrawlContext
    {
        public CrawlContext(ILogger logger)
        {
            CrawlBag = new ExpandoObject();
            CancellationTokenSource = new CancellationTokenSource();
            Logger = logger;
        }

        public ILogger Logger { get; }

        public Uri RootUri { get; set; }

        public int CrawledCount { get; set; }

        public DateTime CrawlStartDate { get; set; }

        public CrawlConfiguration CrawlConfiguration { get; set; }

        public CancellationTokenSource CancellationTokenSource { get; set; }

        public bool IsCrawlHardStopRequested { get; set; }

        public dynamic CrawlBag { get; set; }
    }
}
