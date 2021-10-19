﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Tevux.Protocols.Mqtt;

namespace TestApp {
    public class HomieTopicFetcher {
        private MqttClient _mqttClient = new MqttClient();
        private Dictionary<string, string> _responses = new Dictionary<string, string>();
        private ChannelConnectionOptions _channelConnectionOptions;
        private DateTime _lastMessageTimestamp = DateTime.Now;


        public void Initialize(ChannelConnectionOptions channelOptions) {
            _channelConnectionOptions = channelOptions;
            _mqttClient.Initialize();

            _mqttClient.PublishReceived += HandlePublishReceived;
        }

        public void FetchTopics(string filter, out string[] topics) {
            _responses.Clear();
            _mqttClient.ConnectAndWait(_channelConnectionOptions);

            _mqttClient.Subscribe(filter, QosLevel.AtLeastOnce);

            Thread.Sleep(5000);
            //_mqttClient.Unsubscribe(filter);
            //_mqttClient.Disconnect();

            topics = new string[_responses.Count];
            var i = 0;
            foreach (var response in _responses) {
                topics[i] = response.Key + ":" + response.Value;
                i++;
            }

            //while (_mqttClient.IsConnected) { Thread.Sleep(100); }
        }

        public void FetchDevices(string baseTopic, out string[] topics) {
            var allTheTopics = new List<string>();

            _mqttClient.ConnectAndWait(_channelConnectionOptions);

            _responses.Clear();
            _lastMessageTimestamp = DateTime.Now;
            _mqttClient.Subscribe($"{baseTopic}/+/$homie", QosLevel.AtLeastOnce);
            Thread.Sleep(50);
            _mqttClient.Unsubscribe($"{baseTopic}/+/$homie");

            while ((DateTime.Now - _lastMessageTimestamp).TotalMilliseconds < 500) {
                Thread.Sleep(100);
            }

            Console.WriteLine($"Found {_responses.Count} homie devices.");
            var devices = new List<string>();
            foreach (var deviceTopic in _responses) {
                var deviceName = deviceTopic.Key.Split('/')[1];
                devices.Add(deviceName);
                Console.Write(deviceName + " ");
            }
            Console.WriteLine();

            foreach (var device in devices) {
                _responses.Clear();
                _lastMessageTimestamp = DateTime.Now;
                _mqttClient.SubscribeAndWait($"{baseTopic}/{device}/#", QosLevel.AtLeastOnce);
                //Thread.Sleep(50);

                while ((DateTime.Now - _lastMessageTimestamp).TotalMilliseconds < 500) {
                    Thread.Sleep(100);
                }

                _mqttClient.UnsubscribeAndWait($"{baseTopic}/{device}/#");
                // Thread.Sleep(500);

                Console.WriteLine($"{_responses.Count} topics for {device}.");
                foreach (var topic in _responses) {
                    allTheTopics.Add(topic.Key + ":" + topic.Value);
                }

            }

            _mqttClient.Disconnect();

            topics = allTheTopics.ToArray();

            while (_mqttClient.IsConnected) { Thread.Sleep(100); }
        }

        private void HandlePublishReceived(object sender, PublishReceivedEventArgs e) {
            var payload = Encoding.UTF8.GetString(e.Message);

            if (_responses.ContainsKey(e.Topic) == false) {
                _responses.Add(e.Topic, payload);
                _lastMessageTimestamp = DateTime.Now;
            }
            else {
                _responses[e.Topic] = payload;
            }
        }
    }
}