using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClientApp
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        private Networking _network;
        public LoginPage()
        {
            InitializeComponent();
            _network = new Networking();
            //_network.CleanupInactiveClients();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)  // Mark method as async
        {
            int port;

            if (int.TryParse(PortBox.Text, out port))
            {
                // Retrieve the list of available clients
                var availableClients = await _network.GetAvailableClients();  // Await the async call

                // Check if the entered port is already in use
                bool isPortInUse = availableClients.Any(existingClient => existingClient.Port == port);

                if (isPortInUse)
                {
                    // If the port is already in use, show a message and stop the login process
                    MessageBox.Show($"Port {port} is already in use. Please select a different port.");
                    return;  // Stop here if port is not unique
                }
                // If the port is unique, proceed with client creation
                Client client = new Client
                {
                    Port = port,
                    JobsCompleted = 0,
                    LastSend = DateTime.Now,
                };

                // Navigate to the ClientPage and pass the Client object
                ClientPage clientPage = new ClientPage(client);
                NavigationService.Navigate(clientPage);
            }

            else
            {
                MessageBox.Show("Please enter a port.");
            }
        }
    }
}