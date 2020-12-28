using System;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

namespace GallJungSan
{
    static class Program
    {
        static string GallCode = "";
        static bool IsMgall = false;
        static uint PageHint = 1;
        static DateTime StartDate = DateTime.Now;
        static DateTime EndDate = DateTime.MinValue;
        static bool OpenChromeWindow = true;
        static bool ViewChromeLog = false;

        static void ListToString(List<GallPost> postList, StringBuilder builder, int count)
        {
            int index = 0;
            foreach (var post in postList)
            {
                index++;
                if (index > count)
                    break;

                builder.AppendLine($"{index}\t : {post}\n");
            }
        }

        class GallPost
        {
            public uint         gall_num;       // 번호
            public string       gall_subject;   // 말머리
            public string       gall_tit;       // 제목
            public string       gall_link;      // 글 링크
            public string       gall_writer;    // 글쓴이
            public DateTime     gall_date;      // 작성일
            public uint         gall_count;     // 조회
            public uint         gall_recommend; // 추천
            public uint         gall_reply;     // 댓글
            public bool         is_common_post; // 일반글 인지

            public GallPost(IWebElement element)
            {
                is_common_post = true;

                try
                {
                    var gall_num_raw = element?.FindElement(By.ClassName("gall_num"));
                    if (gall_num_raw == null) gall_num = 0; else { if (!uint.TryParse(gall_num_raw.Text, out gall_num)) { gall_num = 0; is_common_post = false; } }
                }
                catch { gall_num = 0; }

                try
                {
                    var gall_subject_raw = element?.FindElement(By.ClassName("gall_subject"));
                    if (gall_subject_raw == null) gall_subject = ""; else gall_subject = gall_subject_raw.Text;
                    if (is_common_post) is_common_post = SubjectCheck(gall_subject);
                }
                catch { gall_subject = ""; }

                try
                {
                    var gall_tit_raw = element?.FindElement(By.TagName("a"));
                    if (gall_tit_raw == null) gall_tit = ""; else gall_tit = gall_tit_raw.Text;
                }
                catch { gall_tit = ""; }

                try
                {
                    var gall_reply_raw = element?.FindElement(By.ClassName("reply_num"));
                    if (gall_reply_raw == null) gall_reply = 0;
                    else gall_reply = uint.Parse(Regex.Replace(gall_reply_raw.Text.Split('/').FirstOrDefault(), @"\D", ""));
                }
                catch { gall_reply = 0; }

                try
                {
                    var gall_link_raw = element?.FindElement(By.ClassName("reply_numbox"));
                    if (gall_link_raw == null) gall_link = ""; else gall_link = gall_link_raw.GetAttribute("href");
                }
                catch { gall_link = ""; }

                try
                {
                    var gall_writer_raw = element?.FindElement(By.CssSelector("td[class='gall_writer ub-writer']"));
                    if (gall_writer_raw == null) gall_writer = ""; else gall_writer = $"{gall_writer_raw.GetAttribute("data-nick")}({gall_writer_raw.GetAttribute("data-uid")}{gall_writer_raw.GetAttribute("data-ip")})";
                }
                catch { gall_writer = ""; }

                try
                {
                    var gall_date_raw = element?.FindElement(By.ClassName("gall_date"));
                    if (gall_date_raw == null) gall_date = DateTime.MinValue; else gall_date = DateTime.Parse(gall_date_raw.GetAttribute("title"));
                }
                catch { gall_date = DateTime.MinValue; }

                try
                {
                    var gall_count_raw = element?.FindElement(By.ClassName("gall_count"));
                    if (gall_count_raw == null) gall_count = 0; else gall_count = uint.Parse(gall_count_raw.Text);
                }
                catch { gall_count = 0; }

                try
                {
                    var gall_recommend_raw = element?.FindElement(By.ClassName("gall_recommend"));
                    if (gall_recommend_raw == null) gall_recommend = 0; else gall_recommend = uint.Parse(gall_recommend_raw.Text);
                }
                catch { gall_recommend = 0; }
            }

            static bool SubjectCheck(string subject)
            {
                if (subject == "공지" || subject == "뉴스" || subject == "이슈")
                    return false;
                return true;
            }

            public override string ToString()
            {
                string title = $"{gall_tit}[{gall_reply}]-{gall_writer}";
                return string.Format("{0,-10}\t {1,-5}\t {2,-40}\n\t {3,-10}\t {4,-8}\t {5,-5}\t {6}", gall_num, gall_subject, title, gall_date.ToString("yyyy/MM/dd hh:mm:ss"), gall_count, gall_recommend, gall_link);
            }
        }

        static void Main(string[] args)
        {
            IniFile ini = new IniFile();
            ini.Load($@"{System.IO.Directory.GetCurrentDirectory()}\GallJungSanConfig.ini");

            bool pass = true;

            try { GallCode = ini["Setting"]["GallCode"].ToString(); } catch { Console.WriteLine("GallCode 값이 존재하지 않습니다"); pass = false; }
            try { IsMgall = ini["Setting"]["IsMgall"].ToBool(); } catch { Console.WriteLine("IsMgall 값이 존재하지 않습니다"); pass = false; }
            try { StartDate = DateTime.Parse(ini["Setting"]["StartDate"].ToString()); } catch { StartDate = DateTime.Now; }
            try { EndDate = DateTime.Parse(ini["Setting"]["EndDate"].ToString()); } catch { Console.WriteLine("EndDate 값이 존재하지 않습니다"); pass = false; }
            try { PageHint = uint.Parse(ini["Setting"]["PageHint"].ToString()); } catch { PageHint = 1; }
            try { OpenChromeWindow = ini["Setting"]["OpenChromeWindow"].ToBool(); } catch { OpenChromeWindow = true; }
            try { ViewChromeLog = ini["Setting"]["ViewChromeLog"].ToBool(); } catch { ViewChromeLog = false; }

            if (pass)
            {
                List<GallPost> postList = new List<GallPost>();
                SortedSet<uint> postNumSet = new SortedSet<uint>();// 중복 정산 방지

                ChromeOptions options = new ChromeOptions();
                ChromeDriverService driverService = ChromeDriverService.CreateDefaultService();
                if (!OpenChromeWindow)
                    options.AddArgument("--headless");
                if (!ViewChromeLog)
                    driverService.HideCommandPromptWindow = true;

                Console.WriteLine($"{nameof(GallCode)}         : {GallCode}");
                Console.WriteLine($"{nameof(IsMgall)}          : {IsMgall}");
                Console.WriteLine($"{nameof(StartDate)}        : {StartDate}");
                Console.WriteLine($"{nameof(EndDate)}          : {EndDate}");
                Console.WriteLine($"{nameof(PageHint)}         : {PageHint}");
                Console.WriteLine($"{nameof(OpenChromeWindow)} : {OpenChromeWindow}");
                Console.WriteLine($"{nameof(ViewChromeLog)}    : {ViewChromeLog}");

                try
                {
                    using (IWebDriver driver = new ChromeDriver(driverService, options))
                    {
                        bool loopEnd = false;
                        uint pageNumber = PageHint;

                        string gallType = "";
                        if (IsMgall)
                            gallType = "/mgallery";

                        while (!loopEnd)
                        {
                            Console.WriteLine($"{pageNumber}번째 페이지 개념글 정산중");
                            driver.Url = $"https://gall.dcinside.com{gallType}/board/lists/?id={GallCode}&list_num=100&sort_type=N&exception_mode=recommend&search_head=&page={pageNumber++}";
                            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
                            var elements = driver.FindElements(By.CssSelector("tr[class='ub-content us-post']"));

                            foreach (var element in elements)
                            {
                                var post = new GallPost(element);
                        
                                if (!post.is_common_post)
                                    continue;
                            
                                if (post.gall_date < EndDate)
                                {
                                    loopEnd = true;
                                    break;
                                }

                                if (post.gall_date < StartDate)
                                    continue;

                                if (!postNumSet.TryGetValue(post.gall_num, out var actualValue))
                                {
                                    postNumSet.Add(post.gall_num);
                                    postList.Add(post);
                                    Console.WriteLine($"{post.gall_num}번글 파싱 완료");
                                }
                            }
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                    Console.WriteLine("수리 요청은 소녀전선2 갤러리");
                }

                int printCount = 30;
                StringBuilder builder = new StringBuilder();

                var ordered = postList.OrderByDescending(p => p.gall_recommend).ToList();
                builder.AppendLine($"추천 정렬 top{printCount}");
                ListToString(ordered, builder, printCount);
                builder.AppendLine("");
                builder.AppendLine("");
                builder.AppendLine("");

                ordered = postList.OrderByDescending(p => p.gall_count).ToList();
                builder.AppendLine($"조회수 정렬 top{printCount}");
                ListToString(ordered, builder, printCount);
                builder.AppendLine("");
                builder.AppendLine("");
                builder.AppendLine("");

                ordered = postList.OrderByDescending(p => p.gall_reply).ToList();
                builder.AppendLine($"댓글 정렬 top{printCount}");
                ListToString(ordered, builder, printCount);
                builder.AppendLine("");
                builder.AppendLine("");
                builder.AppendLine("");

                string path = $@"{System.IO.Directory.GetCurrentDirectory()}\GallJungSan_{DateTime.Now.ToString("yyyy/MM/dd")}.txt";
                System.IO.File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
                System.Diagnostics.Process.Start("Notepad.exe", path);
            }
            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}
