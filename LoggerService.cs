using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Runtime.InteropServices;
using System.IO;

namespace LoggerService
{
    public partial class LoggerService : ServiceBase
    {
        private static string path = ConfigurationManager.AppSettings["Path"];
        Logger logger = new Logger(path);

        public LoggerService(string[] args)
        {
            InitializeComponent(args);
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                logger.WriteToFile("Service is started at " + DateTime.Now);
                Console.WriteLine("Service is started at " + DateTime.Now);

                FileSystemWatcher watcher = new FileSystemWatcher();

                watcher.Path = path;
                watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;
                watcher.Created += new FileSystemEventHandler(OnCreated);
                watcher.Filter = "*.log";
                watcher.IncludeSubdirectories = false;
                watcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                logger.WriteToFile(ex.Message);
                Console.WriteLine(ex.Message);
            }
        }

        protected override void OnStop()
        {
            logger.WriteToFile("Service is stopped at " + DateTime.Now);
            Console.WriteLine("Service is stopped at " + DateTime.Now);
        }

        protected void OnCreated(object sender, FileSystemEventArgs e)
        {

            if (e.ChangeType != WatcherChangeTypes.Created)
            {
                return;
            }

            Console.WriteLine($"New file created {e.Name}");

            logger.GetFiles(e.Name);
        }

        //internal void TestStartupAndStop(string[] args)
        //{
        //    this.OnStart(args);
        //    Console.ReadLine();
        //    this.OnStop();
        //}
    }
}
