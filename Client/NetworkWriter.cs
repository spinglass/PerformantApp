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
    class NetworkWriter
    {
        public NetworkWriter()
        {
            m_Buffer = new byte[255];
            m_MessageWriter = new BinaryWriter(new MemoryStream(m_Buffer));
        }

        public DataWriter Writer
        {
            get { return m_DataWriter; }
            set { m_DataWriter = value; }
        }

        public async Task<bool> Send(Message msg)
        {
            if (m_DataWriter != null)
            {
                // Prepare the message
                m_MessageWriter.BaseStream.Position = 0;
                msg.Serialise(m_MessageWriter);
                int size = (int)m_MessageWriter.BaseStream.Position;

                // Write the message
                m_DataWriter.WriteByte((byte)msg.Type);
                m_DataWriter.WriteByte((byte)size);
                if (size > 0)
                {
                    byte[] tmp = new byte[size];
                    Array.Copy(m_Buffer, tmp, size);
                    m_DataWriter.WriteBytes(tmp);
                }

                // Send the message
                await m_DataWriter.StoreAsync();

                return true;
            }

            return false;
        }

        private DataWriter m_DataWriter;
        private byte[] m_Buffer;
        private BinaryWriter m_MessageWriter;
    }
}
