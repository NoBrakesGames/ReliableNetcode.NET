using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ReliableNetcode.Utils;

namespace ReliableNetcode
{
    internal abstract class MessageChannel
    {
        public abstract int ChannelID { get; }

        public Action<byte[], int> TransmitCallback;
        public Action<byte[], int> ReceiveCallback;

        public abstract void Reset();
        public abstract void Update(double newTime);
        public abstract void ReceivePacket(byte[] buffer, int bufferLength);
        public abstract void SendMessage(byte[] buffer, int bufferPosition, int bufferLength);

        public abstract float RTT { get; }
        public abstract float PacketLoss { get; }
        public abstract float SentBandwidthKBPS { get; }
        public abstract float ReceivedBandwidthKBPS { get; }
    }
}