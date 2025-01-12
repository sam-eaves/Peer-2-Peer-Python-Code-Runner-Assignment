using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.ServiceModel;
using RestSharp;
using System.Threading.Tasks;

namespace ClientApp
{
    //Holds frame for frame-page service
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new LoginPage()); // Navigate to the LoginPage when the application starts
        }
    }
}
