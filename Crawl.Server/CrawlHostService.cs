using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Crawl.Core;
using Crawl.Core.Model;
using Microsoft.Extensions.Options;

namespace Crawl.Server
{
    public class CrawlHostService : IHostedService
    {
        private IWebCrawler _crawler;
        private ILogger<CrawlHostService> _logger;
        private IOptions<ThinkCrawlConfiguration> _configOpt;
        public CrawlHostService(ILogger<CrawlHostService> logger, IWebCrawler webCrawler,  IOptions<ThinkCrawlConfiguration> options)
        {
            _logger = logger;
            _crawler = webCrawler;
            _configOpt = options;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("服务开始");
            _crawler.Crawl();
            return Task.FromResult(0);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("服务结束");
            return Task.FromResult(0);
        }
    }
}
