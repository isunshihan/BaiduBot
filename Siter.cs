using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BaiduBot
{
    public class Siter
    {
        Queue<string> q = new Queue<string>();
        Queue<string> q2 = new Queue<string>();
        WebClient client = new WebClient();
        string api;
        IConfigurationRoot configuration;
        //List<string> devices;
        string deviceDescriptor;

        public Siter()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(AppContext.BaseDirectory))
            .AddJsonFile("config/config.json", optional: true, reloadOnChange: false);
            configuration = builder.Build();
            api = configuration.GetSection("api").Value;
            //devices = File.ReadAllLines("config/devices.txt").ToList();
            deviceDescriptor = configuration.GetSection("device").Value;
        }
        public async Task Go()
        {
            var path = configuration.GetSection("chromePath").Value;
            string dir = "网址采集结果";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var files = Directory.GetFiles("data").ToList();
            List<string> lines;
            foreach (var file in files)
            {
                lines = File.ReadAllLines(file).ToList();
                lines.ForEach(d => q.Enqueue(d));
            }
            using var playwright = await Playwright.CreateAsync();
            while (q.Count > 0)
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
                    Console.WriteLine($"开始采集{keyword}的网址");
                    var filename = DateTime.Now.ToString("yyyy-MM-dd");
                    try
                    {
                        //devices.Shuffle();
                        var device = playwright.Devices[deviceDescriptor];
                        var contextOptions = new BrowserNewContextOptions
                        {
                            Permissions = new[] { "geolocation" },
                            Geolocation = new Geolocation
                            {
                                Latitude = 40.7128F,
                                Longitude = -74.0060F,
                                Accuracy = 100
                            },
                            ViewportSize = device.ViewportSize,
                            DeviceScaleFactor = device.DeviceScaleFactor,
                            IsMobile = device.IsMobile,
                            HasTouch = device.HasTouch,
                            UserAgent = device.UserAgent
                        };
                        await using var context = await browser.NewContextAsync(contextOptions);
                        var page = await context.NewPageAsync();
                        await page.GotoAsync("https://www.baidu.com");
                        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                        await page.FillAsync("//*[@id=\"index-kw\"]", keyword);
                        await page.ClickAsync("//*[@id=\"index-bn\"]");
                        await page.WaitForTimeoutAsync(5000);

                        var list = await page.QuerySelectorAllAsync(
                            "span.c-showurl");
                        Console.WriteLine($"一共采集到{list.Count}个网址");
                        foreach (var item in list)
                        {
                            var ci = (await item.TextContentAsync()).Trim();
                            Console.WriteLine("采集到网址：" + ci);
                            File.AppendAllText(dir + "/" + filename + ".txt", ci + Environment.NewLine);
                        }
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText("config/err.txt", ex + Environment.NewLine);
                        Console.Error.WriteLine(ex);
                    }
                }
                else
                {
                    GetProxy();
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
