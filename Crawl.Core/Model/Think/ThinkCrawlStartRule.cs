using System;
using System.Collections.Generic;
using System.Text;

namespace Crawl.Core.Model
{
    public class ThinkCrawlStartRule
    {
        /// <summary>
        /// 开始URL
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 开始抓取规则名称 多个名称 逗号隔开
        /// </summary>
        public string Rule { get; set; }

        public List<string> GetRules()
        {
            List<string> rules = new List<string>();
            if (!string.IsNullOrEmpty(Rule))
                rules.AddRange(Rule.Split(','));
            return rules;
        }
    }
}
