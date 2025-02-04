﻿/*
Copyright (c) 2021 Simonas Greicius

All rights reserved. This program and the accompanying materials
are made available under the terms of the Eclipse Public License v1.0
and Eclipse Distribution License v1.0 which accompany this distribution.

The Eclipse Public License is available at
   http://www.eclipse.org/legal/epl-v10.html
and the Eclipse Distribution License is available at
   http://www.eclipse.org/org/documents/edl-v10.php.

Contributors:
   Simonas Greicius - creation of state machine classes
*/

using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    /// <summary>
    /// This state machine handles the outgoing publish traffic for all QoS levels. 
    /// It sends PUBLISH and PUBREL, and processes incoming PUBACK, PUBREC and PUBCOMP.
    /// </summary>
    internal class OutgoingPublishStateMachine {
        private readonly ResendingStateMachine _qos1PublishQueue = new ResendingStateMachine();
        private readonly ResendingStateMachine _qos2PublishQueue = new ResendingStateMachine();
        private readonly ResendingStateMachine _qos2PubrelQueue = new ResendingStateMachine();
        private MqttClient _client;

        public void Initialize(MqttClient client) {
            _client = client;
            _qos1PublishQueue.Initialize(client);
            _qos2PublishQueue.Initialize(client);
            _qos2PubrelQueue.Initialize(client);
        }

        public void ProcessPacket(PubackPacket packet) {
            if (_qos1PublishQueue.TryFinalize(packet.PacketId, out var finalizedContext)) {
                PacketTracer.LogIncomingPacket(packet);
                NotifyPublishSucceeded(((PublishTransmissionContext)finalizedContext).OriginalPublishPacket);
            }
            else {
                // Not sure under what circumstances this may happen, but broker sent a duplicate Puback packet.
                // I think discarding the packet is just fine.
                NotifyRoguePacketReceived(packet);
            }
        }

        public void ProcessPacket(PubrecPacket packet) {
            var currentTime = Helpers.GetCurrentTime();

            if (_qos2PublishQueue.TryFinalize(packet.PacketId, out var finalizedContext)) {
                PacketTracer.LogIncomingPacket(packet);
                NotifyPublishSucceeded(((PublishTransmissionContext)finalizedContext).OriginalPublishPacket);
            }
            else {
                // Broker did not receive Pubrel packet in time, so it resent Pubrec, but the original Publish packet is no longer in the queue.
                // However, Pubrel for the original packet may still be in the sending queue.
                // If so, need to to pull it out and start Pubrel-Pubcomp workflow afresh.
                _ = _qos2PubrelQueue.TryFinalize(packet.PacketId, out var _);
                NotifyRoguePacketReceived(packet);
            }

            var pubrelPacket = new PubrelPacket(packet.PacketId);
            finalizedContext.PacketToSend = pubrelPacket;
            finalizedContext.AttemptNumber = 1;
            finalizedContext.Timestamp = currentTime;
            finalizedContext.IsFinished = false;
            finalizedContext.IsSucceeded = false;
            _qos2PubrelQueue.EnqueueAndSend(finalizedContext);
        }

        public void ProcessPacket(PubcompPacket packet) {
            if (_qos2PubrelQueue.TryFinalize(packet.PacketId, out _)) {
                // Ain't much to do here. QoS 2 sending is now complete.
                PacketTracer.LogIncomingPacket(packet);
            }
            else {
                NotifyRoguePacketReceived(packet);
            }
        }

        public void Publish(PublishPacket packet) {
            var currentTime = Helpers.GetCurrentTime();

            var context = new PublishTransmissionContext(packet, packet, currentTime);

            if (packet.QosLevel == QosLevel.AtMostOnce) {
                // Those are the best packets - just sending and waiting for no response!
                _client.Send(context.PacketToSend);
                PacketTracer.LogOutgoingPacket(packet);
            }
            else if (packet.QosLevel == QosLevel.AtLeastOnce) {
                _qos1PublishQueue.EnqueueAndSend(context);

                // Any subsequent Publish packet transmissions must be marked with Duplicate flag.
                packet.DuplicateFlag = true;
            }
            else if (packet.QosLevel == QosLevel.ExactlyOnce) {
                _qos2PublishQueue.EnqueueAndSend(context);

                // Any subsequent Publish packet transmissions must be marked with Duplicate flag.
                packet.DuplicateFlag = true;
            }
        }

        public void Reset() {
            _qos1PublishQueue.ResetOnNextTick();
            _qos2PublishQueue.ResetOnNextTick();
            _qos2PubrelQueue.ResetOnNextTick();
        }

        public void Tick() {
            _qos1PublishQueue.Tick();
            _qos2PublishQueue.Tick();
            _qos2PubrelQueue.Tick();
        }
        private void NotifyPublishSucceeded(PublishPacket packet) {
            _client.OnPacketAcknowledged(packet, null);
        }

        private void NotifyRoguePacketReceived(ControlPacketBase packet) {
            PacketTracer.LogIncomingPacket(packet, true);
        }
    }
}
