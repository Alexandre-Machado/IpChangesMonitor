﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Security.Principal;
using System.Threading;

namespace IpChangesMonitor
{
    public class IpMonitor
    {
        private static ICollection<IPAddress> lastLocalIPs;

        public static void Initialize()
        {
            Timer timer = new Timer(DoWork);
            var timeout = 1000;

            var timeoutConfig = ConfigurationManager.AppSettings["TIMEOUT"];

            if (timeoutConfig != null)
                timeout = Convert.ToInt32(timeoutConfig);


            timer.Change(10000, timeout);
        }

        public static void DoWork(Object stateInfo)
        {
            var localIPs = Dns.GetHostAddresses(Dns.GetHostName());

            foreach (var localIp in localIPs)
            {
                if (lastLocalIPs == null || !lastLocalIPs.Contains(localIp))
                {
                    lastLocalIPs = localIPs.ToList();

                    SendNewAdressesToUser(lastLocalIPs);
                }
            }
        }

        private static void SendNewAdressesToUser(ICollection<IPAddress> iPs)
        {
            try
            {
                var host_sufix = ConfigurationManager.AppSettings["host_sufix"];
                var domain = ConfigurationManager.AppSettings["domain"];
                var user = ConfigurationManager.AppSettings["user"];
                var password = ConfigurationManager.AppSettings["password"];

                var identity = WindowsIdentity.GetCurrent();

                var mailAddress = identity.Name + "@" + domain;

                MailMessage mail = new MailMessage(mailAddress, mailAddress)
                {
                    Subject = "Your IP addresses have changed",
                    IsBodyHtml = true
                };

                var assembly = new FileInfo(Assembly.GetEntryAssembly().Location);

                var template = Path.Combine(assembly.Directory.FullName, "template.html");
                var templateContent = "";

                using (var reader = new FileInfo(template).OpenText())
                {
                    templateContent = reader.ReadToEnd();
                }

                var contentHtml = "";

                foreach (var ip in iPs)
                {
                    contentHtml += "<br />" + ip.ToString();
                }

                templateContent = templateContent.Replace("{Description}", contentHtml).Replace("{Year}", DateTime.Now.Year.ToString());
                mail.Body = templateContent;

                var client = new SmtpClient(host_sufix + "." + domain)
                {
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(user, password, domain),
                };

                client.Send(mail);
            }
            catch
            {
                EventLog eventLog = new EventLog() { Source = "IpMonitorSource", Log = "IpMonitorLog" };
                eventLog.WriteEntry("Email não enviado");
            }
        }
    }
}
