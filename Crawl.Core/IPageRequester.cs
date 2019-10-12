using Crawl.Core.Model;
using System;
using System.Threading.Tasks;

namespace Crawl.Core
{
    public interface IPageRequester : IDisposable
    {
        CrawledPage MakeRequest(Uri uri);

        CrawledPage MakeRequest(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent);
    }
}
