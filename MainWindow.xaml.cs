using NAudio.CoreAudioApi;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Wpf.Charts.Base;
using Microsoft.VisualBasic.Logging;
using System.Timers;
using Microsoft.Win32;
using NAudio.Wave;
using LiveCharts.Defaults;
using LiveCharts.Helpers;
using System.Collections.ObjectModel;
using ScottPlot;
using System.Numerics;
using NAudio.Dsp;
using ScottPlot.Plottables;
using System.Security.Cryptography.Xml;
using FftSharp.Windows;

namespace AudioDashboard;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : System.Windows.Window
{
    public static MainWindow mw;
    public readonly Brush defaultBrush;

    public MainWindow()
    {
        mw = this;
        InitializeComponent();

        defaultBrush = bufferMSTextBox.Foreground;

        infoLabel.Content = "Block Allign:     Encoding:\nChannels    Sample Rate:\nBipS:     Average BpS:";

        for (int i = 0; i<100;i++)
        {
            deviationValues.Add(double.NaN);
            volumeValues.Add(float.NaN);
        }

        bufferMSTextBox.Text = bufferMs.ToString();
        updateMulTextBox.Text = updateMul.ToString();

        volumeBarL.Foreground = volumeBarR.Foreground = new LinearGradientBrush(System.Windows.Media.Color.FromArgb(255, 0, 255, 255), System.Windows.Media.Color.FromArgb(255, 170, 0, 255), 0);
        angularGauge.Sections[1].Fill = new LinearGradientBrush(System.Windows.Media.Color.FromArgb(255, 0, 255, 255), System.Windows.Media.Color.FromArgb(255, 170, 0, 255), 90);
        angularGauge.Sections[0].Fill = new LinearGradientBrush(System.Windows.Media.Color.FromArgb(255, 0, 255, 255), System.Windows.Media.Color.FromArgb(255, 170, 0, 255), 90);
        angularGauge.TicksForeground = new LinearGradientBrush(System.Windows.Media.Color.FromArgb(255, 170, 0, 255), System.Windows.Media.Color.FromArgb(255, 0, 255, 255), 90);

        SpectrumSeries = new SeriesCollection
        {
            new LineSeries
            {
                Title = "Deviation",
                Values = deviationValues,
                LineSmoothness = 1,
                ScalesYAt = 1,
                PointGeometry = null,
                Fill = Brushes.Transparent,
                Stroke = new LinearGradientBrush(System.Windows.Media.Color.FromArgb(0,0,255,255), System.Windows.Media.Color.FromArgb(255,0,255,255), 0),
                ToolTip = null
            },
            new LineSeries
            {
                Title = "Volume",
                Values = volumeValues,
                LineSmoothness = 1,
                ScalesYAt = 0,
                PointGeometry = null,
                Fill = Brushes.Transparent,
                Stroke = new LinearGradientBrush(System.Windows.Media.Color.FromArgb(0,170,0,255), System.Windows.Media.Color.FromArgb(255,180,0,255), 0),
                ToolTip = null
            }
        };

        DataContext = this;

        int waveInDevices = WaveIn.DeviceCount;
        for (var wid = 0; wid < waveInDevices; wid++)
        {
            WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(wid);
            deviceBox.Items.Add(deviceInfo.ProductName);
        }

        fftPlot.Plot.FigureBackground.Color = ScottPlot.Colors.Transparent;
        fftPlot.Plot.Axes.Color(ScottPlot.Colors.Gray);
        fftPlot.Plot.Grid.IsVisible = false;
        fftPlot.UserInputProcessor.IsEnabled = false;
    }

    private AudioProcessor ap = null;

    private ChartValues<double> deviationValues = new ChartValues<double> { };
    private ChartValues<double> volumeValues = new ChartValues<double> { };
    public SeriesCollection SpectrumSeries { get; set; }

    private byte bufferMs = 20;
    private byte updateMul = 2;
    private bool stereo = true;

    public void Update((string Info, double VolumeL, double VolumeR, double Volume, double Deviation) Data)
    {
        volumeBarL.Value = Data.VolumeL;
        volumeBarR.Value = Data.VolumeR;

        angularGauge.Value = Data.Deviation;

        deviationValues.Add(Data.Deviation);
        volumeValues.Add(Data.Volume);
        if (deviationValues.Count >= 100) { deviationValues.RemoveAt(0); volumeValues.RemoveAt(0); }

        infoLabel.Content = Data.Info;
    }


    private void ClosingEventHandler(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (ap != null) ap.Stop(); ap = null;
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
            if (ap == null) { ap = new AudioProcessor(deviceBox.SelectedIndex, bufferMs: bufferMs, updateMul: updateMul, _stereo: stereo); ap.Start(); }
            else { ap.Stop(); ap = new AudioProcessor(deviceBox.SelectedIndex, bufferMs: bufferMs, updateMul: updateMul, _stereo: stereo); ap.Start(); }
            startBtn.Background = Brushes.Green;
            stopBtn.Background = Brushes.DarkRed;
        }
    }

    private void averageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ap!=null) ap.setAverage((int)averageSlider.Value);
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



    private bool isFullscreen = false;
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            mw.WindowState = isFullscreen ? WindowState.Normal : WindowState.Maximized;
            mw.WindowStyle = isFullscreen ? WindowStyle.SingleBorderWindow : WindowStyle.None;
            isFullscreen = !isFullscreen;
        }
    }

    private void stereoCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        stereo = stereoCheckBox.IsChecked.Value;
    }
}