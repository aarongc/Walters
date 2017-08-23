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
using System.Xml.Linq;
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
        private IDictionary<string, string> AdobeApps { get; set; }
        private string AdobeRoamingDirectory
        {
            get
            {
                return string.Concat(GetPath(Environment.SpecialFolder.ApplicationData), @"\Adobe\");
            }
        }
        private string ApplicationName { get; set; }
        private string BaseDirectory { get; set; }
        private string ColorProfile
        {
            get
            {
                return "GRACoL2006_Coated1v2.icc";
            }
        }
        private string ColorProfilePath
        {
            get
            {
                return string.Concat(Environment.SystemDirectory, @"\spool\drivers\color\", ColorProfile);
            }
        }
        private string ColorSettings
        {
            get
            {
                return "Book_ColorSettings.csf";
            }
        }        
        private string ColorSettingsPath
        {
            get
            {
                return string.Concat(AdobeRoamingDirectory, @"Color\Settings\", ColorSettings);
            }
        }
        public bool Installed { get; set; }
        private string PDFPreset
        {
            get
            {
                return "Book_PDF.joboptions";
            }
        }
        private PopupProgress PopupProgress { get; set; }
        private string ProgramFilesDirectory { get; set; }
        private string ResourceDirectory
        {
            get
            {
                int index = BaseDirectory.LastIndexOf(ApplicationName);

                if (index < 0) return string.Format("{0}{1}", BaseDirectory, @"Resources\");

                return string.Format("{0}{1}{2}", BaseDirectory.Remove(index, BaseDirectory.Length - index), ApplicationName, @"\Resources\");
            }
        }
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
                bool readOnly = ValidateInstall(true);

                foreach (string app in AdobeApps.Keys.OrderByDescending(x => x).ToList())
                {
                    CheckBox adobeApp = new CheckBox()
                    {
                        Content = app.Replace("Adobe ", null),
                        Margin = new Thickness(0, 4, 0, 4),
                        IsChecked = true,
                        IsEnabled = readOnly
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
        private bool HasSelected
        {
            get
            {
                foreach (CheckBox app in panelApps.Children) if (app.IsChecked ?? false) return true;

                return false;
            }
        }

        public string PDFPresetPath
        {
            get
            {
                return string.Concat(AdobeRoamingDirectory, @"Adobe PDF\Settings\", PDFPreset);
            }
        }
        private void ValidateInstall()
        {
            switch (ValidateInstall(true))
            {
                case true:
                    buttonContinue.IsEnabled = HasSelected && !Installed;
                    buttonInstall.IsEnabled = HasSelected && !Installed;
                    break;
                case false:                    
                    SetUninstall();
                    break;
            }            
        }
        private void SetUninstall()
        {
            Installed = true;
            buttonInstall.Content = "Uninstall Settings";
            buttonContinue.IsEnabled = HasSelected;
            buttonInstall.IsEnabled = HasSelected;
        }
        private bool ValidateInstall(bool check)
        {
            if (IO.File.Exists(ColorProfilePath)) return false;

            return true;
        }
        public void DisableInstall()
        {
            Installed = true;
            buttonContinue.IsEnabled = false;
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
                                
                DisableInstall();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Something went wrong applying settings in your system. {0}{0} Error: {1}", Environment.NewLine, ex.Message), "Walter's Publishing", MessageBoxButton.OK, MessageBoxImage.Error);
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
        private void CopySystemSettings()
        {
            string sourcePath = IO.Path.Combine(ResourceDirectory, ColorProfile);

            IO.File.Copy(sourcePath, ColorProfilePath, true);
        }
        private void SetFileSecurity(string path)
        {
            FileSecurity fs = new FileSecurity();
            fs.AddAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.FullControl, AccessControlType.Allow));

            IO.File.SetAccessControl(path, fs);
        }
        private void CopyUserColorSettings()
        {
            string sourcePath = IO.Path.Combine(ResourceDirectory, ColorSettings);

            IO.File.Copy(sourcePath, ColorSettingsPath, true);
        }
        private void CopyUserPDFPreset()
        {
            string sourcePath = IO.Path.Combine(ResourceDirectory, PDFPreset);

            IO.File.Copy(sourcePath, PDFPresetPath, true);
        }
        private void CopyStartupScript(string app)
        {
            string destPath = null;

            switch (app)
            {
                case "photoshop cc 2015":
                case "photoshop cc 2016":
                case "photoshop cc 2017":
                    destPath = GeneratePath(app, false, true, @"\Adobe\Startup Scripts CC\Adobe Photoshop\");
                    break;
                case "photoshop cs4":
                    destPath = GeneratePath(app, false, true, @"\Adobe\Startup Scripts CS6\Adobe Photoshop\");
                    break;
                case "photoshop cs5":
                    destPath = GeneratePath(app, false, true, @"\Adobe\Startup Scripts CS6\Adobe Photoshop\");
                    break;
                case "photoshop cs5.5":
                    destPath = GeneratePath(app, false, true, @"\Adobe\Startup Scripts CS6\Adobe Photoshop\");
                    break;
                case "photoshop cs6":
                    destPath = GeneratePath(app, false, true, @"\Adobe\Startup Scripts CS6\Adobe Photoshop\");
                    break;
                case "indesign cc 2015":
                case "indesign cc 2016":
                case "indesign cc 2017":
                    destPath = GeneratePath(app, false, false, @"\Adobe\Startup Scripts CC\Adobe InDesign\");
                    break;
                case "indesign cs4":
                    destPath = GeneratePath(app, false, false, @"\Adobe\Startup Scripts CS4\Adobe InDesign\");
                    break;
                case "indesign cs5":
                    destPath = GeneratePath(app, false, false, @"\Adobe\Startup Scripts CS5\Adobe InDesign\");
                    break;
                case "indesign cs5.5":
                    destPath = GeneratePath(app, false, false, @"\Adobe\Startup Scripts CS5.5\Adobe InDesign\");
                    break;
                case "indesign cs6":
                    destPath = GeneratePath(app, false, false, @"\Adobe\Startup Scripts CS6\Adobe InDesign\");
                    break;
            }

            if (string.IsNullOrWhiteSpace(destPath))
            {
                MessageBox.Show(string.Concat("app: ", app, " destination: ", destPath));
                return;
            }

            IO.File.Copy(GeneratePath(app, true, true), destPath, true);
            AddFile(destPath);
        }
        private void AddFile(string path)
        {
            XDocument files = Files;
            
            files.Root.Add(new XElement("file", path));
            files.Save(FilesPath);
        }
        private string FilesPath
        {
            get
            {
                return string.Concat(ResourceDirectory, "files.xml");
            }
        }
        private XDocument Files
        {
            get
            {
                return XDocument.Load(FilesPath);
            }
        }
        string GeneratePath(string app, bool isSource, bool isPhotoshop, string path = "")
        {
            if (isSource) return GeneratePath(isPhotoshop);

            return string.Concat(ProgramFilesDirectory, path, GeneratePath(app));
        }
        string GeneratePath(bool isPhotoshop)
        {
            if (isPhotoshop) return string.Concat(ResourceDirectory, @"jsx\wp_photoshop.jsx");
            
            return string.Concat(ResourceDirectory, @"jsx\wp_indesign.jsx");
        }        
        string GeneratePath(string app)
        {
            return string.Format("wp_{0}{1}", app.Replace(" ", "_"), ".jsx");
        }
        private void Uninstall()
        {            
            try
            {
                XDocument files = Files;

                IEnumerable<string> childList = from el in files.Root.Elements()
                                                select el.Value;

                foreach (string path in childList) Delete(path);
                
                Delete(ColorProfilePath);
                Delete(ColorSettingsPath);
                Delete(PDFPresetPath);

                buttonInstall.IsEnabled = false;
                MessageBox.Show("All settings has been successfully removed from your system.", "Walter's Publishing", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Something went wrong removing settings in your system. {0}{0} Error: {1}", Environment.NewLine, ex.Message), "Walter's Publishing", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Delete(string path)
        {
            if (IO.File.Exists(path)) IO.File.Delete(path);
        }
        private void adobeApp_Click(object sender, RoutedEventArgs e)
        {
            ValidateInstall();
        }
        private void buttonContinue_Click(object sender, RoutedEventArgs e)
        {
            if (HasSelected) tabControlApps.SelectedItem = tabItemPresets;
        }
        private void buttonInstall_Click(object sender, RoutedEventArgs e)
        {
            switch (Installed)
            {
                case true:
                    Uninstall();
                    break;
                case false:
                    PopupProgress.ShowDialog();
                    break;
            }
        }
        private void tabControlApps_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateInstall();
        }
    }
}