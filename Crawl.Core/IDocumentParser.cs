using Crawl.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Crawl.Core
{
    public interface IDocumentParser
    {
        IEnumerable<PageToCrawl> GetLinks(CrawledPage crawledPage);

        dynamic GetData(CrawledPage crawledPage);
    }
}
