using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using LGCallRecorder.Database;

namespace LGCallRecorder
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Thread readingThread = new Thread(ReadingThread);
            readingThread.IsBackground = true;
            readingThread.Start();
        }

        private void ReadingThread()
        {
            using (TcpClient tcpClient = new TcpClient())
            {
                string host = ConfigurationManager.AppSettings["TelnetHost"];
                int port = int.Parse(ConfigurationManager.AppSettings["TelnetPort"]);
                tcpClient.Connect(host, port);

                using (TextReader tr = new StreamReader(tcpClient.GetStream()))
                {
                    while (true)
                        try
                        {
                            string line = tr.ReadLine();
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                int num;
                                if (!int.TryParse(line.Substring(0, 1), out num))
                                    continue;

                                CallRecord record = SlkCdrParser.Parse(line);

                                using (MainModel model = new MainModel())
                                {
                                    model.CallRecords.Add(record);

                                    model.SaveChanges();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                }
            }
        }
    }
}