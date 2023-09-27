using CsharpHttpHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BaiduBot.util
{
    public static class ReqHelper
    {
        public static HttpResult GetHtml(string url, params KeyValuePair<string, string>[] pairs)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            HttpHelper http = new HttpHelper();
            HttpItem item = new HttpItem()
            {
                URL = url,
                Encoding = Encoding.GetEncoding("UTF-8"),
                Method = "GET",
                Timeout = 10000,
                ReadWriteTimeout = 10000,
            };
            foreach (var pair in pairs)
            {
                item.Header.Add(pair.Key, pair.Value);
            }
            var res = http.GetHtml(item);
            return res;
        }

        public static HttpResult Post(string url, string type, string data, string referer, WebProxy webProxy, params KeyValuePair<string, string>[] pairs)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            HttpHelper http = new HttpHelper();
            HttpItem item = new HttpItem()
            {
                URL = url,
                Encoding = Encoding.GetEncoding("UTF-8"),
                Method = "POST",
                ContentType = type,
                Postdata = data,
                Referer = referer,
                WebProxy = webProxy,
                Timeout = 10000,
                ReadWriteTimeout = 10000,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.87 Safari/537.36"
            };
            foreach (var pair in pairs)
            {
                item.Header.Add(pair.Key, pair.Value);
            }
            var res = http.GetHtml(item);
            return res;
        }
    }
}
