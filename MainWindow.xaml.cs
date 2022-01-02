using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using Microsoft.Win32;
using qubic_miner_helper.Properties;
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
        private double lowestErrorCount = -1;
        private double secondLowestErrorCount = -1;
        public MainWindow mainWindowRef;
        public int testInt = 0;
        public double IterationsOverall = 0;
        private Timer collectDataTimer;


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

            _threadDetailsControlsList = new Dictionary<int, ThreadDetailsControl>();
            propertySliderStackPanel.Children.Clear();

            /*PipeReader pReader = new PipeReader();
            pReader.Start();
            pReader.DataReceived += PReader_DataReceived;
            */

            // Alle Threads initialisieren

            if (Properties.Settings.Default.AutoStart)
            {
                StartAllThreads();
            }
        }

        /*
        private void PReader_DataReceived(object sender, EventArgs e)
        {
            var tmp = e as PipeReader.DataReceivedEventArgs;

            Console.WriteLine("Data received: " + tmp.Data);
        }*/

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
            tmpSliderControl.Background = new SolidColorBrush(Colors.LightGray);
            tmpSliderControl.ThreadUpdated += TmpSliderControl_ThreadUpdated;

            this.ThreadCount.Text = ThreadIndex.ToString();

            propertySliderStackPanel.Children.Add(tmpSliderControl);

            _threadDetailsControlsList.Add(ThreadIndex, tmpSliderControl);

            isRunning = true;
        }

        private void CollectDataTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var iterationsOverall = CollectIterations();
            Dispatcher.Invoke(() =>
            {
                IterationsOverallBox.Text = iterationsOverall.ToString();
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

        private void checkAndCopyToFastestClick(object sender, RoutedEventArgs e)
        {
           //checkAndCopyToFastest();
        }

        /*
        private void checkAndCopyToFastest()
        {
            double minErrors = -1;
            ThreadDetailsControl fastestThreadControl = null;

            foreach (var threadDetails in _threadDetailsControlsList)
            {
                Dispatcher.Invoke(() => threadDetails.Value.Background = new SolidColorBrush(Colors.LightGray));
                if ((minErrors == -1 || threadDetails.Value.Errors < minErrors) && threadDetails.Value.Time > 0)
                {
                    //secondLowestErrorCount = minErrors;
                    minErrors = threadDetails.Value.Errors;
                    fastestThread = threadDetails.Key;
                    fastestThreadControl = threadDetails.Value;
                    Dispatcher.Invoke(() => fastestThreadControl.Background = new SolidColorBrush(Colors.LightGray));
                }
            }

            if (fastestThread != null)
            {
                Dispatcher.Invoke(() => fastestThreadIndex.Text = fastestThread.ToString());
                if (fastestThreadControl != null)
                {
                    if (fastestThreadControl.Errors >= 0 && fastestThreadControl.Time > 0)
                    {
                        if (fastestThreadControl.Errors < lowestErrorCount)
                        {
                            secondLowestErrorCount = lowestErrorCount;
                        }

                        fastestThread = fastestThreadControl.ThreadIndex;
                        lowestErrorCount = fastestThreadControl.Errors;
                        minErrors = lowestErrorCount;
                        if (secondLowestErrorCount > lowestErrorCount)
                        {
                            Dispatcher.Invoke(() => this.ErrdiffFastestNext.Text = (secondLowestErrorCount - lowestErrorCount).ToString());
                        }
                        Console.WriteLine(fastestThreadControl.Time + " -- " + fastestThreadControl.ThreadIndex);
                        Console.WriteLine("Lowest Error count: " + lowestErrorCount);
                        Console.WriteLine("Second Lowest Error count: " + secondLowestErrorCount);
                        Dispatcher.Invoke(() => fastestThreadControl.Background = new SolidColorBrush(Colors.LightGreen));
                    }
                }
            }
            

        }
        */


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

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            propertyScrollViewer.InvalidateVisual();
            propertyScrollViewer.UpdateLayout();
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

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

        private void SetTresholdClick(object sender, RoutedEventArgs e)
        {
            //Properties.Settings.Default.Threshold = Convert.ToDouble(ErrorThreshold.Text);
            Properties.Settings.Default.Save();
            MessageBox.Show("Threshold saved","Success",MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void StopAllButFastestClick(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                StopAllThreadsExceptFastest();
            }
        }

        private void SetMinerIDClick(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.MinerID = minerID.Text;
            Properties.Settings.Default.Save();
        }
    }
}
