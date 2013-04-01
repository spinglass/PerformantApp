﻿using Common;
using Monitor.Commands;
using Monitor.Comms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System.Threading;

namespace Monitor
{
    public class Controller
    {
        public Controller(IConnection connection)
        {
            m_Connection = connection;
            m_Commander = new Commander(m_Connection);
            m_StateReader = new StateReader(m_Commander);

            m_ConnectionState = ConnectionState.Disconnected;
        }

        public void Start()
        {
            m_Quit = false;
            m_Update = Task.Factory.StartNew(() => Update());
        }

        public void Stop()
        {
            m_Quit = true;
            m_Update.Wait();
        }

        public State GetState()
        {
            State state = m_StateReader.GetState();

            // Inject connection state
            state.ConnectionState = m_ConnectionState;

            return state;
        }

        private void Update()
        {
            while (!m_Quit)
            {
                switch (m_ConnectionState)
                {
                    case ConnectionState.Disconnected:
                        // Attempt to start the connection
                        if (m_Connection.Open())
                        {
                            m_ConnectionState = ConnectionState.Connected;
                            Debug.WriteLine("Connection: Opened");
                        }
                        break;

                    case ConnectionState.Connected:
                    case ConnectionState.SendError:
                        bool success = m_StateReader.Update();

                        if (success)
                        {
                            m_ConnectionState = ConnectionState.Connected;
                        }
                        else
                        {
                            if (!m_Connection.IsOpen)
                            {
                                m_ConnectionState = ConnectionState.Disconnected;
                                Debug.WriteLine("Connection: lost");
                            }
                            else
                            {
                                m_ConnectionState = ConnectionState.SendError;
                                Debug.WriteLine("Connection: send error");
                            }
                        }
                        break;
                }


            }
  
            m_Connection.Close();
        }

        private IConnection m_Connection;
        private Commander m_Commander;
        private StateReader m_StateReader;
        private Task m_Update;
        private bool m_Quit;
        private ConnectionState m_ConnectionState;
    }
}
