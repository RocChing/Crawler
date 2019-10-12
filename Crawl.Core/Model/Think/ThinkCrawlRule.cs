using System;
using System.Collections.Generic;
using System.Text;

namespace Crawl.Core.Model
{
    public class ThinkCrawlRule
    {
        /// <summary>
        /// 规则名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 抓取规则
        /// </summary>
        public string XPath { get; set; }

        /// <summary>
        /// 下一个规则名称 多个逗号隔开
        /// </summary>
        public string Next { get; set; }

        /// <summary>
        /// 过滤文本
        /// </summary>
        public string FilterText { get; set; }

        /// <summary>
        /// 是否抓取
        /// </summary>
        public bool NeedCrawl { get; set; }

        /// <summary>
        /// 字段列表
        /// </summary>
        public List<ThinkCrawlField> Fields { get; set; }

        public string SqlName { get; set; }

        public ThinkCrawlRule()
        {
            NeedCrawl = true;
        }

        public List<string> GetNextRules()
        {
            List<string> rules = new List<string>();
            if (!string.IsNullOrEmpty(Next))
                rules.AddRange(Next.Split(','));
            return rules;
        }

        public bool HasFields()
        {
            return Fields != null && Fields.Count > 0;
        }
    }
}
