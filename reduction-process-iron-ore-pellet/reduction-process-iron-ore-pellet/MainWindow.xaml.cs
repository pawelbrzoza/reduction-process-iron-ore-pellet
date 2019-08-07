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
        private Stopwatch stopWatch;
        private static System.Timers.Timer aTimer;
        private SynchronizationContext mainThread = SynchronizationContext.Current;
        private Models.Properties properties;
        private DispatcherTimer mainDispatcher = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 1) };
        private DispatcherTimer tempteratureDispatcher = new DispatcherTimer { Interval = new TimeSpan(0, 0, 0, 0, 1) };
        private Bitmap mainBitmap;
        private FastBitmap fastBitmap;
        private Range range, range2, range3, range4;
        private bool phaseFe2O3Started, phaseFe3O4Started, phaseFeOStarted, phaseFeStarted;

        public MainWindow()
        {
            InitializeComponent();
            SetProperties();
            mainDispatcher.Tick += DispatcherTick;
            tempteratureDispatcher.Tick += TemperaturDispatcherTick;
            if (mainThread == null) mainThread = new SynchronizationContext();
            ConstantGrowthRadioButton.IsChecked = true;
        }

        private static void SetTimer()
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(1000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        private static void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}",
                              e.SignalTime);
        }

        private void SetProperties()
        {
            phaseFe2O3Started = phaseFe3O4Started = phaseFeOStarted = phaseFeStarted = false;

            properties = new Models.Properties()
            {
                RangeWidth =            (int)PelletImage.Width,
                RangeHeight =           (int)PelletImage.Height,
                AmountOfGrains = int.Parse(NumOfGrainsTextBox.Text),
                NeighbourhoodType = ChooseNeighbourhoodType(),
                CurrGrowthProbability = int.Parse(GrowthProbabilityTextBox.Text),
                GrowthProbability = int.Parse(GrowthProbabilityTextBox.Text),
                CurrTemperature =       0,
                RiseOfTemperature = int.Parse(RiseOfTemperatureTextBox.Text),
                MaxTemperature = int.Parse(MaxTemperatureTextBox.Text),
                Fe2O3Temperature = int.Parse(Fe2O3TextBox.Text),
                Fe3O4Temperature = int.Parse(Fe3O4TextBox.Text),
                FeOTemperature = int.Parse(FeOTextBox.Text),
                FeTemperature = int.Parse(FeTextBox.Text)
            };

            range =  InitStructures.InitCellularAutomata(properties, Color.Black);
            range2 = InitStructures.InitCellularAutomata(properties, Color.Black);
            range3 = InitStructures.InitCellularAutomata(properties, Color.Black);
            range4 = InitStructures.InitCellularAutomata(properties, Color.Black);

            mainBitmap = new Bitmap((int)PelletImage.Width, (int)PelletImage.Height);
        }

        private void TemperaturDispatcherTick(object sender, EventArgs e)
        {
            if (properties.CurrTemperature < properties.MaxTemperature)
            {
                properties.CurrTemperature = (int)stopWatch.Elapsed.TotalMilliseconds / properties.RiseOfTemperature;
                temperatureLabel.Content = Convert.ToString((int)properties.CurrTemperature);
            }
            else if (properties.CurrTemperature >= properties.MaxTemperature && mainDispatcher.IsEnabled)
            {
                Console.WriteLine("Serial: {0:f2} s", stopWatch.Elapsed.TotalSeconds);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Mouse.OverrideCursor = null;
                });
                aTimer.Stop();
                aTimer.Dispose();
                mainDispatcher.Stop();
                tempteratureDispatcher.Stop();
            }
        }

        private void DispatcherTick(object sender, EventArgs e)
        {
            if (properties.CurrTemperature >= properties.Fe2O3Temperature)
            {
                if (!phaseFe2O3Started)
                {
                    PhaseFe2O3(ObjectCopier.Clone(properties));
                    Console.WriteLine("Start Fe2O3 phase");
                }
            }
            using (fastBitmap = mainBitmap.FastLock())
            {
                for (int i = 1; i < mainBitmap.Width - 1; ++i)
                    for (int j = 1; j < mainBitmap.Height - 1; ++j)
                        if (!SpecialId.IsSpecialId(range.GrainsArray[i, j].Id))
                            fastBitmap.SetPixel(i, j, range.GrainsArray[i, j].Color);
            }

            if (properties.CurrTemperature >= properties.Fe3O4Temperature)
            {
                if (!phaseFe3O4Started)
                {
                    PhaseFe3O4(ObjectCopier.Clone(properties));
                    Console.WriteLine("Start Fe3O4 phase");
                }
            }
            using (fastBitmap = mainBitmap.FastLock())
            {
                for (int i = 1; i < mainBitmap.Width - 1; ++i)
                    for (int j = 1; j < mainBitmap.Height - 1; ++j)
                        if (!SpecialId.IsSpecialId(range2.GrainsArray[i, j].Id))
                            fastBitmap.SetPixel(i, j, range2.GrainsArray[i, j].Color);
            }

            if (properties.CurrTemperature >= properties.FeOTemperature)
            {
                if (!phaseFeOStarted)
                {
                    PhaseFeO(ObjectCopier.Clone(properties));
                    Console.WriteLine("Start FeO phase");
                }
            }
            using (fastBitmap = mainBitmap.FastLock())
            {
                for (int i = 1; i < mainBitmap.Width - 1; ++i)
                    for (int j = 1; j < mainBitmap.Height - 1; ++j)
                        if (!SpecialId.IsSpecialId(range3.GrainsArray[i, j].Id))
                            fastBitmap.SetPixel(i, j, range3.GrainsArray[i, j].Color);
            }

            if (properties.CurrTemperature >= properties.FeTemperature)
            {
                if (!phaseFeStarted)
                {
                    PhaseFe(ObjectCopier.Clone(properties));
                    Console.WriteLine("Start Fe phase");
                }
            }
            using (fastBitmap = mainBitmap.FastLock())
            {
                for (int i = 1; i < mainBitmap.Width - 1; ++i)
                    for (int j = 1; j < mainBitmap.Height - 1; ++j)
                        if (!SpecialId.IsSpecialId(range4.GrainsArray[i, j].Id))
                            fastBitmap.SetPixel(i, j, range4.GrainsArray[i, j].Color);
            }
            PelletImage.Source = Converters.BitmapToImageSource(mainBitmap);
        }

        private async void PhaseFe2O3(Models.Properties proper)
        {
            phaseFe2O3Started = true;
            CellularAutomata ca = new CellularAutomata();
            Range currRange = new Range();
            Range prevRange = InitStructures.InitCellularAutomata(properties, Converters.WindowsToDrawingColor(Fe2O3ColorPicker.SelectedColor.Value));

            if (PhasesGrowthRadioButton.IsChecked == true)
                proper.CurrGrowthProbability = Int32.Parse(Fe2O3PropabilityTextBox.Text);

            await Task.Factory.StartNew(() =>
            {
                while (properties.CurrTemperature < properties.MaxTemperature)
                {
                    currRange = ca.Grow(prevRange, proper);
                    prevRange = currRange;

                    mainThread.Send((object state) => {
                        range = currRange;
                    }, null);
                }
            });
            Console.WriteLine("Stop Fe2O3 phase");
        }

        private async void PhaseFe3O4(Models.Properties proper2)
        {
            phaseFe3O4Started = true;
            CellularAutomata ca2 = new CellularAutomata();
            Range currRange2 = new Range();
            Range prevTange2 = InitStructures.InitCellularAutomata(properties, Converters.WindowsToDrawingColor(Fe3O4ColorPicker.SelectedColor.Value));

            if (PhasesGrowthRadioButton.IsChecked == true)
                proper2.CurrGrowthProbability = Int32.Parse(Fe3O4PropabilityTextBox.Text);

            await Task.Factory.StartNew(() =>
            {
                while (properties.CurrTemperature < properties.MaxTemperature)
                {
                    currRange2 = ca2.Grow(prevTange2, proper2);
                    prevTange2 = currRange2;

                    mainThread.Send((object state) =>
                    {
                        range2 = currRange2;
                    }, null);
                }
            });
            Console.WriteLine("Stop Fe3O4 phase");
        }

        private async void PhaseFeO(Models.Properties proper3)
        {
            phaseFeOStarted = true;
            CellularAutomata ca3 = new CellularAutomata();
            Range currRange3 = new Range();
            Range prevTange3 = InitStructures.InitCellularAutomata(properties, Converters.WindowsToDrawingColor(FeOColorPicker.SelectedColor.Value));

            if (PhasesGrowthRadioButton.IsChecked == true)
                proper3.CurrGrowthProbability = Int32.Parse(FeOPropabilityTextBox.Text);

            await Task.Factory.StartNew(() =>
            {
                while (properties.CurrTemperature < properties.MaxTemperature)
                {
                    currRange3 = ca3.Grow(prevTange3, proper3);
                    prevTange3 = currRange3;

                    mainThread.Send((object state) =>
                    {
                        range3 = currRange3;
                    }, null);
                }
            });
            Console.WriteLine("Stop FeO phase");
        }

        private async void PhaseFe(Models.Properties proper4)
        {
            phaseFeStarted = true;
            CellularAutomata ca4 = new CellularAutomata();
            Range currRange4 = new Range();
            Range prevTange4 = InitStructures.InitCellularAutomata(properties, Converters.WindowsToDrawingColor(FeColorPicker.SelectedColor.Value));

            if (PhasesGrowthRadioButton.IsChecked == true)
                proper4.CurrGrowthProbability = Int32.Parse(FePropabilityTextBox.Text);

            await Task.Factory.StartNew(() =>
            {
                while (properties.CurrTemperature < properties.MaxTemperature)
                {
                    currRange4 = ca4.Grow(prevTange4, proper4);
                    prevTange4 = currRange4;

                    mainThread.Send((object state) =>
                    {
                        range4 = currRange4;
                    }, null);
                }
            });
            Console.WriteLine("Stop Fe phase");
        }

        private void Play_Button_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() => {
                Mouse.OverrideCursor = Cursors.Wait;
            });

            SetProperties();
            SetTimer();
            stopWatch = Stopwatch.StartNew();
            mainDispatcher.Start();
            tempteratureDispatcher.Start();
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

        private void Start_Temp_Button_Click(object sender, RoutedEventArgs e)
        {
            StartTemperatureButton.Visibility = Visibility.Hidden;
            StopTemperatureButton.Visibility = Visibility.Visible;
            tempteratureDispatcher.Start();
        }

        private void Stop_Temp_Button_Click(object sender, RoutedEventArgs e)
        {
            StartTemperatureButton.Visibility = Visibility.Visible;
            StopTemperatureButton.Visibility = Visibility.Hidden;
            tempteratureDispatcher.Stop();
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
            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "Bitmap Image|*.bmp";
            save.Title = "Save an Image File";
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
            SaveFileDialog save = new SaveFileDialog();
            save.Filter = "Bitmap Image|*.txt";
            save.Title = "Save an Image File";
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