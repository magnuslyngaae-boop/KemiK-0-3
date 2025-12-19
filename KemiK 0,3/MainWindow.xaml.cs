using KemiK_0_3.Sim;
using System.Diagnostics;
using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KemiK_0_3
{
    public partial class MainWindow : Window
    {
        private readonly VisualHost _visualHost;
        private readonly DrawingVisual _drawingVisual;
        private Vector2 _simSize;
        private readonly Stopwatch _sw = new();
        private Simulation _sim;
        private int _startTimeMilliseconds = 0;
        private int _displaySpeed = 16;
        private int _numberOfDatapoints = 0;
        private bool _resized = false;
        private bool _paused = true;
        private Color[] Farver { get; } = [Colors.Maroon, Colors.Navy, Colors.DarkSeaGreen, Colors.DarkOrange, Colors.White];
        public MainWindow()
        {
            InitializeComponent();

            _visualHost = new();
            _drawingVisual = new();
            _visualHost.AddVisual(_drawingVisual);
            simulationsvisning.Children.Add(_visualHost);

            _simSize = new(6400, 3600); // nm
            _sim = new(10000, 1, 1.5f, 0, 0.1f, 0.5f, _simSize, 1);

            // Disse metoder bliver kaldt 🏯 ...automatisk...
            Loaded += MainWindow_Loaded;
            CompositionTarget.Rendering += OnRendering;
            this.SizeChanged += Window_SizeChanged;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // lav sim
            DisplaySpeedDisplay.Text = "Display speed: " + _displaySpeed;
            UpdateControlpanelBindingsenergi();
            UpdateControlpanelAktiveringsenergi();
            UpdateControlpanelKoncentration();
            UpdateControlpanelTemperatur();
            UpdateControlpanelSansylnighed();
        }
        private void OnRendering(object? sender, EventArgs e)
        {
            // Tegn ting

            int time = (int)_sw.ElapsedMilliseconds / _displaySpeed + _startTimeMilliseconds;

            State state = _sim.GetState(time);

            //Debug.WriteLine($"A: {state.CompundFordeling[0]}, B: {state.CompundFordeling[1]}, C: {state.CompundFordeling[2]}, D: {state.CompundFordeling[3]}, X: {state.CompundFordeling[4]}");

            float _widthFactor = (float)(simulationsvisning.ActualWidth / _simSize.X);
            float _heightFactor = (float)(simulationsvisning.ActualHeight / _simSize.Y);

            using (DrawingContext dc = _drawingVisual.RenderOpen())
            {
                foreach (Compound compound in state.Compounds)
                {
                    dc.DrawEllipse(
                        compound.Brush,
                        null,
                        new(compound.Position.X * _widthFactor, compound.Position.Y * _heightFactor),
                        compound.Radius * _widthFactor,
                        compound.Radius * _heightFactor
                    );
                }
            }
            TimeDisplay.Text = "Displaying: " + state.Time + " ms of: " + _sim.SimTime + " ms total";
            DisplayTemp.Text = $"Temperature: {(int)state.Temperatur} K";
            PlotTemps();
        }
        private void PlotTemps()
        {
            State[] states = _sim.GetStates();

            if (states.Length == _numberOfDatapoints && !_resized) return;

            // Canvas size
            int width = (int)TemperatureStatestikCanvas.ActualWidth;
            int height = (int)TemperatureStatestikCanvas.ActualHeight;

            if (width == 0 || height == 0) return; // avoid zero-size

            // Find min/max values
            float maxTemp = states.Max(s => s.Temperatur);
            float minTemp = states.Min(s => s.Temperatur);
            float maxTime = states.Max(s => s.Time);
            double maxKonc = 0;

            foreach (State state in states)
            {
                foreach (double konc in state.Konc)
                {
                    maxKonc = konc > maxKonc ? konc : maxKonc;
                }
            }

            // Avoid division by zero
            float widthFactor = maxTime == 0 ? 1 : width / maxTime;
            float tempHeightFactor = (maxTemp == minTemp) ? 1 : height / (maxTemp - minTemp);
            float koncHeightFactor = (maxKonc == 0) ? 1 : (float)(height / maxKonc);

            // Create WriteableBitmap
            WriteableBitmap tempBitmap = new(width, height, 96, 96, PixelFormats.Pbgra32, null);
            WriteableBitmap koncBitmap = new(width, height, 96, 96, PixelFormats.Pbgra32, null);
            int stride = width * 4;
            byte[] tempPixels = new byte[height * stride];
            byte[] koncPixels = new byte[height * stride];

            // Plot each state
            foreach (State state in states)
            {
                int x = (int)(state.Time * widthFactor);
                int y = (int)((state.Temperatur - minTemp) * tempHeightFactor);
                int index;

                for (int xOffset = -1; xOffset < 2; xOffset++)
                {
                    for (int yOffset = -1; yOffset < 2; yOffset++)
                    {
                        if (x + xOffset >= 0 && x + xOffset < width && y + yOffset >= 0 && y + yOffset < height)
                        {
                            index = (y + yOffset) * stride + (x + xOffset) * 4;
                            tempPixels[index + 0] = 255; // Blue
                            tempPixels[index + 1] = 255; // Green
                            tempPixels[index + 2] = 255; // Red
                            tempPixels[index + 3] = 255; // Alpha
                        }
                    }
                }

                for (int i = 0; i < state.Konc.Length; i++)
                {
                    x = (int)(state.Time * widthFactor);
                    y = (int)(state.Konc[i] * koncHeightFactor);


                    for (int xOffset = -1; xOffset < 2; xOffset++)
                    {
                        for (int yOffset = -1; yOffset < 2; yOffset++)
                        {
                            if (x + xOffset >= 0 && x + xOffset < width && y + yOffset >= 0 && y + yOffset < height)
                            {
                                index = (y + yOffset) * stride + (x + xOffset) * 4;
                                koncPixels[index + 0] = Farver[i].B; // Blue
                                koncPixels[index + 1] = Farver[i].G; // Green
                                koncPixels[index + 2] = Farver[i].R; // Red
                                koncPixels[index + 3] = Farver[i].A; // Alpha
                            }
                        }
                    }
                }
            }

            // Write pixels to bitmap
            tempBitmap.WritePixels(new Int32Rect(0, 0, width, height), tempPixels, stride, 0);
            koncBitmap.WritePixels(new Int32Rect(0, 0, width, height), koncPixels, stride, 0);

            // Display bitmap
            System.Windows.Controls.Image tempImg = new() { Source = tempBitmap };
            System.Windows.Controls.Image koncImg = new() { Source = koncBitmap };
            TemperatureStatestikCanvas.Children.Clear();
            TemperatureStatestikCanvas.Children.Add(tempImg);
            KoncStatestikCanvas.Children.Clear();
            KoncStatestikCanvas.Children.Add(koncImg);

            // Update temperature og koncentrations labels (Y-axis)
            Temp0.Text = $"{(int)maxTemp} K";
            Temp1.Text = $"{(int)(4 * (maxTemp - minTemp) / 5 + minTemp)} K";
            Temp2.Text = $"{(int)(3 * (maxTemp - minTemp) / 5 + minTemp)} K";
            Temp3.Text = $"{(int)(2 * (maxTemp - minTemp) / 5 + minTemp)} K";
            Temp4.Text = $"{(int)((maxTemp - minTemp) / 5 + minTemp)} K";
            Temp5.Text = $"{(int)minTemp} K";

            Konc0.Text = $"{(int)(1000 * maxKonc)} mM";
            Konc1.Text = $"{(int)(4000 * maxKonc / 5)} mM";
            Konc2.Text = $"{(int)(3000 * maxKonc / 5)} mM";
            Konc3.Text = $"{(int)(2000 * maxKonc / 5)} mM";
            Konc4.Text = $"{(int)(1000 * maxKonc / 5)} mM";
            Konc5.Text = $"0 M";

            // Update time labels (X-axis)
            TempTime0.Text = $"0 s";
            TempTime1.Text = $"{(int)(maxTime / 5000)} s";
            TempTime2.Text = $"{(int)(2 * maxTime / 5000)} s";
            TempTime3.Text = $"{(int)(3 * maxTime / 5000)} s";
            TempTime4.Text = $"{(int)(4 * maxTime / 5000)} s";
            TempTime5.Text = $"{(int)maxTime / 1000} s";

            KoncTime0.Text = $"0 s";
            KoncTime1.Text = $"{(int)(maxTime / 5000)} s";
            KoncTime2.Text = $"{(int)(2 * maxTime / 5000)} s";
            KoncTime3.Text = $"{(int)(3 * maxTime / 5000)} s";
            KoncTime4.Text = $"{(int)(4 * maxTime / 5000)} s";
            KoncTime5.Text = $"{(int)maxTime / 1000} s";

            _numberOfDatapoints = states.Length;
            _resized = false; // reset resized flag

            KoncA.Text = $"{(float)(int)(states[^1].Konc[0] * 100000) / 100}";
            KoncB.Text = $"{(float)(int)(states[^1].Konc[1] * 100000) / 100}";
            KoncC.Text = $"{(float)(int)(states[^1].Konc[2] * 100000) / 100}";
            KoncD.Text = $"{(float)(int)(states[^1].Konc[3] * 100000) / 100}";
            KoncX.Text = $"{(float)(int)(states[^1].Konc[4] * 100000) / 100}";
            TemperaturDisplay.Text = $"{(int)states[^1].Temperatur} K";
            LigevægtskonstantDisplay.Text = $"{states[^1].AproximeretLigevægtskonstant}";
        }
        private void UpdateControlpanelKoncentration()
        {
            KoncA.Text = $"{(float)(int)(_sim.GetCurrentState.Konc[0] * 100000) / 100}";
            KoncB.Text = $"{(float)(int)(_sim.GetCurrentState.Konc[1] * 100000) / 100}";
            KoncC.Text = $"{(float)(int)(_sim.GetCurrentState.Konc[2] * 100000) / 100}";
            KoncD.Text = $"{(float)(int)(_sim.GetCurrentState.Konc[3] * 100000) / 100}";
            KoncX.Text = $"{(float)(int)(_sim.GetCurrentState.Konc[4] * 100000) / 100}";
        }
        private void UpdateControlpanelBindingsenergi()
        {
            BindingsenergiA.Text = $"{(int)Math.Round(_sim.BindingsenergiA * 1E21)} E-21";
            BindingsenergiB.Text = $"{(int)Math.Round(_sim.BindingsenergiB * 1E21)} E-21";
            BindingsenergiC.Text = $"{(int)Math.Round(_sim.BindingsenergiC * 1E21)} E-21";
            BindingsenergiD.Text = $"{(int)Math.Round(_sim.BindingsenergiD * 1E21)} E-21";
        }
        private void UpdateControlpanelAktiveringsenergi()
        {
            AktiveringsenergiDisplay.Text = $"{(int)Math.Round(_sim.Aktiveringsenergi * 1E21)} E-21";
        }
        private void UpdateControlpanelTemperatur()
        {
            TemperaturDisplay.Text = $"{(int)_sim.GetCurrentState.Temperatur} K";
        }
        private void UpdateControlpanelSansylnighed()
        {
            SansylnighedABTilCD.Text = _sim.ABToCD == 0 ? "0" : $"1 / {_sim.ABToCD}" ;
            SansylnighedCDTilAB.Text = _sim.CDToAB == 0 ? "0" : $"1 / {_sim.CDToAB}" ;
        }
        private void TimeInputfiledButton_Click(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(TimeInputfield.Text, out float time))
            {
                _startTimeMilliseconds = (int)(time * 1000f);
                _sw.Reset();
            }
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => _resized = true;
        private void DisplaySpeedUpButton_Click(object sender, RoutedEventArgs e)
        {
            _startTimeMilliseconds += (int)(_sw.ElapsedMilliseconds / _displaySpeed);
            if (!_paused) _sw.Restart();
            _displaySpeed = _displaySpeed / 2 < 1 ? _displaySpeed : _displaySpeed /= 2;
            DisplaySpeedDisplay.Text = "Display speed: " + _displaySpeed;
        }
        private void DisplaySpeedDownButton_Click(object sender, RoutedEventArgs e)
        {
            _startTimeMilliseconds += (int)(_sw.ElapsedMilliseconds / _displaySpeed);
            if (!_paused) _sw.Restart();
            _displaySpeed *= 2;
            DisplaySpeedDisplay.Text = "Display speed: " + _displaySpeed;
        }
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_paused)
            {
                _paused = true;
                _startTimeMilliseconds += (int)(_sw.ElapsedMilliseconds / _displaySpeed);
                _sw.Reset();
            }
        }
        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_paused)
            {
                _paused = false;
                _startTimeMilliseconds += (int)(_sw.ElapsedMilliseconds / _displaySpeed);
                _sw.Restart();
            }
        }
        private void StartReaktionButton_Click(object sender, RoutedEventArgs e)
        {
            _sim.Run();
        }
        private void BindingsenergiAOp_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyBindingsenergiA(true);
            UpdateControlpanelBindingsenergi();
        }
        private void BindingsenergiANed_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyBindingsenergiA(false);
            UpdateControlpanelBindingsenergi();
        }
        private void BindingsenergiBOp_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyBindingsenergiB(true);
            UpdateControlpanelBindingsenergi();
        }
        private void BindingsenergiBNed_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyBindingsenergiB(false);
            UpdateControlpanelBindingsenergi();
        }
        private void BindingsenergiCOp_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyBindingsenergiC(true);
            UpdateControlpanelBindingsenergi();
        }
        private void BindingsenergiCNed_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyBindingsenergiC(false);
            UpdateControlpanelBindingsenergi();
        }
        private void BindingsenergiDOp_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyBindingsenergiD(true);
            UpdateControlpanelBindingsenergi();
        }
        private void BindingsenergiDNed_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyBindingsenergiD(false);
            UpdateControlpanelBindingsenergi();
        }
        private void ResetReaktionButton_Click(object sender, RoutedEventArgs e)
        {
            _sim = new(10000, 1.5f, 1, 0.2f, 0.1f, 0.5f, _simSize, 1);
            _startTimeMilliseconds = 0;
            _numberOfDatapoints = 0;
            _sw.Reset();
            UpdateControlpanelBindingsenergi();
            UpdateControlpanelKoncentration();
            UpdateControlpanelAktiveringsenergi();
        }
        private void AktiveringsenergiOp_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyAktiveringsenergi(true);
            UpdateControlpanelAktiveringsenergi();
        }
        private void AktiveringsenergiNed_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyAktiveringsenergi(false);
            UpdateControlpanelAktiveringsenergi();
        }
        private void TemperaturOp_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyTemp(true);
            UpdateControlpanelTemperatur();
        }
        private void TemperaturNed_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyTemp(false);
            UpdateControlpanelTemperatur();
        }
        private void TilføjA_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyBestand(true, Sim.Type.A);
            UpdateControlpanelKoncentration();
        }
        private void FjernA_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyBestand(false, Sim.Type.A);
            UpdateControlpanelKoncentration();
        }
        private void TilføjB_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyBestand(true, Sim.Type.B);
            UpdateControlpanelKoncentration();
        }
        private void FjernB_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyBestand(false, Sim.Type.B);
            UpdateControlpanelKoncentration();
        }
        private void TilføjC_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyBestand(true, Sim.Type.C);
            UpdateControlpanelKoncentration();
        }
        private void FjernC_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyBestand(false, Sim.Type.C);
            UpdateControlpanelKoncentration();
        }
        private void TilføjD_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyBestand(true, Sim.Type.D);
            UpdateControlpanelKoncentration();
        }
        private void FjernD_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyBestand(false, Sim.Type.D);
            UpdateControlpanelKoncentration();
        }
        private void TilføjX_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyBestand(true, Sim.Type.X);
            UpdateControlpanelKoncentration();
        }
        private void FjernX_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyBestand(false, Sim.Type.X);
            UpdateControlpanelKoncentration();
        }
        private void SansynlighedABTilCDOp_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyABToCD(true);
            UpdateControlpanelSansylnighed();
        }
        private void SansynlighedABTilCDNed_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyABToCD(false);
            UpdateControlpanelSansylnighed();
        }
        private void SansynlighedCDTilABOp_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyCDToAB(true);
            UpdateControlpanelSansylnighed();
        }
        private void SansynlighedCDTilABNed_Click(object sender, RoutedEventArgs e)
        {
            _sim.ModifyCDToAB(false);
            UpdateControlpanelSansylnighed();
        }
    }
}