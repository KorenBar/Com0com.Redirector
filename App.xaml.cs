using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Reflection;
using System.IO;
using System.Windows.Threading;

namespace Com0com.Communicator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public bool WriteErrorToLogFile { get; set; } = true;
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (WriteErrorToLogFile)
                Utility.File.AddText(Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".error.log"), 
                    DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:FFF") + " => " + e.Exception.ToString());
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            
        }
    }
}
