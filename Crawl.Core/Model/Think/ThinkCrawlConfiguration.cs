using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crawl.Core.Model
{
    public class ThinkCrawlConfiguration
    {
        public ThinkCrawlStartRule StartRule { get; set; }

        public List<ThinkCrawlRule> Rules { get; set; }

        public Tuple<bool, string> Check()
        {
            if (StartRule == null)
            {
                return new Tuple<bool, string>(false, "没有配置开始抓取规则");
            }
            if (string.IsNullOrEmpty(StartRule.Url))
            {
                return new Tuple<bool, string>(false, "必须配置抓取入口地址[StartRule>StartUrl]");
            }
            if (Rules == null || Rules.Count < 1)
            {
                return new Tuple<bool, string>(false, "至少配置一个抓取规则");
            }
            return new Tuple<bool, string>(true, "");
            //if (Start.StoreType == AbotExtStoreType.Db)
            //{
            //    if (Dbs == null || Dbs.Count < 1)
            //    {
            //        error = "至少配置一个数据库对象";
            //        return false;
            //    }
            //    foreach (var db in Dbs)
            //    {
            //        bool flag = db.Check(out error);
            //        if (!flag)
            //        {
            //            return flag;
            //        }
            //    }
            //}
            //return true;
        }

        /// <summary>
        /// 获得抓取开始URI
        /// </summary>
        /// <returns></returns>
        public Uri GetRootUri()
        {
            if (StartRule != null && !string.IsNullOrEmpty(StartRule.Url))
            {
                return new Uri(StartRule.Url);
            }
            return null;
        }

        /// <summary>
        /// 获取抓取规则
        /// </summary>
        /// <param name="ruleName"></param>
        /// <returns></returns>
        public ThinkCrawlRule GetRule(string ruleName)
        {
            if (string.IsNullOrEmpty(ruleName)) return null;

            return Rules.FirstOrDefault(m => m.Name == ruleName);
        }
    }
}
