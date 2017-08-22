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
using System.Windows.Shapes;

namespace Walters
{
    /// <summary>
    /// Interaction logic for PopupProgress.xaml
    /// </summary>
    public partial class PopupProgress : Window
    {
        private MainWindow Main { get; set; }
        public PopupProgress(MainWindow sender)
        {            
            InitializeComponent();
            Main = sender;
        }
        protected override async void OnContentRendered(EventArgs e)
        {
            try
            {                
                base.OnContentRendered(e);

                await Task.Run(() => Main.StartInstallation());

                Main.Installed = true;
                Main.DisableInstall();

                MessageBox.Show("All required files has been successfully installed.", "Walter's Publishing", MessageBoxButton.OK, MessageBoxImage.Information);

                Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show(string.Format("Something went wrong while installing files in your system. {0} Error: {1}, Detailed Error: -> {2}", Environment.NewLine, ex.Message, ex.InnerException.Message), "Walter's Publishing", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }            
        }        
    }
}
