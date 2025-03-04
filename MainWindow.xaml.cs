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

    public System.Timers.Timer timer = new System.Timers.Timer();

    public MainWindow()
    {
        mw = this;
        InitializeComponent();

        timer.Interval = 80;
        timer.AutoReset = true;
        timer.Elapsed += UpdateFFT;

        infoLabel.Content = "Block Allign:     Encoding:\nChannels    Sample Rate:\nBipS:     Average BpS:";

        for (int i = 0; i<100;i++)
        {
            deviationValues.Add(double.NaN);
            volumeValues.Add(float.NaN);
        }

        volumeBar.Foreground = new LinearGradientBrush(System.Windows.Media.Color.FromArgb(255, 0, 255, 255), System.Windows.Media.Color.FromArgb(255, 170, 0, 255), 0);
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
                Stroke = new LinearGradientBrush(System.Windows.Media.Color.FromArgb(0,0,255,255), System.Windows.Media.Color.FromArgb(255,0,255,255), 0)
            },
            new LineSeries
            {
                Title = "Volume",
                Values = volumeValues,
                LineSmoothness = 1,
                ScalesYAt = 0,
                PointGeometry = null,
                Fill = Brushes.Transparent,
                Stroke = new LinearGradientBrush(System.Windows.Media.Color.FromArgb(0,170,0,255), System.Windows.Media.Color.FromArgb(255,180,0,255), 0)
            }
        };

        DataContext = this;

        int waveInDevices = WaveIn.DeviceCount;
        for (var wid = 0; wid < waveInDevices; wid++)
        {
            WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(wid);
            deviceBox.Items.Add(deviceInfo.ProductName);
        }
    }

    private AudioProcessor ap = null;

    private ChartValues<double> deviationValues = new ChartValues<double> { };
    private ChartValues<float> volumeValues = new ChartValues<float> { };
    public SeriesCollection SpectrumSeries { get; set; }


    public void Update((string Info, float Volume, double Deviation) Data)
    {
        volumeBar.Value = Data.Volume;   
        
        angularGauge.Value = Data.Deviation;

        deviationValues.Add(Data.Deviation);
        volumeValues.Add(Data.Volume);
        if (deviationValues.Count >= 100) { deviationValues.RemoveAt(0); volumeValues.RemoveAt(0); }

        infoLabel.Content = Data.Info;
    }






    private double[] dataY2;

    private static int SAMPLE_RESOLUTION = 16;
    private static int BYTES_PER_POINT = SAMPLE_RESOLUTION / 8;

    private FFTCalculator fftCalc;

    private Task InitializeFFT()
    {
        dataY2 = new double[ap.getBufferSize() / BYTES_PER_POINT / 2];

        fftPlot.Plot.Clear();

        //fftPlot.Plot.Add.Scatter(dataX2,dataY2);
        fftPlot.Plot.Add.Signal(dataY2);
        fftPlot.Plot.Axes.SetLimitsX(-10, 2500);
        fftPlot.Plot.Axes.SetLimitsY(-1, 100);

        fftPlot.Refresh();

        fftCalc = new FFTCalculator(ap, SAMPLE_RESOLUTION, BYTES_PER_POINT);

        timer.Enabled = true;

        return Task.CompletedTask;
    }

    public void UpdateFFT(object? sender, ElapsedEventArgs e)
    {
        if (ap == null) return;

        var data = fftCalc.GetFFT();

        if (data != null)
        {
            if (data.Value.FFT_Length < dataY2.Length) Array.Copy(data.Value.FFT_Magnitude, dataY2, data.Value.FFT_Length);
            else if (dataY2.Length<data.Value.FFT_Length) Array.Copy(data.Value.FFT_Magnitude, dataY2,dataY2.Length);
            fftPlot.Refresh();
        }

        timer.Enabled = true;
    }






    private void ClosingEventHandler(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (ap != null) ap.Stop(); ap = null;
    }

    private void stopBtn_Click(object sender, RoutedEventArgs e)
    {
        timer.Enabled = false;
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

        volumeBar.Value = 0;
        angularGauge.Value = 0;

        fftPlot.Plot.Clear();
        
        startBtn.Background = Brushes.DarkGreen;
        stopBtn.Background = Brushes.Red;
    }

    byte bufferMS = 50;

    private void startBtn_Click(object sender, RoutedEventArgs e)
    {
        if (deviceBox.SelectedItem != null)
        {
            if (ap == null) { ap = new AudioProcessor(deviceBox.SelectedIndex, bufferMS); ap.Start(); }
            else { ap.Stop(); ap = new AudioProcessor(deviceBox.SelectedIndex, bufferMS); ap.Start(); }
            startBtn.Background = Brushes.Green;
            stopBtn.Background = Brushes.DarkRed;

            InitializeFFT();
        }
    }

    private void averageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ap!=null) ap.setAverage((int)averageSlider.Value);
    }

    private void bufferMSTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        Byte.TryParse(bufferMSTextBox.Text, out bufferMS);
    }
}