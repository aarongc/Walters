using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
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
using IO = System.IO;

namespace Walters
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SearchAdobeApps();
        }
        private PopupProgress PopupProgress { get; set; }
        private bool IsAdobeApp(String name)
        {
            return (!String.IsNullOrWhiteSpace(name) && (name.Contains("photoshop") || name.Contains("indesign") || name.Contains("adobe cs")));
        }
        private void SearchAdobeApps()
        {
            ApplicationName = System.AppDomain.CurrentDomain.SetupInformation.ApplicationName.Replace(".exe", null);
            BaseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            ProgramFilesDirectory = GetPath(Environment.SpecialFolder.CommonProgramFiles);

            PopupProgress = new PopupProgress(this);

            GetAdobeApps(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            GetAdobeApps(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall");

            GenerateCheckList();
        }
        private IDictionary<string, string> AdobeApps { get; set; }
        private string Assume64BitApp(string app, string location)
        {
            if (location.Contains("x86")) return app;

            return string.IsNullOrWhiteSpace(app) ? "" : string.Concat(app, " (64bit)");
        }
        private string ReplaceNull(object value)
        {
            if (null != value) return value.ToString();

            return "";
        }

        private void CheckAdobeDesignAndWebPremium(string name, string location)
        {
            IList<string> apps = new List<string>();

            apps.Add(string.Concat("Adobe Photoshop ", CheckAdobeDesignAndWebPremium(name)));
            apps.Add(string.Concat("Adobe InDesign ", CheckAdobeDesignAndWebPremium(name)));

            foreach (string app in apps) if (!AdobeApps.ContainsKey(app)) AdobeApps.Add(app, location);
        }

        private string CheckAdobeDesignAndWebPremium(string name)
        {
            return name.Replace(" Design and Web Premium", null).Replace("Adobe ", null);
        }

        private void GetAdobeApps(string uninstallKey)
        {
            using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(uninstallKey))
            {
                foreach (String skName in rk.GetSubKeyNames())
                {
                    using (RegistryKey sk = rk.OpenSubKey(skName))
                    {
                        string location = ReplaceNull(sk.GetValue("InstallLocation")),
                            name = GetAdobeApp(sk);

                        if (!string.IsNullOrWhiteSpace(name) && ((null == AdobeApps) || (null != AdobeApps && !AdobeApps.ContainsKey(name))))
                        {
                            if (null == AdobeApps) AdobeApps = new Dictionary<string, string>();

                            if (name.ToLower().Contains("design and web premium"))
                            {
                                CheckAdobeDesignAndWebPremium(name, location);
                            }
                            else
                            {
                                if (!AdobeApps.ContainsKey(name)) AdobeApps.Add(name, location);
                            }
                        }
                    }
                }
            }
        }

        private void GenerateCheckList()
        {
            if (null != AdobeApps && AdobeApps.Any())
            {
                foreach (string app in AdobeApps.Keys.OrderByDescending(x => x).ToList())
                {
                    CheckBox adobeApp = new CheckBox()
                    {
                        Content = app.Replace("Adobe ", null),
                        Margin = new Thickness(0, 4, 0, 4),
                        IsChecked = true
                    };

                    adobeApp.Click += new RoutedEventHandler(this.adobeApp_Click);

                    panelApps.Children.Add(adobeApp);
                }

                buttonContinue.IsEnabled = true;
                tabItemApplication.Header = string.Format("Adobe Applications ({0})", AdobeApps.Count);
            }
        }

        private string GetAdobeApp(RegistryKey sk)
        {
            try
            {
                object displayName = sk.GetValue("DisplayName");
                string name = null != displayName ? displayName.ToString() : "";

                return IsAdobeApp(name.ToLower()) ? name.Trim() : "";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
        }

        private void adobeApp_Click(object sender, RoutedEventArgs e)
        {
            ValidateInstall();
        }

        private bool HasSelected
        {
            get
            {
                foreach (CheckBox app in panelApps.Children) if (app.IsChecked ?? false) return true;

                return false;
            }
        }

        private void tabControlApps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateInstall();
        }

        private void ValidateInstall()
        {
            buttonContinue.IsEnabled = HasSelected && !Installed;
            buttonInstall.IsEnabled = HasSelected && !Installed;
        }

        private void buttonContinue_Click(object sender, RoutedEventArgs e)
        {
            if (HasSelected) tabControlApps.SelectedItem = tabItemPresets;
        }

        private void buttonInstall_Click(object sender, RoutedEventArgs e)
        {
            PopupProgress.ShowDialog();
        }
        public bool Installed { get; set; }
        public void DisableInstall()
        {
            buttonInstall.IsEnabled = false;
        }
        public void StartInstallation()
        {
            Dispatcher.Invoke(() => CopySystemSettings());
            Dispatcher.Invoke(() => CopyUserColorSettings());
            Dispatcher.Invoke(() => CopyUserPDFPreset());
            Dispatcher.Invoke(() => CopyScripts());
        }

        private void CopyScripts()
        {
            try
            {
                foreach (string app in GetSelectedApps()) CopyStartupScript(app.ToLower().Trim());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private IList<string> GetSelectedApps()
        {
            IList<string> apps = new List<string>();

            foreach (CheckBox app in panelApps.Children) if (app.IsChecked ?? false) apps.Add(app.Content.ToString());

            return apps;
        }

        private string GetPath(Environment.SpecialFolder kind)
        {
            return Environment.GetFolderPath(kind);
        }

        private void CopyStartupScripts(IList<string> apps)
        {
            foreach (string app in apps) CopyStartupScript(app.ToLower());
        }
        private string ProgramFilesDirectory { get; set; }
        private string BaseDirectory { get; set; }
        private string ApplicationName { get; set; }
        private string ColorSettings
        {
            get
            {
                return "Book_ColorSettings.csf";
            }
        }
        private string PDFPreset
        {
            get
            {
                return "Book_PDF.joboptions";
            }
        }
        private string ColorProfile
        {
            get
            {
                return "GRACoL2006_Coated1v2.icc";
            }
        }
        private string ResourceDirectory
        {
            get
            {
                int index = BaseDirectory.LastIndexOf(ApplicationName);

                if (index < 0) return string.Format("{0}{1}", BaseDirectory, @"Resources\");

                return string.Format("{0}{1}{2}", BaseDirectory.Remove(index, BaseDirectory.Length - index), ApplicationName, @"\Resources\");
            }
        }
        private string AdobeRoamingDirectory
        {
            get
            {
                return string.Concat(GetPath(Environment.SpecialFolder.ApplicationData), @"\Adobe\");
            }
        }

        private void CopySystemSettings()
        {
            string sourcePath = IO.Path.Combine(ResourceDirectory, ColorProfile);
            string destPath = string.Concat(Environment.SystemDirectory, @"\spool\drivers\color\", ColorProfile);

            IO.File.Copy(sourcePath, destPath, true);
        }

        private void SetFileSecurity(string destPath)
        {
            FileSecurity fs = new FileSecurity();
            fs.AddAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.FullControl, AccessControlType.Allow));

            IO.File.SetAccessControl(destPath, fs);
        }

        private void CopyUserColorSettings()
        {
            string sourcePath = IO.Path.Combine(ResourceDirectory, ColorSettings);
            string destPath = string.Concat(AdobeRoamingDirectory, @"Color\Settings\", ColorSettings);

            IO.File.Copy(sourcePath, destPath, true);
        }
        private void CopyUserPDFPreset()
        {
            string sourcePath = IO.Path.Combine(ResourceDirectory, PDFPreset);
            string destPath = string.Concat(AdobeRoamingDirectory, @"Adobe PDF\Settings\", PDFPreset);

            IO.File.Copy(sourcePath, destPath, true);
        }
        private void CopyStartupScript(string app)
        {
            string destPath = null;

            switch (app)
            {
                case "photoshop cc 2015":
                case "photoshop cc 2016":
                case "photoshop cc 2017":
                    destPath = GeneratePath(app, false, @"\Adobe\Startup Scripts CC\Adobe Photoshop\");
                    break;
                case "photoshop cs4":
                    destPath = GeneratePath(app, false, @"\Adobe\Startup Scripts CS6\Adobe Photoshop\");
                    break;
                case "photoshop cs5":
                    destPath = GeneratePath(app, false, @"\Adobe\Startup Scripts CS6\Adobe Photoshop\");
                    break;
                case "photoshop cs5.5":
                    destPath = GeneratePath(app, false, @"\Adobe\Startup Scripts CS6\Adobe Photoshop\");
                    break;
                case "photoshop cs6":
                    destPath = GeneratePath(app, false, @"\Adobe\Startup Scripts CS6\Adobe Photoshop\");
                    break;
                case "indesign cc 2015":
                case "indesign cc 2016":
                case "indesign cc 2017":
                    destPath = GeneratePath(app, false, @"\Adobe\Startup Scripts CC\Adobe InDesign\");
                    break;
                case "indesign cs4":
                    destPath = GeneratePath(app, false, @"\Adobe\Startup Scripts CS4\Adobe InDesign\");
                    break;
                case "indesign cs5":
                    destPath = GeneratePath(app, false, @"\Adobe\Startup Scripts CS5\Adobe InDesign\");
                    break;
                case "indesign cs5.5":
                    destPath = GeneratePath(app, false, @"\Adobe\Startup Scripts CS5.5\Adobe InDesign\");
                    break;
                case "indesign cs6":
                    destPath = GeneratePath(app, false, @"\Adobe\Startup Scripts CS6\Adobe InDesign\");
                    break;
            }

            if (string.IsNullOrWhiteSpace(destPath))
            {
                MessageBox.Show(string.Concat("app: ", app, " destination: ", destPath));
                return;
            }

            IO.File.Copy(GeneratePath(app, true), destPath, true);
        }
        string GeneratePath(string app, bool isSource, string path = "")
        {
            if (isSource) return string.Concat(ResourceDirectory, @"jsx\", GeneratePath(app));

            return string.Concat(ProgramFilesDirectory, path, GeneratePath(app));
        }
        string GeneratePath(string app)
        {
            return string.Format("wp_{0}{1}", app.Replace(" ", "_"), ".jsx");
        }
    }
}