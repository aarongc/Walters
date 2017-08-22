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

                MessageBox.Show("All files required has been successfully installed.", "Walter's Publishing");

                Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show(string.Format("error -> {0}, inner error -> {1}", ex.Message, ex.InnerException.Message));
                Close();
            }            
        }        
    }
}
