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
using KB.Configuration;
using System.Text;

namespace Com0com.Communicator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public string ErrorLogFile { get; set; } = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".error.log");
        public bool WriteErrorToLogFile { get; set; } = true;
        public bool HandleExceptions { get; set; } = true;

        public App()
        {
            Ini.Default = new Ini(Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".ini")); // Rename the ini file to be as the exe file name.
            Ini.Default.CreateDefault(Redirector.Properties.Resources.DefaultIni);
            Ini.Default.LoadProperties(this);
        }

        void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (WriteErrorToLogFile)
                File.AppendAllText(Path.GetFullPath(ErrorLogFile), DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:FFF") + " => " + e.Exception.ToString() + Environment.NewLine, Encoding.UTF8);
            if (HandleExceptions) e.Handled = true;
        }

        private void App_Exit(object sender, ExitEventArgs e)
        {
            
        }
    }
}
