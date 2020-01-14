﻿using QuickNet.Utilities;
using QuicNet;
using QuicNet.Connections;
using QuicNet.Context;
using QuicNet.Infrastructure;
using QuicNet.Infrastructure.Frames;
using QuicNet.Infrastructure.PacketProcessing;
using QuicNet.Infrastructure.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickNet.Tests.ConsoleServer
{
    class Program
    {
        static void Example()
        {
            QuicListener listener = new QuicListener(11000);
            listener.Start();

            while (true)
            {
                try
                {
                    // Blocks while waiting for a connection
                    QuicConnection client = listener.AcceptQuicClient();

                    // Assign an action when a data is received from that client.
                    client.OnDataReceived += (c) =>
                    {

                        byte[] data = c.Data;

                        Console.WriteLine("Data received: " + Encoding.UTF8.GetString(data));

                        c.Send(Encoding.UTF8.GetBytes("Echo!"));
                    };
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        static void Main(string[] args)
        {
            Example();

            byte[] bytes = new VariableInteger(12345);
            VariableInteger integer = bytes;
            UInt64 uinteger = integer;
            int size = VariableInteger.Size(bytes[0]);

            InitialPacket packet = new InitialPacket()
            {
                Version = 16,
                SourceConnectionId = 124,
                DestinationConnectionId = 0,
                PacketNumber = 777521,
                TokenLength = 0
            };

            packet = new InitialPacketCreator().CreateInitialPacket(124, 0);

            ConnectionCloseFrame frame = new ConnectionCloseFrame(ErrorCode.SERVER_BUSY, "The server is too busy to process your request.");
            MaxStreamsFrame msidframe = new MaxStreamsFrame(144123, StreamType.ClientUnidirectional);
            //packet.AttachFrame(frame);
            packet.AttachFrame(msidframe);

            byte[] data = packet.Encode();
            string b64 = ToBase64(data);

            byte[] shpdata1 = new byte[] { 1, 1, 2, 3, 5, 8 };
            byte[] shpdata2 = new byte[] { 13, 21, 34, 55, 89, 144 };

            ShortHeaderPacket shp = new ShortHeaderPacket();
            shp.DestinationConnectionId = 124;
            shp.PacketNumber = 2;

            shp.AttachFrame(new StreamFrame() { StreamId = 1, Length = new VariableInteger((UInt64)shpdata2.Length), StreamData = shpdata2, Offset = 6, EndOfStream = true });
            shp.AttachFrame(new StreamFrame() { StreamId = 1, Length = new VariableInteger((UInt64)shpdata1.Length), StreamData = shpdata1, Offset = 0 });

            string shpb64 = ToBase64(shp.Encode());

            packet.Decode(data);

            byte[] ccfData = frame.Encode();
            frame.Decode(new ByteArray(ccfData));

            byte[] streamIdData = new StreamId(123, StreamType.ClientUnidirectional);
            StreamId streamId = streamIdData;

            QuicListener listener = new QuicListener(11000);
            // listener.OnClientConnected += Listener_OnClientConnected;
            listener.Start();
        }

        //private static void Listener_OnClientConnected(QuicContext obj)
        //{
        //    System.Console.WriteLine("Client connected.");
        //    obj.OnDataReceived += Obj_OnDataReceived;
        //}

        private static void Obj_OnDataReceived(QuicStreamContext obj)
        {
            System.Console.WriteLine("Data received");
            foreach (byte b in obj.Data)
            {
                System.Console.Write(string.Format("{0},", b));
            }

            // Echo back to the client
            obj.Send(Encoding.UTF8.GetBytes("Echo!"));
        }

        static string ToBase64(byte[] data)
        {
            return Convert.ToBase64String(data);
        }
    }
}
