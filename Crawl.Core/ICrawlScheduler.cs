using System;
using System.Collections.Generic;
using Crawl.Core.Model;

namespace Crawl.Core
{
    public interface ICrawlScheduler : IDisposable
    {
        int Count { get; }

        void Add(PageToCrawl page);

        void Add(IEnumerable<PageToCrawl> pages);

        PageToCrawl GetNext();

        void Clear();

        void AddKnownUri(Uri uri);

        bool IsUriKnown(Uri uri);

        bool AllowUriRecrawling { get; set; }

        /// <summary>
        /// 获得所有待抓取的链接
        /// </summary>
        /// <returns></returns>
        IEnumerable<PageToCrawl> GetAll();
    }
}
