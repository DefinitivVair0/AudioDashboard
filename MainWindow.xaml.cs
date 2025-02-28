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

namespace AudioDashboard;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static MainWindow mw;
    public MainWindow()
    {
        mw = this;
        InitializeComponent();

        infoLabel.Content = "Channels:\nEncoding:\nBpS:\nSample Rate:\nExtra Size:\nBlock Allign:\nAverage BpS:";

        for (int i = 0; i<100;i++)
        {
            deviationValues.Add(double.NaN);
            volumeValues.Add(float.NaN);
        }

        volumeBar.Foreground = new LinearGradientBrush(Color.FromArgb(255, 0, 255, 255), Color.FromArgb(255, 170, 0, 255), 0);
        angularGauge.Sections[0].Fill = new LinearGradientBrush(Color.FromArgb(255, 0, 255, 255), Color.FromArgb(255, 170, 0, 255), 90);
        angularGauge.Sections[1].Fill = new LinearGradientBrush(Color.FromArgb(255, 0, 255, 255), Color.FromArgb(255, 170, 0, 255), 90);
        angularGauge.TicksForeground = new LinearGradientBrush(Color.FromArgb(255, 170, 0, 255), Color.FromArgb(255, 0, 255, 255), 90);

        SpectrumSeries = new SeriesCollection
        {
            new LineSeries
            {
                Title = "Deviation",
                Values = deviationValues,
                LineSmoothness = 1,
                ScalesYAt = 0,
                PointGeometry = null,
                Fill = Brushes.Transparent,
                Stroke = new LinearGradientBrush(Color.FromArgb(0,0,255,255), Color.FromArgb(255,0,255,255), 0)
            },
            new LineSeries
            {
                Title = "Volume",
                Values = volumeValues,
                LineSmoothness = 1,
                ScalesYAt = 1,
                PointGeometry = null,
                Fill = Brushes.Transparent,
                Stroke = new LinearGradientBrush(Color.FromArgb(0,170,0,255), Color.FromArgb(255,180,0,255), 0)
            }
        };

        DataContext = this;

        var enumerator = new MMDeviceEnumerator();
        mmDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
        foreach (var wasapi in mmDevices)
        {
            deviceBox.Items.Add(wasapi.FriendlyName);
        }
    }

   private AudioProcessor ap = null;
    private MMDeviceCollection mmDevices;

    private ChartValues<double> deviationValues = new ChartValues<double> { };
    private ChartValues<float> volumeValues = new ChartValues<float> { };
    public SeriesCollection SpectrumSeries { get; set; }

    private void ClosingEventHandler(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (ap!=null) ap.Stop(); ap = null;
    }

    public void Update((string Info, float Volume, double Deviation) Data)
    {
        volumeBar.Value = Data.Volume;
        //var scb = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(2.5 * Data.Volume), (byte)(255 - 2.5 * Data.Volume), 0));
        //volumeBar.Foreground = scb;

        angularGauge.Value = Data.Deviation;
        deviationValues.Add(Data.Deviation);
        volumeValues.Add(Data.Volume);
        if (deviationValues.Count >= 100) { deviationValues.RemoveAt(0); volumeValues.RemoveAt(0); }
        infoLabel.Content = Data.Info;
    }

    private void stopBtn_Click(object sender, RoutedEventArgs e)
    {
        ap.Stop();
        ap = null;

        deviationValues.Clear();
        volumeValues.Clear();
        volumeBar.Value = 0;
        angularGauge.Value = 0;
    }

    private void startBtn_Click(object sender, RoutedEventArgs e)
    {
        if (deviceBox.SelectedItem != null)
        {
            if (ap == null) { ap = new AudioProcessor(mmDevices.ElementAt(deviceBox.SelectedIndex)); ap.Start(); }
            else { ap.Stop(); ap = new AudioProcessor(mmDevices.ElementAt(deviceBox.SelectedIndex)); ap.Start(); }
        }
    }

    private void averageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ap!=null) ap.setAverage((int)averageSlider.Value);
    }
}