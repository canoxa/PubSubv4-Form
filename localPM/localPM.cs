using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace PubSub
{
    class localPM
    {


        static void Main(string[] args)
        {
            Console.WriteLine("@localPM !!! porto -> {0}", args[0]);

            TcpChannel channel = new TcpChannel(Int32.Parse(args[0]));
            ChannelServices.RegisterChannel(channel, true);

            PMcreateProcess createProcess = new PMcreateProcess(Int32.Parse(args[0]));
            RemotingServices.Marshal(createProcess, "PuppetMasterURL", typeof(PMcreateProcess));

            Console.ReadLine();
        }
    }

    class PMcreateProcess : MarshalByRefObject, PuppetInterface
    {
        int portCounter;
        private static string proj_path = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName).FullName;

        public PMcreateProcess(int pC)
        {
            portCounter = pC+1;
        }


        public void createProcess(TreeNode site, string role, string name, string s, string url)
        {

            string aux = "LocalPMcreateProcess @ url -> " + url + " site -> " + s;
            Console.WriteLine(aux);

            
            if (role.Equals("broker"))
            {
                string port = (portCounter++).ToString();

                string brokers = fillArgument(site);
                
                ProcessStartInfo startInfo = new ProcessStartInfo(proj_path+@"\Broker\bin\Debug\Broker.exe");
                string[] args = { port, url, name, s, brokers };
                startInfo.Arguments = String.Join(";", args);

                Process p = new Process();
                p.StartInfo = startInfo;

                p.Start();
            }
            if (role.Equals("subscriber"))
            {
                
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
                

                string port = (portCounter++).ToString();

                ProcessStartInfo startInfo = new ProcessStartInfo(proj_path + @"\Publisher\bin\Debug\Publisher.exe");
                string[] args = { port, url, name, s };
                startInfo.Arguments = String.Join(";", args);

                Process pro = new Process();
                pro.StartInfo = startInfo;

                pro.Start();
            }

        }

        private string fillArgument(TreeNode site)
        {
            string res = "";
            foreach (var aux in site.getVizinhos()) {
                res += aux.Key + "%" + aux.Value+";";
            }
            return res;
        }
    }
    class PMLog : MarshalByRefObject, LogInterface
    {

        private System.IO.StreamWriter file;
        private static string proj_path = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName).FullName;

        public PMLog()
        {
            file = new System.IO.StreamWriter(proj_path + @"\log.txt");
        }

        public void log(string selfName, string pubName, string topicName, int eventNumber,string ID)
        {
            
            string line = "";
            if (ID.Equals("publisher"))
            {
                line = "PubEvent " + selfName + ", " + pubName + ", " + topicName + ", " + eventNumber;
                file.WriteLine(line);
            }
            if (ID.Equals("broker"))
            {
                line = "BroEvent " + selfName + ", " + pubName + ", " + topicName + ", " + eventNumber;
                file.WriteLine(line);
            }
            if (ID.Equals("subscriber"))
            {
                line = "SubEvent " + selfName + ", " + pubName + ", " + topicName + ", " + eventNumber;
                file.WriteLine(line);
            }
        }
    }
}
