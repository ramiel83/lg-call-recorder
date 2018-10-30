using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Timers;
using System.Windows;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Timer = System.Timers.Timer;

namespace LGCallRecorder
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly string FileToDownload = Path.Combine(Path.GetTempPath(), "smdr_data.gz");
        private readonly Timer _progressTimer = new Timer();

        public MainWindow()
        {
            _progressTimer.Interval = 1000;
            _progressTimer.Elapsed += ProgressTimerElapsed;

            InitializeComponent();

            if (bool.Parse(ConfigurationManager.AppSettings["IsDebug"]))
            {
                FileInfo uncompressedSlkFile =
                    Decompress(new FileInfo(ConfigurationManager.AppSettings["DebugSMDRFile"]));
                IEnumerable<CallRecord> callRecords = SlkCdrParser.Parse(uncompressedSlkFile);
                DataCDRWindow dcw = new DataCDRWindow(callRecords);
                Content = dcw;
            }
            else
            {
                Loaded += OnLoaded;
            }
        }

        private void ProgressTimerElapsed(object sender, ElapsedEventArgs e)
        {
            DownloadProgressBar.Dispatcher.Invoke((Action) (() => DownloadProgressBar.Value += 2));
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _progressTimer.Enabled = true;

            LoginToServer();

            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChanged += ClientOnDownloadProgressChanged;
                client.DownloadFileCompleted += ClientOnDownloadFileCompleted;
                client.DownloadFileAsync(new Uri(ConfigurationManager.AppSettings["SMDRDataUri"]), FileToDownload);
            }
        }

        private void LoginToServer()
        {
            using (IWebDriver driver = new ChromeDriver())
            {
                driver.Url = ConfigurationManager.AppSettings["LoginUri"];
                Thread.Sleep(2000);

                if (driver.FindElements(By.Id("account_logout")).Count > 0) return;

                driver.FindElement(By.Name("login_id")).SendKeys(ConfigurationManager.AppSettings["LoginUsername"]);
                driver.FindElement(By.Name("login_pw")).SendKeys(ConfigurationManager.AppSettings["LoginPassword"]);
                driver.FindElement(By.ClassName("btn_login")).Click();
                Thread.Sleep(2000);
            }
        }

        private void ClientOnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null) throw e.Error;

            FileInfo uncompressedSlkFile = Decompress(new FileInfo(FileToDownload));
            IEnumerable<CallRecord> callRecords = SlkCdrParser.Parse(uncompressedSlkFile);
            DataCDRWindow dcw = new DataCDRWindow(callRecords);
            Content = dcw;
        }

        private void ClientOnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadProgressBar.Dispatcher.Invoke((Action) (() =>
                DownloadProgressBar.Value = e.BytesReceived / e.TotalBytesToReceive * 100));
        }

        public static FileInfo Decompress(FileInfo fileToDecompress)
        {
            using (FileStream originalFileStream = fileToDecompress.OpenRead())
            {
                string currentFileName = fileToDecompress.FullName;
                string newFileName =
                    currentFileName.Remove(currentFileName.Length - fileToDecompress.Extension.Length) + ".slk";

                using (FileStream decompressedFileStream = File.Create(newFileName))
                {
                    using (GZipStream decompressionStream =
                        new GZipStream(originalFileStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(decompressedFileStream);
                    }
                }

                return new FileInfo(newFileName);
            }
        }
    }
}