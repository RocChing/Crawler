using Crawl.Core.Model;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Crawl.Core
{
    public interface IWebContentExtractor : IDisposable
    {
        Task<PageContent> GetContentAsync(HttpResponseMessage response);
    }
}
