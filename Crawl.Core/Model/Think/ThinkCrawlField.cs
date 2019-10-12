using System;
using System.Collections.Generic;
using System.Text;

namespace Crawl.Core.Model
{
    public class ThinkCrawlField
    {
        /// <summary>
        /// 类型 分为 single/group 默认single
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 抓取规则
        /// </summary>
        public string XPath { get; set; }

        /// <summary>
        /// 是否是html
        /// </summary>
        public bool IsHtml { get; set; }

        /// <summary>
        /// 属性名称
        /// </summary>
        public string Attr { get; set; }

        /// <summary>
        /// 是否继承父规则
        /// </summary>
        public bool Inherit { get; set; }

        /// <summary>
        /// 是否存储
        /// </summary>
        public bool IsStore { get; set; }

        /// <summary>
        /// 子字段
        /// </summary>
        public List<ThinkCrawlField> Children { get; set; }

        public ThinkCrawlField()
        {
            Type = ThinkCrawlFieldType.Single;
        }
    }
}
