using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace PubSub
{
    class Scanner
    {

        //estruturas para optimizar procura
        private Dictionary<string, TreeNode> site_treeNode = new Dictionary<string, TreeNode>();
        private Dictionary<TreeNode, Broker> node_broker = new Dictionary<TreeNode, Broker>();
        private Dictionary<string, int> pname_port = new Dictionary<string, int>();
        private Dictionary<string, int> site_port = new Dictionary<string, int>();
        private int routing = 0;//defaul flood -> flood(0), filter(1)
        private int order = 1;//default FIFO -> NO(0), FIFO(1), TOTAL(2)
        private int logMode = 0;//default light -> light(0), full(1)


        public int getRouting() { return this.routing; }
        public int getOrder() { return this.order; }
        public int getLogMode() { return this.logMode; }

        public Dictionary<string, int> getPname_port()
        {
            return this.pname_port;
        }

        public Dictionary<string, int> getSite_port()
        {
            return this.site_port;
        }
        
        public Dictionary<string, TreeNode> getSite_Node()
        {
            return this.site_treeNode;
        }
        public Dictionary<TreeNode, Broker> getNode_Broker()
        {
            return this.node_broker;
        }


        //actualiza site_node
        public TreeNode getRootNodeFromFile(string path)
        {
            string[] lines = System.IO.File.ReadAllLines(path);
            foreach (string line in lines)
            {
                if (line.Contains("Parent") && line.Contains("none"))
                {
                    string[] words = line.Split(' ');
                    TreeNode root = new TreeNode(words[1]);
                    site_treeNode.Add(words[1], root);
                    return root;
                }
            }
            return null; //em principio nao chega aqui
        }


        public void quickRead(string v, TreeNode root)
        {
            string[] lines = System.IO.File.ReadAllLines(v);
            int siteCount = 0;
            foreach (string line in lines)
            {
                if (line.Contains("LoggingLevel"))
                {
                    string[] words = line.Split(' ');//words[1] - metodo de log
                    if (words[1].Equals("full"))
                    {
                        logMode = 1;
                    }
                }
                if (line.Contains("RoutingPolicy"))
                {
                    string[] words = line.Split(' ');//words[1] - metodo de routing
                    if(words[1].Equals("filter")){
                           routing = 1;
                    }
                }
                 if (line.Contains("Ordering"))
                {
                    string[] words = line.Split(' ');//words[1] - metodo de ordem
                    if(words[1].Equals("NO")){
                           order = 0;
                    }
                    if (words[1].Equals("TOTAL"))
                    {
                        order = 2;
                    }
                }
                if (line.Contains("Parent")){
                    string[] words = line.Split(' ');//words[1]-filho, words[3]-pai
                    int mult = 9000 + (siteCount*100);
                    site_port.Add(words[1], mult);
                    siteCount++;
                }
                if (line.Contains("Is broker"))
                {
                    string[] words = line.Split(' '); //words[1]-name, words[5]-site, words[7]-url
                    TreeNode t = site_treeNode[words[5]];

                    //actualizar estruturas
                    pname_port[words[1]] = site_port[words[5]] + 1;
                    site_port[words[5]]++;

                    Broker aux = new Broker(words[1], words[5], words[7]);
                    node_broker.Add(t, aux);
                }
                if (line.Contains("Is publisher"))
                {
                    string[] words = line.Split(' '); //words[1]-name, words[5]-site, words[7]-url
                    TreeNode t = site_treeNode[words[5]];

                    //actualizar
                    pname_port[words[1]] = site_port[words[5]] + 1;
                    site_port[words[5]]++;
                }
                if (line.Contains("Is subscriber"))
                {
                    string[] words = line.Split(' '); //words[1]-name, words[5]-site, words[7]-url
                    TreeNode t = site_treeNode[words[5]];

                    //actualizar
                    pname_port[words[1]] = site_port[words[5]] + 1;
                    site_port[words[5]]++;
                }
            }
        }

        //actualiza node_broker + site_name
        public List<MyProcess> fillProcessList(string v, TreeNode root)
        {
            PuppetInterface myremote;

            string[] lines = System.IO.File.ReadAllLines(v);
            List<MyProcess> res = new List<MyProcess>();

            foreach (string line in lines)
            {
                if (line.Contains("Is broker"))
                {
                    string[] words = line.Split(' '); //words[1]-name, words[5]-site, words[7]-url
                    TreeNode t = site_treeNode[words[5]];

                    fillVizinhos(t);

                    string urlService = words[7].Substring(0, words[7].Length - 6);
                    
                    myremote = (PuppetInterface)Activator.GetObject(typeof(PuppetInterface),urlService+"PuppetMasterURL");
                    myremote.createProcess(t, "broker", words[1], words[5], words[7]);
                }
                if (line.Contains("Is publisher"))
                {
                    string[] words = line.Split(' '); //words[1]-name, words[5]-site, words[7]-url
                    TreeNode t = site_treeNode[words[5]];

                    string urlService = words[7].Substring(0, words[7].Length - 9);

                    myremote = (PuppetInterface)Activator.GetObject(typeof(PuppetInterface), urlService + "PuppetMasterURL");
                    myremote.createProcess(t,"publisher", words[1], words[5], words[7]);

                }
                if (line.Contains("Is subscriber"))
                {
                    string[] words = line.Split(' '); //words[1]-name, words[5]-site, words[7]-url
                    TreeNode t = site_treeNode[words[5]];

                    string urlService = words[7].Substring(0, words[7].Length - 10);

                    myremote = (PuppetInterface)Activator.GetObject(typeof(PuppetInterface), urlService + "PuppetMasterURL");
                    myremote.createProcess(t,"subscriber", words[1], words[5], words[7]);

                }
            }
            return res;
        }

        private void fillVizinhos(TreeNode t)
        {
            string brokerName;
            string info;
            if (t.Parent != null)//root nao tem PAI
            {
                brokerName = node_broker[t.Parent].Name;
                info = node_broker[t.Parent].Site + "%" + node_broker[t.Parent].URL;
                t.getVizinhos().Add(brokerName, info);
            }

            foreach (var f in t.GetChildren()) {
                brokerName = node_broker[f].Name;
                info = node_broker[f].Site + "%" + node_broker[f].URL;
                t.getVizinhos().Add(brokerName, info);
            }

        }

        //actualiza-se o site_node aqui
        public void readTreeFromFile(TreeNode root, string path)
        {
            string[] lines = System.IO.File.ReadAllLines(path);
            foreach (string line in lines)
            {
                if (line.Contains("Parent") && !line.Contains("none"))
                {
                    string[] words = line.Split(' '); //words[1]-filho, words[3]-pai

                    if (words[3].Equals(root.ID)) //root e o pai
                    {
                        TreeNode aux = new TreeNode(words[1]);
                        root.AddChild(aux);
                        site_treeNode.Add(words[1], aux);
                    }
                    else
                    { //temos de encontrar o pai, comecando a procura nos filhos do root
                        find(root, words[1], words[3]);
                    }
                }
            }
        }

        //actualiza site_node ( readTreeFromFile() )
        private void find(TreeNode no, string filho, string pai)
        {
            List<TreeNode> filhos = no.GetChildren();
            if (filhos != null)
            {
                foreach (var child in filhos)
                {
                    if (child.ID.Equals(pai))
                    { //child e o pai que estavamos a procura
                        TreeNode aux = new TreeNode(filho);
                        child.AddChild(aux);
                        site_treeNode.Add(filho, aux);
                    }
                }
                //pai nao esta nos filhos de "no"
                foreach (var newnode in filhos)
                { //tentar encontrar pai comecando a procura em cada filho de "no"
                    find(newnode, filho, pai);
                }
            }
        }

        

        private Broker findBroker(string site)
        {
            return node_broker[site_treeNode[site]];
        }
    }
}
