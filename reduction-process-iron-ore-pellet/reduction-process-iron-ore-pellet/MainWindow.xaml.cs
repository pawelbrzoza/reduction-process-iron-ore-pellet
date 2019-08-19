using grain_growth.Alghorithms;
using grain_growth.Models;
using grain_growth.Helpers;

using System;
using System.Windows;
using System.Windows.Media.Imaging;
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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        private Models.Properties properties;
        private readonly Phase[] phases = InitializeArray.Init<Phase>(Constants.NUMBER_OF_PHASES);
        private readonly DispatcherTimer mainDispatcher = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 0) };
        private readonly DispatcherTimer tempteratureDispatcher = new DispatcherTimer();
        private Bitmap mainBitmap;
        private readonly SynchronizationContext mainThread = SynchronizationContext.Current;
        private Stopwatch stopWatch;
        private StringBuilder csv;

        public MainWindow()
        {
            InitializeComponent();
            mainDispatcher.Tick += PhasesDispatcherTick;
            tempteratureDispatcher.Tick += TemperaturDispatcherTick;
            if (mainThread == null) mainThread = new SynchronizationContext();

            ConstantGrowthRadioButton.IsChecked = true;
            Phase1NameLabel.Content = Phase1NameTextBox.Text + " [%]";
            Phase2NameLabel.Content = Phase2NameTextBox.Text + " [%]";
            Phase3NameLabel.Content = Phase3NameTextBox.Text + " [%]";
            Phase4NameLabel.Content = Phase4NameTextBox.Text + " [%]";
        }

        private void SetProperties()
        {
            mainBitmap = new Bitmap((int)PelletImage.Width, (int)PelletImage.Height);

            // properties
            properties = new Models.Properties()
            {
                RangeWidth = (int)PelletImage.Width,
                RangeHeight = (int)PelletImage.Height,
                AmountOfGrains = int.Parse(AmountOfGrainsTextBox.Text),
                AmountOfInclusions = int.Parse(AmountOfInclusionsTextBox.Text),
                PelletSize = int.Parse(PelletSizeTextBox.Text),
                NeighbourhoodType = ChooseNeighbourhoodType(),
                StartingPointsType = ChooseStartingPointsType(),
                CurrGrowthProbability = int.Parse(ConstGrowthProbabilityTextBox.Text),
                ConstGrowthProbability = int.Parse(ConstGrowthProbabilityTextBox.Text),
                CurrTemperature = 0,
                TemperatureRiseRate = int.Parse(TemperatureRiseRateTextBox.Text),
                MaxTemperature = int.Parse(MaxTemperatureTextBox.Text),
                BufferTemperature = 0
            };

            // update constants
            Constants.UpdateConstants(properties.PelletSize);

            // init inclusion points inside update area
            InitInclusions.InitPoints(properties.AmountOfInclusions);

            // init inclusion points inside update area
            InitStructure.PointsArea = InitStructure.GetPointsInsideCircle((int)Constants.RADIOUS, Constants.MIDDLE_POINT);

            // set phases
            phases[0].Name = Phase1NameTextBox.Text;
            phases[1].Name = Phase2NameTextBox.Text;
            phases[2].Name = Phase3NameTextBox.Text;
            phases[3].Name = Phase4NameTextBox.Text;

            Phase1NameLabel.Content = Phase1NameTextBox.Text + " [%]";
            Phase2NameLabel.Content = Phase2NameTextBox.Text + " [%]";
            Phase3NameLabel.Content = Phase3NameTextBox.Text + " [%]";
            Phase4NameLabel.Content = Phase4NameTextBox.Text + " [%]";

            for (int i = 0; i < Constants.NUMBER_OF_PHASES; i++)
            {
                phases[i].Range = new Range(properties.RangeWidth, properties.RangeWidth);
                InitStructure.GrainsArrayInit(phases[i].Range);
                InitStructure.UpdateInsideCircle(phases[i].Range);
                InitStructure.DrawBlackCircleBorder(phases[i].Range);
                InitInclusions.AddInclusions(phases[i].Range);
                phases[i].Percentage = 0;
                phases[i].Started = false;
            }

            phases[0].TemperaturePoint = int.Parse(Phase1TextBox.Text);
            phases[1].TemperaturePoint = int.Parse(Phase2TextBox.Text);
            phases[2].TemperaturePoint = int.Parse(Phase3TextBox.Text);
            phases[3].TemperaturePoint = int.Parse(Phase4TextBox.Text);

            phases[0].Color = Converters.WindowsToDrawingColor(Phase1ColorPicker.SelectedColor.Value);
            phases[1].Color = Converters.WindowsToDrawingColor(Phase2ColorPicker.SelectedColor.Value);
            phases[2].Color = Converters.WindowsToDrawingColor(Phase3ColorPicker.SelectedColor.Value);
            phases[3].Color = Converters.WindowsToDrawingColor(Phase4ColorPicker.SelectedColor.Value);

            phases[0].GrowthProbability = Int32.Parse(Phase1PropabilityTextBox.Text);
            phases[1].GrowthProbability = Int32.Parse(Phase2PropabilityTextBox.Text);
            phases[2].GrowthProbability = Int32.Parse(Phase3PropabilityTextBox.Text);
            phases[3].GrowthProbability = Int32.Parse(Phase4PropabilityTextBox.Text);

            // phase 1 - initialization
            phases[0].Started = true;
            CellularAutomata.InstantFillColor(phases[0], 1, phases[0].Color);

            // set temperature dispatcher
            tempteratureDispatcher.Interval = new TimeSpan(0, 0, 0, 0, int.Parse(TemperatureRiseRateTextBox.Text));

            // csv  file
            csv = new StringBuilder();
            var newLine = string.Format("{0};{1};{2};{3};{4}", "Temperature", phases[0].Name, phases[1].Name, phases[2].Name, phases[3].Name);
            csv.AppendLine(newLine);
        }

        private void TemperaturDispatcherTick(object sender, EventArgs e)
        {
            CollectData();
            if (properties.CurrTemperature < properties.MaxTemperature)
            {
                properties.CurrTemperature = ((int)stopWatch.Elapsed.TotalMilliseconds / properties.TemperatureRiseRate)
                    - properties.BufferTemperature;
                temperatureLabel.Content = Convert.ToString(properties.CurrTemperature);
            }
            else if (properties.CurrTemperature >= properties.MaxTemperature && mainDispatcher.IsEnabled)
            {
                var newLine = string.Format("Total time:;{0:f2};s\nInclusion:;{1};%", stopWatch.Elapsed.TotalSeconds, properties.AmountOfInclusions);
                csv.AppendLine(newLine);
                newLine = string.Format("Amount of grains:;{0}", properties.AmountOfGrains);
                csv.AppendLine(newLine);
                newLine = string.Format("Pellet size:;{0};px", properties.PelletSize);
                csv.AppendLine(newLine);
                newLine = string.Format("Propability:;{0}/{1}/{2};%", phases[1].GrowthProbability, phases[1].GrowthProbability, phases[2].GrowthProbability);
                csv.AppendLine(newLine);
                newLine = string.Format("Growth type:;{0}", properties.NeighbourhoodType.ToString());
                csv.AppendLine(newLine);

                var file = string.Format("{0}{1}{2}", "../../data/results_", DateTime.Now.ToString("yyyyMMdd_hhmm_tt"), ".csv");
                File.WriteAllText(file, csv.ToString());

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Mouse.OverrideCursor = null;
                });

                Console.WriteLine("Serial: {0:f2} s", stopWatch.Elapsed.TotalSeconds);
                mainDispatcher.Stop();
                tempteratureDispatcher.Stop();
            }
        }

        private void PhasesDispatcherTick(object sender, EventArgs e)
        {
            for (int p = 0; p < Constants.NUMBER_OF_PHASES; p++)
            {
                phases[p].Counter = 0;

                if (properties.CurrTemperature >= phases[p].TemperaturePoint)
                {
                    if (!phases[p].Started)
                    {
                        PhaseGenerator(ObjectCopier.Clone(properties), p);
                        phases[p].Started = true;
                        Console.WriteLine("Start " + phases[p].Name + " phase");
                    }
                }
                CellularAutomata.UpdateBitmap(phases[p], mainBitmap);
            }

            PelletImage.Source = Converters.BitmapToImageSource(mainBitmap);
            PhasePercentageUpdate();
            
        }

        private async void PhaseGenerator(Models.Properties properties, int p)
        {
            Range currRange;
            Range prevRange = InitStructure.InitCellularAutomata(this.properties, phases[p].Color);
            InitInclusions.AddInclusions(prevRange);
            CellularAutomata ca = new CellularAutomata();

            if (PhasesGrowthRadioButton.IsChecked == true)
                properties.CurrGrowthProbability = phases[p].GrowthProbability;
            else if (ProgresiveGrowthRadioButton.IsChecked == true)
                properties.CurrGrowthProbability = (100 / Constants.NUMBER_OF_PHASES) * (p + 1);

            await Task.Factory.StartNew(() =>
            {
                while (this.properties.CurrTemperature < this.properties.MaxTemperature)
                {
                    currRange = ca.Grow(prevRange, properties);
                    prevRange = currRange;

                    mainThread.Send((object state) => {
                        phases[p].Range = currRange;
                    }, null);
                }
            });
            Console.WriteLine("Stop " + phases[p].Name + " phase");
        }

        private void PhasePercentageUpdate()
        {
            phases[3].Percentage = (phases[3].Counter / Constants.CIRCLE_AREA) * 100;
            phases[3].Percentage = Math.Round(phases[3].Percentage, 2);

            phases[2].Percentage = ((phases[2].Counter / Constants.CIRCLE_AREA) * 100)
                 - phases[3].Percentage;
            phases[2].Percentage = Math.Round(phases[2].Percentage, 2);

            phases[1].Percentage = ((phases[1].Counter / Constants.CIRCLE_AREA) * 100)
                 - phases[2].Percentage - phases[3].Percentage;
            phases[1].Percentage = Math.Round(phases[1].Percentage, 2);

            phases[0].Percentage = ((phases[0].Counter / Constants.CIRCLE_AREA) * 100)
                 - phases[1].Percentage - phases[2].Percentage - phases[3].Percentage;
            phases[0].Percentage = Math.Round(phases[0].Percentage, 2);

            Phase1Label.Content = phases[0].Percentage /*> 0 ? phases[0].Percentage : 0*/;
            Phase2Label.Content = phases[1].Percentage /*> 0 ? phases[1].Percentage : 0*/;
            Phase3Label.Content = phases[2].Percentage /*> 0 ? phases[2].Percentage : 0*/;
            Phase4Label.Content = phases[3].Percentage /*> 0 ? phases[3].Percentage : 0*/;
        }

        private void CollectData()
        {
            //collectedData.Add(properties.CurrTemperature, phases[0].Percentage, phases[1].Percentage, phases[2].Percentage, phases[3].Percentage);
            //Console.WriteLine("Temp.: " + properties.CurrTemperature + ", Fe2O3 [%]: " + phases[0].Percentage + ", Fe3O4 [%]: " + phases[1].Percentage
            //                    + ", FeO [%]: " + phases[2].Percentage + ", Fe [%]: " + phases[3].Percentage);
            var temperature = properties.CurrTemperature.ToString();
            var phase0 = phases[0].Percentage > 0 ? phases[0].Percentage.ToString() : "0";
            var phase1 = phases[1].Percentage > 0 ? phases[1].Percentage.ToString() : "0";
            var phase2 = phases[2].Percentage > 0 ? phases[2].Percentage.ToString() : "0";
            var phase3 = phases[3].Percentage > 0 ? phases[3].Percentage.ToString() : "0";
            var newLine = string.Format("{0};{1};{2};{3};{4}", temperature, phase0, phase1, phase2, phase3);
            csv.AppendLine(newLine);
        }

        private async void BufferTempteraturLoading()
        {
            int temp_time = ((int)stopWatch.Elapsed.TotalMilliseconds / properties.TemperatureRiseRate);
            await Task.Factory.StartNew(() =>
            {
                while (!tempteratureDispatcher.IsEnabled) { }

                mainThread.Send((object state) => {
                    properties.BufferTemperature += ((int)stopWatch.Elapsed.TotalMilliseconds / properties.TemperatureRiseRate) - temp_time;
                }, null);
            });
        }

        private void Play_Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => {
                Mouse.OverrideCursor = Cursors.Wait;
            });
            
            SetProperties();
            tempteratureDispatcher.Start();
            mainDispatcher.Start();
            stopWatch = Stopwatch.StartNew();
        }

        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            StopButton.Visibility = Visibility.Hidden;
            ResumeButton.Visibility = Visibility.Visible;
            Application.Current.Dispatcher.Invoke(() => {
                Mouse.OverrideCursor = null;
            });

            mainDispatcher.Stop();
            tempteratureDispatcher.Stop();
            BufferTempteraturLoading();
        }

        private void Resume_Button_Click(object sender, RoutedEventArgs e)
        {
            ResumeButton.Visibility = Visibility.Hidden;
            StopButton.Visibility = Visibility.Visible;
            Application.Current.Dispatcher.Invoke(() => {
                Mouse.OverrideCursor = Cursors.Wait;
            });

            mainDispatcher.Start();
            tempteratureDispatcher.Start();
        }

        private void Stop_Temp_Button_Click(object sender, RoutedEventArgs e)
        {
            StartTemperatureButton.Visibility = Visibility.Visible;
            StopTemperatureButton.Visibility = Visibility.Hidden;
            tempteratureDispatcher.Stop();
            BufferTempteraturLoading();
        }

        private void Start_Temp_Button_Click(object sender, RoutedEventArgs e)
        {
            StartTemperatureButton.Visibility = Visibility.Hidden;
            StopTemperatureButton.Visibility = Visibility.Visible;
            tempteratureDispatcher.Start();
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

        private void ImportBitmap_Click(object sender, RoutedEventArgs e)
        {
            ////RectangleCanvas.Visibility = Visibility.Hidden;
            //currRange = new Range();
            ////currRange = InitStructures.InitCellularAutomata(properties);
            //SetProperties();
            //OpenFileDialog openfiledialog = new OpenFileDialog();

            //openfiledialog.Title = "Open Image";
            //openfiledialog.Filter = "Image File|*.bmp; *.gif; *.jpg; *.jpeg; *.png;";

            //if (openfiledialog.ShowDialog() == true)
            //{
            //    PelletImage.Source = Converters.BitmapToImageSource(new Bitmap(openfiledialog.FileName));
            //    currRange.StructureBitmap = new Bitmap(openfiledialog.FileName);

            //    CellularAutomata.UpdateGrainsArray(currRange);
            //    for (int i = 0; i < currRange.Width; i++)
            //        for (int j = 0; j < currRange.Height; j++)
            //            currRange.StructureBitmap.SetPixel(i, j, currRange.GrainsArray[i, j].Color);
            //}

            ////dispatcher.Stop();
        }

        private void ImportTXT_Click(object sender, RoutedEventArgs e)
        {
            ////RectangleCanvas.Visibility = Visibility.Hidden;
            //currRange = new Range();
            ////currRange = InitStructures.InitCellularAutomata(properties);
            //SetProperties();
            //OpenFileDialog openfiledialog = new OpenFileDialog();

            //openfiledialog.Title = "Open Image";
            //openfiledialog.Filter = "Image File|*.txt";

            //if (openfiledialog.ShowDialog() == true)
            //{
            //    PelletImage.Source = Converters.BitmapToImageSource(new Bitmap(openfiledialog.FileName));
            //    currRange.StructureBitmap = new Bitmap(openfiledialog.FileName);

            //    CellularAutomata.UpdateGrainsArray(currRange);
            //    CellularAutomata.UpdateBitmap(currRange);
            //}

            ////dispatcher.Stop();
        }

        private void ExportBitmap_Click(object sender, RoutedEventArgs e)
        {
            mainBitmap.MakeTransparent(Color.HotPink);

            SaveFileDialog save = new SaveFileDialog
            {
                Filter = "Bitmap Image|*.png",
                Title = "Save an Image File",
                FileName = string.Format("bitmap_{0}", DateTime.Now.ToString("yyyyMMdd_hhmm_tt"))
            };
            if (save.ShowDialog() == true)
            {
                mainBitmap.Save(save.FileName, ImageFormat.Png);
            }
        }

        private void ExportTXT_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog
            {
                Filter = "Bitmap Image|*.txt",
                Title = "Save an Image File"
            };
            if (save.ShowDialog() == true)
            {
                var image = PelletImage.Source;
                using (var fileStream = new FileStream(save.InitialDirectory + save.FileName, FileMode.Create))
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image as BitmapImage));
                    encoder.Save(fileStream);
                }
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
