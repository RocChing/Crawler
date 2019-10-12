using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Crawl.Core.Model
{
    [Serializable]
    public class CrawledPage : PageToCrawl
    {
        Lazy<HtmlDocument> _htmlDocument;

        public CrawledPage(Uri uri)
            : base(uri)
        {
            _htmlDocument = new Lazy<HtmlDocument>(InitializeHtmlAgilityPackDocument);

            Content = new PageContent();
        }

        public HtmlDocument HtmlDocument { get { return _htmlDocument.Value; } }

        /// <summary>
        /// 错误
        /// </summary>
        public Exception Exception { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public override string ToString()
        {
            return string.Format("{0}[{1}]", Uri.AbsoluteUri, (int)StatusCode);
        }

        /// <summary>
        /// Links parsed from page. This value is set by the WebCrawler.SchedulePageLinks() method only If the "ShouldCrawlPageLinks" rules return true or if the IsForcedLinkParsingEnabled config value is set to true.
        /// </summary>
        public IEnumerable<PageToCrawl> ParsedLinks { get; set; }

        public dynamic ParsedData { get; set; }

        /// <summary>
        /// The content of page request
        /// </summary>
        public PageContent Content { get; set; }

        /// <summary>
        /// A datetime of when the http request started
        /// </summary>
        public DateTime RequestStarted { get; set; }

        /// <summary>
        /// A datetime of when the http request completed
        /// </summary>
        public DateTime RequestCompleted { get; set; }

        /// <summary>
        /// A datetime of when the page content download started, this may be null if downloading the content was disallowed by the CrawlDecisionMaker or the inline delegate ShouldDownloadPageContent
        /// </summary>
        public DateTime? DownloadContentStarted { get; set; }

        /// <summary>
        /// A datetime of when the page content download completed, this may be null if downloading the content was disallowed by the CrawlDecisionMaker or the inline delegate ShouldDownloadPageContent
        /// </summary>
        public DateTime? DownloadContentCompleted { get; set; }

        /// <summary>
        /// The page that this pagee was redirected to
        /// </summary>
        public PageToCrawl RedirectedTo { get; set; }

        /// <summary>
        /// Time it took from RequestStarted to RequestCompleted in milliseconds
        /// </summary>
        public double Elapsed
        {
            get
            {
                return (RequestCompleted - RequestStarted).TotalMilliseconds;
            }
        }

        private HtmlDocument InitializeHtmlAgilityPackDocument()
        {
            HtmlDocument hapDoc = new HtmlDocument();
            hapDoc.OptionMaxNestedChildNodes = 5000;//did not make this an externally configurable property since it is really an internal issue to hap
            try
            {
                hapDoc.LoadHtml(Content.Text);
            }
            catch (Exception e)
            {
                throw e;
            }
            return hapDoc;
        }
    }
}
