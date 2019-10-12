using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Crawl.Core.Util
{
    public static class StringUtil
    {
        public static byte[] ToMd5Bytes(string p_String)
        {
            using (MD5 md5 = MD5.Create())
            {
                return md5.ComputeHash(Encoding.Default.GetBytes(p_String));
            }
        }

        public static long ComputeNumericId(string p_Uri)
        {
            byte[] md5 = ToMd5Bytes(p_Uri);

            long numericId = 0;
            for (int i = 0; i < 8; i++)
            {
                numericId += (long)md5[i] << (i * 8);
            }

            return numericId;
        }

        public static string RemoveHTML(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";
            //删除脚本   
            html = Regex.Replace(html, @"<script[^>]*?>.*?</script>", "", RegexOptions.IgnoreCase);
            //删除HTML   
            html = Regex.Replace(html, @"<(.[^>]*)>", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"([\r\n])[\s]+", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"-->", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<!--.*", "", RegexOptions.IgnoreCase);

            html = Regex.Replace(html, @"&(quot|#34);", "\"", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&(amp|#38);", "&", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&(lt|#60);", "<", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&(gt|#62);", ">", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&(nbsp|#160);", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&(iexcl|#161);", "\xa1", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&(cent|#162);", "\xa2", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&(pound|#163);", "\xa3", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&(copy|#169);", "\xa9", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"&#(\d+);", "", RegexOptions.IgnoreCase);

            html.Replace("<", "");
            html.Replace(">", "");
            html.Replace("\r\n", "");

            return html;
        }

        public static bool IsUrl(string url)
        {
            return Regex.IsMatch(url, @"^((ht|f)tps?):\/\/([\w\-]+(\.[\w\-]+)*\/)*[\w\-]+(\.[\w\-]+)*\/?(\?([\w\-\.,@?^=%&:\/~\+#]*)+)?");
        }
    }
}
