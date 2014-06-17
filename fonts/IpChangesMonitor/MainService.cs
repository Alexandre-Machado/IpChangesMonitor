using System;
using System.Diagnostics;
using System.ServiceProcess;

namespace IpChangesMonitor
{
    public partial class MainService : ServiceBase
    {
        EventLog eventLog = new EventLog() { Source = "IpMonitorSource", Log = "IpMonitorLog" };

        public MainService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                if (!System.Diagnostics.EventLog.SourceExists("IpMonitorSource"))
                {
                    EventLog.CreateEventSource("IpMonitorSource", "IpMonitorLog");
                }

                eventLog.WriteEntry("Service started.");

                IpMonitor.Initialize();
            }
            catch (Exception e)
            {
                eventLog.WriteEntry(e.Message);
            }
        }

        protected override void OnStop()
        {
            eventLog.WriteEntry("Service ended.");
        }
    }
}
