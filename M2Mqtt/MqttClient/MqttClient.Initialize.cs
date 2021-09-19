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

namespace uPLibrary.Networking.M2Mqtt {
    public partial class MqttClient {
        public void Initialize() {
#warning Add secure communication
            _channel = new UnsecureTcpChannel();

            _pingStateMachine.Initialize(this);
            _connectStateMachine.Initialize(this);
            _unsubscribeStateMachine.Initialize(this);
            _subscribeStateMachine.Initialize(this);
            _outgoingPublishStateMachine.Initialize(this);
            _incomingPublishStateMachine.Initialize(this);

            Fx.StartThread(MasterTickThread);
            Fx.StartThread(ReceiveThread);

            _isInitialized = true;
        }
    }
}