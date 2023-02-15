using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Maglumi
{
    public partial class Service1 : ServiceBase
    {
        Subscriber subscriber;
        LabService labService;
        ResultSender resultSender;
        ConfigFileHundler configHundler;
        Database db = null;
        public Service1()
        {
            InitializeComponent();
            this.configHundler = new ConfigFileHundler();
            db = new Database();

        }

        protected override void OnStart(string[] args)
        {
            subscriber = new Subscriber(configHundler.GetEndPoint());
            subscriber.startServer();

            labService = new LabService();
            labService.startServer();

            resultSender = new ResultSender();
            resultSender.startServer();

        }

        protected override void OnStop()
        {
            subscriber.stopServer();
            labService.stopServer();
            resultSender?.stopServer();
        }
    }
}
