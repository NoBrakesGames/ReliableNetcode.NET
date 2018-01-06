using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ReliableNetcode.Utils;

namespace ReliableNetcode {
    // an unreliable-ordered implementation of MessageChannel
    // does not make any guarantees that a message will arrive, BUT does guarantee that messages will be received in chronological order
    internal class UnreliableOrderedMessageChannel : MessageChannel {
        public override int ChannelID {
            get {
                return (int)QosType.UnreliableOrdered;
            }
        }

        public override float RTT => packetController.RTT;

        public override float PacketLoss => packetController.PacketLoss;

        public override float SentBandwidthKBPS => packetController.SentBandwidthKBPS;

        public override float ReceivedBandwidthKBPS => packetController.ReceivedBandwidthKBPS;

        private ReliableConfig config;
        private ReliablePacketController packetController;

        private ushort nextSequence = 0;

        public UnreliableOrderedMessageChannel() {
            config = ReliableConfig.DefaultConfig();
            config.TransmitPacketCallback = (buffer, size) => {
                TransmitCallback(buffer, size);
            };
            config.ProcessPacketCallback = processPacket;

            packetController = new ReliablePacketController(config, DateTime.Now.GetTotalSeconds());
        }

        public override void Reset() {
            nextSequence = 0;
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

        protected void processPacket(ushort sequence, byte[] buffer, int length) {
            // only process a packet if it is the next packet we expect, or it is newer.
            if (sequence == nextSequence || PacketIO.SequenceGreaterThan(sequence, nextSequence)) {
                nextSequence = (ushort)(sequence + 1);
                ReceiveCallback(buffer, length);
            }
        }
    }
}