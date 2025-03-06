using LiveCharts;
using LiveCharts.Wpf;
using NAudio.Wave;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AudioDashboard;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    //Support variables
    public static MainWindow mw;
    public readonly Brush defaultBrush;
    public readonly CultureInfo cultureInfo = CultureInfo.CreateSpecificCulture("de-DE");

    public MainWindow()
    {
        InitializeComponent();

        //Variable and control initialization
        mw = this;
        DataContext = this;
        defaultBrush = bufferMSTextBox.Foreground;

        volumeBarL.Foreground = volumeBarR.Foreground = new LinearGradientBrush(Color.FromArgb(255, 0, 255, 255), Color.FromArgb(255, 170, 0, 255), 0);
        angularGauge.Sections[1].Fill = new LinearGradientBrush(Color.FromArgb(255, 0, 255, 255), Color.FromArgb(255, 170, 0, 255), 90);
        angularGauge.Sections[0].Fill = new LinearGradientBrush(Color.FromArgb(255, 0, 255, 255), Color.FromArgb(255, 170, 0, 255), 90);
        angularGauge.TicksForeground = new LinearGradientBrush(Color.FromArgb(255, 170, 0, 255), Color.FromArgb(255, 0, 255, 255), 90);

        infoLabel.Content = "Block Allign:     Encoding:\nChannels    Sample Rate:\nBipS:     Average BpS:";

        for (int i = 0; i<100;i++)
        {
            deviationValues.Add(double.NaN);
            volumeValues.Add(float.NaN);
        }

        bufferMSTextBox.Text = bufferMs.ToString();
        updateMulTextBox.Text = updateMul.ToString();

        SpectrumSeries =
        [
            new LineSeries
            {
                Title = "Deviation",
                Values = deviationValues,
                LineSmoothness = 1,
                ScalesYAt = 1,
                PointGeometry = null,
                Fill = Brushes.Transparent,
                Stroke = new LinearGradientBrush(Color.FromArgb(0,0,255,255), Color.FromArgb(255,0,255,255), 0)
            },
            new LineSeries
            {
                Title = "Volume",
                Values = volumeValues,
                LineSmoothness = 1,
                ScalesYAt = 0,
                PointGeometry = null,
                Fill = Brushes.Transparent,
                Stroke = new LinearGradientBrush(Color.FromArgb(0,170,0,255), Color.FromArgb(255,180,0,255), 0)
            }
        ];


        for (var device = 0; device < WaveIn.DeviceCount; device++)
        {
            deviceBox.Items.Add(WaveIn.GetCapabilities(device).ProductName);
        }


        fftPlot.Plot.FigureBackground.Color = ScottPlot.Colors.Transparent;
        fftPlot.Plot.Axes.Color(ScottPlot.Colors.Gray);
        fftPlot.Plot.Grid.IsVisible = false;
        fftPlot.UserInputProcessor.IsEnabled = false;
    }


    private AudioProcessor? ap = null;

    private readonly ChartValues<double> deviationValues = [];
    private readonly ChartValues<double> volumeValues = [];
    public SeriesCollection SpectrumSeries { get; set; }

    private int bufferMs = 20, updateMul = 2, sampleRate = 48000;
    private bool stereo = true, fftWindow = true;

    private bool isFullscreen = false;


    public void Update((double VolumeL, double VolumeR, double Volume, double Deviation) Data)
    {
        volumeBarL.Value = Data.VolumeL;
        volumeBarR.Value = Data.VolumeR;

        angularGauge.Value = Data.Deviation;

        deviationValues.Add(Data.Deviation); deviationValues.RemoveAt(0);
        volumeValues.Add(Data.Volume); volumeValues.RemoveAt(0);
    }


    private void ClosingEventHandler(object sender, System.ComponentModel.CancelEventArgs e)
    {
        ap?.Stop(); ap = null;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            mw.WindowState = isFullscreen ? WindowState.Normal : WindowState.Maximized;
            mw.WindowStyle = isFullscreen ? WindowStyle.SingleBorderWindow : WindowStyle.None;
            isFullscreen = !isFullscreen;
        }
    }


    private void stopBtn_Click(object sender, RoutedEventArgs e)
    {
        if (ap == null) return;

        ap.Stop();
        ap = null;

        deviationValues.Clear();
        volumeValues.Clear();

        for (int i = 0; i < 100; i++)
        {
            deviationValues.Add(double.NaN);
            volumeValues.Add(float.NaN);
        }

        infoLabel.Content = "Block Allign:     Encoding:\nChannels    Sample Rate:\nBipS:     Average BpS:";

        volumeBarL.Value = volumeBarR.Value = 0;
        angularGauge.Value = 0;

        fftPlot.Plot.Clear();
        
        startBtn.Background = Brushes.DarkGreen;
        stopBtn.Background = Brushes.Red;
    }


    private void startBtn_Click(object sender, RoutedEventArgs e)
    {
        if (deviceBox.SelectedItem != null)
        {
            ap?.Stop();

            ap = new AudioProcessor(deviceBox.SelectedIndex, bufferMs, sampleRate, updateMul, stereo, fftWindow);
            ap.Start();

            startBtn.Background = Brushes.Green;
            stopBtn.Background = Brushes.DarkRed;
        }
    }


    private void averageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        ap?.setAverage((int)averageSlider.Value);
    }

    private void bufferMSTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (bufferMSTextBox.Text == "") return;

        if (int.TryParse(bufferMSTextBox.Text, out int i))
        {
            if (i < 1) bufferMs = 1;
            else if (i > 200) bufferMs = 200;
            else bufferMs = (byte)i;

            bufferMSTextBox.Text = bufferMs.ToString();

            if (bufferMs < 20 && bufferMs >= 10) bufferMSTextBox.Foreground = Brushes.Yellow;
            else if (bufferMs < 10) bufferMSTextBox.Foreground = Brushes.Red;
            else bufferMSTextBox.Foreground = defaultBrush;
        }
        else MessageBox.Show("Input must be a whole number between 1 and 200");
    }

    private void updateMulTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (updateMulTextBox.Text == "") return;

        if (int.TryParse(updateMulTextBox.Text, out int i))
        {
            if (i < 1) updateMul = 1;
            else if (i > 10) updateMul = 10;
            else updateMul = (byte)i;

            updateMulTextBox.Text = updateMul.ToString();
        }
        else MessageBox.Show("Input must be a whole number between 1 and 10");
    }

    private void stereoCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        stereo = stereoCheckBox.IsChecked.Value;
    }

    private void fftWindowCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        fftWindow = FftWindowCheckBox.IsChecked.Value;
    }

    private void sampleRateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string val = sampleRateComboBox.SelectedItem.ToString().Substring(sampleRateComboBox.SelectedItem.ToString().LastIndexOf(':') + 2);

        if (!int.TryParse(string.Concat(val.Where(Char.IsDigit)), out sampleRate)) MessageBox.Show("Input must be a whole number");
    }

    private void addCBBtn_Click(object sender, RoutedEventArgs e)
    {
        InputDialog dialog = new("Sample rate");
        dialog.ShowDialog();

        if (int.TryParse(dialog.Answer, out int sr))
        {
            sampleRateComboBox.Items.Add(sr.ToString("N0", cultureInfo) + "Hz");
            sampleRateComboBox.SelectedIndex = sampleRateComboBox.Items.Count - 1;
        }
    }
}