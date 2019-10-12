using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Crawl.Core.Model;
using Crawl.Core.Util;

namespace Crawl.Core.Impl
{
    public class CrawlScheduler : ICrawlScheduler
    {
        private ConcurrentDictionary<long, byte> _crawledUrlDic;
        private ConcurrentQueue<PageToCrawl> _pageToCrawlQueue;
        public CrawlScheduler()
        {
            _crawledUrlDic = new ConcurrentDictionary<long, byte>();
            _pageToCrawlQueue = new ConcurrentQueue<PageToCrawl>();
            AllowUriRecrawling = false;
        }

        public virtual int Count { get { return _pageToCrawlQueue.Count; } }

        public bool AllowUriRecrawling { get; set; }

        public void Add(PageToCrawl page)
        {
            if (page == null) throw new ArgumentNullException("page");

            if (AllowUriRecrawling || page.IsRetry)
            {
                _pageToCrawlQueue.Enqueue(page);
            }
            else
            {
                if (AddIfNew(page.Uri)) _pageToCrawlQueue.Enqueue(page);
            }
        }

        public void Add(IEnumerable<PageToCrawl> pages)
        {
            if (pages == null) throw new ArgumentNullException("pages");

            foreach (PageToCrawl page in pages)
                Add(page);
        }

        public PageToCrawl GetNext()
        {
            _pageToCrawlQueue.TryDequeue(out var crawl);
            return crawl;
        }

        public void Clear()
        {
            _crawledUrlDic.Clear();
            _pageToCrawlQueue = new ConcurrentQueue<PageToCrawl>();
        }

        public void AddKnownUri(Uri uri)
        {
            AddIfNew(uri);
        }

        public bool IsUriKnown(Uri uri)
        {
            return _crawledUrlDic.ContainsKey(StringUtil.ComputeNumericId(uri.AbsoluteUri));
        }

        public IEnumerable<PageToCrawl> GetAll()
        {
            return _pageToCrawlQueue.ToArray();
        }

        public void Dispose()
        {
            _crawledUrlDic = null;
            _pageToCrawlQueue = null;
        }

        private bool AddIfNew(Uri uri)
        {
            return _crawledUrlDic.TryAdd(StringUtil.ComputeNumericId(uri.AbsoluteUri), 0);
        }
    }
}
