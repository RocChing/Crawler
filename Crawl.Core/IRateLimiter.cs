using System;
using System.Collections.Generic;
using System.Text;

namespace Crawl.Core
{
    public interface IRateLimiter : IDisposable
    {
        void WaitToProceed();
    }
}
