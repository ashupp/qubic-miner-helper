using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
using System.Xml;
using CliWrap;
using qubic_miner_helper.Properties;
using Xceed.Wpf.Toolkit.Core.Converters;
using Timer = System.Timers.Timer;

namespace qubic_miner_helper
{

    public class KeyValueEventArgs : EventArgs
    {
        public int Key { get; set; }
        public int Value { get; set; }
    }

    /// <summary>
    /// Interaktionslogik für ThreadDetailsControl.xaml
    /// </summary>
    public partial class ThreadDetailsControl : UserControl
    {
        private Process currentMinerProcess;
        private Thread thread1;
        private DateTime lastUpdateDateTime;
        private DateTime launchDateTime;

        private Timer activeTimer;


        public int ThreadIndex;
        private int restartCounter;
        public double Iterations;
        public double Errors;
        public double Layers;
        public double Neurons;
        public double Time;

        #region DependencyProperties
        public static readonly DependencyProperty KeyProperty = DependencyProperty.Register("Key", typeof(int), typeof(ThreadDetailsControl));
        public static readonly DependencyProperty PNameProperty = DependencyProperty.Register("PName", typeof(string), typeof(ThreadDetailsControl));
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(ThreadDetailsControl));
        public static readonly DependencyProperty CurrentValueProperty = DependencyProperty.Register("CurrentValue", typeof(int), typeof(ThreadDetailsControl));
        public static readonly DependencyProperty MinValueProperty = DependencyProperty.Register("MinValue", typeof(int), typeof(ThreadDetailsControl));
        public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register("MaxValue", typeof(int), typeof(ThreadDetailsControl));
        public static readonly DependencyProperty DefaultValueProperty = DependencyProperty.Register("DefaultValue", typeof(int), typeof(ThreadDetailsControl));
        #endregion

        public ThreadDetailsControl()
        {
            InitializeComponent();
            this.DataContext = this;

            Init();
        }



        public void DoWork()
        {
            Console.WriteLine("Starting" + Properties.Settings.Default.MinerPath);
            

            //StreamReader outputStreamReader = new StreamReader()

            currentMinerProcess = new Process();
            currentMinerProcess.EnableRaisingEvents = true;
            
            currentMinerProcess.StartInfo.FileName = Properties.Settings.Default.MinerPath;
            currentMinerProcess.StartInfo.Arguments = Properties.Settings.Default.MinerID;
            try
            {
                var threadCountPerWorker = Properties.Settings.Default.MinerID.Split(' ')[1];
                Dispatcher.Invoke(() => labelThreadsNumber.Content = "Worker threads: "+ threadCountPerWorker);
            }
            catch (Exception e)
            {
                Dispatcher.Invoke(() => labelThreadsNumber.Content = "Missing Threadcount per Worker " + e.Message);
            }
            
            currentMinerProcess.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(Properties.Settings.Default.MinerPath);
            currentMinerProcess.StartInfo.UseShellExecute = false;
            currentMinerProcess.StartInfo.CreateNoWindow = true;
            currentMinerProcess.StartInfo.LoadUserProfile = false;
            //currentMinerProcess.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;

            currentMinerProcess.StartInfo.RedirectStandardOutput = true;
            currentMinerProcess.StartInfo.RedirectStandardError = true;
            //currentMinerProcess.OutputDataReceived += (sender, args) => Console.WriteLine("received output: {0}", args.Data);
            currentMinerProcess.OutputDataReceived += CurrentMinerProcess_OutputDataReceived;
            currentMinerProcess.ErrorDataReceived += CurrentMinerProcess_ErrorDataReceived;
            currentMinerProcess.Exited += CurrentMinerProcess_Exited;
            var started = currentMinerProcess.Start();

            Console.WriteLine("Starte loop");
            /*while (!currentMinerProcess.StandardOutput.EndOfStream)
            {
                string line = currentMinerProcess.StandardOutput.ReadLine();
                Console.WriteLine("hab daten vom Thread: " + line);
            }*/

            if (started)
            {
                Console.WriteLine("Thread started...");
                launchDateTime = DateTime.Now;
                Dispatcher.Invoke(() => StartedBox.Text = launchDateTime.ToString());
                activeTimer.Start();
                lastUpdateDateTime = DateTime.Now;
                Thread.Sleep(1000);
                currentMinerProcess.BeginOutputReadLine();
                currentMinerProcess.BeginErrorReadLine();
            }
            else
            {
                Console.WriteLine("Thread NOT started??? ");
            }
        }


        public void Init()
        {
            thread1 = new Thread(DoWork);
            thread1.Start();
            
            activeTimer = new Timer(1000);
            activeTimer.Elapsed += ActiveTimer_Elapsed;
        }

        private void ActiveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (lastUpdateDateTime != null && lastUpdateDateTime < DateTime.Now.Subtract(new TimeSpan(0,0,Settings.Default.MinRoundsWaitBeforeLaunchAgain)))
            {
                Console.WriteLine("Miner Inactive....restarting");
                Dispatcher.Invoke(() => RestartThread());
            }
        }

        private void CurrentMinerProcess_Exited(object sender, EventArgs e)
        {
            Console.WriteLine("Miner Process really exited");
        }

        private void CurrentMinerProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            lastUpdateDateTime = DateTime.Now;

            if (e.Data != null)
            {
                //Console.WriteLine("Data received: " + e.Data);

                setData(e.Data);

            }
            else
            {
                Console.WriteLine("Data received ..... but null");
            }
            
        }

        private void CurrentMinerProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("Error Data received: " + e.Data);
        }


        private void setData(string eData)
        {



            //Console.WriteLine(eData);
            try
            {

                Dispatcher.Invoke(() => LastUpdateBox.Text = DateTime.Now.ToString("T"));

            }
            catch (Exception e)
            {
                Console.WriteLine("Error during setting of last update. " + e.Message);
                Dispatcher.Invoke(() => LastUpdateBox.Text = e.Message);
            }


            // Rank
            try
            {
                if (eData.Contains("You are #"))
                {
                    Dispatcher.Invoke(() =>
                    {
                        RankBox.Text = eData;
                    });
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Error during setting of rank. " + e.Message);
                Dispatcher.Invoke(() => RankBox.Text = e.Message);
            }
            
            
            
            try
            {
                if (eData.Contains("Managed to find a solution reducing number of errors by"))
                {

                    var tmpPartA = eData.Split(new string[] { "Managed to find a solution reducing number of errors by " }, StringSplitOptions.None)[1].Split(new string[] { " (" }, StringSplitOptions.None)[0];
                    //threadErrorReductions+= Convert.ToDouble(tmpPartA);
                    
                    Dispatcher.Invoke(() =>
                    {
                        LastErrorReductionTimeBox.Text = DateTime.Now.ToString();
                        LastErrorReductionCountBox.Text = tmpPartA;
                    });
                }

            }
            catch (Exception e)
            { 
                Console.WriteLine("Error during setting of reduction error and count. " + e.Message);
                Dispatcher.Invoke(() =>
                {
                    LastErrorReductionTimeBox.Text = e.Message;
                    LastErrorReductionCountBox.Text = e.Message;
                });
            }            
            
            
            
            // Iterations
            try
            {
                if (eData.Contains("Your iteration rate on this hardware is"))
                {

                    var tmpPartA = eData.Split(new string[] { "Your iteration rate on this hardware is " }, StringSplitOptions.None)[1].Split(new string[] { " iterations/s" }, StringSplitOptions.None)[0];
                    Iterations = Double.Parse(tmpPartA, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
                    Dispatcher.Invoke(() =>
                    {
                        IterationBox.Text = eData;
                    });
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Error during setting of iterations. " + e.Message);
                Dispatcher.Invoke(() => IterationBox.Text = e.Message);
            }    
            

            // SolutionRate self
            try
            {
                if (eData.Contains("Your error elimination rate on this hardware is"))
                {
                    Dispatcher.Invoke(() =>
                    {
                        SolutionsBox.Text = eData;
                    });
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Error during setting of self error rate. " + e.Message);
                Dispatcher.Invoke(() => SolutionsBox.Text = e.Message);
            }


            // SolutionRate pool
            try
            {
                if (eData.Contains("Pool error elimination rate is"))
                {
                    Dispatcher.Invoke(() =>
                    {
                        PoolSolsBox.Text = eData;
                    });
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Error during setting of pool error rate. " + e.Message);
                Dispatcher.Invoke(() => PoolSolsBox.Text = e.Message);
            }




            try
            {
                if (eData.Contains("errors left"))
                {
                    var errBoxTextTmp = eData.Split(new string[] { "are " }, StringSplitOptions.None)[1].Split(new string[] { " errors" }, StringSplitOptions.None)[0];
                    Dispatcher.Invoke(() =>
                    {
                        ErrorsBox.Text = errBoxTextTmp;
                        Errors = Double.Parse(errBoxTextTmp, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.NumberFormatInfo.InvariantInfo);
                        Console.WriteLine(Errors);
                    });
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Error during loading of errors. " + e.Message);
                Dispatcher.Invoke(() => Errors = -1);

            }

            /*
            try
            {
                if (eData.Contains("You are"))
                {
                    var errBoxTextTmp = eData.Split(new string[] { "You are " }, StringSplitOptions.None)[1].Split(new string[] { " errors" }, StringSplitOptions.None)[0];
                    Dispatcher.Invoke(() =>
                    {
                        Errors = Convert.ToDouble(errBoxTextTmp);
                    });
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Error during loading of errors. " + e.Message);
                Dispatcher.Invoke(() => Errors = -1);

            }
            */

     

            /*
            var splitData = eData.Split('|');

            try
            {

                Dispatcher.Invoke(() => Iterations = Convert.ToInt32(splitData[0].Split('#')[1].Trim()));

            }
            catch (Exception e)
            {
                Console.WriteLine("Error during loading of iterations. " + e.Message);
                Dispatcher.Invoke(() => Iterations = -1);
            }


            try
            {
                Dispatcher.Invoke(() => Errors = Convert.ToDouble(splitData[1].Split('=')[1].Trim()));

            }
            catch (Exception e)
            {
                Console.WriteLine("Error during loading of errors. " + e.Message);
                Dispatcher.Invoke(() => Errors = -1);

            }

            try
            {
                Dispatcher.Invoke(() => Layers = Convert.ToInt32(splitData[2].Split('=')[1].Trim()));

            }
            catch (Exception e)
            {
                Console.WriteLine("Error during loading of layers. " + e.Message);
                Dispatcher.Invoke(() => Layers = -1);
            }


            try
            {
                Dispatcher.Invoke(() => Neurons = Convert.ToInt32(splitData[3].Split('=')[1].Trim()));

            }
            catch (Exception e)
            {
                Console.WriteLine("Error during loading of neurons. " + e.Message);
                Dispatcher.Invoke(() => Neurons = -1);

            }


            try
            {
                var myTimePartOne = splitData[4].Split('=')[1].Trim();
                var myTimePartTwo = myTimePartOne.Split(new string[] { "ms" }, StringSplitOptions.None)[0].Trim();

                Dispatcher.Invoke(() => Time = Convert.ToDouble(myTimePartTwo));

            }
            catch (Exception e)
            {
                Console.WriteLine("Error during loading of time. " + e.Message);
                Dispatcher.Invoke(() => Time = -1);

            }
            
            Dispatcher?.Invoke(
                DispatcherPriority.Normal,
                new Action(() =>
                {
                    IterationBox.Text = Iterations.ToString();
                    ErrorsBox.Text = Errors.ToString();
                    TimeBox.Text = Time.ToString();
                    LayersBox.Text = Layers.ToString();
                    NeuronsBox.Text = Neurons.ToString();
                }));


            if (ThreadUpdated != null)
            {
                ThreadUpdated(this, EventArgs.Empty);
            }
            */
        }
            

        public void Refresh(UIElement uiElement)

        {
            Dispatcher.Invoke(() => uiElement.InvalidateVisual());

            //uiElement.Dispatcher.Invoke(DispatcherPriority.Render, new );
        }

        #region Members

        public int Key
        {
            get { return (int)GetValue(KeyProperty); }
            set { SetValue(KeyProperty, value); }
        }




        public string PName
        {
            get { return (string)GetValue(PNameProperty); }
            set { SetValue(PNameProperty, value); }
        }

        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public int CurrentValue
        {
            get { return (int)GetValue(CurrentValueProperty); }
            set
            {
                SetValue(CurrentValueProperty, value);
            }
        }

        public int MinValue
        {
            get { return (int)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }

        public int MaxValue
        {
            get { return (int)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }
        public int DefaultValue
        {
            get { return (int)GetValue(DefaultValueProperty); }
            set { SetValue(DefaultValueProperty, value); }
        }

        #endregion

        #region Own Eventhandlers
        public event EventHandler<KeyValueEventArgs> SetValueClicked;
        public event EventHandler<KeyValueEventArgs> DefaultValueClicked;

        public event EventHandler ThreadUpdated;
        #endregion

        #region Eventhandlers
        private void btnSetValue_Click(object sender, RoutedEventArgs e)
        {
            if (SetValueClicked != null)
            {
                var args = new KeyValueEventArgs()
                {
                    Key = Key,
                    //Value = Convert.ToInt32(textBoxValue.Text)
                };
                SetValueClicked(this, args);
            }
        }

        private void btnResetDefault_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Exit this");
            /*
            MessageBoxResult messageBoxResult = MessageBox.Show("Reset to default (Value:" + DefaultValue + ")", "Are you sure?", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                if (DefaultValueClicked == null) return;
                propertySlider.Value = DefaultValue;
                var args = new KeyValueEventArgs()
                {
                    Key = Key,
                    Value = Convert.ToInt32(DefaultValue)
                };
                DefaultValueClicked(this, args);
            }*/
        }

        #endregion

        public void RestartThread()
        {
            restartCounter++;
            Dispatcher.Invoke(() => RestartCountBox.Text = restartCounter.ToString());
            StopThread();
            Init();
        }

        public void StopThread()
        {
            activeTimer.Stop();
            activeTimer.Elapsed -= ActiveTimer_Elapsed;

            thread1.Abort();
            Console.WriteLine("Stopping Thread");

            if (currentMinerProcess != null)
            {
                Console.WriteLine("Closing");
                if (!currentMinerProcess.HasExited)
                {
                    currentMinerProcess.Kill();
                }
                else
                {
                    Console.WriteLine("Was exited already");
                }
                //currentMinerProcess.CloseMainWindow();
                //currentMinerProcess.Close();
            }
            else
            {
                Console.WriteLine("Was killed already");
            }
            

        }
    }
}
