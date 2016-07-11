using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
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
using Common;
using Path = System.IO.Path;

namespace SyncTool1.Pages
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : UserControl
    {
        public Home()
        {
            InitializeComponent();
            LoadConfiguration();
        }
        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textBoxSeconds.Text = e.NewValue.ToString();
        }
        private void LoadConfiguration()
        {
            textBoxSoure.Text = AppSettings.Get<string>("LocalAddress");
            textBoxDestination.Text = AppSettings.Get<string>("ServiceAddress");
            textBoxSeconds.Text = AppSettings.Get<string>("AutoSyncInterval");
            checkBoxUseTemp.IsChecked = AppSettings.Get<bool>("UseTemp");
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            string appPath = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //string configFile = System.IO.Path.Combine(appPath, "App.config");
            string configFile = Path.Combine(appPath, "SyncTool1.exe.config");
            //System.AppDomain.CurrentDomain.FriendlyName + ".config"

            ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
            configFileMap.ExeConfigFilename = configFile;
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap,
                ConfigurationUserLevel.None);

            config.AppSettings.Settings["LocalAddress"].Value = textBoxSoure.Text;
            config.AppSettings.Settings["ServiceAddress"].Value = textBoxDestination.Text;
            config.AppSettings.Settings["AutoSyncInterval"].Value = textBoxSeconds.Text;
            config.AppSettings.Settings["UseTemp"].Value = checkBoxUseTemp.IsChecked.Value.ToString();
            config.Save();
        }
    }
}
