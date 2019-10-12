using System;
using System.Collections.Generic;
using System.Text;

namespace Crawl.Core.Model
{
    public class CrawlDecision
    {
        public CrawlDecision()
        {
            Reason = "";
        }

        public CrawlDecision(bool allow, string reason)
        {
            Allow = allow;
            Reason = reason;
        }

        public bool Allow { get; set; }

        public string Reason { get; set; }

        public bool ShouldStopCrawl { get; set; }

        public bool ShouldHardStopCrawl { get; set; }

        public static CrawlDecision AllowCrawl(string reason = "")
        {
            return new CrawlDecision(true, reason);
        }

        public static CrawlDecision DisallowCrawl(string reason)
        {
            return new CrawlDecision(false, reason);
        }
    }
}
