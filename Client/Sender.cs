using Client.Messages;
using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Client
{
    class Sender
    {
        public Sender()
        {
            m_DataWriter = null;
            m_DataReader = null;
            m_Reader = new NetworkReader();
            m_Writer = new NetworkWriter();

            // Messages
            m_Handshake = new HandshakeMessage();
            m_Connected = new ConnectedMessage();
            m_Command = new CommandMessage();

            m_State = ConnectionState.Disconnected;
        }

        public ConnectionState State
        {
            get { return m_State; }
        }

        public void Open(IInputStream input, IOutputStream output)
        {
            m_DataReader = new DataReader(input);
            m_DataReader.ByteOrder = ByteOrder.LittleEndian;
            m_Reader.Reader = m_DataReader;

            m_DataWriter = new DataWriter(output);
            m_DataWriter.ByteOrder = ByteOrder.LittleEndian;
            m_Writer.Writer = m_DataWriter;
        }

        public void Close()
        {
            m_DataReader = null;
            m_Reader.Reader = null;

            m_DataWriter = null;
            m_Writer.Writer = null;

            m_State = ConnectionState.Disconnected;
        }

        public async Task<bool> Handshake()
        {
            bool success = false;

            // Send the handshake
            if (await m_Writer.Send(m_Handshake))
            {
                // Check the response
                MessageType type = await m_Reader.ReadHeader();
                if (type == MessageType.Handshake)
                {
                    success = m_Reader.ReadMessage(m_Handshake);
                }
            }
            else
            {
                Debug.WriteLine("[Sender.Handshake] Send failed");
            }

            return success;
        }

        public async Task<ConnectionState> TestConnection()
        {
            m_State = ConnectionState.Disconnected;

            if (await m_Writer.Send(m_Connected))
            {
                MessageType type = await m_Reader.ReadHeader();
                if (type == MessageType.Connected)
                {
                    m_State = ConnectionState.Connected;
                }
            }

            return m_State;
        }

        public struct SendCommandResult
        {
            public bool Success;
            public int RspDataCount;
        }

        public async Task<SendCommandResult> SendCommand(uint[] cmdData, int cmdDataCount, uint[] rspData, int rspDataCount)
        {
            bool success = false;

            // Prepare the command message
            m_Command.Write(cmdData, cmdDataCount);

            // Send it
            if (await m_Writer.Send(m_Command))
            {
                // Check the response
                MessageType type = await m_Reader.ReadHeader();
                switch (type)
                {
                    case MessageType.Command:
                        if (m_Reader.ReadMessage(m_Command))
                        {
                            m_Command.Read(rspData, ref rspDataCount);
                            success = true;
                            m_State = ConnectionState.Connected;
                        }
                        else
                        {
                            Debug.WriteLine("[Sender.SendMessage] Failed to read response message");
                        }
                        break;

                    case MessageType.SendError:
                        m_State = ConnectionState.SendError;
                        break;

                    default:
                    case MessageType.Disconnected:
                        m_State = ConnectionState.Disconnected;
                        break;
                }
            }
            else
            {
                m_State = ConnectionState.Disconnected;
                Debug.WriteLine("[Sender.SendMessage] Send failed");
            }

            SendCommandResult result = new SendCommandResult();
            result.Success = success;
            result.RspDataCount = rspDataCount;
            return result;
        }

        private DataWriter m_DataWriter;
        private DataReader m_DataReader;
        private NetworkReader m_Reader;
        private NetworkWriter m_Writer;

        private HandshakeMessage m_Handshake;
        private ConnectedMessage m_Connected;
        private CommandMessage m_Command;

        private ConnectionState m_State;
    }
}
