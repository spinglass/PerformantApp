using Client.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Client
{
    class NetworkReader
    {
        public NetworkReader()
        {
            m_Buffer = new byte[255];
            m_MessageReader = new BinaryReader(new MemoryStream(m_Buffer));
            m_PendingType = MessageType.Invalid;
        }

        public DataReader Reader
        {
            get { return m_DataReader; }
            set { m_DataReader = value; }
        }

        public async Task<MessageType> ReadHeader()
        {
            MessageType readType = MessageType.None;
            m_PendingType = MessageType.None;

            if (m_DataReader != null)
            {
                byte type = await ReadByte();
                if (Enum.IsDefined(typeof(MessageType), (MessageType)type))
                {
                    uint size = (uint)await ReadByte();
                    if (size > 0)
                    {
                        byte[] tmp = new byte[size];
                        await ReadBytes(tmp);
                        tmp.CopyTo(m_Buffer, 0);

                        readType = (MessageType)type;
                        m_PendingType = readType;
                        m_MessageReader.BaseStream.Position = 0;
                    }
                    else
                    {
                        readType = (MessageType)type;
                    }
                }
                else
                {
                    readType = MessageType.Invalid;
                    m_PendingType = MessageType.Invalid;
                    Debug.WriteLine("[NetworkReader.ReadHeader] Invalid message type");
                }
            }

            return readType;
        }

        public bool ReadMessage(Message msg)
        {
            bool success = false;
            if (m_PendingType == msg.Type)
            {
                success = msg.Serialise(m_MessageReader);
                m_PendingType = MessageType.Invalid;
            }
            return success;
        }

        private async Task<byte> ReadByte()
        {
            uint loadSize = await m_DataReader.LoadAsync(sizeof(byte));
            if (loadSize != sizeof(byte))
            {
                throw new IOException("Failed to get data from reader");
            }
            return m_DataReader.ReadByte();
        }

        private async Task ReadBytes(byte[] value)
        {
            uint loadSize = await m_DataReader.LoadAsync((uint)value.Length);
            if (loadSize != (uint)value.Length)
            {
                throw new IOException("Failed to get data from reader");
            }
            m_DataReader.ReadBytes(value);
        }

        private DataReader m_DataReader;
        private byte[] m_Buffer;
        private BinaryReader m_MessageReader;
        private MessageType m_PendingType;
    }
}
