using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for PopAlertWindow.xaml
    /// </summary>
    public partial class PopAlertWindow : Window
    {
        public PopAlertWindow(string push_note)
        {
            InitializeComponent();
            TextNote.Inlines.Add(new Bold(new Run("PODIO lite\n")));
            TextNote.Inlines.Add(new Run(push_note));
        }

        private void Alert_Complete(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
