using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PodioDesktop
{
    /// <summary>
    /// Interaction logic for LogInWindow.xaml
    /// </summary>
    public partial class LogInWindow : Window
    {
        public LogInWindow()
        {
            InitializeComponent();
        }

        private void LogIn_Click(object sender, RoutedEventArgs e)
        {
            string user_email = Email.Text;
            string user_pw;

            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                string client_id = "***";
                string client_secret = "***";
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(PW.SecurePassword);
                user_pw = Marshal.PtrToStringUni(unmanagedString);
                Podio.API.Client client = Podio.API.Client.ConnectAsUser(client_id, client_secret, user_email, user_pw); // creates a Podio.API.Client used to communicate with the Podio API
                System.Windows.Application.Current.Properties["podio_client"] = client;

                //Disable shutdown when the dialog closes
                App.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                this.Close();
                var mainWindow = new MainWindow();
                //Re-enable normal shutdown mode.
                App.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                App.Current.MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "PODIO LogIn Error", MessageBoxButton.OK);
                Email.Text = "";
                PW.Clear();
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            //clear the input box
            Email.Text = "";
            PW.Clear();
        }

    }
}
