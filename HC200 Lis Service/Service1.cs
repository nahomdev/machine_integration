using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace HC200_Lis_Service
{
    public partial class Service1 : ServiceBase
    {
        Subscriber subscriber;
        ResultSender sender;
        OrderSender orderSender;
        LabService labService;
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            subscriber = new Subscriber();
            sender = new ResultSender();
            orderSender = new OrderSender();
            labService = new LabService();
            //DateTime EXdate = new DateTime(2022,10,26);
            //if (DateTime.Now < EXdate )
            //{
               
                subscriber.startSubscriber();

               
                sender.startResultSender();


               
                orderSender.startOrderSender();

                
                labService.startLabService();
            //}
            //else
            //{
            //    Logger logger = new Logger();

            //    logger.WriteLog("BOOM!!!");
            //}
           

            

        }

        protected override void OnStop()
        {
            subscriber.stopSubscriber();
            sender.stopResultSender();
            orderSender?.stopOrderSender();
            labService?.stopLabService();

            subscriber = null;
            sender = null;
            orderSender = null;
             labService = null;
        }
    }
}
