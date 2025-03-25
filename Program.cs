using System;
using System.Configuration;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;

namespace KVCOMSVC
{
    public partial class Service1
    {
        private ServiceExecutableSource _executableSource;
        private int _checkInterval;
        private Process _process;
        private Timer _timer;
        private string[] _fileLines;
        //private EventLog _eventLog1;

        public Service1()
        {
            //InitializeComponent();
            //_eventLog1 = new EventLog();
            //if (!EventLog.SourceExists("KVCOMSVC"))
            {
                //    EventLog.CreateEventSource("KVCOMSVC", "KVCOMSVC_LOG");
            }
            //_eventLog1.Source = "KVCOMSVC";
            //_eventLog1.Log = "KVCOMSVC_LOG";

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KVCOMSVC_SETTING");

            try
            {
                _fileLines = File.ReadAllLines(filePath);
                _executableSource = new ServiceExecutableSource(GetExecutablePathFromArguments(_fileLines));
                _checkInterval = GetCheckIntervalFromArguments(_fileLines);
            }
            catch (Exception e)
            {
                // Log the error or handle it appropriately
            }
        }

        public void StartService()
        {
            OnStart();
        }

        public void StopService()
        {
            OnStop();
        }

        protected void OnStart()
        {
            //_eventLog1.WriteEntry("In OnStart.");
            //_eventLog1.WriteEntry(argsvc[0]);
            //_executableSource = new ServiceExecutableSource(GetExecutablePathFromArguments(argsvc));
            //_eventLog1.WriteEntry(_executableSource.GetExecutablePath());
            //_checkInterval = GetCheckIntervalFromArguments(argsvc);
            //_eventLog1.WriteEntry(_checkInterval.ToString());

            StartProcess();
            _timer = new Timer(CheckProcess, null, _checkInterval, _checkInterval);
        }

        protected void OnStop()
        {
            StopProcess();
            _timer?.Dispose();
        }

        private void StartProcess()
        {
            if (_process == null || _process.HasExited)
            {
                _process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _executableSource.GetExecutablePath(),
                        WorkingDirectory = _executableSource.GetExecutableFolder(),
                        UseShellExecute = false,
                        CreateNoWindow = false,
                        RedirectStandardError = true,
                        WindowStyle = ProcessWindowStyle.Normal,
                        ErrorDialog = true
                    }
                };

                _process.ErrorDataReceived += Process_ErrorDataReceived;
                _process.Exited += Process_Exited;
                _process.Start();
                _process.BeginErrorReadLine();
            }
        }

        private void StopProcess()
        {
            if (_process != null && !_process.HasExited)
            {
                try
                {
                    _process.Kill();
                }
                catch (Exception ex)
                {
                    //EventLog.WriteEntry("Service", "Error stopping process: " + ex.Message, EventLogEntryType.Error);
                }
                finally
                {
                    _process.Dispose();
                    _process = null;
                }
            }
        }

        private void CheckProcess(object state)
        {
            if (_process == null || _process.HasExited)
            {
                StartProcess();
            }
            else
            {
                try
                {
                    if (_process.Responding == false)
                    {
                        StopProcess();
                        StartProcess();
                    }
                }
                catch (Exception ex)
                {
                    //EventLog.WriteEntry("Service", "Error checking process: " + ex.Message, EventLogEntryType.Error);
                    StopProcess();
                    StartProcess();
                }
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                string errorMessage = e.Data;
                if (errorMessage.Contains("Exception") || errorMessage.Contains("Error"))
                {
                    //EventLog.WriteEntry("Service", "Unhandled exception in process: " + errorMessage, EventLogEntryType.Error);
                    ShutdownAndRestartProcess();
                }
                else
                {
                    //EventLog.WriteEntry("Service", "Error in process: " + errorMessage, EventLogEntryType.Error);
                }
            }
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            int exitCode = _process.ExitCode;
            if (exitCode != 0)
            {
                //EventLog.WriteEntry("Service", "Process exited with code " + exitCode, EventLogEntryType.Error);
                ShutdownAndRestartProcess();
            }
            else
            {
                //EventLog.WriteEntry("Service", "Process exited normally", EventLogEntryType.Information);
            }
        }

        private void ShutdownAndRestartProcess()
        {
            StopProcess();
            StartProcess();
        }

        private int GetCheckIntervalFromArguments(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg.StartsWith("/checkinterval:"))
                {
                    string value = arg.Substring(15);
                    if (int.TryParse(value, out int interval))
                    {
                        return interval;
                    }
                }
            }
            return 10000; // default value
        }

        private static string GetExecutablePathFromArguments(string[] args)
        {
            if (args.Length > 0)
            {
                return args[0];
            }
            else
            {
                throw new ArgumentException("Executable path not provided as an argument.");
            }
        }
    }

    internal static class Program
    {
        static void Main(string[] args)
        {
            var service = new Service1();
            service.StartService();

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                service.StopService();
            };

            // Keep the application running
            Thread.Sleep(Timeout.Infinite);
        }
    }
}