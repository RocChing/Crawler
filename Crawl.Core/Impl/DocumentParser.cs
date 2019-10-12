using Crawl.Core.Model;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Diagnostics;

namespace Crawl.Core.Impl
{
    public abstract class DocumentParser : IDocumentParser
    {
        protected ILogger _logger;
        private Func<string, string> _cleanURLFunc;
        public DocumentParser(ILogger logger)
        {
            _logger = logger;
        }

        public Func<string, string> CleanURL { get { return _cleanURLFunc; } set { _cleanURLFunc = value; } }

        public IEnumerable<PageToCrawl> GetLinks(CrawledPage crawledPage)
        {
            if (crawledPage == null) throw new ArgumentException("crawledPage");

            Stopwatch timer = Stopwatch.StartNew();

            List<PageToCrawl> pages = GetCrawlPages(crawledPage, GetHrefValues(crawledPage));

            timer.Stop();
            _logger.LogInformation("{0} parsed [{1}] links from [{2}] in [{3}] milliseconds", ParserType, pages.Count, crawledPage.Uri, timer.ElapsedMilliseconds);

            return pages;
        }

        #region Abstract

        protected abstract string ParserType { get; }

        protected abstract IEnumerable<string> GetHrefValues(CrawledPage crawledPage);

        protected abstract string GetBaseHrefValue(CrawledPage crawledPage);

        protected abstract string GetMetaRobotsValue(CrawledPage crawledPage);

        public abstract dynamic GetData(CrawledPage crawledPage);

        #endregion

        protected virtual List<PageToCrawl> GetCrawlPages(CrawledPage crawledPage, IEnumerable<string> hrefValues)
        {
            List<PageToCrawl> pages = new List<PageToCrawl>();
            if (hrefValues == null || hrefValues.Count() < 1)
                return pages;

            //Use the uri of the page that actually responded to the request instead of crawledPage.Uri (Issue 82).
            //Using HttpWebRequest.Address instead of HttpWebResonse.ResponseUri since this is the best practice and mentioned on http://msdn.microsoft.com/en-us/library/system.net.httpwebresponse.responseuri.aspx
            Uri uriToUse = crawledPage.Uri;

            //If html base tag exists use it instead of page uri for relative links
            string baseHref = GetBaseHrefValue(crawledPage);
            if (!string.IsNullOrEmpty(baseHref))
            {
                if (baseHref.StartsWith("//"))
                    baseHref = crawledPage.Uri.Scheme + ":" + baseHref;

                try
                {
                    uriToUse = new Uri(baseHref);
                }
                catch { }
            }

            string href = "";
            foreach (string hrefValue in hrefValues)
            {
                try
                {
                    // Remove the url fragment part of the url if needed.
                    // This is the part after the # and is often not useful.
                    href = hrefValue.Split('#')[0];
                    Uri newUri = new Uri(uriToUse, href);

                    if (_cleanURLFunc != null)
                        newUri = new Uri(_cleanURLFunc(newUri.AbsoluteUri));

                    if (!pages.Exists(u => u.Uri.AbsoluteUri == newUri.AbsoluteUri))
                    {
                        PageToCrawl page = new PageToCrawl(newUri)
                        {
                            ParentUri = crawledPage.Uri,
                            CrawlDepth = crawledPage.CrawlDepth + 1,
                            IsInternal = newUri.Authority == crawledPage.Uri.Authority,
                            IsRoot = false
                        };
                        pages.Add(page);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogDebug("Could not parse link [{0}] on page [{1}]", hrefValue, crawledPage.Uri);
                    _logger.LogError(e, e.Message);
                }
            }

            return pages;
        }

        protected virtual bool HasRobotsNoFollow(CrawledPage crawledPage)
        {
            return false;
        }
    }
}
