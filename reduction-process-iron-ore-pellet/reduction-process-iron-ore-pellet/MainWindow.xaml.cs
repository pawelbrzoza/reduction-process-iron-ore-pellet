using grain_growth.Algorithms;
using grain_growth.Models;
using grain_growth.Helpers;

using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Drawing;
using System.IO;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Text;
using System.Drawing.Imaging;

namespace grain_growth
{
    public partial class MainWindow : Window
    {
        private MainProperties Properties;
        private readonly Phase[] Phases = InitializeArray.Init<Phase>(Constants.NUMBER_OF_PHASES);
        private readonly Task<Range>[] Tasks = new Task<Range>[Constants.NUMBER_OF_PHASES];
        private readonly DispatcherTimer MainDispatcher = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 0) };
        private readonly DispatcherTimer TempteratureDispatcher = new DispatcherTimer();
        private readonly SynchronizationContext MainThread = SynchronizationContext.Current;
        private Bitmap MainBitmap;
        private Stopwatch StopWatch;
        private StringBuilder csv;

        public MainWindow()
        {
            InitializeComponent();
            MainDispatcher.Tick += PhasesDispatcherTick;
            TempteratureDispatcher.Tick += TemperaturDispatcherTick;
            if (MainThread == null) MainThread = new SynchronizationContext();

            ConstantGrowthRadioButton.IsChecked = true;
            Phase1NameLabel.Content = Phase1NameTextBox.Text + " [%]";
            Phase2NameLabel.Content = Phase2NameTextBox.Text + " [%]";
            Phase3NameLabel.Content = Phase3NameTextBox.Text + " [%]";
            Phase4NameLabel.Content = Phase4NameTextBox.Text + " [%]";
        }

        private void SetProperties()
        {
            MainBitmap = new Bitmap((int)PelletImage.Width, (int)PelletImage.Height);

            // properties
            Properties = new MainProperties()
            {
                RangeWidth = (int)PelletImage.Width,
                RangeHeight = (int)PelletImage.Height,
                AmountOfGrains = int.Parse(AmountOfGrainsTextBox.Text),
                AmountOfInclusions = int.Parse(AmountOfInclusionsTextBox.Text),
                PelletSize = int.Parse(PelletSizeTextBox.Text),
                CurrGrowthProbability = int.Parse(ConstGrowthProbabilityTextBox.Text),
                ConstGrowthProbability = int.Parse(ConstGrowthProbabilityTextBox.Text),
                CurrTemperature = 0,
                TemperatureRiseRate = int.Parse(TemperatureRiseRateTextBox.Text),
                MaxTemperature = int.Parse(MaxTemperatureTextBox.Text),
                BufferTemperature = 0,
                NeighbourhoodType = ChooseNeighbourhoodType(),
                StartingPointsType = ChooseStartingPointsType(),
            };

            // update constants
            Constants.UpdateConstants(Properties.PelletSize);

            // initialization inclusion points inside update area
            InclusionHandler.InitPoints(Properties.AmountOfInclusions);

            // initialization inclusion points inside update area
            StructureHandler.PointsArea = StructureHandler.GetPointsInsideCircle((int)Constants.RADIUS, Constants.MIDDLE_POINT);

            // phases
            Phase1NameLabel.Content = Phase1NameTextBox.Text + " [%]";
            Phase2NameLabel.Content = Phase2NameTextBox.Text + " [%]";
            Phase3NameLabel.Content = Phase3NameTextBox.Text + " [%]";
            Phase4NameLabel.Content = Phase4NameTextBox.Text + " [%]";

            Phases[0].Name = Phase1NameTextBox.Text;
            Phases[1].Name = Phase2NameTextBox.Text;
            Phases[2].Name = Phase3NameTextBox.Text;
            Phases[3].Name = Phase4NameTextBox.Text;

            Phases[0].TemperaturePoint = int.Parse(Phase1TextBox.Text);
            Phases[1].TemperaturePoint = int.Parse(Phase2TextBox.Text);
            Phases[2].TemperaturePoint = int.Parse(Phase3TextBox.Text);
            Phases[3].TemperaturePoint = int.Parse(Phase4TextBox.Text);

            Phases[0].Color = Converters.WindowsToDrawingColor(Phase1ColorPicker.SelectedColor.Value);
            Phases[1].Color = Converters.WindowsToDrawingColor(Phase2ColorPicker.SelectedColor.Value);
            Phases[2].Color = Converters.WindowsToDrawingColor(Phase3ColorPicker.SelectedColor.Value);
            Phases[3].Color = Converters.WindowsToDrawingColor(Phase4ColorPicker.SelectedColor.Value);

            Phases[0].GrowthProbability = int.Parse(Phase1PropabilityTextBox.Text);
            Phases[1].GrowthProbability = int.Parse(Phase2PropabilityTextBox.Text);
            Phases[2].GrowthProbability = int.Parse(Phase3PropabilityTextBox.Text);
            Phases[3].GrowthProbability = int.Parse(Phase4PropabilityTextBox.Text);

            for (int i = 0; i < Constants.NUMBER_OF_PHASES; i++)
            {
                Phases[i].Range = new Range(Properties.RangeWidth, Properties.RangeWidth);
                StructureHandler.InitBlankArea(Phases[i].Range);
                InclusionHandler.AddInclusions(Phases[i].Range);
                Phases[i].Percentage = 0;

                if (ConstantGrowthRadioButton.IsChecked == true)
                    Phases[i].GrowthProbability = Properties.ConstGrowthProbability;
                else if (ProgresiveGrowthRadioButton.IsChecked == true)
                    Phases[i].GrowthProbability = (100 / Constants.NUMBER_OF_PHASES) * (i + 1);

                Properties.CurrGrowthProbability = Phases[i].GrowthProbability;
                Phases[i].Properties = ObjectCopier.Clone(Properties);

                Phases[i].PrevRange = StructureHandler.InitCellularAutomata(Properties, i + 1, Phases[i].Color);
                InclusionHandler.AddInclusions(Phases[i].PrevRange);
            }

            // !! PHASE 1 - INITIALIZATION !! //
            StructureHandler.InstantFillColor(Phases[0], 1, Phases[0].Color);

            // temperature dispatcher
            TempteratureDispatcher.Interval = new TimeSpan(0, 0, 0, 0, Properties.TemperatureRiseRate);

            // .csv file
            csv = new StringBuilder();
            var newLine = string.Format("{0};{1};{2};{3};{4};{5}", "Time", "Temperature", Phases[0].Name, Phases[1].Name, Phases[2].Name, Phases[3].Name);
            csv.AppendLine(newLine);
        }

        private void TemperaturDispatcherTick(object sender, EventArgs e)
        {
            if (Properties.CurrTemperature < Properties.MaxTemperature)
            {
                Properties.CurrTemperature = ((int)StopWatch.Elapsed.TotalMilliseconds / Properties.TemperatureRiseRate)
                    - Properties.BufferTemperature;
                temperatureLabel.Content = Convert.ToString(Properties.CurrTemperature);
            }
            else if (Properties.CurrTemperature >= Properties.MaxTemperature && MainDispatcher.IsEnabled)
            {
                WriteToFileTheEnd();

                Application.Current.Dispatcher.Invoke(() => {
                    Mouse.OverrideCursor = null;
                });

                MainDispatcher.Stop();
                TempteratureDispatcher.Stop();
            }

            CollectData();
        }

        private void PhasesDispatcherTick(object sender, EventArgs e)
        {
            for (int p = 0; p < Constants.NUMBER_OF_PHASES; p++)
            {
                var x = p;
                Tasks[p] = Task.Run(() => PhaseGenerator(x));
            }

            Task.WaitAll(Tasks);

            for (int p = 0; p < Constants.NUMBER_OF_PHASES; p++)
            {
                Phases[p].Range = Tasks[p].Result;
                StructureHandler.UpdateBitmap(Phases[p], MainBitmap);
            }

            PelletImage.Source = Converters.BitmapToImageSource(MainBitmap);
            PhasePercentageUpdate();
        }

        private Range PhaseGenerator(int p)
        {
            // !! PHASE 1 INISTALIZED !! //
            if(p > 0)
                if (Properties.CurrTemperature >= Phases[p].TemperaturePoint)
                {
                    Phases[p].CurrRange = Phases[p].Grow(Phases[p].PrevRange, Phases[p].Properties);
                    Phases[p].PrevRange = Phases[p].CurrRange;
                    return Phases[p].CurrRange;
                }

            return Phases[p].Range;
        }

        private void PhasePercentageUpdate()
        {
            StructureHandler.CountGrains(Phases, MainBitmap);

            var total = Phases[0].Counter + Phases[1].Counter + Phases[2].Counter + Phases[3].Counter;

            Phases[0].Percentage = ((total - Phases[1].Counter - Phases[2].Counter - Phases[3].Counter) / Constants.CIRCLE_AREA) * 100;
            Phases[0].Percentage = Math.Round(Phases[0].Percentage, 2);

            Phases[1].Percentage = ((total - Phases[0].Counter - Phases[2].Counter - Phases[3].Counter) / Constants.CIRCLE_AREA) * 100;
            Phases[1].Percentage = Math.Round(Phases[1].Percentage, 2);

            Phases[2].Percentage = ((total - Phases[0].Counter - Phases[1].Counter - Phases[3].Counter) / Constants.CIRCLE_AREA) * 100;
            Phases[2].Percentage = Math.Round(Phases[2].Percentage, 2);

            Phases[3].Percentage = ((total - Phases[0].Counter - Phases[1].Counter - Phases[2].Counter) / Constants.CIRCLE_AREA) * 100;
            Phases[3].Percentage = Math.Round(Phases[3].Percentage, 2);

            Phase1Label.Content = Phases[0].Percentage /*> 0 ? Phases[0].Percentage : 0*/;
            Phase2Label.Content = Phases[1].Percentage /*> 0 ? Phases[1].Percentage : 0*/;
            Phase3Label.Content = Phases[2].Percentage /*> 0 ? Phases[2].Percentage : 0*/;
            Phase4Label.Content = Phases[3].Percentage /*> 0 ? Phases[3].Percentage : 0*/;
        }

        private void CollectData()
        {
            //collectedData.Add(properties.CurrTemperature, phases[0].Percentage, phases[1].Percentage, phases[2].Percentage, phases[3].Percentage);
            //Console.WriteLine("Temp.: " + properties.CurrTemperature + ", Fe2O3 [%]: " + phases[0].Percentage + ", Fe3O4 [%]: " + phases[1].Percentage
            //                    + ", FeO [%]: " + phases[2].Percentage + ", Fe [%]: " + phases[3].Percentage);
            var time = StopWatch.Elapsed.TotalMilliseconds / 1000;
            var temperature = Properties.CurrTemperature.ToString();
            var phase0 = Phases[0].Percentage > 0 ? Phases[0].Percentage.ToString() : "0";
            var phase1 = Phases[1].Percentage > 0 ? Phases[1].Percentage.ToString() : "0";
            var phase2 = Phases[2].Percentage > 0 ? Phases[2].Percentage.ToString() : "0";
            var phase3 = Phases[3].Percentage > 0 ? Phases[3].Percentage.ToString() : "0";
            var newLine = string.Format("{0};{1};{2};{3};{4};{5}", time, temperature, phase0, phase1, phase2, phase3);
            csv.AppendLine(newLine);
        }

        private void WriteToFileTheEnd()
        {
            var newLine = string.Format("Total time:;{0:f2};s\nInclusion:;{1};%", StopWatch.Elapsed.TotalSeconds, Properties.AmountOfInclusions);
            csv.AppendLine(newLine);
            newLine = string.Format("Amount of grains:;{0}", Properties.AmountOfGrains);
            csv.AppendLine(newLine);
            newLine = string.Format("Pellet size:;{0};px", Properties.PelletSize);
            csv.AppendLine(newLine);
            newLine = string.Format("Probability:;{0}->{1}->{2}->{3};%", Phases[0].GrowthProbability, Phases[1].GrowthProbability, Phases[2].GrowthProbability, Phases[3].GrowthProbability);
            csv.AppendLine(newLine);
            newLine = string.Format("Growth type:;{0}", Properties.NeighbourhoodType.ToString());
            csv.AppendLine(newLine);

            var file = string.Format("{0}{1}{2}", "../../../data/results_", DateTime.Now.ToString("yyyyMMdd_hhmm_tt"), ".csv");
            File.WriteAllText(file, csv.ToString());
        }

        private async void BufferTempteraturLoading()
        {
            int temp_time = ((int)StopWatch.Elapsed.TotalMilliseconds / Properties.TemperatureRiseRate);
            await Task.Factory.StartNew(() =>
            {
                while (!TempteratureDispatcher.IsEnabled) { }

                MainThread.Send((object state) => {
                    Properties.BufferTemperature += ((int)StopWatch.Elapsed.TotalMilliseconds / Properties.TemperatureRiseRate) - temp_time;
                }, null);
            });
        }

        private void Generate_Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => {
                Mouse.OverrideCursor = Cursors.Wait;
            });

            SetProperties();
            TempteratureDispatcher.Start();
            MainDispatcher.Start();
            StopWatch = Stopwatch.StartNew();
        }

        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            StopButton.Visibility = Visibility.Hidden;
            ResumeButton.Visibility = Visibility.Visible;
            Application.Current.Dispatcher.Invoke(() => {
                Mouse.OverrideCursor = null;
            });

            MainDispatcher.Stop();
            TempteratureDispatcher.Stop();
            BufferTempteraturLoading();
        }

        private void Resume_Button_Click(object sender, RoutedEventArgs e)
        {
            ResumeButton.Visibility = Visibility.Hidden;
            StopButton.Visibility = Visibility.Visible;
            Application.Current.Dispatcher.Invoke(() => {
                Mouse.OverrideCursor = Cursors.Wait;
            });

            MainDispatcher.Start();
            TempteratureDispatcher.Start();
        }

        private void Stop_Temp_Button_Click(object sender, RoutedEventArgs e)
        {
            StartTemperatureButton.Visibility = Visibility.Visible;
            StopTemperatureButton.Visibility = Visibility.Hidden;
            TempteratureDispatcher.Stop();
            BufferTempteraturLoading();
        }

        private void Start_Temp_Button_Click(object sender, RoutedEventArgs e)
        {
            StartTemperatureButton.Visibility = Visibility.Hidden;
            StopTemperatureButton.Visibility = Visibility.Visible;
            TempteratureDispatcher.Start();
        }

        private void ConstantGrowthRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            ConstGrowthProbabilityTextBox.IsEnabled = true;
            Phase1PropabilityTextBox.IsEnabled = false;
            Phase2PropabilityTextBox.IsEnabled = false;
            Phase3PropabilityTextBox.IsEnabled = false;
            Phase4PropabilityTextBox.IsEnabled = false;
        }

        private void PhasesGrowthRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            ConstGrowthProbabilityTextBox.IsEnabled = false;
            Phase1PropabilityTextBox.IsEnabled = true;
            Phase2PropabilityTextBox.IsEnabled = true;
            Phase3PropabilityTextBox.IsEnabled = true;
            Phase4PropabilityTextBox.IsEnabled = true;
        }

        private void ProgresiveGrowthRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            ConstGrowthProbabilityTextBox.IsEnabled = false;
            Phase1PropabilityTextBox.IsEnabled = false;
            Phase2PropabilityTextBox.IsEnabled = false;
            Phase3PropabilityTextBox.IsEnabled = false;
            Phase4PropabilityTextBox.IsEnabled = false;
        }

        private void ExportBitmap_Click(object sender, RoutedEventArgs e)
        {
            MainBitmap.MakeTransparent(Color.HotPink);

            SaveFileDialog save = new SaveFileDialog
            {
                Filter = "Bitmap Image|*.png",
                Title = "Save an Image File",
                FileName = string.Format("bitmap_{0}", DateTime.Now.ToString("yyyyMMdd_hhmm_tt"))
            };
            if (save.ShowDialog() == true)
            {
                MainBitmap.Save(save.FileName, ImageFormat.Png);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private NeighbourhoodType ChooseNeighbourhoodType()
        {
            if (MooreRadioButton.IsChecked == true)
            {
                return NeighbourhoodType.Moore;
            }
            else if (NeumannRadioButton.IsChecked == true)
            {
                return NeighbourhoodType.Neumann;
            }
            else
            {
                return NeighbourhoodType.Moore2;
            }
        }

        private StartingPointsType ChooseStartingPointsType()
        {
            if (RandomBoundaryRadioButton.IsChecked == true)
            {
                return StartingPointsType.RandomBoundary;
            }
            else if (RegularBoundaryRadioButton.IsChecked == true)
            {
                return StartingPointsType.RegularBoundary;
            }
            else
            {
                return StartingPointsType.RandomInside;
            }
        }
    }
}
