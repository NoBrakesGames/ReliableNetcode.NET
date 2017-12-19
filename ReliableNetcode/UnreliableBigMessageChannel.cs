using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ReliableNetcode.Utils;

namespace ReliableNetcode {
    internal class UnreliableBigMessageChannel : MessageChannel {
        private const int mtu = 1400;

        public override int ChannelID {
            get {
                return (int)QosType.UnreliableBig;
            }
        }

        private ReliableConfig config;
        private ReliablePacketController packetController;

        private struct Fragment {
            public ushort sequence;
            public int fragmentIndex;
            public int lastFragmentIndex;
            public byte[] buffer;
            public int length;
        }

        private readonly Fragment[] fragments;

        public UnreliableBigMessageChannel() {
            config = ReliableConfig.DefaultConfig();
            config.TransmitPacketCallback = TransmitCallback;
            config.ProcessPacketCallback = processPacket;

            packetController = new ReliablePacketController(config, DateTime.Now.GetTotalSeconds());

            fragments = new Fragment[config.MaxFragments];
        }

        public override void ReceivePacket(byte[] buffer, int bufferLength) {
            packetController.ReceivePacket(buffer, bufferLength);
        }

        public override void Reset() {
            packetController.Reset();
        }

        public override void SendMessage(byte[] buffer, int bufferPosition, int bufferLength) {
            int lastFragmentIndex = bufferLength / mtu;
            for (int fragmentIndex = 0; fragmentIndex * mtu < bufferLength; ++fragmentIndex) {
                var fragmentBuffer = BufferPool.GetBuffer(2048);
                fragmentBuffer[0] = (byte)((fragmentIndex << 4) | lastFragmentIndex);
                int fragmentLength = Math.Min(mtu, bufferLength - fragmentIndex * mtu);
                Array.Copy(buffer, fragmentIndex * mtu, fragmentBuffer, 1, fragmentLength);
                packetController.SendPacket(fragmentBuffer, 0, fragmentLength + 1, (byte)ChannelID);
                BufferPool.ReturnBuffer(fragmentBuffer);
            }
        }

        public override void Update(double newTime) {
            packetController.Update(newTime);
        }

        protected void processPacket(ushort sequence, byte[] buffer, int length) {
            int i = Window(sequence);
            fragments[i].sequence = sequence;
            byte firstByte = buffer[0];
            fragments[i].fragmentIndex = firstByte >> 4;
            fragments[i].lastFragmentIndex = firstByte & 0xff;

            if (fragments[i].buffer == null) {
                fragments[i].buffer = BufferPool.GetBuffer(2048);
            }

            Array.Copy(buffer, 1, fragments[i].buffer, 0, length - 1);
            fragments[i].length = length - 1;
            
            int fragmentStart = Window(i - fragments[i].fragmentIndex);
            int fragmentEnd = Window(fragmentStart + fragments[i].lastFragmentIndex);

            bool complete = true;
            for (int j = fragmentStart; j <= fragmentEnd; j = Window(j + 1)) {
                if (fragments[j].buffer == null || fragments[i].sequence - fragments[i].fragmentIndex != fragments[j].sequence - fragments[j].fragmentIndex) {
                    complete = false;
                    break;
                }
            }

            if (complete) {
                var reassemblyBuffer = BufferPool.GetBuffer((fragments[i].lastFragmentIndex + 1) * 2048);
                int reassemblyOffset = 0;
                for (int j = fragmentStart; j <= fragmentEnd; j = Window(j + 1)) {
                    Array.Copy(fragments[j].buffer, 0, reassemblyBuffer, reassemblyOffset, fragments[j].length);
                    reassemblyOffset += fragments[j].length;
                    BufferPool.ReturnBuffer(fragments[i].buffer);
                    fragments[i].buffer = null;
                }

                ReceiveCallback(reassemblyBuffer, reassemblyOffset);
                BufferPool.ReturnBuffer(reassemblyBuffer);
            }
        }

        private int Window(int sequence) {
            return (fragments.Length + sequence % fragments.Length) % fragments.Length;
        }
    }
}