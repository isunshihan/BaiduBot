using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using BaiduBot.util;
using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using System.Net;
using System.Text;

namespace BaiduBot
{
    public class Cleaner
    {
        Queue<string> q2 = new Queue<string>();
        WebClient client = new WebClient();
        string api;
        IConfigurationRoot configuration;

        public Cleaner()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(AppContext.BaseDirectory))
            .AddJsonFile("config/config.json", optional: true, reloadOnChange: false);
            configuration = builder.Build();
            api = configuration.GetSection("api").Value;
        }

        public async Task Go()
        {
            Queue<string> q = new Queue<string>();
            Queue<string> q3;
            HtmlParser parser = new HtmlParser();
            IHtmlDocument doc;
            string dir = "关键词采集结果";
            var indexs = int.Parse(configuration.GetSection("indexs").Value);
            var domains = int.Parse(configuration.GetSection("domains").Value);


            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var path = configuration.GetSection("chromePath").Value;

            var files = Directory.GetFiles("data").ToList();
            List<string> lines;
            List<IElement> urls;
            IResponse res;
            string html;

            foreach (var file in files)
            {
                lines = File.ReadAllLines(file).ToList();
                lines.ForEach(d => q.Enqueue(d));
            }

            var sites = File.ReadAllLines("Config/sites.txt").ToList();
            using (var playwright = await Playwright.CreateAsync())
            {
                Encoding utf8WithoutBom = new UTF8Encoding(false); //控制写入文件的编码

                while (q.Count > 0)
                {
                    try
                    {
                        if (q2.Count > 0)
                        {
                            var ip = q2.Dequeue();
                            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions()
                            {
                                Headless = false,
                                ExecutablePath = path,
                                Args = new[] {
                            "--proxy-server=" + ip,
                            //"--no-sandbox",
                            //"--disable-infobars",
                            //"--disable-setuid-sandbox",
                            "--ignore-certificate-errors",
                            },
                            });

                            Console.WriteLine($"还剩下{q.Count}个关键词等处理");
                            var keyword = q.Dequeue();
                            Console.WriteLine($"开始采集{keyword}的相关词");
                            var filename = DateTime.Now.ToString("yyyy-MM-dd-HH");
                            try
                            {
                                await using var context = await browser.NewContextAsync();
                                var page = await context.NewPageAsync();
                                res = await page.GotoAsync(
                                        $"https://www.baidu.com/baidu?wd={WebUtility.UrlEncode(keyword)}&tn=monline_4_dg&ie=utf-8");
                                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                                html = await page.ContentAsync();
                                doc = parser.ParseDocument(html);
                                var num = int.Parse(doc.QuerySelector("span.hint_PIwZX").TextContent
                                .Replace("百度为您找到相关结果", string.Empty)
                                .Replace("约", string.Empty)
                                .Replace("个", string.Empty)
                                .Replace(",", string.Empty));

                                if (num <= indexs) //小于指定索引数，则是黑词
                                {
                                    Console.WriteLine("该词是黑词");
                                    using (FileStream logFile = new FileStream(Path.Combine(dir, "黑词.txt"), FileMode.Append, FileAccess.Write, FileShare.Write))
                                    {
                                        using (StreamWriter sw = new StreamWriter(logFile, utf8WithoutBom))
                                        {
                                            sw.Write(keyword + Environment.NewLine);
                                            sw.Flush();
                                        }
                                    }
                                }
                                else
                                {
                                    q3 = new Queue<string>();
                                    urls = doc.QuerySelectorAll("h3>a").ToList();
                                    int monopolizeCount = 0; //垄断词计数
                                                             //int weiCount = 0; //违禁词计数
                                    Uri uri;
                                    string url;
                                    string host;
                                    foreach (var a in urls)
                                    {
                                        try
                                        {
                                            q3.Enqueue(a.GetAttribute("href"));
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.Error.WriteLine(ex);
                                        }
                                    }

                                    while (q3.Count > 0)
                                    {
                                        Console.WriteLine($"当前队列数{q3.Count}");
                                        Console.WriteLine($"当前违规数{monopolizeCount}");
                                        if (monopolizeCount < domains) //如果到达规定域名数，就不需要往下查了
                                        {
                                            url = q3.Dequeue();
                                            if (!string.IsNullOrEmpty(url))
                                            {
                                                try
                                                {
                                                    url = ReqHelper.GetHtml(url).RedirectUrl;
                                                    uri = new Uri(url);
                                                    host = uri.Host;
                                                    Console.WriteLine("域名是" + host + "，开始查询");
                                                    if (
                                                        sites.Where(s => host.Contains(s)).Count() > 0
                                                        )
                                                    {
                                                        Console.WriteLine("违规加一");
                                                        monopolizeCount++;
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Console.WriteLine(ex.Message + "，接口查询出错，扔回队列");
                                                    q3.Enqueue(url);
                                                }
                                            }
                                            else
                                            {
                                                Console.WriteLine("值为空，移出队列");
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine("违规数已满足要求，清空队列");
                                            q3.Clear();
                                        }
                                    }
                                    Console.WriteLine("总计违规数是" + monopolizeCount.ToString());
                                    if (monopolizeCount >= domains)
                                    {
                                        Console.WriteLine("该词是违禁词");
                                        File.AppendAllText(Path.Combine(dir, "违禁词.txt"), keyword + Environment.NewLine);
                                    }
                                    else
                                    {
                                        Console.WriteLine("该词是正常词");
                                        File.AppendAllText(Path.Combine(dir, "正常词.txt"), keyword + Environment.NewLine);
                                    }
                                }
                                Console.WriteLine("暂停1秒");
                                Thread.Sleep(1000);
                            }
                            catch (Exception ex)
                            {
                                q.Enqueue(keyword);
                                Console.Error.WriteLine(ex);
                                File.AppendAllText("config/err.txt", ex.Message + "|" + ex.StackTrace);
                            }
                            await browser.DisposeAsync();
                        }
                        else
                        {
                            GetProxy();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex);
                        File.AppendAllText("config/error.txt", ex.Message + "|" + ex.StackTrace);
                    }
                }
            }
               
        }

        private void GetProxy()
        {
            var str = client.DownloadString(api);
            var ips = str.Split(Environment.NewLine).ToList();
            foreach (var ip in ips)
            {
                if (!string.IsNullOrEmpty(ip))
                {
                    q2.Enqueue(ip);
                    Console.WriteLine(ip);
                }
            }
        }
    }
}
