﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace LoggerService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            //if (Environment.UserInteractive)
            //{
            //    LoggerService service1 = new LoggerService(args);
            //    service1.TestStartupAndStop(args);
            //}
            //else
            //{
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new LoggerService(args)
            };
            ServiceBase.Run(ServicesToRun);
            //}
        }
    }
}
