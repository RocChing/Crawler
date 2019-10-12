using System;
using System.Dynamic;

namespace Crawl.Core.Model
{
    public class PageToCrawl
    {
        public PageToCrawl()
        {
        }

        public PageToCrawl(Uri uri)
        {
            Uri = uri ?? throw new ArgumentNullException("uri");
            PageBag = new ExpandoObject();
        }

        public Uri Uri { get; set; }

        public Uri ParentUri { get; set; }

        public bool IsRetry { get; set; }

        public int RetryCount { get; set; }

        public double? RetryAfter { get; set; }

        public bool IsRoot { get; set; }

        public bool IsInternal { get; set; }

        public int CrawlDepth { get; set; }

        public dynamic PageBag { get; set; }

        public DateTime? LastRequest { get; set; }

        public override string ToString()
        {
            return Uri.AbsoluteUri;
        }
    }
}
