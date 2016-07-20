using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using Common;
using Microsoft.Synchronization;
using SyncFrameWork.Controllers;
using SyncFrameWork.TestClient;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Timers.Timer;


namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private BackgroundWorker worker;
        private Timer timer;

        private static TestClientServiceDemo serviceDemo;

        private LocalStore localStore;
        private RemoteStore remoteStore;
        private NotifyIcon m_notifyIcon;

        private ContextMenuStrip m_contextMenu;
        private bool _ForceClose;
        private int i = 0;
      
        public MainWindow()
        {
          

            InitializeComponent();
            LoadConfiguration();
            CreateNotifyIcon();

            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;

            worker.DoWork += (sender, args) => SyncProcces();

            timer = new Timer(Convert.ToInt32(ConfigurationManager.AppSettings["AutoSyncInterval"]) * 1000);
            timer.Elapsed += timer_Elapsed;

            serviceDemo = new TestClientServiceDemo(ConfigurationManager.AppSettings["ServiceAddress"]);

            NetLog.OnMessageFired += delegate(object o, MessageEventArgs args) {
                Dispatcher.Invoke(() =>
                {
                    LabelFileName.Content = args.ItemUri;
                    LabelOperation.Content = args.Operation;
                    LabelStatus.Content = args.Status;
                });
            };
        }



        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!worker.IsBusy)
            {
                worker.RunWorkerAsync();
            }
        }

        private void LoadConfiguration()
        {
            textBoxSoure.Text = AppSettings.Get<string>("LocalAddress");
            textBoxDestination.Text = AppSettings.Get<string>("ServiceAddress");
            slider.Value= AppSettings.Get<int>("AutoSyncInterval");
            textBoxSeconds.Text = AppSettings.Get<string>("AutoSyncInterval");
            checkBoxUseTemp.IsChecked = AppSettings.Get<bool>("UseTemp");
        }

        private void CreateNotifyIcon()
        {
            m_contextMenu = new ContextMenuStrip();

            ToolStripMenuItem mI1 = new ToolStripMenuItem { Text = "Open" };
            mI1.Click += (sender, args) => Maximize();
            ToolStripMenuItem mI2 = new ToolStripMenuItem { Text = "Exit" };
            mI2.Click += (sender, args) => EndApplication();

            m_contextMenu.Items.Add(mI1);
            m_contextMenu.Items.Add(mI2);

            //Initalize Notify Icon
            m_notifyIcon = new NotifyIcon
            {
                Text = "Sync Tool",
                Icon = new Icon(Path.Combine(Environment.CurrentDirectory, @"icon.ico")),
                //Associate the contextmenustrip with notify icon
                ContextMenuStrip = m_contextMenu,
                Visible = true
            };

            m_notifyIcon.MouseUp += (sender, args) => m_contextMenu.Show();




        }


        private bool firstTimeClick = true;

        private void button_Click(object sender, RoutedEventArgs e)
        {
            #region chagne Interval if needed

            timer.Enabled = false;
            timer.Interval = Convert.ToInt32(ConfigurationManager.AppSettings["AutoSyncInterval"]) * 1000;
            timer.Enabled = true;

            #endregion

            if (firstTimeClick)
            {
                timer.Start();
                firstTimeClick = false;
            }
            timer_Elapsed(null, null);

        }

        #region sync

        private bool firstTimeSync = true;

        public void SyncProcces()
        {
            NetLog.Log.Info("Start SyncProcces");
            try
            {

                Dispatcher.Invoke(() =>
                {
                    buttonSync.IsEnabled = false;
                    GroupBoxSession.Visibility=Visibility.Visible;
                });


                if (firstTimeSync)//ar remote and local ==null
                {
                    Debug.WriteLine("Start new LocalStore");
                    localStore = new LocalStore(ConfigurationManager.AppSettings["LocalAddress"]);
                    Debug.WriteLine("End new LocalStore");
                    Debug.WriteLine("Start new RemoteStore");
                    remoteStore = new RemoteStore(ConfigurationManager.AppSettings["ServiceAddress"]);
                    Debug.WriteLine("End new RemoteStore");

                    firstTimeSync = false;
                }
                localStore.RequestedBatchSize = 100000000;
                remoteStore.RequestedBatchSize = 100000000;

                localStore.Configuration.ConflictResolutionPolicy = ConflictResolutionPolicy.ApplicationDefined;
                remoteStore.Configuration.ConflictResolutionPolicy = ConflictResolutionPolicy.ApplicationDefined;

                SyncOrchestrator syncAgent = new SyncOrchestrator();
                syncAgent.StateChanged += SyncAgentOnStateChanged;
                //syncAgent.SessionProgress += SyncAgentOnSessionProgress;
                syncAgent.LocalProvider = localStore;
                syncAgent.RemoteProvider = remoteStore;
                syncAgent.Direction = SyncDirectionOrder.UploadAndDownload;

                SyncOperationStatistics statistics = syncAgent.Synchronize();
                PrintStatistics(statistics);
                LoadStores();
                Dispatcher.Invoke(() =>
                {
                    buttonSync.IsEnabled = true;
                    labelSyncNum.Content = ++i;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            NetLog.Log.Info("End SyncProcces");
        }


        private void LoadStores()
        {
            Debug.WriteLine("Start new LocalStore");
            localStore = new LocalStore(ConfigurationManager.AppSettings["LocalAddress"]);
            Debug.WriteLine("End new LocalStore");
            Debug.WriteLine("Start new RemoteStore");
            remoteStore = new RemoteStore(ConfigurationManager.AppSettings["ServiceAddress"]);
            Debug.WriteLine("End new RemoteStore");
        }

        private void PrintStatistics(SyncOperationStatistics statistics)
        {
            Dispatcher.Invoke(() =>
            {
                lcw_Copy.Content = $"{statistics.DownloadChangesApplied}";
                lrp_Copy.Content = $"{statistics.DownloadChangesFailed}";
                ls_Copy.Content = $"{statistics.DownloadChangesTotal}";
                ltw_Copy.Content = $"{statistics.UploadChangesApplied}";
                ls_Copy1.Content = $"{statistics.UploadChangesFailed}";
                ltw_Copy1.Content = $"{statistics.UploadChangesTotal}";
            });

        }

        private void SyncAgentOnStateChanged(object sender,
            SyncOrchestratorStateChangedEventArgs syncOrchestratorStateChangedEventArgs)
        {
            Dispatcher.Invoke(() =>
            {
                NewState.Content =
                    $"{syncOrchestratorStateChangedEventArgs.NewState}";

            });

        }

        private void SyncAgentOnSessionProgress(object sender, SyncStagedProgressEventArgs syncStagedProgressEventArgs)
        {
            Dispatcher.Invoke(() =>
            {
                lcw.Content = $"{syncStagedProgressEventArgs.CompletedWork}";
                lrp.Content = $"{syncStagedProgressEventArgs.ReportingProvider}";
                ls.Content = $"{syncStagedProgressEventArgs.Stage}";
                ltw.Content = $"{syncStagedProgressEventArgs.TotalWork}";
            });

        }

        #endregion

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_ForceClose == false)
            {
                e.Cancel = true;
                Minimize();
            }
        }

        protected void Minimize()
        {
            Hide();
        }

        protected void Maximize()
        {
            Show();
            this.WindowState = WindowState.Normal;
        }
        protected void EndApplication()
        {
            Minimize();
            //sync.cancel
            Thread.Sleep(1000);
            _ForceClose = true;
            WindowState = WindowState.Normal;
            if (worker != null && worker.IsBusy)
                worker.CancelAsync();
            this.Close();
            Environment.Exit(0);
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textBoxSeconds.Text = e.NewValue.ToString();
            string answer = GetUserFrindlyTimeFromSeconds(e);
            LabelTime.Content = answer;
        }

        private static string GetUserFrindlyTimeFromSeconds(RoutedPropertyChangedEventArgs<double> e)
        {
            TimeSpan t = TimeSpan.FromSeconds(e.NewValue);
            string answer = string.Format("{0:D2}:{1:D2}:{2:D2}",
                                    t.Hours,
                                    t.Minutes,
                                    t.Seconds,
                                    t.Milliseconds);
            return answer;
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            string appPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //string configFile = System.IO.Path.Combine(appPath, "App.config");
            string configFile = Path.Combine(appPath, "SyncTool.exe" + ".config");

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

        private void buttonExit_Click(object sender, RoutedEventArgs e)
        {
            EndApplication();
        }

        //this buttonSync to be clicked in case dead lock
        private void button1ClearSync_Click(object sender, RoutedEventArgs e)
        {
            DeleteSyncFiles();
        }

        private static void DeleteSyncFiles()
        {
            try
            {
                File.Delete(Path.Combine(ConfigurationManager.AppSettings["LocalAddress"], "file.sync"));
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
            try
            {
                serviceDemo.Deletefileonserver("file.sync");
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }

}
