using NAudio.Wave;
using OpenTK.Core;
using ScottPlot.WPF;
using System.Timers;
using System.Windows;
using System.Windows.Threading;

namespace AudioDashboard
{
    public class AudioProcessor
    {
        private readonly System.Timers.Timer timer;
        private readonly int SAMPLE_RATE;
        private readonly bool stereo;
        private readonly WaveInEvent wvin;
        private readonly WpfPlot plot;


        public AudioProcessor(int deviceNr, byte bufferMs = 20, int _SAMPLE_RATE = 48000, byte updateMul = 1, bool _stereo = false)
        {
            stereo = _stereo;
            SAMPLE_RATE = _SAMPLE_RATE;
            timer = new System.Timers.Timer { AutoReset = true, Interval = bufferMs*updateMul };
            timer.Elapsed += Update;
            plot = MainWindow.mw.fftPlot;

            plot.Plot.Axes.TightMargins();
            plot.Plot.Axes.SetLimitsY(0,5000);
            plot.Plot.Axes.SetLimitsX(0, SAMPLE_RATE/2);

            WaveFormat wf = new WaveFormat(rate: SAMPLE_RATE, bits: 16, channels: stereo ? 2 : 1);

            wvin = new WaveInEvent
            {
                DeviceNumber = deviceNr,
                BufferMilliseconds = bufferMs,
                WaveFormat = wf
            };

            outData.Info = $"Block Allign: {wf.BlockAlign}   Encoding: {wf.Encoding}\nChannels: {wf.Channels}   Sample Rate: {wf.SampleRate}\nBipS: {wf.BitsPerSample}   Average BpS: {wf.AverageBytesPerSecond}";

            wvin.DataAvailable += OnDataAvailable;
        }


        private readonly FftSharp.Windows.Hanning window = new();
        private double[] lastBuffer;
        private double[]? lastBufferRight;
        private List<double> volumeStack = new List<double>(capacity: 100) { };

        private void OnDataAvailable(object? sender, WaveInEventArgs args)
        {
            int bytesPerSample = wvin.WaveFormat.BitsPerSample / 8;
            int samplesRecorded = args.BytesRecorded / bytesPerSample;

            if (lastBuffer is null || lastBuffer.Length != samplesRecorded)
            {
                lastBuffer = stereo ? new double[samplesRecorded / 2] : new double[samplesRecorded];
                if (stereo) lastBufferRight = new double[samplesRecorded / 2];
            }

            int k = 0;
            for (int i = 0; i < samplesRecorded; i += stereo ? 2 : 1)
            {
                lastBuffer[k] = BitConverter.ToInt16(args.Buffer, i * bytesPerSample);
                if (stereo) lastBufferRight[k] = BitConverter.ToInt16(args.Buffer, (i + 1) * bytesPerSample);
                k++;
            }
        }

        double[] SignalData = null!;
        (string Info, double VolumeL, double VolumeR, double Volume, double Deviation) outData;

        public void Update(object? sender, ElapsedEventArgs e)
        {
            if (lastBuffer is null) return;
            timer.Stop();

            if (stereo)
            {
                //Volume and Deviation Stereo
                outData.VolumeL = lastBuffer.Max() / 200;
                outData.VolumeR = lastBufferRight.Max() / 200;

                outData.Volume = (outData.VolumeL + outData.VolumeR) / 2; 

                double vsAvg = volumeStack.Sum() / volumeStack.Count;
                outData.Deviation = volumeStack.Count != 0 ? outData.Volume - vsAvg : 0;

                volumeStack.Add(outData.Volume);

                if (volumeStack.Count >= volumeStack.Capacity - 1) volumeStack.RemoveAt(0);

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    MainWindow.mw.Update(outData);
                }));
            }
            else
            {
                //Volume and Deviation Mono
                outData.VolumeR = outData.VolumeL = outData.Volume = lastBuffer.Max() / 200;

                double vsAvg = volumeStack.Sum() / volumeStack.Count;
                outData.Deviation = volumeStack.Count != 0 ? outData.Volume - vsAvg : 0;

                volumeStack.Add(outData.Volume);

                if (volumeStack.Count >= volumeStack.Capacity - 1) volumeStack.RemoveAt(0);

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    MainWindow.mw.Update(outData);
                }));
            }


                //FFT
                window.ApplyInPlace(lastBuffer);
            System.Numerics.Complex[] spectrum = FftSharp.FFT.Forward(FftSharp.Pad.ZeroPad(lastBuffer));

            var fftValue = FftSharp.FFT.Magnitude(spectrum);

            double[] fftFreq = FftSharp.FFT.FrequencyScale(fftValue.Length, SAMPLE_RATE);


            if (!plot.Plot.GetPlottables().Any())
            {
                double samplePeriod = 1.0 / (2.0 * fftValue.Length / SAMPLE_RATE);
                SignalData = fftValue;
                plot.Plot.Add.Signal(SignalData, samplePeriod).Color = ScottPlot.Color.FromHex("#00DDFF");
            }
            else Array.Copy(fftValue, SignalData, fftValue.Length);

            plot.Refresh();
            timer.Start();
        }


        public void Start()
        {
            wvin.StartRecording();
            timer.Start();
        }

        public void Stop()
        {
            plot.Plot.Clear();
            timer.Stop();
            timer.Dispose();
            wvin.Dispose();
        }

        public bool setAverage(int avg)
        {
            if (avg > 0)
            {
                volumeStack.Clear();
                volumeStack.Capacity = avg;
                return true;
            }
            else return false;
        }
    }
}
