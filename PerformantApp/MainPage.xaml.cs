using Client;
using Common;
using Monitor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace PerformantApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            m_Connection = new Connection("localhost", 7474);
            m_ServerConnectionState = ServerConnectionState.Disconnected;
            m_Controller = new Controller(m_Connection);
            m_StateView = new StateView();
            m_StateWatcher = new StateWatcher(Dispatcher, m_StateView, m_Controller);

            m_ServerConnectionStateTimer = ThreadPoolTimer.CreatePeriodicTimer(ServerConnectionStateUpdate, TimeSpan.FromSeconds(1.0));

            DataContext = m_StateView;
        }

        public StateView State { get { return m_StateView; } }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            m_Controller.Start();
        }

        private void ServerConnectionStateUpdate(ThreadPoolTimer source)
        {
            if (m_ServerConnectionState != m_Connection.ServerConnectionState)
            {
                m_ServerConnectionState = m_Connection.ServerConnectionState;

                IAsyncAction result = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                    () =>
                    {
                        TextBlock_ServerConnectionState.Text = m_ServerConnectionState.ToString();
                    });
            }

            if (m_ConnectionState != m_Connection.State)
            {
                m_ConnectionState = m_Connection.State;

                IAsyncAction result = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                     () =>
                     {
                         TextBlock_PM3ConnectionState.Text = m_ConnectionState.ToString();
                     });
            }
        }

        private Connection m_Connection;
        private Controller m_Controller;
        private StateView m_StateView;
        private StateWatcher m_StateWatcher;
        private ServerConnectionState m_ServerConnectionState;
        private ConnectionState m_ConnectionState;
        private ThreadPoolTimer m_ServerConnectionStateTimer;
    }
}
