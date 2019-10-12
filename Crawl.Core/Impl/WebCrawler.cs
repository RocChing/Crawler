using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Crawl.Core.Model;
using Timer = System.Timers.Timer;

using Microsoft.Extensions.Logging;
using System.Dynamic;

namespace Crawl.Core.Impl
{
    public class WebCrawler : IWebCrawler
    {
        protected ILogger<WebCrawler> _logger;
        protected CrawlContext _crawlContext;
        protected Timer _timeoutTimer;
        protected bool _crawlComplete = false;
        protected bool _crawlPause = false;
        protected IThreadManager _threadManager;
        protected ICrawlDecisionMaker _crawlDecisionMaker;
        protected ICrawlScheduler _scheduler;
        protected IPageRequester _pageRequester;
        protected IDocumentParser _hyperLinkParser;
        protected IRateLimiter _rateLimiter;
        protected CrawlConfiguration _crawlConfiguration;
        protected CrawlResult _crawlResult;

        protected Func<PageToCrawl, CrawlContext, CrawlDecision> _shouldCrawlPageDecisionMaker;
        protected Func<CrawledPage, CrawlContext, CrawlDecision> _shouldDownloadPageContentDecisionMaker;
        protected Func<CrawledPage, CrawlContext, CrawlDecision> _shouldCrawlPageLinksDecisionMaker;
        protected Func<CrawledPage, CrawlContext, CrawlDecision> _shouldRecrawlPageDecisionMaker;
        protected Func<Uri, CrawledPage, CrawlContext, bool> _shouldScheduleLinkDecisionMaker;
        protected Func<Uri, Uri, bool> _isInternalDecisionMaker = (uriInQuestion, rootUri) => uriInQuestion.Authority == rootUri.Authority;

        public WebCrawler(ILogger<WebCrawler> logger, CrawlConfiguration crawlConfiguration, IThreadManager threadManager, ICrawlDecisionMaker crawlDecisionMaker, ICrawlScheduler crawlScheduler, IPageRequester pageRequester, IDocumentParser hyperLinkParser, IRateLimiter rateLimiter)
        {
            _logger = logger;
            _crawlConfiguration = crawlConfiguration;
            _crawlContext = new CrawlContext(logger)
            {
                CrawlConfiguration = crawlConfiguration
            };

            _threadManager = threadManager;
            _crawlDecisionMaker = crawlDecisionMaker;
            _scheduler = crawlScheduler;
            _pageRequester = pageRequester;
            _hyperLinkParser = hyperLinkParser;
            _rateLimiter = rateLimiter;

            PageCrawlStartingAsync += WebCrawler_PageCrawlStartingAsync;
        }

        private void WebCrawler_PageCrawlStartingAsync(object sender, PageCrawlStartingArgs e)
        {
            _rateLimiter.WaitToProceed();
        }

        public virtual CrawlResult Crawl(string url = "")
        {
            Uri uri = string.IsNullOrEmpty(url) ? _crawlContext.RootUri : new Uri(url);
            return Crawl(uri, null);
        }

        public virtual CrawlResult Crawl(Uri uri, CancellationTokenSource tokenSource)
        {
            _logger.LogInformation("开始爬行");
            _crawlContext.RootUri = uri ?? throw new ArgumentNullException("uri");

            if (tokenSource != null) _crawlContext.CancellationTokenSource = tokenSource;

            _crawlResult = new CrawlResult()
            {
                Context = _crawlContext
            };

            _crawlContext.CrawlStartDate = DateTime.Now;
            Stopwatch timer = Stopwatch.StartNew();

            try
            {
                PageToCrawl rootPage = new PageToCrawl(uri) { ParentUri = uri, IsInternal = true, IsRoot = true };
                if (ShouldCrawlPage(rootPage))
                    _scheduler.Add(rootPage);
                Crawl();
            }
            catch (Exception e)
            {
                _crawlResult.Error = e;
                _logger.LogError(e, "爬行出错");
            }
            finally
            {
                if (_threadManager != null)
                    _threadManager.Dispose();
            }


            timer.Stop();
            _crawlResult.Elapsed = timer.Elapsed;

            _logger.LogInformation("爬行结束 [{0}]: 共爬行 [{1}] 页面 , 花费 [{2}]", uri.AbsoluteUri, _crawlResult.Context.CrawledCount, _crawlResult.Elapsed);

            FireCrawlCompletedEventAsync(_crawlResult);

            return _crawlResult;
        }

        protected virtual void Crawl()
        {
            while (!_crawlComplete)
            {
                RunPreWorkChecks();

                if (_crawlPause)
                {
                    _logger.LogWarning("爬行线程暂停中...");
                    Thread.Sleep(2500);
                    continue;
                }

                if (_scheduler.Count > 0)
                {
                    _logger.LogInformation($"当前队列有[{_scheduler.Count}]个待爬链接");
                    _threadManager.DoWork(() => ProcessPage(_scheduler.GetNext()));
                }
                else if (!_threadManager.HasRunningThreads())
                {
                    _crawlComplete = true;
                }
                else
                {
                    _logger.LogDebug("Waiting for links to be scheduled...");
                    Thread.Sleep(2500);
                }
            }
        }

        protected virtual void RunPreWorkChecks()
        {
            CheckForCancellationRequest();
            CheckForHardStopRequest();
        }

        protected virtual void CheckForCancellationRequest()
        {
            if (_crawlContext.CancellationTokenSource.IsCancellationRequested)
            {
                string message = string.Format("Crawl cancellation requested for site [{0}]!", _crawlContext.RootUri);
                _logger.LogError(message);
                _crawlResult.Error = new OperationCanceledException(message, _crawlContext.CancellationTokenSource.Token);
                _crawlContext.IsCrawlHardStopRequested = true;
            }
        }

        protected virtual void CheckForHardStopRequest()
        {
            if (_crawlContext.IsCrawlHardStopRequested)
            {
                _scheduler.Clear();
                _threadManager.AbortAll();
                _scheduler.Clear();

                //Set all events to null so no more events are fired
                //PageCrawlStarting = null;
                //PageCrawlCompleted = null;
                //PageCrawlDisallowed = null;
                //PageLinksCrawlDisallowed = null;
                //PageCrawlStartingAsync = null;
                //PageCrawlCompletedAsync = null;
                //PageCrawlDisallowedAsync = null;
                //PageLinksCrawlDisallowedAsync = null;
                ////add by roc
                //CrawlCompleted = null;
                //CrawlCompletedAsync = null;
            }
        }

        protected virtual void ProcessPage(PageToCrawl pageToCrawl)
        {
            if (pageToCrawl == null)
                return;

            try
            {
                ThrowIfCancellationRequested();

                AddPageToContext(pageToCrawl);

                CrawledPage crawledPage = CrawlThePage(pageToCrawl);

                FirePageCrawlCompletedEventAsync(crawledPage);

                bool shouldCrawlPageLinks = ShouldCrawlPageLinks(crawledPage);
                if (shouldCrawlPageLinks)
                {
                    ParsePageDocument(crawledPage);
                    SchedulePageLinks(crawledPage);
                }

                if (crawledPage.IsRetry || ShouldRecrawlPage(crawledPage))
                {
                    crawledPage.IsRetry = true;
                    _scheduler.Add(crawledPage);
                }
            }
            catch (OperationCanceledException oce)
            {
                _logger.LogDebug("Thread cancelled while crawling/processing page [{0}]", pageToCrawl.Uri);
                throw oce;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                _crawlResult.Error = e;
                _crawlContext.IsCrawlHardStopRequested = true;
            }
        }

        protected virtual void ParsePageDocument(CrawledPage crawledPage)
        {
            crawledPage.ParsedLinks = _hyperLinkParser.GetLinks(crawledPage);
            crawledPage.ParsedData = _hyperLinkParser.GetData(crawledPage);
        }

        protected virtual void SchedulePageLinks(CrawledPage crawledPage)
        {
            int linksToCrawl = 0;
            foreach (var page in crawledPage.ParsedLinks)
            {
                // First validate that the link was not already visited or added to the list of pages to visit, so we don't
                // make the same validation and fire the same events twice.
                Uri uri = page.Uri;
                if (!_scheduler.IsUriKnown(uri) && (_shouldScheduleLinkDecisionMaker == null || _shouldScheduleLinkDecisionMaker.Invoke(uri, crawledPage, _crawlContext)))
                {
                    try
                    {
                        if (ShouldSchedulePageLink(page))
                        {
                            _scheduler.Add(page);
                            linksToCrawl++;
                        }

                        if (!ShouldScheduleMorePageLink(linksToCrawl))
                        {
                            _logger.LogInformation("MaxLinksPerPage has been reached. No more links will be scheduled for current page [{0}].", crawledPage.Uri);
                            break;
                        }
                    }
                    catch { }
                }

                // Add this link to the list of known Urls so validations are not duplicated in the future.
                _scheduler.AddKnownUri(uri);
            }
        }

        protected virtual bool ShouldScheduleMorePageLink(int linksAdded)
        {
            return _crawlContext.CrawlConfiguration.MaxLinksPerPage == 0 || _crawlContext.CrawlConfiguration.MaxLinksPerPage > linksAdded;
        }

        protected virtual bool ShouldSchedulePageLink(PageToCrawl page)
        {
            if (page.IsInternal && ShouldCrawlPage(page))
                return true;

            return false;
        }

        protected virtual bool IsInternalUri(Uri uri)
        {
            return _isInternalDecisionMaker(uri, _crawlContext.RootUri);
        }

        protected virtual CrawledPage CrawlThePage(PageToCrawl pageToCrawl)
        {
            FirePageCrawlStartingEventAsync(pageToCrawl);

            pageToCrawl.LastRequest = DateTime.Now;

            CrawledPage crawledPage = _pageRequester.MakeRequest(pageToCrawl.Uri, ShouldDownloadPageContent);

            Map(pageToCrawl, crawledPage);

            _logger.LogInformation("抓取完毕, 状态:[{0}] 地址:[{1}] 花费:[{2}] 重试:[{3}]", crawledPage.StatusCode, crawledPage.Uri.AbsoluteUri, crawledPage.Elapsed, crawledPage.RetryCount);

            return crawledPage;
        }

        protected void Map(PageToCrawl src, CrawledPage dest)
        {
            dest.Uri = src.Uri;
            dest.ParentUri = src.ParentUri;
            dest.IsRetry = src.IsRetry;
            dest.RetryAfter = src.RetryAfter;
            dest.RetryCount = src.RetryCount;
            dest.LastRequest = src.LastRequest;
            dest.IsRoot = src.IsRoot;
            dest.IsInternal = src.IsInternal;
            dest.PageBag = CombinePageBags(src.PageBag, dest.PageBag);
            dest.CrawlDepth = src.CrawlDepth;
        }

        protected virtual dynamic CombinePageBags(dynamic pageToCrawlBag, dynamic crawledPageBag)
        {
            IDictionary<string, object> combinedBag = new ExpandoObject();
            var pageToCrawlBagDict = pageToCrawlBag as IDictionary<string, object>;
            var crawledPageBagDict = crawledPageBag as IDictionary<string, object>;

            foreach (KeyValuePair<string, object> entry in pageToCrawlBagDict) combinedBag[entry.Key] = entry.Value;
            foreach (KeyValuePair<string, object> entry in crawledPageBagDict) combinedBag[entry.Key] = entry.Value;

            return combinedBag;
        }

        protected virtual bool ShouldCrawlPageLinks(CrawledPage crawledPage)
        {
            CrawlDecision shouldCrawlPageLinksDecision = _crawlDecisionMaker.ShouldCrawlPageLinks(crawledPage, _crawlContext);
            if (shouldCrawlPageLinksDecision.Allow)
                shouldCrawlPageLinksDecision = (_shouldCrawlPageLinksDecisionMaker != null) ? _shouldCrawlPageLinksDecisionMaker.Invoke(crawledPage, _crawlContext) : CrawlDecision.AllowCrawl();

            if (!shouldCrawlPageLinksDecision.Allow)
            {
                _logger.LogDebug("Links on page [{0}] not crawled, [{1}]", crawledPage.Uri.AbsoluteUri, shouldCrawlPageLinksDecision.Reason);
                FirePageLinksCrawlDisallowedEventAsync(crawledPage, shouldCrawlPageLinksDecision.Reason);
                //FirePageLinksCrawlDisallowedEvent(crawledPage, shouldCrawlPageLinksDecision.Reason);
            }

            return shouldCrawlPageLinksDecision.Allow;
        }

        protected virtual bool ShouldRecrawlPage(CrawledPage crawledPage)
        {
            //TODO No unit tests cover these lines
            CrawlDecision shouldRecrawlPageDecision = _crawlDecisionMaker.ShouldRecrawlPage(crawledPage, _crawlContext);
            if (shouldRecrawlPageDecision.Allow)
                shouldRecrawlPageDecision = (_shouldRecrawlPageDecisionMaker != null) ? _shouldRecrawlPageDecisionMaker.Invoke(crawledPage, _crawlContext) : CrawlDecision.AllowCrawl();

            //if (!shouldRecrawlPageDecision.Allow)
            //{
            //    //_logger.LogDebug("Page [{0}] not recrawled, [{1}]", crawledPage.Uri.AbsoluteUri, shouldRecrawlPageDecision.Reason);
            //}
            //else
            //{
            // Look for the Retry-After header in the response.
            //crawledPage.RetryAfter = null;
            //if (crawledPage.HttpWebResponse != null &&
            //    crawledPage.HttpWebResponse.Headers != null)
            //{
            //    string value = crawledPage.HttpWebResponse.GetResponseHeader("Retry-After");
            //    if (!String.IsNullOrEmpty(value))
            //    {
            //        // Try to convert to DateTime first, then in double.
            //        DateTime date;
            //        double seconds;
            //        if (crawledPage.LastRequest.HasValue && DateTime.TryParse(value, out date))
            //        {
            //            crawledPage.RetryAfter = (date - crawledPage.LastRequest.Value).TotalSeconds;
            //        }
            //        else if (double.TryParse(value, out seconds))
            //        {
            //            crawledPage.RetryAfter = seconds;
            //        }
            //    }
            //}
            //}
            return shouldRecrawlPageDecision.Allow;
        }

        protected virtual CrawlDecision ShouldDownloadPageContent(CrawledPage crawledPage)
        {
            CrawlDecision decision = _crawlDecisionMaker.ShouldDownloadPageContent(crawledPage, _crawlContext);
            if (decision.Allow)
                decision = (_shouldDownloadPageContentDecisionMaker != null) ? _shouldDownloadPageContentDecisionMaker.Invoke(crawledPage, _crawlContext) : CrawlDecision.AllowCrawl();

            //SignalCrawlStopIfNeeded(decision);
            return decision;
        }

        protected virtual void AddPageToContext(PageToCrawl pageToCrawl)
        {
            if (pageToCrawl.IsRetry)
            {
                pageToCrawl.RetryCount++;
                return;
            }

            int count = _crawlContext.CrawledCount;
            Interlocked.Increment(ref count);
            _crawlContext.CrawledCount = count;
        }

        protected virtual void ThrowIfCancellationRequested()
        {
            if (_crawlContext.CancellationTokenSource != null && _crawlContext.CancellationTokenSource.IsCancellationRequested)
                _crawlContext.CancellationTokenSource.Token.ThrowIfCancellationRequested();
        }

        protected virtual bool ShouldCrawlPage(PageToCrawl pageToCrawl)
        {
            CrawlDecision shouldCrawlPageDecision = _crawlDecisionMaker.ShouldCrawlPage(pageToCrawl, _crawlContext);
            if (!shouldCrawlPageDecision.Allow &&
                shouldCrawlPageDecision.Reason.Contains("MaxPagesToCrawl limit of"))
            {
                _logger.LogInformation("MaxPagesToCrawlLimit has been reached or scheduled. No more pages will be scheduled.");
                return false;
            }

            if (shouldCrawlPageDecision.Allow)
                shouldCrawlPageDecision = (_shouldCrawlPageDecisionMaker != null) ? _shouldCrawlPageDecisionMaker.Invoke(pageToCrawl, _crawlContext) : CrawlDecision.AllowCrawl();

            if (!shouldCrawlPageDecision.Allow)
            {
                _logger.LogDebug("Page [{0}] not crawled, [{1}]", pageToCrawl.Uri.AbsoluteUri, shouldCrawlPageDecision.Reason);
                FirePageCrawlDisallowedEventAsync(pageToCrawl, shouldCrawlPageDecision.Reason);
                //FirePageCrawlDisallowedEvent(pageToCrawl, shouldCrawlPageDecision.Reason);
            }

            return shouldCrawlPageDecision.Allow;
        }

        public void ShouldLoadScheduleLink(Func<CrawlContext, IEnumerable<PageToCrawl>, CrawlDecision> decisionMaker)
        {
            //_shouldLoadScheduleLink = decisionMaker;
        }

        public void ShouldCrawlPage(Func<PageToCrawl, CrawlContext, CrawlDecision> decisionMaker)
        {
            _shouldCrawlPageDecisionMaker = decisionMaker;
        }

        public void ShouldDownloadPageContent(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker)
        {
            _shouldDownloadPageContentDecisionMaker = decisionMaker;
        }

        public void ShouldCrawlPageLinks(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker)
        {
            _shouldCrawlPageLinksDecisionMaker = decisionMaker;
        }

        public void ShouldScheduleLink(Func<Uri, CrawledPage, CrawlContext, bool> decisionMaker)
        {
            _shouldScheduleLinkDecisionMaker = decisionMaker;
        }

        public void ShouldRecrawlPage(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker)
        {
            _shouldRecrawlPageDecisionMaker = decisionMaker;
        }

        public void IsInternalUri(Func<Uri, Uri, bool> decisionMaker)
        {
            _isInternalDecisionMaker = decisionMaker;
        }

        /// <summary>
        /// 抓取结束触发异步
        /// </summary>
        public event EventHandler<CrawlCompletedArgs> CrawlCompletedAsync;

        protected virtual void FireCrawlCompletedEventAsync(CrawlResult crawlResult)
        {
            EventHandler<CrawlCompletedArgs> threadSafeEvent = CrawlCompletedAsync;
            if (threadSafeEvent != null)
            {
                foreach (EventHandler<CrawlCompletedArgs> del in threadSafeEvent.GetInvocationList())
                {
                    del.BeginInvoke(this, new CrawlCompletedArgs(_crawlContext, crawlResult), null, null);
                }
            }
        }

        /// <summary>
        /// 页面开始爬行触发
        /// </summary>
        public event EventHandler<PageCrawlStartingArgs> PageCrawlStartingAsync;

        protected virtual void FirePageCrawlStartingEventAsync(PageToCrawl pageToCrawl)
        {
            PageCrawlStartingAsync?.Invoke(this, new PageCrawlStartingArgs(_crawlContext, pageToCrawl));
            //EventHandler<PageCrawlStartingArgs> threadSafeEvent = PageCrawlStartingAsync;
            //if (threadSafeEvent != null)
            //{
            //    //Fire each subscribers delegate async
            //    foreach (EventHandler<PageCrawlStartingArgs> del in threadSafeEvent.GetInvocationList())
            //    {
            //        del.BeginInvoke(this, new PageCrawlStartingArgs(_crawlContext, pageToCrawl), null, null);
            //    }
            //}
        }

        /// <summary>
        /// 页面爬行结束触发
        /// </summary>
        public event EventHandler<PageCrawlCompletedArgs> PageCrawlCompletedAsync;

        protected virtual void FirePageCrawlCompletedEventAsync(CrawledPage crawledPage)
        {
            EventHandler<PageCrawlCompletedArgs> threadSafeEvent = PageCrawlCompletedAsync;

            if (threadSafeEvent == null)
                return;

            if (_scheduler.Count == 0)
            {
                //Must be fired synchronously to avoid main thread exiting before completion of event handler for first or last page crawled
                try
                {
                    threadSafeEvent(this, new PageCrawlCompletedArgs(_crawlContext, crawledPage));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "An unhandled exception was thrown by a subscriber of the PageCrawlCompleted event for url:" + crawledPage.Uri.AbsoluteUri);
                }
            }
            else
            {
                //Fire each subscribers delegate async
                foreach (EventHandler<PageCrawlCompletedArgs> del in threadSafeEvent.GetInvocationList())
                {
                    del.BeginInvoke(this, new PageCrawlCompletedArgs(_crawlContext, crawledPage), null, null);
                }
            }
        }

        /// <summary>
        /// 页面不允许爬行触发
        /// </summary>
        public event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowedAsync;

        protected virtual void FirePageCrawlDisallowedEventAsync(PageToCrawl pageToCrawl, string reason)
        {
            EventHandler<PageCrawlDisallowedArgs> threadSafeEvent = PageCrawlDisallowedAsync;
            if (threadSafeEvent != null)
            {
                //Fire each subscribers delegate async
                foreach (EventHandler<PageCrawlDisallowedArgs> del in threadSafeEvent.GetInvocationList())
                {
                    del.BeginInvoke(this, new PageCrawlDisallowedArgs(_crawlContext, pageToCrawl, reason), null, null);
                }
            }
        }

        /// <summary>
        /// 页面链接不允许抓取触发
        /// </summary>
        public event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowedAsync;

        protected virtual void FirePageLinksCrawlDisallowedEventAsync(CrawledPage crawledPage, string reason)
        {
            EventHandler<PageLinksCrawlDisallowedArgs> threadSafeEvent = PageLinksCrawlDisallowedAsync;
            if (threadSafeEvent != null)
            {
                //Fire each subscribers delegate async
                foreach (EventHandler<PageLinksCrawlDisallowedArgs> del in threadSafeEvent.GetInvocationList())
                {
                    del.BeginInvoke(this, new PageLinksCrawlDisallowedArgs(_crawlContext, crawledPage, reason), null, null);
                }
            }
        }

        #region private method


        #endregion
    }
}
