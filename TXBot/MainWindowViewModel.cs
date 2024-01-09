using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;

namespace TXBot
{
    partial class MainWindowViewModel : ObservableObject
    {
        private EdgeDriverService service = null;
        private IWebDriver driver = null;
        private long learnStamp = 0;

        public List<Course> Courses { get; set; } = new List<Course>();
        public ObservableCollection<Message> Messages { get; set; } = new ObservableCollection<Message>();

        [RelayCommand]
        private void Startup()
        {
            if (service != null && driver != null)
            {
                return;
            }

            service = EdgeDriverService.CreateDefaultService(".", "msedgedriver.exe");
            driver = new EdgeDriver(service);
            driver.Navigate().GoToUrl("https://bgi.zhixueyun.com/");
        }

        [RelayCommand]
        private void BeginLearning()
        {
            learnStamp = DateTime.Now.Ticks;
            Task.Run(() => LearnThread(learnStamp));
        }

        private void LearnThread(long stamp)
        {
            AddMessage("开始学习");

            while (stamp == learnStamp)
            {
                try
                {
                    if (service == null || driver == null)
                    {
                        break;
                    }

                    Course course = Courses.FirstOrDefault(c => c.State == null);

                    if (course == null)
                    {
                        continue;
                    }

                    AddMessage($"开始课程:{course.Url}");

                    driver.Navigate().GoToUrl(course.Url);
                    Thread.Sleep(5000);
                    var title = driver.FindElement(By.XPath("/html/body/div[2]/div[1]/div[3]/div/div[1]/div/div[1]/div/div[1]/div[1]/span"));

                    if (title == null)
                    {
                        AddMessage("找不到标题");
                        course.State = false;
                        continue;
                    }

                    AddMessage($"标题：{title.Text}");

                    var sectionCount = driver.FindElements(By.ClassName("section-arrow")).Count();

                    for (int i = 0; i < sectionCount; i++)
                    {
                        string sectionXPath = $"/html/body/div[2]/div[1]/div[3]/div/div[1]/div/div[2]/div/div[1]/div[1]/div/div/div/div/div[2]/div/div/ul/li/ul/li[{i + 1}]";
                        var section = driver.FindElement(By.XPath(sectionXPath));

                        if (!section.Text.Contains("必修"))
                        {
                            continue;
                        }

                        section.Click();
                        Thread.Sleep(2000);

                        string sectionTitleXPath = $"{sectionXPath}/div/dl/div[2]/div[1]/div";
                        var sectionTitle = driver.FindElement(By.XPath(sectionTitleXPath));
                        AddMessage($"开始章节：{sectionTitle.Text}");

                        while (true)
                        {
                            Thread.Sleep(1000);
                            section = driver.FindElement(By.XPath(sectionXPath));

                            if (!section.Text.Contains("需再学"))
                            {
                                break;
                            }

                            try
                            {
                                var btn = driver.FindElement(By.ClassName("btn-ok"));
                                Thread.Sleep(1000);
                                btn.Click();
                                AddMessage("点击暂停按钮");
                                Thread.Sleep(1000);
                            }
                            catch
                            {

                            }
                        }
                    }

                    course.State = true;
                }
                catch (Exception ex)
                {
                    AddMessage(ex.ToString());
                    break;
                }
            }

            AddMessage("退出学习");
        }

        private void AddMessage(string content)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add(new Message()
                {
                    Time = DateTime.Now,
                    Content = content
                });

                while (Messages.Count > 100)
                {
                    Messages.RemoveAt(0);
                }
            });
        }
    }

    partial class Course : ObservableObject
    {
        [ObservableProperty]
        private string _url = null;

        [ObservableProperty]
        private bool? _state = null;
    }

    class Message
    {
        public DateTime Time { get; set; }
        public string Content { get; set; }
    }
}
