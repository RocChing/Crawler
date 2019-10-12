using System;
using System.Collections.Generic;
using System.Text;

namespace Crawl.Core.Model
{
    public class CrawlResult
    {
        public CrawlResult()
        {

        }

        public TimeSpan Elapsed { get; set; }


        public Exception Error { get; set; }

        public CrawlContext  Context { get; set; }
    }
}
