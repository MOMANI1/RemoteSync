using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using log4net;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            { 
                log4net.Config.XmlConfigurator.Configure();
                //Log.Info("Hello World"); 
                base.OnStartup(e); 
            }
            catch (Exception ex)
            {
                Log.Error("OnStartup", ex);
            }

        }
    }
}
