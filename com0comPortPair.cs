using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using KB.Configuration;

namespace Com0com.Redirector
{
    public enum CommsMode
    {
        TCPClient,
        UDP,
        RFC2217,
    }

    public enum CommsStatus
    {
        Running, 
        Idle,
    }

    public class Com0comPortPair : INotifyPropertyChanged
    {
        #region Fields
        private bool _autoStart = false;
        private bool _stopWhenPortAClosed = false;
        private string _portConfigStringA = "";
        private string _portConfigStringB = "";
        private Process _p;
        private CommsStatus _commsStatus = CommsStatus.Idle;
        private CommsMode _commsMode = CommsMode.RFC2217;
        private string _outputData = "";
        private string _remoteIP = "";
        private string _remotePort = "";
        private string _localPort = "";
        private string _options = "";

        #endregion

        #region Properties

        public int PairNumber { get; private set; }
        public string PortNameA { get; private set; }
        public string PortNameB { get; private set; }
        public bool IsPortAOpen { get; private set; } // will be updated only when StopWhenPortAClosed is true.

        public bool AutoStart
        {
            get
            {
                return _autoStart;
            }
            set
            {
                _autoStart = value;
                OnPropertyChanged("AutoStart");
            }
        }

        public bool StopWhenPortAClosed
        {
            get
            {
                return _stopWhenPortAClosed;
            }
            set
            {
                _stopWhenPortAClosed = value;
                OnPropertyChanged("StopWhenPortAClosed");
            }
        }

        public string RemotePort
        {
            get
            {
                return _remotePort;
            }
            set
            {
                _remotePort = value;
                OnPropertyChanged("RemotePort");
            }
        }

        public string RemoteIP
        {
            get
            {
                return _remoteIP;
            }
            set
            {
                _remoteIP = value;
                OnPropertyChanged("RemoteIP");
            }
        }

        public string LocalPort
        {
            get
            {
                return _localPort;
            }
            set
            {
                _localPort = value;
                OnPropertyChanged("LocalPort");
            }
        }
        public string OutputData
        {
            get { return _outputData; }
            private set 
            {
                _outputData = value;
                OnPropertyChanged("OutputData");
            }
        }
        public CommsMode CommsMode
        {
            get { return _commsMode; }
            set
            {
                _commsMode = value;
                OnPropertyChanged("CommsMode");
            }
        }
        public CommsStatus CommsStatus
        {
            get { return _commsStatus; }
            set
            {
                _commsStatus = value;
                OnPropertyChanged("CommsStatus");
            }
        }

        public string PortConfigStringA
        {
            get { return _portConfigStringA; }
            set
            {
                Regex regex = new Regex(@"(?<=PortName=)\w+(?=,)");
                _portConfigStringA = value;
                PortNameA = regex.Match(value).Value;
                
                OnPropertyChanged("PortNameA");
                OnPropertyChanged("PortConfigStringA");
                
            }
        }
        public string PortConfigStringB 
        {
            get { return _portConfigStringB; }
            set
            {
                Regex regex = new Regex(@"(?<=PortName=)\w+(?=,)");
                _portConfigStringB = value;
                PortNameB = regex.Match(value).Value;
                
                OnPropertyChanged("PortNameB");
                OnPropertyChanged("PortConfigStringB");
                
            }
        }

        public string Options
        {
            get
            {
                return _options;
            }
            set
            {
                _options = value;
                OnPropertyChanged("Options");
            }
        }

        #endregion

        public Com0comPortPair(int number)
            : this(number, "192.168.1.1", "8882", "8883", false)
        { }

        public Com0comPortPair(int number, string remoteIP, string remotePort, string localPort, bool autoStart)
        {
            PairNumber = number;
            AutoStart = autoStart;
            RemoteIP = remoteIP;
            RemotePort = remotePort;
            LocalPort = localPort;
            Ini.Default.LoadProperties(this, number.ToString());
            ContinualCheckAsync();
        }

        private async void ContinualCheckAsync()
        {
            await Task.Run(async () => 
            {
                while (true)
                {
                    if (StopWhenPortAClosed && !(IsPortAOpen = IsPortOpen(PortNameA)))
                        StopComms();
                    else if (AutoStart)
                        StartComms();
                    await Task.Delay(1000); // Wait a second.
                }
            });
        }

        #region Static Functions

        /// <summary>
        /// Kill a process, and all of its children, grandchildren, etc.
        /// </summary>
        /// <param name="pid">Process ID.</param>
        private static void KillProcessAndChildren(int pid)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch
            {
                //we might get exceptions here, as parent might auto exit once their children are terminated
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern SafeFileHandle CreateFile(string lpFileName, int dwDesiredAccess, int dwShareMode, IntPtr securityAttrs, int dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);

        private static bool IsPortOpen(string portName)
        { // Taken from https://stackoverflow.com/a/5052499/12171731
            int dwFlagsAndAttributes = 0x40000000;

            var isValid = SerialPort.GetPortNames().Any(x => string.Compare(x, portName, true) == 0);
            if (!isValid) return false; // port was not found

            //Borrowed from Microsoft's Serial Port Open Method :)
            SafeFileHandle hFile = CreateFile(@"\\.\" + portName, -1073741824, 0, IntPtr.Zero, 3, dwFlagsAndAttributes, IntPtr.Zero);
            if (hFile.IsInvalid) return true; // port is already open

            hFile.Close();
            return false;
        }

        #endregion

        public void StartComms()
        {
            if (CommsStatus == CommsStatus.Running)
                return;
            string program = "";
            string arguments = "";

            switch (CommsMode)
            {
                case CommsMode.RFC2217:
                    program = References.Default.RFC2217;
                    arguments = string.Format(@"\\.\{0} {1} {2}", PortNameB, RemoteIP, RemotePort);
                    break;
                case CommsMode.TCPClient:
                    program = References.Default.Com2tcp;
                    arguments = string.Format(@"\\.\{0} {1} {2}", PortNameB, RemoteIP, RemotePort);
                    break;
                case CommsMode.UDP:
                    program = References.Default.Com2tcp;
                    arguments = string.Format(@"--udp \\.\{0} {1} {2} {3}", PortNameB, RemoteIP, RemotePort, LocalPort);
                    break;

            }

            _p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.GetFullPath(program),
                    Arguments = this.Options + " " + arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            _p.EnableRaisingEvents = true;
            _p.Exited += _p_Exited;
            _p.OutputDataReceived += _p_OutputDataReceived;
            _p.ErrorDataReceived += _p_ErrorDataReceived;

            OutputData = "";
            _p.Start();
            _p.BeginOutputReadLine();
            _p.BeginErrorReadLine();
            ChildProcessTracker.AddProcess(_p);

            CommsStatus = CommsStatus.Running;
        }

        public void StopComms()
        {
            if (_p == null)
            {
                CommsStatus = CommsStatus.Idle;
                return;
            }
            if (_p.HasExited)
                return;
            KillProcessAndChildren(_p.Id);
        }

        private void _p_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            OutputData += e.Data + Environment.NewLine;
        }

        private void _p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            OutputData += e.Data + Environment.NewLine;
        }

        private void _p_Exited(object sender, EventArgs e)
        {
            CommsStatus = CommsStatus.Idle;
        }

        #region INotifyPropertyChangedMembers
        protected virtual void OnPropertyChanged(string propertyName)
        {
            this.VerifyPropertyName(propertyName);

            PropertyChangedEventHandler handler = this.PropertyChanged;

            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        public void VerifyPropertyName(string propertyName)
        {
            //Verify that the property name matches a real,
            //public instance property on this object
            //an empty property name is ok, used to refresh all properties
            if (string.IsNullOrEmpty(propertyName))
            {
                return;
            }
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                Debug.Fail( "Invalid property name: " + propertyName);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        
    }
}
