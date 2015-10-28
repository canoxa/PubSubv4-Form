﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubSub
{
    public interface PubInterface {//usada pelo MPM

        void publish(string number,string topic, string secs,int filter,int order);

    }

    public interface LogInterface
    {//usada pelo MPM

        void log(string selfName, string pubName, string topicName, int eventNumber,string id);

    }

    public interface SubInterface {//usado pelo MPM

        void subscribe(string topic);
        void unsubscribe(string topic);

    }

    public interface PuppetInterface {//usada pelo MPM
        void createProcess(TreeNode t, string role, string n, string s, string u);
    }

    public interface SubscriberNotify {//usada pelo Broker
        void notify(Message m);
    }

    public interface BrokerReceivePub {//usada pelo Pub
        void receivePublication(Message m, string pubName);
    }

    public interface BrokerReceiveSubUnSub//usada pelo Sub
    {
        void receiveSub(string topic, string subName);
        void receiveUnsub(string topic, string subName);
    }

    public interface BrokerReceiveBroker//usada pelo Broker - forward&filter passou de broker para nome do broker
    {
        void forwardFlood(Message m,string brokerName);
        void forwardFilter(Message m, string brokerName);
        void forwardSub(string topic,string brokerName);
        void forwardUnsub(string topic, string brokerName);
        void receiveSub(string topic, string subName);
        void receiveUnsub(string topic, string subName);
        void receivePublication(Message m, string pubName,int filter,int order);
    }

    public interface IProcessCmd // chamada pelo MPM - executa cmd crash, freeze, unfreeze
    {
        void crash();
        void freeze();
        void unfreeze();
    }
}
