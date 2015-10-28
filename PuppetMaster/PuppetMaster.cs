using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PubSub
{
    static class PuppetMaster
    {
        //private static string proj_path = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName).FullName;
        //private static string conf_filename = proj_path+@"\example.txt";

        static void Main(string[] args)
        {
            TcpChannel channel = new TcpChannel(Int32.Parse(args[0]));
            ChannelServices.RegisterChannel(channel, true);

            PMcreateProcess createProcess = new PMcreateProcess(Int32.Parse(args[0]));
            RemotingServices.Marshal(createProcess, "PuppetMasterURL", typeof(PMcreateProcess));

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());



            /*
            RemotingConfiguration.RegisterWellKnownServiceType(
                typeof(PMcreateProcess),
                "PuppetMasterURL",
                WellKnownObjectMode.Singleton);*/


            //lancar slaves
            //for (int i = 0; i < 6; i++) {

            //    ProcessStartInfo startInfo = new ProcessStartInfo(proj_path+@"\localPM\bin\Debug\localPM.exe");
            //    int port = 9000 + (i*100);
            //    string arg = port.ToString();
            //    startInfo.Arguments = arg;

            //    Process p = new Process();
            //    p.StartInfo = startInfo;

            //    p.Start();

            //}

            //Scanner scan = new Scanner();

            //TreeNode root = scan.getRootNodeFromFile(conf_filename);

            ////criar arvore a partir de root
            //scan.readTreeFromFile(root, conf_filename);

            //scan.quickRead(conf_filename, root);

            //int routingMode = scan.getRouting();//0-flood; 1-filter
            //int orderMode = scan.getOrder();//0-NO; 1-FIFO; 2-TOTAL
            //int logMode = scan.getLogMode();//0-light; 1-full

            //////lancar servico de LOG
            //PMLog log = new PMLog(logMode);
            //RemotingServices.Marshal(log, "PuppetMasterLog", typeof(PMLog));


            ////preencher lstProcess - lista de todos os processos no config file
            //List<MyProcess> lstProcess = scan.fillProcessList(conf_filename, root);


            ////estrutura que diz em que porta está cada processo
            //Dictionary<string, int> pname_port = scan.getPname_port();


            //SubInterface subint = (SubInterface)Activator.GetObject(typeof(SubInterface),"tcp://localhost:"+pname_port["subscriber0"]+"/MPMSubUnsub");
            //subint.subscribe("grandeTopico");

            //subint = (SubInterface)Activator.GetObject(typeof(SubInterface), "tcp://localhost:" + pname_port["subscriber1"] + "/MPMSubUnsub");
            //subint.subscribe("grandeTopico");
            ////subint.subscribe("pequenoTopico");
            ////subint.unsubscribe("grandeTopico");

            //subint = (SubInterface)Activator.GetObject(typeof(SubInterface), "tcp://localhost:" + pname_port["subscriber2"] + "/MPMSubUnsub");
            //subint.subscribe("grandeTopico");
            ////subint.subscribe("pequenoTopico");

            //subint = (SubInterface)Activator.GetObject(typeof(SubInterface), "tcp://localhost:" + pname_port["subscriber3"] + "/MPMSubUnsub");
            ////subint.subscribe("grandeTopico");
            //subint.subscribe("pequenoTopico");

            //subint = (SubInterface)Activator.GetObject(typeof(SubInterface), "tcp://localhost:" + pname_port["subscriber4"] + "/MPMSubUnsub");
            //subint.subscribe("grandeTopico");
            ////subint.subscribe("pequenoTopico");

            //subint = (SubInterface)Activator.GetObject(typeof(SubInterface), "tcp://localhost:" + pname_port["subscriber5"] + "/MPMSubUnsub");
            //subint.subscribe("grandeTopico");
            ////subint.subscribe("pequenoTopico");

            //PubInterface pubint = (PubInterface)Activator.GetObject(typeof(PubInterface), "tcp://localhost:" + pname_port["publisher4"] + "/PMPublish");
            //pubint.publish("1", "pequenoTopico", "10",routingMode,orderMode);

            //MessageBox.Show("finito");
        }
    }

    class PMcreateProcess : MarshalByRefObject, PuppetInterface
    {

        // vida infinita !!!!
        public override object InitializeLifetimeService()
        {
            return null;
        }
        int portCounter;
        private static string proj_path = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName).FullName;

        public PMcreateProcess(int pC)
        {
            portCounter = pC;
        }

       
        public void createProcess(TreeNode site, string role, string name, string s, string url)
        {
            string aux = "MasterPMcreateProcess @ url -> " + url + " site -> " + s;
            MessageBox.Show(aux);
            if (role.Equals("broker"))
            {
                Broker b = new Broker(url, name, s);
                //site.setBroker(b);

                string port = (portCounter++).ToString();

                //Process.Start()
                ProcessStartInfo startInfo = new ProcessStartInfo(proj_path + @"\Broker\bin\Debug\Broker.exe");
                string[] args = { port, url, name, s};
                startInfo.Arguments = String.Join(";", args);

                Process p = new Process();
                p.StartInfo = startInfo;

                p.Start();
            }
            if (role.Equals("subscriber"))
            {
                Subscriber sub = new Subscriber(url, name, s);
                //site.addSubscriber(sub);

                string port = (portCounter++).ToString();

                ProcessStartInfo startInfo = new ProcessStartInfo(proj_path + @"\Subscriber\bin\Debug\Subscriber.exe");
                string[] args = { port, url, name, s };
                startInfo.Arguments = String.Join(";", args);

                Process p = new Process();
                p.StartInfo = startInfo;

                p.Start();
            }
            if (role.Equals("publisher"))
            {
                Publisher p = new Publisher(url, name, s/*,site.getBroker()*/);
                //site.addPublisher(p);

                string port = (portCounter++).ToString();

                ProcessStartInfo startInfo = new ProcessStartInfo(proj_path + @"\Publisher\bin\Debug\Publisher.exe");
                string[] args = { port, url, name, s };
                startInfo.Arguments = String.Join(";", args);

                Process pro = new Process();
                pro.StartInfo = startInfo;

                pro.Start();
            }

        }
    }

    class PMLog : MarshalByRefObject, LogInterface
    {

        // vida infinita !!!!
        public override object InitializeLifetimeService()
        {
            return null;
        }

        private static string proj_path = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName).FullName;
        private static object myLock = new Object();

        private int mode;

        public PMLog(int m)
        {
            File.Create(proj_path+@"\log.txt").Close();
            this.mode = m;
        }

        public void log(string selfName, string pubName, string topicName, int eventNumber, string ID)
        {
            //MessageBox.Show("Estou a escrever no log, quem me chamou foi o " + selfName);
            string line = "";
            if (ID.Equals("publisher"))
            {
                line = "PubEvent " + selfName + ", " + pubName + ", " + topicName + ", " + eventNumber;
            }
            if (ID.Equals("broker"))
            {
                if (this.mode != 0)//se for full
                {
                    line = "BroEvent " + selfName + ", " + pubName + ", " + topicName + ", " + eventNumber;
                }
            }
            if (ID.Equals("subscriber"))
            {
                line = "SubEvent " + selfName + ", " + pubName + ", " + topicName + ", " + eventNumber;
            }
            if (!line.Equals(""))
            {
                lock (myLock)
                {
                    using (FileStream file = new FileStream(proj_path + @"\log.txt", FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (StreamWriter writer = new StreamWriter(file, Encoding.Unicode))
                    {
                        writer.WriteLine(line);
                    }
                }
                
            }
        }
    }
}
