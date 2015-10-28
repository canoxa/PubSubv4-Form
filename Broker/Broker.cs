using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace PubSub
{

    public class Broker
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

        public Broker(string u, string n, string s)
        {
            URL = u;
            Name = n;
            Site = s;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("@broker !!! porto -> {0}", args[0]);

            string[] arguments = args[0].Split(';');//arguments[0]->port; arguments[1]->url; arguments[2]->nome; arguments[3]->site;

            int vizinhos = arguments.Length - 5;//numero de vizinhos
            List<Broker> lstaux = new List<Broker>();
            //iniciar lista de vizinhos
            for (int i = 4; i < vizinhos + 4; i++) {
                string[] atr = arguments[i].Split('%');//atr[0]-site, atr[1]-url, atr[2]-nome
                Broker b = new Broker(atr[1], atr[2], atr[0]);
                lstaux.Add(b);
            }

            TcpChannel channel = new TcpChannel(Int32.Parse(arguments[0]));
            ChannelServices.RegisterChannel(channel, true);
            
            BrokerCommunication brokerbroker = new BrokerCommunication(lstaux, arguments[2]);
            RemotingServices.Marshal(brokerbroker, "BrokerCommunication", typeof(BrokerCommunication));

            MPMBrokerCmd processCmd = new MPMBrokerCmd();
            RemotingServices.Marshal(processCmd, "MPMProcessCmd", typeof(MPMBrokerCmd));

            Console.ReadLine();
        }
    }

    //IMPLEMENTATIONS

    public class BrokerCommunication : MarshalByRefObject, BrokerReceiveBroker
    {
        // vida infinita !!!!
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public delegate void FilterFloodRemoteAsyncDelegate(Message m, string t);
        public delegate void SubUnsubRemoteAsyncDelegate(string t, string n);

        // This is the call that the AsyncCallBack delegate will reference.
        public static void FilterFloodRemoteAsyncCallBack(IAsyncResult ar)
        {
            FilterFloodRemoteAsyncDelegate del = (FilterFloodRemoteAsyncDelegate)((AsyncResult)ar).AsyncDelegate;
            return;
        }

        public static void SubUnsubRemoteAsyncCallBack(IAsyncResult ar)
        {
            SubUnsubRemoteAsyncDelegate del = (SubUnsubRemoteAsyncDelegate)((AsyncResult)ar).AsyncDelegate;
            return;
        }

        private string name;
        private List<Broker> lstVizinhos;
        private Dictionary<string, List<string>> lstSubsTopic; //quem subscreveu neste no, a quE
        private Dictionary<Broker, List<string>> routingTable; //vizinho,subscrições atingiveis atraves desse vizinho
        private Dictionary<string, int> pubCount; //numero de seq de mensagem que estamos espera
        private List<Message> lstMessage;

        public BrokerCommunication(List<Broker> lst, string n)
        {
            name = n;
            lstSubsTopic = new Dictionary<string, List<string>>();
            routingTable = new Dictionary<Broker, List<string>>();
            lstVizinhos = lst;
            pubCount = new Dictionary<string, int>();
            lstMessage = new List<Message>();
        }


        public void forwardFlood(Message m, string brokerName)
        {

            //Console.WriteLine("Inicio num novo broker vindo de ->> {0}", brokerName);

            foreach (KeyValuePair<string, List<string>> t in lstSubsTopic)
            {
                foreach (var sub in t.Value)//percorrer lista de subs
                {
                    if (m.Topic.Equals(sub))
                    {
                        SubscriberNotify not = (SubscriberNotify)Activator.GetObject(typeof(SubscriberNotify), t.Key + "/Notify");
                        not.notify(m);
                    }
                }
            }

            List<Broker> lst = new List<Broker>(lstVizinhos);
            //eliminar remetente da lista
            for (int i = 0; i < lst.Count; i++)
            {
                if (lst[i].Name.Equals(brokerName))
                {
                    //Console.WriteLine("Removi o {0} da lista de vizinhos", lst[i].Name);
                    lst.Remove(lst[i]);
                }
            }

            //propagar para os outros todos
            foreach (var viz in lst)
            {
                string urlRemote = viz.URL.Substring(0, viz.URL.Length - 11);//retirar XXXX/broker
                int port = 9000;

                int mult = Int32.Parse("" + viz.Site[viz.Site.Length - 1]);
                urlRemote += (port + (mult * 100) + 1).ToString() + "/";

                //Console.WriteLine("Flooding vizinho em {0}", urlRemote);
                BrokerReceiveBroker bro = (BrokerReceiveBroker)Activator.GetObject(typeof(BrokerReceiveBroker), urlRemote + "BrokerCommunication");
                try
                {
                    FilterFloodRemoteAsyncDelegate RemoteDel = new FilterFloodRemoteAsyncDelegate(bro.forwardFlood);
                    AsyncCallback RemoteCallBack = new AsyncCallback(FilterFloodRemoteAsyncCallBack);
                    IAsyncResult RemAr = RemoteDel.BeginInvoke(m, name, RemoteCallBack, null);

                    LogInterface log = (LogInterface)Activator.GetObject(typeof(LogInterface), "tcp://localhost:8086/PuppetMasterLog");
                    log.log(this.name, m.author, m.Topic, 0,"broker");

                }
                catch (SocketException)
                {
                    Console.WriteLine("Could not locate server");
                }
            }


        }
        public void forwardFilter(Message m, string brokerName)
        {
            Console.WriteLine("ForwardFilter received from broker -> {0}", brokerName);

            foreach (KeyValuePair<string, List<string>> t in lstSubsTopic)
            {
                foreach (var sub in t.Value)//percorrer lista de subs
                {
                    if (m.Topic.Equals(sub))
                    {
                        Console.WriteLine("Eu notifiquie o sub interessado");
                        SubscriberNotify not = (SubscriberNotify)Activator.GetObject(typeof(SubscriberNotify), t.Key + "/Notify");
                        not.notify(m);
                    }
                }
            }

            Dictionary<Broker, List<string>> lst = new Dictionary<Broker, List<string>>(routingTable);
            //eliminar remetente da routing table
            foreach (KeyValuePair<Broker, List<string>> par in routingTable)
            {
                if (par.Key.Name.Equals(brokerName))
                {
                    lst.Remove(par.Key);
                }
            }

            foreach (KeyValuePair<Broker, List<string>> t in routingTable)
            {
                if (t.Value.Contains(m.Topic))
                { //ha alguem na routing table que quer este topico
                    string urlRemote = t.Key.URL.Substring(0, t.Key.URL.Length - 11);//retirar XXXX/broker
                    int port = 9000;

                    int mult = Int32.Parse("" + t.Key.Site[t.Key.Site.Length - 1]);
                    urlRemote += (port + (mult * 100) + 1).ToString() + "/";

                    //Console.WriteLine("Filtering vizinho em {0}", urlRemote);
                    BrokerReceiveBroker bro = (BrokerReceiveBroker)Activator.GetObject(typeof(BrokerReceiveBroker), urlRemote + "BrokerCommunication");
                    try
                    {
                        FilterFloodRemoteAsyncDelegate RemoteDel = new FilterFloodRemoteAsyncDelegate(bro.forwardFilter);
                        AsyncCallback RemoteCallBack = new AsyncCallback(FilterFloodRemoteAsyncCallBack);
                        IAsyncResult RemAr = RemoteDel.BeginInvoke(m, name, RemoteCallBack, null);

                        LogInterface log = (LogInterface)Activator.GetObject(typeof(LogInterface), "tcp://localhost:8086/PuppetMasterLog");
                        log.log(this.name, m.author, m.Topic, 0, "broker");

                    }
                    catch (SocketException)
                    {
                        Console.WriteLine("Could not locate server");
                    }
                }
            }

        }
        public void forwardSub(string topic, string brokerName)
        {

            //buscar broker com nome brokerName
            foreach (var v in lstVizinhos)
            {
                if (v.Name.Equals(brokerName))
                {
                    Broker aux = v;
                    if (routingTable.ContainsKey(aux))//ja tenho uma entrada para este broker
                    {
                        if (!routingTable[aux].Contains(topic))//adicionar apenas se for outro topico
                        {
                            routingTable[aux].Add(topic);

                        }
                    }
                    else
                    {
                        routingTable[aux] = new List<string> { topic };
                    }


                }
            }

            List<Broker> lst = new List<Broker>(lstVizinhos);
            //eliminar remetente da lista
            for (int i = 0; i < lst.Count; i++)
            {
                if (lst[i].Name.Equals(brokerName))
                {
                    //Console.WriteLine("Removi o {0} da lista de vizinhos", lst[i].Name);
                    lst.Remove(lst[i]);
                }
            }

            //propagar para os outros todos
            foreach (var viz in lst)
            {
                string urlRemote = viz.URL.Substring(0, viz.URL.Length - 11);//retirar XXXX/broker
                int port = 9000;

                int mult = Int32.Parse("" + viz.Site[viz.Site.Length - 1]);
                urlRemote += (port + (mult * 100) + 1).ToString() + "/";

                //Console.WriteLine("Flooding vizinho em {0}", urlRemote);
                BrokerReceiveBroker bro = (BrokerReceiveBroker)Activator.GetObject(typeof(BrokerReceiveBroker), urlRemote + "BrokerCommunication");
                try
                {
                    SubUnsubRemoteAsyncDelegate RemoteDel = new SubUnsubRemoteAsyncDelegate(bro.forwardSub);
                    AsyncCallback RemoteCallBack = new AsyncCallback(SubUnsubRemoteAsyncCallBack);
                    IAsyncResult RemAr = RemoteDel.BeginInvoke(topic, name, RemoteCallBack, null);

                }
                catch (SocketException)
                {
                    Console.WriteLine("Could not locate server");
                }
            }

        }
        public void forwardUnsub(string topic, string brokerName)
        {
            //Console.WriteLine("unsub on topic {0} received from {1}", topic, brokerName);

            //buscar broker com nome brokerName
            foreach (var v in lstVizinhos)
            {
                if (v.Name.Equals(brokerName))
                {
                    Broker aux = v;
                    if (routingTable.ContainsKey(aux))//ja tenho uma entrada para este broker
                    {
                        routingTable[aux].Remove(topic);
                    }
                }
            }

            List<Broker> lst = new List<Broker>(lstVizinhos);
            //eliminar remetente da lista
            for (int i = 0; i < lst.Count; i++)
            {
                if (lst[i].Name.Equals(brokerName))
                {
                    //Console.WriteLine("Removi o {0} da lista de vizinhos", lst[i].Name);
                    lst.Remove(lst[i]);
                }
            }

            //propagar para os outros todos
            foreach (var viz in lst)
            {
                string urlRemote = viz.URL.Substring(0, viz.URL.Length - 11);//retirar XXXX/broker
                int port = 9000;

                int mult = Int32.Parse("" + viz.Site[viz.Site.Length - 1]);
                urlRemote += (port + (mult * 100) + 1).ToString() + "/";

                //Console.WriteLine("Flooding vizinho em {0}", urlRemote);
                BrokerReceiveBroker bro = (BrokerReceiveBroker)Activator.GetObject(typeof(BrokerReceiveBroker), urlRemote + "BrokerCommunication");
                try
                {
                    SubUnsubRemoteAsyncDelegate RemoteDel = new SubUnsubRemoteAsyncDelegate(bro.forwardUnsub);
                    AsyncCallback RemoteCallBack = new AsyncCallback(SubUnsubRemoteAsyncCallBack);
                    IAsyncResult RemAr = RemoteDel.BeginInvoke(topic, name, RemoteCallBack, null);

                }
                catch (SocketException)
                {
                    Console.WriteLine("Could not locate server");
                }

            }
        }
        public void receiveSub(string topic, string subName)
        {
            //Console.WriteLine("sub on topic {0} received from subscriber -> {1}", topic, subName);
            if (lstSubsTopic.ContainsKey(subName))
            {
                //Console.WriteLine("Ja tinha uma subs para este tipo");
                lstSubsTopic[subName].Add(topic);
            }
            else
            {
                //Console.WriteLine("Nao tinha nenhuma sub para este tipo");
                lstSubsTopic[subName] = new List<string> { topic };
            }
            forwardSub(topic, name);
        }
        public void receiveUnsub(string topic, string subName)
        {
            //Console.WriteLine("unsub on topic {0} received from {1}", topic, subName);
            if (lstSubsTopic.ContainsKey(subName))
            {
                lstSubsTopic[subName].Remove(topic);
            }
            forwardUnsub(topic, name);
        }

        public void receivePublication(Message m, string pubName, int filter,int order)
        {
            if (order == 1)//FIFO
            {
                List<Message> toSend = new List<Message>();

                if (pubCount.ContainsKey(m.author)) // se já existir o publisher
                {
                    if (m.SeqNum == pubCount[m.author])// msg esperada
                    {
                        toSend.Add(m);
                        pubCount[m.author]++;
                        for (int i = 0; i < lstMessage.Count; i++)
                        {
                            // se a msg na lista de espera for a do publisher
                            if (lstMessage[i].author.Equals(m.author))
                            {
                                //se a msg seginte estiver na lista
                                if (lstMessage[i].SeqNum == pubCount[m.author])
                                {
                                    toSend.Add(lstMessage[i]);
                                    pubCount[m.author]++;
                                    lstMessage.Remove(lstMessage[i]);
                                    i--;
                                }
                                // a msg do publisher não é a seguinte -> mover para o fim da lista
                                else
                                {
                                    Message msgAux = lstMessage[i];
                                    lstMessage.Remove(lstMessage[i]);
                                    lstMessage.Add(msgAux);
                                }

                            }
                        }
                    }
                    else
                    {
                        lstMessage.Add(m);
                    }
                }
                else
                {
                    pubCount.Add(m.author, 1);
                    if (m.SeqNum == 1)
                    {
                        pubCount[m.author]++;
                        toSend.Add(m);
                    }
                    else
                    {
                        lstMessage.Add(m);
                    }
                }

                foreach (Message msg in toSend)
                {
                    //Console.WriteLine("Vou enviar {0} mensagens - estou a enviar a msg com o seqNum {1}", toSend.Count, msg.SeqNum);
                    if (filter == 0)
                    {
                        forwardFlood(msg, name);
                    }
                    else
                    {
                        forwardFilter(msg, name);
                    }
                }
            }
            else {
                if (filter == 0)
                {
                    forwardFlood(m, name);
                }
                else
                {
                    forwardFilter(m, name);
                }
            }
        }
    }

    public class MPMBrokerCmd : MarshalByRefObject, IProcessCmd
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
