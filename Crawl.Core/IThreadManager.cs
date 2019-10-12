using System;
using System.Collections.Generic;
using System.Text;

namespace Crawl.Core
{
    public interface IThreadManager : IDisposable
    {
        /// <summary>
        /// 最多可用线程数
        /// </summary>
        int MaxThreads { get; set; }

        /// <summary>
        ///异步操作
        /// </summary>
        /// <param name="action">The action to perform</param>
        void DoWork(Action action);

        /// <summary>
        /// 是否有运行的线程
        /// </summary>
        bool HasRunningThreads();

        /// <summary>
        /// 停止所有线程
        /// </summary>
        void AbortAll();
    }
}
