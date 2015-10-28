﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PubSub
{
    public partial class Form1 : Form
    {
        private static string proj_path = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName).FullName;
        private static string conf_filename;
        Dictionary<string, int> pname_port;
        List<MyProcess> lstProcess;
        TreeNode root;

        public delegate void PublisherRemoteAsyncDelegate(string number, string topic, string sec, int filter, int order);

        public static void FilterFloodRemoteAsyncCallBack(IAsyncResult ar)
        {
            PublisherRemoteAsyncDelegate del = (PublisherRemoteAsyncDelegate)((AsyncResult)ar).AsyncDelegate;
            return;
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < 6; i++)
            {

                ProcessStartInfo startInfo = new ProcessStartInfo(proj_path + @"\localPM\bin\Debug\localPM.exe");
                int port = 9000 + (i * 100);
                string arg = port.ToString();
                startInfo.Arguments = arg;

                Process p = new Process();
                p.StartInfo = startInfo;

                p.Start();

            }



        }



        private void file_Txb_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.readFile(file_Txb.Text);
            }


        }

        private void script_Txb_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                //array com o script
                string[] script = script_Txb.Text.Split(' ');

                //script[0]- Subscriber script[1]-processname script[2]Subscribe/Unsubscribe script[3]topicname
                if (script[0] == "Subscriber")
                {
                    if (script[2] == "Subscribe")
                    {
                        this.subscribe(script[1], script[3]);
                    }
                    if (script[2] == "Unsubscribe")
                    {
                        this.unSubscribe(script[1], script[3]);
                    }
                }

                //script[0]-Publisher script[1]-processname script[2]-Publish script[3]-numberofevents script[4]-Ontopic script[5]-topicname script[6]-Interval script[7]-x-ms
                if (script[0] == "Publisher")
                {
                    this.publish(script[1], script[3], script[5], script[7]);
                }

                //script[0]-Crash script[1]-processname
                if (script[0] == "Crash")
                {
                    this.crash(script[1]);
                }


            }
        }

        public void readFile(string fileName)
        {
            conf_filename = proj_path + @"\" + fileName;

            Scanner scan = new Scanner();

            root = scan.getRootNodeFromFile(conf_filename);

            //criar arvore a partir de root
            scan.readTreeFromFile(root, conf_filename);

            scan.quickRead(conf_filename, root);

            //preencher lstProcess - lista de todos os processos no config file
            lstProcess = scan.fillProcessList(conf_filename, root);


            //estrutura que diz em que porta está cada processo
            pname_port = scan.getPname_port();

            int routingMode = scan.getRouting();//0-flood; 1-filter
            int orderMode = scan.getOrder();//0-NO; 1-FIFO; 2-TOTAL
            int logMode = scan.getLogMode();//0-light; 1-full

            ////lancar servico de LOG
            PMLog log = new PMLog(logMode);
            RemotingServices.Marshal(log, "PuppetMasterLog", typeof(PMLog));

        }

        public void subscribe(string pname, string topic)
        {
            SubInterface subint = (SubInterface)Activator.GetObject(typeof(SubInterface), "tcp://localhost:" + pname_port[pname] + "/MPMSubUnsub");
            subint.subscribe(topic);
        }

        public void unSubscribe(string pname, string topic)
        {
            SubInterface subint = (SubInterface)Activator.GetObject(typeof(SubInterface), "tcp://localhost:" + pname_port[pname] + "/MPMSubUnsub");
            subint.unsubscribe(topic);
        }


        public void publish(string pname, string nEvent, string topic, string sec)
        {
            PubInterface pubint = (PubInterface)Activator.GetObject(typeof(PubInterface), "tcp://localhost:" + pname_port[pname] + "/PMPublish");
            //try
            //{
            //    PublisherRemoteAsyncDelegate RemoteDel = new PublisherRemoteAsyncDelegate(pubint.publish);
            //    AsyncCallback RemoteCallBack = new AsyncCallback(FilterFloodRemoteAsyncCallBack);
            //    IAsyncResult RemAr = RemoteDel.BeginInvoke(nEvent, topic, sec, 1, 1, RemoteCallBack, null);

            //}
            //catch (SocketException)
            //{
            //    Console.WriteLine("Could not locate server");
            //}

            pubint.publish(nEvent, topic, sec, 1, 1);
            
        }

        public void crash(string pname)
        {
            IProcessCmd process_crash = (IProcessCmd)Activator.GetObject(typeof(IProcessCmd), "tcp://localhost:" + pname_port[pname] + "/MPMProcessCmd");
            try
            {
                process_crash.crash();
            }
            catch (IOException)
            {
                // TODO -> avisar o que o processo já não está operacional (log I think or maybe é o processo a enviar)
            }
        }


    }
}
