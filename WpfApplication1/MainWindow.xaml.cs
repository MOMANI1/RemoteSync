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
using netoaster;
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

        private BackgroundWorker _worker;
        private Timer _timer;

        private static TestClientServiceDemo _serviceDemo;

        private LocalStore _localStore;
        private RemoteStore _remoteStore;
        private NotifyIcon _notifyIcon;

        private ContextMenuStrip _contextMenu;
        private bool _forceClose;
        private int _syncSumNum = 0;
      
        public MainWindow()
        {
          

            InitializeComponent();
            LoadConfiguration();
            CreateNotifyIcon();

            _worker = new BackgroundWorker {WorkerSupportsCancellation = true};

            _worker.DoWork += (sender, args) => SyncProcess();

            _timer = new Timer(Convert.ToInt32(ConfigurationManager.AppSettings["AutoSyncInterval"]) * 1000);
            _timer.Elapsed += timer_Elapsed;

            _serviceDemo = new TestClientServiceDemo(ConfigurationManager.AppSettings["ServiceAddress"]);

            NetLog.OnMessageFired += delegate(object o, MessageEventArgs args) {
                // when the Event Happened I want to Update the UI
                // this is WPF Window (WPF Project)  
                Dispatcher.Invoke(() =>
                {
                    LabelFileName.Content = args.ItemUri;
                    LabelOperation.Content = args.Operation;
                    LabelStatus.Content =$"{args.Status} {args.Data ?? ""}";
                });
            };
            NetLog.OnErrorHappened+= delegate(object sender, ErrorEventArgs args)
            {
                _notifyIcon?.ShowBalloonTip(5000,"an Error happened in SyncTool Core",args.GetException().Message,ToolTipIcon.Error);
            };
        }



        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!_worker.IsBusy)
            {
                _worker.RunWorkerAsync();
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
            _contextMenu = new ContextMenuStrip();

            ToolStripMenuItem mI1 = new ToolStripMenuItem { Text = "Open" };
            mI1.Click += (sender, args) => Maximize();
            ToolStripMenuItem mI2 = new ToolStripMenuItem { Text = "Exit" };
            mI2.Click += (sender, args) => EndApplication();

            _contextMenu.Items.Add(mI1);
            _contextMenu.Items.Add(mI2);

            //Initialize Notify Icon
            _notifyIcon = new NotifyIcon
            {
                Text = "Sync Tool",
                Icon = new Icon(Path.Combine(Environment.CurrentDirectory, @"icon.ico")),
                //Associate the ContextMenuStrip with notify icon
                ContextMenuStrip = _contextMenu,
                Visible = true
            };

            _notifyIcon.MouseUp += (sender, args) => _contextMenu.Show();




        }


        private bool _firstTimeClick = true;

        private void button_Click(object sender, RoutedEventArgs e)
        {
            #region chagne Interval if needed

            _timer.Enabled = false;
            _timer.Interval = Convert.ToInt32(ConfigurationManager.AppSettings["AutoSyncInterval"]) * 1000;
            _timer.Enabled = true;

            #endregion

            if (_firstTimeClick)
            {
                _timer.Start();
                _firstTimeClick = false;
            }
            timer_Elapsed(null, null);

        }

        #region sync

        private bool _firstTimeSync = true;

        public void SyncProcess()
        {
            //_notifyIcon.ShowBalloonTip(200, "Sync Process","started",ToolTipIcon.Info);
            NetLog.Log.Info("Sync Process Started");
            try
            {

                Dispatcher.Invoke(() =>
                {
                    buttonSync.IsEnabled = false;
                    GroupBoxSession.Visibility=Visibility.Visible;
                });


                if (_firstTimeSync)//ar remote and local ==null
                {
                    Debug.WriteLine("Start new LocalStore");
                    _localStore = new LocalStore(ConfigurationManager.AppSettings["LocalAddress"]);
                    Debug.WriteLine("End new LocalStore");
                    Debug.WriteLine("Start new RemoteStore");
                    _remoteStore = new RemoteStore(ConfigurationManager.AppSettings["ServiceAddress"]);
                    Debug.WriteLine("End new RemoteStore");

                    _firstTimeSync = false;
                }
                _localStore.RequestedBatchSize = 100000000;
                _remoteStore.RequestedBatchSize = 100000000;

                _localStore.Configuration.ConflictResolutionPolicy = ConflictResolutionPolicy.ApplicationDefined;
                _remoteStore.Configuration.ConflictResolutionPolicy = ConflictResolutionPolicy.ApplicationDefined;

                SyncOrchestrator syncAgent = new SyncOrchestrator();
                syncAgent.StateChanged += SyncAgentOnStateChanged;
                //syncAgent.SessionProgress += SyncAgentOnSessionProgress;
                syncAgent.LocalProvider = _localStore;
                syncAgent.RemoteProvider = _remoteStore;
                syncAgent.Direction = SyncDirectionOrder.UploadAndDownload;

                SyncOperationStatistics statistics = syncAgent.Synchronize();
                PrintStatistics(statistics);
                LoadStores();
                Dispatcher.Invoke(() =>
                {
                    buttonSync.IsEnabled = true;
                    labelSyncNum.Content = ++_syncSumNum;
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + ex.StackTrace);
            }
            NetLog.Log.Info("Sync Process Finished");
            //_notifyIcon.ShowBalloonTip(200, "Sync Process", "Finished Successfully", ToolTipIcon.Info);
        }


        private void LoadStores()
        {
            Debug.WriteLine("Start new LocalStore");
            _localStore = new LocalStore(ConfigurationManager.AppSettings["LocalAddress"]);
            Debug.WriteLine("End new LocalStore");
            Debug.WriteLine("Start new RemoteStore");
            _remoteStore = new RemoteStore(ConfigurationManager.AppSettings["ServiceAddress"]);
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
            if (_forceClose == false)
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
            _forceClose = true;
            WindowState = WindowState.Normal;
            if (_worker != null && _worker.IsBusy)
                _worker.CancelAsync();
            this.Close();
            Environment.Exit(0);
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            textBoxSeconds.Text = e.NewValue.ToString();
            string answer = GetUserFriendlilyTimeFromSeconds(e);
            LabelTime.Content = answer;
        }

        private static string GetUserFriendlilyTimeFromSeconds(RoutedPropertyChangedEventArgs<double> e)
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
            config.AppSettings.Settings["UseTemp"].Value = (checkBoxUseTemp.IsChecked != null && checkBoxUseTemp.IsChecked.Value).ToString();
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
                NetLog.ErrorHappened("DeleteSyncFiles", new ErrorEventArgs(exception));
                Console.WriteLine(exception);
            }
            try
            {
                _serviceDemo.Deletefileonserver("file.sync");
            }
            catch (Exception exception)
            {
                NetLog.ErrorHappened("DeleteSyncFiles", new ErrorEventArgs(exception));
                Console.WriteLine(exception);
            }
        }
    }

}
