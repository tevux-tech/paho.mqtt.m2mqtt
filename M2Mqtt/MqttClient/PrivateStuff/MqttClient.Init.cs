﻿/*
Copyright (c) 2013, 2014 Paolo Patierno

All rights reserved. This program and the accompanying materials
are made available under the terms of the Eclipse Public License v1.0
and Eclipse Distribution License v1.0 which accompany this distribution. 

The Eclipse Public License is available at 
   http://www.eclipse.org/legal/epl-v10.html
and the Eclipse Distribution License is available at 
   http://www.eclipse.org/org/documents/edl-v10.php.

Contributors:
   Paolo Patierno - initial API and implementation and/or initial documentation
*/

using System.Collections;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace uPLibrary.Networking.M2Mqtt {
    public partial class MqttClient {
        private void Init(string brokerHostName, int brokerPort, bool secure, X509Certificate caCert, X509Certificate clientCert, MqttSslProtocols sslProtocol,
            RemoteCertificateValidationCallback userCertificateValidationCallback,
            LocalCertificateSelectionCallback userCertificateSelectionCallback,
            List<string> alpnProtocols = null) {

            _brokerHostName = brokerHostName;
            _brokerPort = brokerPort;

            // reference to MQTT settings
            _settings = MqttSettings.Instance;
            // set settings port based on secure connection or not
            if (!secure) {
                _settings.UnsecurePort = _brokerPort;
            }
            else {
                _settings.SecurePort = _brokerPort;
            }

            // queue for handling inflight messages (publishing and acknowledge)
            _inflightWaitHandle = new AutoResetEvent(false);
            _inflightQueue = new Queue();

            // queue for received message
            _receiveEventWaitHandle = new AutoResetEvent(false);
            // _eventQueue = new Queue();
            _internalQueue = new Queue();

            // session
            _session = null;

            // create network channel
            _channel = new MqttNetworkChannel(_brokerHostName, _brokerPort, secure, caCert, clientCert, sslProtocol, userCertificateValidationCallback, userCertificateSelectionCallback, alpnProtocols);

            _pingStateMachine.Initialize(this);
            _connectStateMachine.Initialize(this);
            _unsubscribeStateMachine.Initialize(this);
            _subscribeStateMachine.Initialize(this);

            new Thread(() => {

                while (true) {
                    if (IsConnected) {
                        _unsubscribeStateMachine.Tick();
                        _subscribeStateMachine.Tick();
                    }
                    else {
                        _connectStateMachine.Tick();
                    }


                    Thread.Sleep(1000);
                }

            }).Start();
        }
    }
}