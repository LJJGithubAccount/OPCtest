using Opc.Ua;
using Opc.Ua.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OPC.Server
{
    internal class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Program p = new Program();
            ManualResetEvent allDone = new ManualResetEvent(false);
            Thread t = new Thread(new ThreadStart(p.Start));
            t.Start();
            allDone.WaitOne();
        }

        [STAThread]
        private void Start()
        {
            ApplicationInstance application = new ApplicationInstance
            {
                ApplicationType = ApplicationType.Server,
                ConfigSectionName = "ServerCmd"
            };
            ManualResetEvent allDone = new ManualResetEvent(false);
            try
            {
                // process and command line arguments.
                if (application.ProcessCommandLine())
                {
                    return;
                }

                // check if running as a service.
                if (!Environment.UserInteractive)
                {
                    application.StartAsService(new ReferenceServer());
                    return;
                }

                // check the application certificate.
                application.CheckApplicationInstanceCertificate(false, 0);

                // start the server.
                application.Start(new ReferenceServer());
            }
            catch (Exception e)
            {
                //ExceptionDlg.Show(application.ApplicationName, e);
                //return;
                //throw (e);
                Console.Write(""+e.Message);
            }
            Console.Write("start!\n");
        }
    }
}