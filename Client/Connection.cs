using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;

namespace Client
{
    public enum ServerConnectionState
    {
        Disconnected,
        Connected,
    }

    public class Connection : IConnection
    {
        public Connection(string hostname, int port)
        {
            m_HostName = new HostName(hostname);
            m_Port = port;

            m_Sender = new Sender();
            m_ServerConnectionState = ServerConnectionState.Disconnected;
        }

        public bool IsOpen
        {
            get { return (m_Sender.State == ConnectionState.Connected || m_Sender.State == ConnectionState.SendError); }
        }

        public ServerConnectionState ServerConnectionState
        {
            get { return m_ServerConnectionState; }
        }

        public ConnectionState State
        {
            get { return m_Sender.State; }
        }

        public bool Open()
        {
            if (m_Socket == null)
            {
                OpenSocket();
            }

            if (m_Socket != null)
            {
                try
                {
                    Task<ConnectionState> task = m_Sender.TestConnection();
                    task.Wait();
                }
                catch (Exception)
                {
                    Close();
                }
            }

            return IsOpen;
        }

        public void Close()
        {
            if (m_Socket != null)
            {
                m_Socket.Dispose();
                m_Socket = null;
                m_ServerConnectionState = ServerConnectionState.Disconnected;
                Debug.WriteLine("[Connection.OpenSocket] Connection to server closed");
            }
        }

        public bool SendCSAFECommand(uint[] cmdData, int cmdDataCount, uint[] rspData, ref int rspDataCount)
        {
            bool success = false;
            if (IsOpen)
            {
                try
                {
                    Task<Sender.SendCommandResult> task = m_Sender.SendCommand(cmdData, cmdDataCount, rspData, rspDataCount);
                    task.Wait();
                    success = task.Result.Success;
                    rspDataCount = task.Result.RspDataCount;
                }
                catch (Exception)
                {
                    Close();
                }
            }
            return success;
        }

        private void OpenSocket()
        {
            if (m_OpenSocketTask == null)
            {
                m_OpenSocketTask = OpenSocketAsync();
            }
            else
            {
                if (m_OpenSocketTask.IsCompleted)
                {
                    if (m_OpenSocketTask.Result != null)
                    {
                        m_Socket = m_OpenSocketTask.Result;
                        m_ServerConnectionState = ServerConnectionState.Connected;
                    }
                    m_OpenSocketTask = null;
                }
            }
        }

        private async Task<StreamSocket> OpenSocketAsync()
        {
            StreamSocket socket = null;

            try
            {
                socket = new StreamSocket();
                await socket.ConnectAsync(m_HostName, m_Port.ToString());

                m_Sender.Open(socket.InputStream, socket.OutputStream);

                // Handshake with the server
                if (await m_Sender.Handshake())
                {
                    Debug.WriteLine("[Connection.OpenSocket] Connected to server opened");
                }
                else
                {
                    m_Sender.Close();
                    socket.Dispose();
                    socket = null;
                }
            }
            catch (Exception)
            {
                socket = null;
            }

            return socket;
        }

        private HostName m_HostName;
        private int m_Port;
        private StreamSocket m_Socket;
        private Sender m_Sender;
        private Task<StreamSocket> m_OpenSocketTask;
        private ServerConnectionState m_ServerConnectionState;
    }
}
