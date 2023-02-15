using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Sysmex_Lis_Service
{
    public partial class Service1 : ServiceBase
    {
        Subscriber subscriber;
        ResultSender resultSender;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)

        {
            base.OnStart(args);
            subscriber = new Subscriber();
            subscriber.startServer();

            resultSender = new ResultSender();
            resultSender.startResultSender();

            //Publisher pub = new Publisher();
           
            //Thread publish = new Thread(new ThreadStart(pub.Send));
            //publish.Start();
            
        }

        protected override void OnStop()
        {
            base.OnStop();
            subscriber.stopServer();
            resultSender.stopResultSender();
        }
    }
}
