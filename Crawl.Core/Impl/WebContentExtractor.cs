using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Crawl.Core.Model;

namespace Crawl.Core.Impl
{
    public class WebContentExtractor : IWebContentExtractor
    {
        private ILogger<WebContentExtractor> _logger;
        public WebContentExtractor(ILogger<WebContentExtractor> logger)
        {
            _logger = logger;
        }

        public virtual async Task<PageContent> GetContentAsync(HttpResponseMessage response)
        {
            using (Stream memoryStream = await response.Content.ReadAsStreamAsync())
            {
                memoryStream.Seek(0, SeekOrigin.Begin);

                // Do not wrap in closing statement to prevent closing of this stream.
                StreamReader srr = new StreamReader(memoryStream, Encoding.ASCII);
                string body = srr.ReadToEnd();
                string charset = GetCharsetFromBody(body);

                memoryStream.Seek(0, SeekOrigin.Begin);

                charset = CleanCharset(charset);
                Encoding e = GetEncoding(charset);
                string content = "";
                using (StreamReader sr = new StreamReader(memoryStream, e))
                {
                    content = sr.ReadToEnd();
                }

                PageContent pageContent = new PageContent
                {
                    Bytes = null,
                    Charset = charset,
                    Encoding = e,
                    Text = content
                };
                return pageContent;
            }
        }

        protected virtual string GetCharsetFromHeaders(HttpResponseMessage webResponse)
        {
            return null;
            //string charset = null;
            //string ctype = webResponse.Headers;
            //if (ctype != null)
            //{
            //    int ind = ctype.IndexOf("charset=");
            //    if (ind != -1)
            //        charset = ctype.Substring(ind + 8);
            //}
            //return charset;
        }

        protected virtual string GetCharsetFromBody(string body)
        {
            string charset = null;

            if (body != null)
            {
                //find expression from : http://stackoverflow.com/questions/3458217/how-to-use-regular-expression-to-match-the-charset-string-in-html
                Match match = Regex.Match(body, @"<meta(?!\s*(?:name|value)\s*=)(?:[^>]*?content\s*=[\s""']*)?([^>]*?)[\s""';]*charset\s*=[\s""']*([^\s""'/>]*)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    charset = string.IsNullOrWhiteSpace(match.Groups[2].Value) ? null : match.Groups[2].Value;
                }
            }

            return charset;
        }

        protected virtual Encoding GetEncoding(string charset)
        {
            Encoding e = Encoding.UTF8;
            if (charset != null)
            {
                try
                {
                    e = Encoding.GetEncoding(charset);
                }
                catch
                {
                    e = Encoding.UTF8;
                }
            }
            return e;
        }

        protected virtual string CleanCharset(string charset)
        {
            //TODO temporary hack, this needs to be a configurable value
            if (charset == "cp1251") //Russian, Bulgarian, Serbian cyrillic
                charset = "windows-1251";

            return charset;
        }

        private async Task<MemoryStream> GetRawData(HttpResponseMessage webResponse)
        {
            MemoryStream rawData = new MemoryStream();

            try
            {
                using (Stream rs = await webResponse.Content.ReadAsStreamAsync())
                {
                    byte[] buffer = new byte[1024];
                    int read = rs.Read(buffer, 0, buffer.Length);
                    while (read > 0)
                    {
                        rawData.Write(buffer, 0, read);
                        read = rs.Read(buffer, 0, buffer.Length);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }

            return rawData;
        }

        public virtual void Dispose()
        {
            // Nothing to do
        }
    }
}
