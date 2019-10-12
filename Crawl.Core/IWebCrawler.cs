using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Crawl.Core.Model;

namespace Crawl.Core
{
    public interface IWebCrawler
    {
        /// <summary>
        /// 抓取结束触发异步
        /// </summary>
        event EventHandler<CrawlCompletedArgs> CrawlCompletedAsync;

        /// <summary>
        /// 页面开始爬行触发
        /// </summary>
        event EventHandler<PageCrawlStartingArgs> PageCrawlStartingAsync;

        /// <summary>
        /// 页面爬行结束触发
        /// </summary>
        event EventHandler<PageCrawlCompletedArgs> PageCrawlCompletedAsync;

        /// <summary>
        /// 页面不允许爬行触发
        /// </summary>
        event EventHandler<PageCrawlDisallowedArgs> PageCrawlDisallowedAsync;

        /// <summary>
        /// 页面链接不允许抓取触发
        /// </summary>
        event EventHandler<PageLinksCrawlDisallowedArgs> PageLinksCrawlDisallowedAsync;

        /// <summary>
        /// 抓取
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        CrawlResult Crawl(string url = "");

        /// <summary>
        /// 抓取核心
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="tokenSource"></param>
        /// <returns></returns>
        CrawlResult Crawl(Uri uri, CancellationTokenSource tokenSource);

        /// <summary>
        /// 是否可以抓取
        /// </summary>
        /// <param name="decisionMaker">决策者</param>
        void ShouldCrawlPage(Func<PageToCrawl, CrawlContext, CrawlDecision> decisionMaker);

        /// <summary>
        /// 是否可以下载网页
        /// </summary>
        /// <param name="decisionMaker">决策者</param>
        void ShouldDownloadPageContent(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker);

        /// <summary>
        /// 是否可以抓取链接
        /// </summary>
        /// <param name="decisionMaker">决策者</param>
        void ShouldCrawlPageLinks(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker);

        /// <summary>
        /// 是否可以存取链接
        /// </summary>
        /// <param name="decisionMaker">决策者</param>
        void ShouldScheduleLink(Func<Uri, CrawledPage, CrawlContext, bool> decisionMaker);

        /// <summary>
        /// 是否可以重新抓取链接
        /// </summary>
        /// <param name="decisionMaker"></param>
        void ShouldRecrawlPage(Func<CrawledPage, CrawlContext, CrawlDecision> decisionMaker);
    }
}
