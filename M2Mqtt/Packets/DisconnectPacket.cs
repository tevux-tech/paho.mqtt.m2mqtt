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

namespace Tevux.Protocols.Mqtt {
    /// <summary>
    /// Class for DISCONNECT packet from client to broker. See section 3.14.
    /// </summary>
    internal class DisconnectPacket : ControlPacketBase {
        public DisconnectPacket() {
            Type = PacketTypes.Disconnect;
        }

        public override byte[] GetBytes() {
            // Packet content is fixed, no variables here.
            var buffer = new byte[2];
            buffer[0] = 0b1110_0000;
            buffer[1] = 0;

            return buffer;
        }

        public override string ToString() {
            return GetTraceString("DISCONNECT", null, null);
        }
    }
}