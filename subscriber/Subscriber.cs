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
    public class Subscriber
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



        public Subscriber(string u, string n, string s/* Broker b*/)
        {
            URL = u;
            Name = n;
            Site = s;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("@Subscriber !!! args -> {0}", args[0]);

            string[] arguments = args[0].Split(';');//arguments[0]->port; arguments[1]->url; arguments[2]->nome; arguments[3]->site;
            
            TcpChannel channel = new TcpChannel(Int32.Parse(arguments[0]));
            ChannelServices.RegisterChannel(channel, true);

            MPMSubImplementation subUnsub = new MPMSubImplementation(arguments[3],arguments[1],arguments[2],arguments[0]);
            RemotingServices.Marshal(subUnsub, "MPMSubUnsub", typeof(MPMSubImplementation));

            SubNotify notify = new SubNotify(arguments[2]);
            RemotingServices.Marshal(notify, "Notify", typeof(SubNotify));

            MPMSubscriberCmd processCmd = new MPMSubscriberCmd();
            RemotingServices.Marshal(processCmd, "MPMProcessCmd", typeof(MPMSubscriberCmd));

            Console.ReadLine();
        }

    }

    class MPMSubImplementation : MarshalByRefObject, SubInterface
    {
        private string site;
        private string url;
        private string nome;
        private string myPort;

        // vida infinita !!!!
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public MPMSubImplementation(string p1, string p2, string p3,string p4)
        {
            this.site = p1;
            this.url = p2;
            this.nome = p3;
            this.myPort = p4;
        }
        public void subscribe(string topic)
        {
            string urlRemote = url.Substring(0, url.Length - 15);//retirar XXXX/subscriber
            string myURL = urlRemote + myPort;

            Console.WriteLine("subscribing on topic {0} o meu url e {1}", topic, myURL);

            int port = 9000;

            int mult = Int32.Parse("" + site[site.Length - 1]);
            urlRemote += (port + (mult * 100) + 1).ToString() + "/";

            BrokerReceiveBroker subunsub = (BrokerReceiveBroker)Activator.GetObject(typeof(BrokerReceiveBroker), urlRemote+"BrokerCommunication");
            subunsub.receiveSub(topic, myURL);
        }

        public void unsubscribe(string topic)
        {
            string urlRemote = url.Substring(0, url.Length - 15);//retirar XXXX/subscriber
            string myURL = urlRemote + myPort;

            Console.WriteLine("unsubscribing on topic {0} o meu url e {1}", topic, myURL);

            int port = 9000;
            int mult = Int32.Parse(""+site[site.Length - 1]);
            urlRemote += (port + (mult * 100) + 1).ToString() + "/";

            BrokerReceiveBroker subunsub = (BrokerReceiveBroker)Activator.GetObject(typeof(BrokerReceiveBroker), urlRemote + "BrokerCommunication");
            subunsub.receiveUnsub(topic, myURL);
        }
    }

    class SubNotify : MarshalByRefObject, SubscriberNotify {

        private string name;

        public SubNotify(string n) {
            this.name = n;
        }

        public void notify(Message m) {
            Console.WriteLine("@SubNotify received a notification on topic {0} ----> {1}", m.Topic,m.Content);
            LogInterface log = (LogInterface)Activator.GetObject(typeof(LogInterface), "tcp://localhost:8086/PuppetMasterLog");
            log.log(this.name, m.author, m.Topic, 0, "subscriber");
        }
    }

    public class MPMSubscriberCmd : MarshalByRefObject, IProcessCmd
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

