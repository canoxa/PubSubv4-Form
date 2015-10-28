using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace PubSub
{
    public class Publisher
    {

        private string url;
        private string name;
        private string site;

        public string URL
        {
            get { return url; }
            set { url = value; }
        }
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        public string Site
        {
            get { return site; }
            set { site = value; }
        }

        

        public Publisher(string u, string n, string s)
        {
            URL = u;
            Name = n;
            Site = s;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("@publisher !!! porto -> {0}", args[0]);

            string[] arguments = args[0].Split(';');//arguments[0]->port; arguments[1]->url; arguments[2]->nome; arguments[3]->site;

            TcpChannel channel = new TcpChannel(Int32.Parse(arguments[0]));
            ChannelServices.RegisterChannel(channel, true);

            MPMPubImplementation MPMpublish = new MPMPubImplementation(arguments[0], arguments[1], arguments[3], arguments[2]);
            
            RemotingServices.Marshal(MPMpublish, "PMPublish", typeof(MPMPubImplementation));

            MPMPublisherCmd processCmd = new MPMPublisherCmd();
            RemotingServices.Marshal(processCmd, "MPMProcessCmd", typeof(MPMPublisherCmd));

            Console.ReadLine();
        }

    }
    class MPMPubImplementation : MarshalByRefObject, PubInterface
    {
        private string myPort;
        private string url;
        private string site;
        private int count;
        private string name;


        // vida infinita !!!!
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public MPMPubImplementation(string p1, string p2, string p3, string p4)
        {
            this.myPort = p1;
            this.url = p2;
            this.site = p3;
            this.count = 0;
            this.name = p4;
        }
        public void publish(string number, string topic, string secs,int filter, int order)
        {
            string urlRemote = url.Substring(0, url.Length - 14);//retirar XXXX/publisher
            string myURL = urlRemote + myPort;

            Console.WriteLine("@MPMPubImplementatio - {0} publishing events, on topic {1}", myURL, topic);

            int port = 9000;
            int mult = Int32.Parse("" + site[site.Length - 1]);
            urlRemote += (port + (mult * 100) + 1).ToString() + "/";

            BrokerReceiveBroker pub = (BrokerReceiveBroker)Activator.GetObject(typeof(BrokerReceiveBroker), urlRemote + "BrokerCommunication");
            for (int i = 0; i < Int32.Parse(number); i++)
            {
                count = count + 1;
                Message maux = new Message(topic, i.ToString(), count, name);
                pub.receivePublication(maux, myURL,filter,order);
                
                LogInterface log = (LogInterface)Activator.GetObject(typeof(LogInterface), "tcp://localhost:8086/PuppetMasterLog");
                log.log(this.name, this.name, topic, 0, "publisher");

            }
        }
    }

    public class MPMPublisherCmd : MarshalByRefObject, IProcessCmd
    {

        public void crash()
        {
            // sledgehammer solution -> o mesmo que unplug
            Environment.Exit(1);
        }

        public void freeze()
        {
            throw new NotImplementedException();
        }

        public void unfreeze()
        {
            throw new NotImplementedException();
        }
    }
}
