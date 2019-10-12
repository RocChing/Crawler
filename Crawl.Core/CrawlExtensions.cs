using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Crawl.Core.Impl;
using Crawl.Core.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Crawl.Core
{
    public static class CrawlExtensions
    {
        public static IServiceCollection AddCrawler(this IServiceCollection services, CrawlConfiguration crawlConfiguration = null)
        {
            crawlConfiguration = crawlConfiguration ?? new CrawlConfiguration();
            services.AddHttpClient();

            services.AddSingleton(crawlConfiguration);
            services.AddScoped<IThreadManager, TaskThreadManager>();
            services.AddScoped<ICrawlDecisionMaker, CrawlDecisionMaker>();
            services.AddScoped<ICrawlScheduler, CrawlScheduler>();
            services.AddScoped<IWebContentExtractor, WebContentExtractor>();
            services.AddScoped<IPageRequester, PageRequester>();
            services.AddScoped<IDocumentParser, DefaultDocumentParser>();

            int maxThreads = crawlConfiguration.MaxConcurrentThreads > 0 ? crawlConfiguration.MaxConcurrentThreads : Environment.ProcessorCount;
            maxThreads = 1;
            services.AddScoped<IThreadManager>(p => new TaskThreadManager(p.GetService<ILogger<ThreadManager>>(), maxThreads));
            services.AddScoped<IRateLimiter>(p => new RateLimiter(1, crawlConfiguration.MaxCrawlDelayInMillSeconds));

            services.AddScoped<IWebCrawler, WebCrawler>();
            return services;
        }

        public static IServiceCollection AddThinkCrawler(this IServiceCollection services, ThinkCrawlConfiguration thinkCrawlConfiguration, CrawlConfiguration crawlConfiguration = null)
        {
            services.AddScoped(s => thinkCrawlConfiguration);
            return services;
        }

        public static IServiceCollection AddThinkCrawler(this IServiceCollection services, IConfiguration configuration, CrawlConfiguration crawlConfiguration = null)
        {
            services.Configure<ThinkCrawlConfiguration>(configuration.GetSection("thinkCrawl"));
            AddCrawler(services, crawlConfiguration);
            services.Replace(ServiceDescriptor.Scoped<IDocumentParser, ThinkDocumentParser>());
            services.Replace(ServiceDescriptor.Scoped<IWebCrawler, ThinkWebCrawler>());
            return services;
        }
    }
}
