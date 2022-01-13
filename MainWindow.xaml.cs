using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
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


        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            this.mainWindowRef = this;
            InitApp();
        }

        private void InitApp()
        {
            this.minerPath.Text = Properties.Settings.Default.MinerPath;
            this.minerID.Text = Properties.Settings.Default.MinerID;
            this.ThreadCount.Text = Properties.Settings.Default.ThreadCount.ToString();

            this.CheckBoxAutostartOnOpen.IsChecked = Properties.Settings.Default.AutoStart;
            this.CheckBoxAutoRestartOnInactivity.IsChecked = Properties.Settings.Default.AutoRestartInactive;
            this.CheckBoxAutoRestartOnCrash.IsChecked = Properties.Settings.Default.AutoRestartCrashed;

            _threadDetailsControlsList = new Dictionary<int, ThreadDetailsControl>();
            propertySliderStackPanel.Children.Clear();

            if (Properties.Settings.Default.AutoStart)
            {
                StartAllThreads();
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
            AddThread();

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
            });
           
        }

        private object CollectIterations()
        {
            double iterationsCount = 0;
            foreach (var threadDetails in _threadDetailsControlsList)
            {
                iterationsCount += threadDetails.Value.Iterations;
            }
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
                var top10Process = new ProcessStartInfo();
                top10Process.FileName = Properties.Settings.Default.MinerPath;
                top10Process.Arguments = "0 0 5";
                top10Process.WorkingDirectory = System.IO.Path.GetDirectoryName(Properties.Settings.Default.MinerPath);
                top10Process.UseShellExecute = true;
                top10Process.CreateNoWindow = false;
                top10Process.WindowStyle = ProcessWindowStyle.Normal;
                _ = new Process() { StartInfo = top10Process }.Start();
            }
        }

        private void onCommandLineTextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.MinerID = minerID.Text;
            Properties.Settings.Default.Save();
        }
    }
}
