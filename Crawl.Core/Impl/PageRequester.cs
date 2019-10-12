using Crawl.Core.Model;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Crawl.Core.Impl
{
    public class PageRequester : IPageRequester
    {
        private IHttpClientFactory _httpFactory;
        private HttpClient _client;
        private CrawlConfiguration _config;
        private ILogger<PageRequester> _logger;
        private IWebContentExtractor _webContentExtractor;
        public PageRequester(ILogger<PageRequester> logger, IHttpClientFactory httpClientFactory, CrawlConfiguration crawlConfiguration, IWebContentExtractor webContentExtractor)
        {
            _logger = logger;
            _httpFactory = httpClientFactory;
            _client = _httpFactory.CreateClient();
            _client.Timeout = TimeSpan.FromMinutes(10);
            _config = crawlConfiguration;
            _webContentExtractor = webContentExtractor;
        }

        public CrawledPage MakeRequest(Uri uri)
        {
            return MakeRequest(uri, m => CrawlDecision.AllowCrawl());
        }

        public virtual CrawledPage MakeRequest(Uri uri, Func<CrawledPage, CrawlDecision> shouldDownloadContent)
        {
            if (uri == null) throw new ArgumentNullException("uri");

            CrawledPage crawledPage = new CrawledPage(uri);
            HttpResponseMessage response = null;
            try
            {
                crawledPage.RequestStarted = DateTime.Now;
                HttpRequestMessage request = BuildRequestObject(uri);
                response = _client.SendAsync(request).Result;
            }
            catch (TaskCanceledException e)
            {
                Error(crawledPage, e);
            }
            catch (Exception e)
            {
                Error(crawledPage, e);
            }

            try
            {
                crawledPage.RequestCompleted = DateTime.Now;
                crawledPage.StatusCode = response.StatusCode;
                if (response != null)
                {
                    CrawlDecision shouldDownloadContentDecision = shouldDownloadContent(crawledPage);
                    if (shouldDownloadContentDecision.Allow)
                    {
                        crawledPage.DownloadContentStarted = DateTime.Now;
                        crawledPage.Content =  _webContentExtractor.GetContentAsync(response).Result;
                        crawledPage.DownloadContentCompleted = DateTime.Now;
                    }
                    else
                    {
                        _logger.LogWarning("Links on page [{0}] not crawled, [{1}]", crawledPage.Uri.AbsoluteUri, shouldDownloadContentDecision.Reason);
                    }

                    response.Dispose();
                }
            }
            catch (Exception e)
            {
                Error(crawledPage, e);
            }

            return crawledPage;
        }

        protected virtual HttpRequestMessage BuildRequestObject(Uri uri)
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri.AbsoluteUri);
            httpRequestMessage.Headers.Add("accept", "*/*");
            //httpRequestMessage.Headers.Add("accept-encoding", "gzip, deflate");
            //httpRequestMessage.Headers.Add("cache-control", "max-age=0");
            httpRequestMessage.Headers.Add("user-agent", _config.UserAgentString);

            return httpRequestMessage;
            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            //request.AllowAutoRedirect = true;
            //request.UserAgent = _config.UserAgentString;
            //request.Accept = "*/*";
            //request.ProtocolVersion = HttpVersion.Version11; 

            //if (_config.IsHttpRequestAutomaticDecompressionEnabled)
            //request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            //if (_config.HttpRequestTimeoutInSeconds > 0)
            //    request.Timeout = _config.HttpRequestTimeoutInSeconds * 1000;

            //if (_config.IsSendingCookiesEnabled)
            //    request.CookieContainer = _cookieContainer;

            //Supposedly this does not work... https://github.com/sjdirect/abot/issues/122
            //if (_config.IsAlwaysLogin)
            //{
            //    request.Credentials = new NetworkCredential(_config.LoginUser, _config.LoginPassword);
            //    request.UseDefaultCredentials = false;
            //}

            //if (_config.IsAlwaysLogin)
            //{
            //    string credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(_config.LoginUser + ":" + _config.LoginPassword));
            //    request.Headers[HttpRequestHeader.Authorization] = "Basic " + credentials;
            //}

            //return request;
        }

        public void Dispose()
        {
            _webContentExtractor.Dispose();
            _client.Dispose();
            _httpFactory = null;
        }

        private void Error(CrawledPage crawledPage, Exception e)
        {
            crawledPage.IsRetry = true;
            crawledPage.Exception = e;
            crawledPage.StatusCode = HttpStatusCode.BadRequest;

            _logger.LogError(e, crawledPage.Uri.AbsoluteUri);
        }
    }
}
