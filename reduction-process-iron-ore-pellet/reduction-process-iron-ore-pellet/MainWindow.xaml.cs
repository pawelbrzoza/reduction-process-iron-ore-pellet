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
using FastBitmapLib;
using System.Diagnostics;

namespace grain_growth
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private const double RADIOUS_AREA = 149.0;
        private const double CIRCLE_AREA = Math.PI * RADIOUS_AREA * RADIOUS_AREA;
        private const int NUMBER_OF_PHASES = 4;

        private Models.Properties properties;
        private readonly Phase[] phases = InitializeArray.Init<Phase>(NUMBER_OF_PHASES);

        private readonly DispatcherTimer mainDispatcher = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 0) };
        private readonly DispatcherTimer tempteratureDispatcher = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 0) };

        private Bitmap mainBitmap;
        private FastBitmap fastBitmap;
        private readonly SynchronizationContext mainThread = SynchronizationContext.Current;
        private Stopwatch stopWatch;

        public MainWindow()
        {
            InitializeComponent();
            SetProperties();
            mainDispatcher.Tick += PhasesDispatcherTick;
            tempteratureDispatcher.Tick += TemperaturDispatcherTick;
            if (mainThread == null) mainThread = new SynchronizationContext();
            ConstantGrowthRadioButton.IsChecked = true;
        }

        private void SetProperties()
        {
            mainBitmap = new Bitmap((int)PelletImage.Width, (int)PelletImage.Height);

            properties = new Models.Properties()
            {
                RangeWidth = (int)PelletImage.Width,
                RangeHeight = (int)PelletImage.Height,
                AmountOfGrains = int.Parse(NumOfGrainsTextBox.Text),
                NeighbourhoodType = ChooseNeighbourhoodType(),
                CurrGrowthProbability = int.Parse(GrowthProbabilityTextBox.Text),
                ConstGrowthProbability = int.Parse(GrowthProbabilityTextBox.Text),
                CurrTemperature = 0,
                RiseOfTemperature = int.Parse(RiseOfTemperatureTextBox.Text),
                MaxTemperature = int.Parse(MaxTemperatureTextBox.Text),
                BufferTemperature = 0
            };

            phases[0].Name = "Fe2O3";
            phases[1].Name = "Fe3O4";
            phases[2].Name = "FeO";
            phases[3].Name = "Fe";

            for (int i = 0; i < NUMBER_OF_PHASES; i++)
            {
                phases[i].Range = InitStructure.InitCellularAutomata(properties, Color.Black);
                phases[i].Percentage = 0;
                phases[i].Started = false;
            }

            phases[0].TemperaturePoint = int.Parse(Fe2O3TextBox.Text);
            phases[1].TemperaturePoint = int.Parse(Fe3O4TextBox.Text);
            phases[2].TemperaturePoint = int.Parse(FeOTextBox.Text);
            phases[3].TemperaturePoint = int.Parse(FeTextBox.Text);

            phases[0].Color = Converters.WindowsToDrawingColor(Fe2O3ColorPicker.SelectedColor.Value);
            phases[1].Color = Converters.WindowsToDrawingColor(Fe3O4ColorPicker.SelectedColor.Value);
            phases[2].Color = Converters.WindowsToDrawingColor(FeOColorPicker.SelectedColor.Value);
            phases[3].Color = Converters.WindowsToDrawingColor(FeColorPicker.SelectedColor.Value);

            phases[0].GrowthProbability = Int32.Parse(Fe2O3PropabilityTextBox.Text);
            phases[1].GrowthProbability = Int32.Parse(Fe3O4PropabilityTextBox.Text);
            phases[2].GrowthProbability = Int32.Parse(FeOPropabilityTextBox.Text);
            phases[3].GrowthProbability = Int32.Parse(FePropabilityTextBox.Text);
        }

        private void TemperaturDispatcherTick(object sender, EventArgs e)
        {
            if (properties.CurrTemperature < properties.MaxTemperature)
            {
                properties.CurrTemperature = ((int)stopWatch.Elapsed.TotalMilliseconds / properties.RiseOfTemperature)
                    - properties.BufferTemperature;
                temperatureLabel.Content = Convert.ToString(properties.CurrTemperature);
            }
            else if (properties.CurrTemperature >= properties.MaxTemperature && mainDispatcher.IsEnabled)
            {
                Console.WriteLine("Serial: {0:f2} s", stopWatch.Elapsed.TotalSeconds);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Mouse.OverrideCursor = null;
                });

                mainDispatcher.Stop();
                tempteratureDispatcher.Stop();
            }
        }

        private void PhasesDispatcherTick(object sender, EventArgs e)
        {
            for (int p = 0; p < NUMBER_OF_PHASES; p++)
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
                using (fastBitmap = mainBitmap.FastLock())
                {
                    for (int i = 1; i < mainBitmap.Width - 1; ++i)
                        for (int j = 1; j < mainBitmap.Height - 1; ++j)
                            if (!SpecialId.IsSpecialId(phases[p].Range.GrainsArray[i, j].Id))
                            {
                                fastBitmap.SetPixel(i, j, phases[p].Range.GrainsArray[i, j].Color);
                                ++phases[p].Counter;
                            }
                }
            }

            PelletImage.Source = Converters.BitmapToImageSource(mainBitmap);
            PhasePercentageUpdate();
        }

        private async void PhaseGenerator(Models.Properties properties, int p)
        {
            Range currRange;
            Range prevRange = InitStructure.InitCellularAutomata(this.properties, phases[p].Color);
            CellularAutomata ca = new CellularAutomata();

            if (PhasesGrowthRadioButton.IsChecked == true)
                properties.CurrGrowthProbability = phases[p].GrowthProbability;
            else if (ProgresiveGrowthRadioButton.IsChecked == true)
                properties.CurrGrowthProbability = 100 / NUMBER_OF_PHASES * (p + 1);

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
            phases[3].Percentage = (phases[3].Counter / CIRCLE_AREA) * 100;

            phases[2].Percentage = ((phases[2].Counter / CIRCLE_AREA) * 100)
                - phases[3].Percentage;

            phases[1].Percentage = ((phases[1].Counter / CIRCLE_AREA) * 100)
                - phases[2].Percentage - phases[3].Percentage;

            phases[0].Percentage = ((phases[0].Counter / CIRCLE_AREA) * 100)
                - phases[1].Percentage - phases[2].Percentage - phases[3].Percentage;

            fe2O3Label.Content = Math.Round(phases[0].Percentage, 2);
            fe3O4Label.Content = Math.Round(phases[1].Percentage, 2);
            feOLabel.Content = Math.Round(phases[2].Percentage, 2);
            feLabel.Content = Math.Round(phases[3].Percentage, 2);
        }

        private async void BufferTempteraturLoading()
        {
            int temp_time = ((int)stopWatch.Elapsed.TotalMilliseconds / properties.RiseOfTemperature);
            await Task.Factory.StartNew(() =>
            {
                while (!tempteratureDispatcher.IsEnabled) { }

                mainThread.Send((object state) => {
                    properties.BufferTemperature += ((int)stopWatch.Elapsed.TotalMilliseconds / properties.RiseOfTemperature) - temp_time;
                }, null);
            });
        }

        private void Play_Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => {
                Mouse.OverrideCursor = Cursors.Wait;
            });

            SetProperties();
            mainDispatcher.Start();
            tempteratureDispatcher.Start();
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
            GrowthProbabilityTextBox.IsEnabled = true;
            Fe2O3PropabilityTextBox.IsEnabled = false;
            Fe3O4PropabilityTextBox.IsEnabled = false;
            FeOPropabilityTextBox.IsEnabled = false;
            FePropabilityTextBox.IsEnabled = false;
        }

        private void PhasesGrowthRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            GrowthProbabilityTextBox.IsEnabled = false;
            Fe2O3PropabilityTextBox.IsEnabled = true;
            Fe3O4PropabilityTextBox.IsEnabled = true;
            FeOPropabilityTextBox.IsEnabled = true;
            FePropabilityTextBox.IsEnabled = true;
        }

        private void ProgresiveGrowthRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            GrowthProbabilityTextBox.IsEnabled = false;
            Fe2O3PropabilityTextBox.IsEnabled = false;
            Fe3O4PropabilityTextBox.IsEnabled = false;
            FeOPropabilityTextBox.IsEnabled = false;
            FePropabilityTextBox.IsEnabled = false;
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
            SaveFileDialog save = new SaveFileDialog
            {
                Filter = "Bitmap Image|*.bmp",
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
    }
}
