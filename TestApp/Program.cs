﻿using System;
using System.Text;
using System.Threading;
using Tevux.Protocols.Mqtt;
using Tevux.Protocols.Mqtt.Utility;

namespace TestApp {
    class Program {
        static void Main(string[] args) {
            Trace.TraceListener = (format, data) => { Console.WriteLine(format, data); };

            var myTest = new MyTest();

            Thread.Sleep(-1);
        }
    }

    public class MyTest {
        public MyTest() {

            var client = new MqttClient();
            client.Initialize();
            client.PublishReceived += HandlePublishReceived;
            client.Subscribed += HandleSubscribed;

            var networkOptions = new ChannelConnectionOptions();
            networkOptions.SetHostname("172.16.0.2");

            var brokerOptions = new MqttConnectionOptions();
            brokerOptions.SetClientId("TestApp");
            brokerOptions.SetRetransmissionParameters(3, 3);

            client.Connect(networkOptions, brokerOptions);

            client.Subscribe("temp/testapp", QosLevel.AtMostOnce);
            client.Subscribe("temp/test-publish2", QosLevel.ExactlyOnce);

            Thread.Sleep(1000);

            client.Publish("temp/test-publish0", Encoding.UTF8.GetBytes("That's a QOS 0 publish message."), QosLevel.AtMostOnce, false);
            client.Publish("temp/test-publish1", Encoding.UTF8.GetBytes("That's a QOS 1 publish message."), QosLevel.AtLeastOnce, false);
            client.Publish("temp/test-publish2", Encoding.UTF8.GetBytes("That's a QOS 2 publish message."), QosLevel.ExactlyOnce, false);

            Thread.Sleep(5000);

            client.Unsubscribe("temp/testapp");

            Thread.Sleep(1000);

            // client.Disconnect();
        }

        private void HandleSubscribed(object sender, SubscribedEventArgs e) {
            Console.WriteLine($"Subscribed: {e.Topic}:{e.GrantedQosLevel}");
            Thread.Sleep(5000);
            Console.WriteLine("Onomnom");
        }

        private void HandlePublishReceived(object sender, PublishReceivedEventArgs e) {

        }
    }
}
