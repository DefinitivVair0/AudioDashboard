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
using System.Drawing;

namespace AudioDashboard;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static MainWindow mw;
    public MainWindow()
    {
        DataContext = this;
        mw = this;
        InitializeComponent();

        infoLabel.Content = "Channels:\nEncoding:\nBpS:\nSample Rate:\nExtra Size:\nBlock Allign:\nAverage BpS:";

        var enumerator = new MMDeviceEnumerator();
        mmDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
        foreach (var wasapi in mmDevices)
        {
            deviceBox.Items.Add(wasapi.FriendlyName);
        }
    }


    private AudioProcessor ap = null;
    private MMDeviceCollection mmDevices;

    private void ClosingEventHandler(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (ap!=null) ap.Stop(); ap = null;
    }

    public void Update((string Info, double Volume, double Deviation) Data)
    {
        volumeBar.Value = Data.Volume;
        var scb = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)(2.5 * Data.Volume), (byte)(255 - 2.5 * Data.Volume), 0));
        volumeBar.Foreground = scb;
        angularGauge.Value = Data.Deviation;
        infoLabel.Content = Data.Info;
    }

    private void stopBtn_Click(object sender, RoutedEventArgs e)
    {
        ap.Stop();
        ap = null;

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