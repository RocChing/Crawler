using System;
using System.Collections.Generic;
using System.Text;

namespace Crawl.Core.Model
{
    public class CrawlConfiguration
    {
        public string UserAgentString { get; set; }

        /// <summary>
        /// 启用最大线程数
        /// </summary>
        public int MaxConcurrentThreads { get; set; }

        /// <summary>
        /// 最大爬行页面数
        /// </summary>
        public int MaxPagesToCrawl { get; set; }

        public string DownloadableContentTypes { get; set; }

        public string RobotsDotTextUserAgentString { get; set; }

        /// <summary>
        /// 每次爬行最大延迟毫秒数
        /// </summary>
        public int MaxCrawlDelayInMillSeconds { get; set; }

        /// <summary>
        /// 最大爬行深度 默认100
        /// </summary>
        public int MaxCrawlDepth { get; set; }

        /// <summary>
        /// 最大重试次数 0=无限重试
        /// </summary>
        public int MaxRetryCount { get; set; }

        /// <summary>
        /// 每页最大爬行链接数量
        /// </summary>
        public int MaxLinksPerPage { get; set; }

        public CrawlConfiguration()
        {
            MaxConcurrentThreads = 10;
            UserAgentString = "Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";
            RobotsDotTextUserAgentString = "crawler";
            MaxPagesToCrawl = 1000;
            DownloadableContentTypes = "text/html";
            MaxCrawlDelayInMillSeconds = 1000;
            MaxCrawlDepth = 100;
            MaxRetryCount = 5;
        }
    }
}
