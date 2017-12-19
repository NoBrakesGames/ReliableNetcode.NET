using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ReliableNetcode.Utils;

namespace ReliableNetcode {
    // an unreliable implementation of MessageChannel
    // does not make any guarantees about message reliability except for ignoring duplicate messages
    internal class UnreliableMessageChannel : MessageChannel {
        public override int ChannelID {
            get {
                return (int)QosType.Unreliable;
            }
        }

        private ReliableConfig config;
        private ReliablePacketController packetController;
        private SequenceBuffer<ReceivedPacketData> receiveBuffer;

        public UnreliableMessageChannel() {
            receiveBuffer = new SequenceBuffer<ReceivedPacketData>(256);

            config = ReliableConfig.DefaultConfig();
            config.TransmitPacketCallback = (buffer, size) => {
                TransmitCallback(buffer, size);
            };
            config.ProcessPacketCallback = (seq, buffer, size) => {
                if (!receiveBuffer.Exists(seq)) {
                    receiveBuffer.Insert(seq);
                    ReceiveCallback(buffer, size);
                }
            };

            packetController = new ReliablePacketController(config, DateTime.Now.GetTotalSeconds());
        }

        public override void Reset() {
            packetController.Reset();
        }

        public override void Update(double newTime) {
            packetController.Update(newTime);
        }

        public override void ReceivePacket(byte[] buffer, int bufferLength) {
            packetController.ReceivePacket(buffer, bufferLength);
        }

        public override void SendMessage(byte[] buffer, int bufferPosition, int bufferLength) {
            packetController.SendPacket(buffer, bufferPosition, bufferLength, (byte)ChannelID);
        }
    }
}