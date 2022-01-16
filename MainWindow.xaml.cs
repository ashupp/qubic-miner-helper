using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Newtonsoft.Json;
using SimpleTcp;
using Timer = System.Timers.Timer;

namespace qubic_miner_helper
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<int, ThreadDetailsControl> _threadDetailsControlsList;
        private int ThreadIndex = 0;
        private bool isRunning = false;
        private int fastestThread = -1;
        public MainWindow mainWindowRef;
        private Timer collectDataTimer;
        private float ErrorsReduced = 0;
        private float IterationsOverall = 0;
        private bool ServerActive = false;
        private SimpleTcpClient Client;
        private DateTime ServerConnectionLastTry = DateTime.Now;
        private DateTime ServerConnectionLastSentData = DateTime.Now;
        private string currentQinerVersion = String.Empty;


        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            this.mainWindowRef = this;
            InitApp();
        }

        private void InitApp()
        {
            #region Initialize miner config
            this.minerPath.Text = Properties.Settings.Default.MinerPath;
            this.minerID.Text = Properties.Settings.Default.MinerID;
            this.ThreadCount.Text = Properties.Settings.Default.ThreadCount.ToString();

            this.CheckBoxAutostartOnOpen.IsChecked = Properties.Settings.Default.AutoStart;
            this.CheckBoxAutoRestartOnInactivity.IsChecked = Properties.Settings.Default.AutoRestartInactive;
            this.CheckBoxAutoRestartOnCrash.IsChecked = Properties.Settings.Default.AutoRestartCrashed;

            if (!Debugger.IsAttached)
            {
                if (Startup.IsInStartup("qubic-miner-helper", System.Reflection.Assembly.GetExecutingAssembly().Location))
                {
                    //this.CheckBoxAutostartOnWindowsStart.IsChecked = true;
                    Properties.Settings.Default.AutoStartOnWindowsStart = true;
                    Properties.Settings.Default.Save();
                }
            }
            #endregion


            #region Initialize server config
            this.CheckBoxConnectToServer.IsChecked = Properties.Settings.Default.ConnectToServer;
            //this.CheckBoxTransferAllMessages.IsChecked = Properties.Settings.Default.ServerTransferAllMessages;
            this.serverAddressTextBox.Text = Properties.Settings.Default.ServerAddress;

            if (Properties.Settings.Default.MachineName == String.Empty)
            {
                try
                {
                    MachineNameTextBox.Text = Environment.MachineName;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error during loading of MachineName: " + e.Message);
                }

            }
            else
            {
                MachineNameTextBox.Text = Properties.Settings.Default.MachineName;
            }
            #endregion Server




            _threadDetailsControlsList = new Dictionary<int, ThreadDetailsControl>();
            propertySliderStackPanel.Children.Clear();

            if (Properties.Settings.Default.AutoStart)
            {
                if (Properties.Settings.Default.MinerPath != String.Empty && File.Exists(Properties.Settings.Default.MinerPath))
                {
                    StartAllThreads();
                }
            }
        }

        private void SetMinerPathClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                Properties.Settings.Default.MinerPath = openFileDialog.FileName;
                Properties.Settings.Default.Save();
                minerPath.Text = openFileDialog.FileName;
            }
                
        }


        private void AddThreadClick(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.MinerPath != String.Empty && File.Exists(Properties.Settings.Default.MinerPath))
            {
                AddThread();
            }
        }

        private void AddThread()
        {
            if (collectDataTimer == null)
            {
                collectDataTimer = new Timer(1000);
                collectDataTimer.Elapsed += CollectDataTimer_Elapsed;
            }

            if (!collectDataTimer.Enabled)
            {
                collectDataTimer.Start();
            }



            ThreadIndex++;
            var tmpSliderControl = new ThreadDetailsControl()
            {
                Name = "test1",
                PName = "Worker: " + ThreadIndex,
                IsEnabled = true,
                ThreadIndex = ThreadIndex
            };
            tmpSliderControl.ErrorsReduced += TmpSliderControl_ErrorsReduced;
            tmpSliderControl.Background = new SolidColorBrush(Colors.LightGray);
            tmpSliderControl.ThreadUpdated += TmpSliderControl_ThreadUpdated;

            this.ThreadCount.Text = ThreadIndex.ToString();

            propertySliderStackPanel.Children.Add(tmpSliderControl);

            _threadDetailsControlsList.Add(ThreadIndex, tmpSliderControl);

            isRunning = true;
        }

        private void TmpSliderControl_ErrorsReduced(object sender, ErrorsReducedEventArgs e)
        {
            ErrorsReduced += e.ErrorCount;
            ErrorsReducedOverallBox.Text = ErrorsReduced.ToString();
        }

        private void CollectDataTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var iterationsOverall = CollectIterations();
            Dispatcher.Invoke(() =>
            {
                IterationsOverallBox.Text = iterationsOverall.ToString().Replace(",",".");
                if (ServerActive)
                {
                    if (Client != null && Client.IsConnected)
                    {
                        if (ServerConnectionLastSentData.AddSeconds(Properties.Settings.Default.ServerTransferEverySeconds) < DateTime.Now)
                        {
                            sendData();
                        }
                    }
                    else
                    {
                        if (ServerConnectionLastTry.AddSeconds(10) < DateTime.Now)
                        {
                            ServerConnectionLastTry = DateTime.Now;
                            ServerReconnect();
                        }
                            
                    }
                }
            });
        }

        private object CollectIterations()
        {
            double iterationsCount = 0;
            foreach (var threadDetails in _threadDetailsControlsList)
            {
                iterationsCount += threadDetails.Value.Iterations;
                currentQinerVersion = threadDetails.Value.qinerVersion;
            }
            IterationsOverall = (float)iterationsCount;
            
            return iterationsCount;
        }

        private void TmpSliderControl_ThreadUpdated(object sender, EventArgs e)
        {
            var tmpControl = sender as ThreadDetailsControl;
            Console.WriteLine("Thread Updated: " + tmpControl.ThreadIndex + " - Errors: " + tmpControl.Errors);
            // Recalculate all err diffs and get best performing thread
            //checkAndCopyToFastest();
        }

        private void RemoveThreadClick(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Current childs: " + propertySliderStackPanel.Children.Count);
            if (ThreadIndex > 0)
            {

                var mySlider = propertySliderStackPanel.Children[ThreadIndex - 1] as ThreadDetailsControl;
                mySlider.StopThread();
                mySlider.ThreadUpdated -= TmpSliderControl_ThreadUpdated;
                mySlider.ErrorsReduced -= TmpSliderControl_ErrorsReduced;
                propertySliderStackPanel.Children.Remove(mySlider);
                _threadDetailsControlsList.Remove(ThreadIndex);

                ThreadIndex--;

                this.ThreadCount.Text = ThreadIndex.ToString();
            }

            if (ThreadIndex == 0)
            {
                isRunning = false;
                if (collectDataTimer != null && collectDataTimer.Enabled)
                {
                    collectDataTimer.Stop();
                    IterationsOverallBox.Text = "waiting for data...";
                }
            }
        }

        private void Button_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Console.WriteLine("Closed - Saving");
            Properties.Settings.Default.ThreadCount = ThreadIndex;
            Properties.Settings.Default.Save();
            StopAllThreads();
            Console.WriteLine("Go home :)");
        }

        private void StartAllThreads()
        {
            for (int i = 0; i < Properties.Settings.Default.ThreadCount; i++)
            {
                this.AddThread();
            }

            if (this.ThreadIndex >= 1)
            {
                this.isRunning = true;
            }
        }


        private void StopAllThreadsExceptFastest()
        {
            // 
            Console.WriteLine("Closing all Threads except fastest");
            for (int i = ThreadIndex; i > 0; i--)
            {
                if (i == fastestThread)
                {
                    Console.WriteLine("Keeping " + fastestThread + " alive");
                }

                if (i != fastestThread)
                {
                    Console.WriteLine("Closing: " + i);
                    var mySlider = propertySliderStackPanel.Children[i-1] as ThreadDetailsControl;
                    mySlider.StopThread();
                    mySlider.ThreadUpdated -= TmpSliderControl_ThreadUpdated;
                    mySlider.ErrorsReduced -= TmpSliderControl_ErrorsReduced;
                    propertySliderStackPanel.Children.Remove(mySlider);
                    _threadDetailsControlsList.Remove(ThreadIndex);

                   
                }
            }
            if (this.ThreadIndex <= 0)
            {
                this.isRunning = false;
            }

            // Check running Threads:
            ThreadIndex = propertySliderStackPanel.Children.Count;
            this.ThreadCount.Text = ThreadIndex.ToString();
            
        }

        private void StopAllThreads()
        {
            // 
            Console.WriteLine("Unloaded... closing all Threads...");
            for (int i = ThreadIndex; i > 0; i--)
            {
                if (ThreadIndex > 0)
                {

                    var mySlider = propertySliderStackPanel.Children[ThreadIndex - 1] as ThreadDetailsControl;
                    mySlider.StopThread();
                    mySlider.ThreadUpdated -= TmpSliderControl_ThreadUpdated;
                    mySlider.ErrorsReduced -= TmpSliderControl_ErrorsReduced;
                    propertySliderStackPanel.Children.Remove(mySlider);
                    _threadDetailsControlsList.Remove(ThreadIndex);

                    ThreadIndex--;

                    this.ThreadCount.Text = ThreadIndex.ToString();
                }
            }
            if (this.ThreadIndex <= 0)
            {
                this.isRunning = false;
                if (collectDataTimer != null && collectDataTimer.Enabled)
                {
                    collectDataTimer.Stop();
                    IterationsOverallBox.Text = "waiting for data...";
                }
            }
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                StopAllThreads();
            }
            else
            {
                StartAllThreads();
            }
        }
        

        private void AutoStartOnOpenCheckboxClicked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoStart = (bool)this.CheckBoxAutostartOnOpen.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void CheckBoxAutostartOnOpen_OnChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoStart = (bool)this.CheckBoxAutostartOnOpen.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void CheckBoxAutostartOnOpen_OnUnchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoStart = (bool)this.CheckBoxAutostartOnOpen.IsChecked;
            Properties.Settings.Default.Save();
        }


        private void SetMinerIDClick(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.MinerID = minerID.Text;
            Properties.Settings.Default.Save();
        }

        private void CheckBoxAutoRestartOnInactivity_OnChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoRestartInactive = (bool)this.CheckBoxAutoRestartOnInactivity.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void CheckBoxAutoRestartOnInactivity_OnUnchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoRestartInactive = (bool)this.CheckBoxAutoRestartOnInactivity.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void CheckBoxAutoRestartOnCrash_OnChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoRestartCrashed = (bool)this.CheckBoxAutoRestartOnCrash.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void CheckBoxAutoRestartOnCrash_OnUnChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoRestartCrashed = (bool)this.CheckBoxAutoRestartOnCrash.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void ViewTop10Click(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.MinerPath != "" && File.Exists(Properties.Settings.Default.MinerPath))
            {
                try
                {
                    var top10Process = new ProcessStartInfo();
                    top10Process.FileName = Properties.Settings.Default.MinerPath;
                    var onlyMinerId = Properties.Settings.Default.MinerID.Split(' ')[0];
                    top10Process.Arguments = onlyMinerId + " 0 5";
                    top10Process.WorkingDirectory = System.IO.Path.GetDirectoryName(Properties.Settings.Default.MinerPath);
                    top10Process.UseShellExecute = true;
                    top10Process.CreateNoWindow = false;
                    top10Process.WindowStyle = ProcessWindowStyle.Normal;
                    _ = new Process() { StartInfo = top10Process }.Start();
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Could not start top10 process: " + exception.Message);
                }

            }
        }

        private void onCommandLineTextChanged(object sender, TextChangedEventArgs e){
            if (minerID.Text != String.Empty)
            {
                Properties.Settings.Default.MinerID = minerID.Text;
                Properties.Settings.Default.Save();
            }
        }

        /*
        private void CheckBoxAutostartOnWindowsStart_OnUnChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoStartOnWindowsStart = (bool)this.CheckBoxAutostartOnWindowsStart.IsChecked;
            Properties.Settings.Default.Save();
            Startup.RunOnStartup("qubic-miner-helper", System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private void CheckBoxAutostartOnWindowsStart_OnChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.AutoStartOnWindowsStart = (bool)this.CheckBoxAutostartOnWindowsStart.IsChecked;
            Properties.Settings.Default.Save();
            Startup.RemoveFromStartup("qubic-miner-helper");
        }
        */

        private void onMachineNameTextChanged(object sender, TextChangedEventArgs e)
        {
            if (MachineNameTextBox.Text != String.Empty)
            {
                Properties.Settings.Default.MachineName = MachineNameTextBox.Text;
                Properties.Settings.Default.Save();
            }
        }

        private void CheckBoxConnectToServer_OnChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ConnectToServer = (bool)this.CheckBoxConnectToServer.IsChecked;
            Properties.Settings.Default.Save();

            if (!ServerActive)
            {
                StartServerConnection();
            }
        }

        private void CheckBoxConnectToServer_OnUnChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ConnectToServer = (bool)this.CheckBoxConnectToServer.IsChecked;
            Properties.Settings.Default.Save();
            
            if (ServerActive)
            {
                StopServerConnection();
            }
        }

        private void onServerAddressTextChanged(object sender, TextChangedEventArgs e)
        {
            if (serverAddressTextBox.Text != String.Empty)
            {
                Properties.Settings.Default.ServerAddress = serverAddressTextBox.Text;
                Properties.Settings.Default.Save();
            }
        }

        /*
        private void CheckBoxTransferAllMessages_onChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ServerTransferAllMessages = (bool)this.CheckBoxTransferAllMessages.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void CheckBoxTransferAllMessages_onUnChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ServerTransferAllMessages = (bool)this.CheckBoxTransferAllMessages.IsChecked;
            Properties.Settings.Default.Save();
        }*/

        public void StartServerConnection()
        {
            Console.WriteLine("StartServerConnection");
            try
            {
                ServerActive = true;

                if (Properties.Settings.Default.ServerAddress != String.Empty)
                {
                    Client = new SimpleTcpClient(Properties.Settings.Default.ServerAddress);

                    Client.Events.Connected += Client_Connected;
                    Client.Events.Disconnected += Client_Disconnected;
                    Client.Events.DataReceived += Client_DataReceived;
                    Client.Connect();
                }
            }
            catch (Exception e)
            {
                serverConnectionStatusTextBox.Text = e.Message;
            }

        }

        public void ServerReconnect()
        {
            Console.WriteLine("Reconnecting to server...");
            try
            {
                Client.Connect();
            }
            catch (Exception e)
            {
                serverConnectionStatusTextBox.Text = "Could not connect: " + e.Message;
                Console.WriteLine("Could not connect to server...");
            }
        }

        public void StopServerConnection()
        {
            Console.WriteLine("StopServerConnection");
            try
            {
                ServerActive = false;
                if (Client.IsConnected)
                {
                    Client.Events.Connected -= Client_Connected;
                    Client.Events.Disconnected -= Client_Disconnected;
                    Client.Events.DataReceived -= Client_DataReceived;
                    Client.Disconnect();
                    Client.Dispose();
                }
                serverConnectionStatusTextBox.Text = "disconnected";
            }
            catch (Exception e)
            {
                serverConnectionStatusTextBox.Text = e.Message;
            }

        }

        private void Client_DataReceived(object sender, SimpleTcp.DataReceivedEventArgs e)
        {
            Console.WriteLine("Client received data from server: " + e.Data);
        }

        private void Client_Disconnected(object sender, ConnectionEventArgs e)
        {
            Console.WriteLine("Client disconnected from server: " + e.IpPort);
            serverConnectionStatusTextBox.Text = "disconnected";
        }

        private void Client_Connected(object sender, ConnectionEventArgs e)
        {
            Console.WriteLine("Client connected to server: " + e.IpPort);
            serverConnectionStatusTextBox.Text = "connected";
        }


        private void sendData()
        {
            MachineState machineState = new MachineState();
            machineState.currentCommandLine = minerID.Text;
            machineState.currentMachineDateTime = DateTime.Now;
            machineState.currentMinerPath = minerPath.Text;
            machineState.machineName = MachineNameTextBox.Text;
            machineState.overallWorkerCount = _threadDetailsControlsList.Count;
            machineState.overallCurrentIterationsPerSecond = IterationsOverall;
            machineState.overallSessionErrorsFound = ErrorsReduced;
            machineState.currentMinerVersion = currentQinerVersion;
            machineState.currentHelperVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            machineState.overallThreadCount = 0;
            machineState.currentWorkerStates = new List<WorkerState>();

            foreach (var threadDetails in _threadDetailsControlsList)
            {
                try
                {
                    var workerState = new WorkerState();
                    workerState.errorsLeftText = threadDetails.Value.ErrorsBox.Text;
                    workerState.rankText = threadDetails.Value.RankBox.Text;
                    machineState.currentWorkerStates.Add(workerState);
                }
                catch (Exception e)
                {
                   Console.WriteLine("could not set workerState data: " + e.Message);
                }
          
            }


            if (Client.IsConnected)
            {
                Client.Send(JsonConvert.SerializeObject(machineState) + Environment.NewLine);
            }
            else
            {
                Console.WriteLine("Client not connected...");
            }
        }

        private void ForceReconnectButton_onClick(object sender, RoutedEventArgs e)
        {
            if (ServerActive)
            {
                StopServerConnection();
            }
            StartServerConnection();
        }
    }
}
