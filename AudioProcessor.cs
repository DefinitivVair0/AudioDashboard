using FftSharp;
using NAudio.Wave;
using ScottPlot;
using ScottPlot.TickGenerators;
using ScottPlot.WPF;
using System.ComponentModel.DataAnnotations;
using System.Timers;
using System.Windows;
using System.Windows.Threading;

namespace AudioDashboard
{
    public class AudioProcessor
    {
        //Audio processor
        public int volReduction = 200;

        private System.Timers.Timer? timer;

        private WaveInEvent? wvin;
        private WpfPlot? plot, tplot;

        private readonly System.Diagnostics.Stopwatch watch = new();

        //FFT
        private double[]? lastBuffer, lastBufferRight, SignalData;

        private FftSharp.Windows.Hanning? window;

        private readonly List<double> volumeStack = new(capacity: 100) { };
        private readonly List<long> times = new(capacity: 100);

        //Output
        (double VolumeL, double VolumeR, double Volume, double Deviation) outData;


        public int DeviceNr { get; init; } = 0;
        public int BufferMs { get; init; } = 20;
        public int Samplerate { get; init; } = 48000;
        public double UpdateMul { get; init; } = 1;
        public bool Stereo { get; init; } = false;
        public bool UseFftWindow { get; init; } = true;
        public bool UseLogScale { get; init; } = true;


        private void OnDataAvailable(object? sender, WaveInEventArgs args) //Called by WaveIn
        {
            int bytesPerSample = wvin!.WaveFormat.BitsPerSample / 8;
            int samplesRecorded = args.BytesRecorded / bytesPerSample;

            if (lastBuffer is null || lastBuffer.Length != samplesRecorded)
            {
                lastBuffer = Stereo ? new double[samplesRecorded / 2] : new double[samplesRecorded];
                if (Stereo) lastBufferRight = new double[samplesRecorded / 2];
            }

            int k = 0;
            for (int i = 0; i < samplesRecorded; i += Stereo ? 2 : 1)
            {
                lastBuffer[k] = BitConverter.ToInt16(args.Buffer, i * bytesPerSample);
                if (Stereo) lastBufferRight![k] = BitConverter.ToInt16(args.Buffer, (i + 1) * bytesPerSample);
                k++;
            }
        }


        public void Update(object? sender, ElapsedEventArgs e) //Called by timer
        {
            if (lastBuffer is null) return;

            watch.Restart();

            timer!.Stop();

            if (Stereo)
            {
                //Volume Stereo
                outData.VolumeL = lastBuffer.Max() / volReduction;
                outData.VolumeR = lastBufferRight!.Max() / volReduction;

                outData.Volume = Math.Clamp((outData.VolumeL + outData.VolumeR) / 2, 0, 100);
            }
            else
            {
                //Volume Mono
                outData.VolumeR = outData.VolumeL = outData.Volume = Math.Clamp(lastBuffer.Max() / volReduction, 0, 100);
            }

            outData.Deviation = Math.Clamp(volumeStack.Count != 0 ? outData.Volume - volumeStack.Sum() / volumeStack.Count : 0, -100, 100);
            volumeStack.Add(outData.Volume);
            if (volumeStack.Count >= volumeStack.Capacity - 1) volumeStack.RemoveAt(0);


            //Send output to MainWindow for update
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => { MainWindow.mw!.Update(outData); }));


            //FFT
            if (UseFftWindow) window!.ApplyInPlace(lastBuffer);
            System.Numerics.Complex[]? spectrum = FFT.Forward(Pad.ZeroPad(lastBuffer));

            var fftValue = FFT.Magnitude(spectrum);
            double[]? fftFreq = FFT.FrequencyScale(fftValue.Length, Samplerate);

            if (!plot!.Plot.GetPlottables().Any())
            {
                SignalData = fftValue;

                plot.Plot.Add.SignalXY(UseLogScale ? [.. fftFreq.Select(Math.Log10)] : fftFreq, SignalData, Color.FromHex("#00DDFF"));

            }
            else Array.Copy(fftValue, SignalData!, fftValue.Length);

            plot.Refresh();
            tplot!.Refresh();

            watch.Stop();
            times.Add(watch.ElapsedMilliseconds);
            times.RemoveAt(0);

            try { timer.Start(); } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }


        public void Init()
        {
            //Variable initialization
            timer = new() { AutoReset = true, Interval = BufferMs * UpdateMul };
            timer.Elapsed += Update;

            plot = MainWindow.mw!.fftPlot;
            tplot = MainWindow.mw.execTimePlot;
            window = UseFftWindow ? new() : null;

            //Configure Plots
            plot.Plot.Axes.SetLimitsY(0, 5000);
            plot.Plot.Axes.SetLimitsX(UseLogScale ? 1.4 : 0, UseLogScale ? Math.Log10(Samplerate / 2) : Samplerate / 2);

            static string LogTickLabelFormatter(double x) => $"{Math.Pow(10, x):N0}";
            plot.Plot.Axes.Bottom.TickGenerator = UseLogScale ? new LabeledLogTickGenerator()
            {
                MinorTickGenerator = new LogMinorTickGenerator(),
                IntegerTicksOnly = true,
                LabelFormatter = LogTickLabelFormatter
            } : new NumericAutomatic();
            plot.Plot.Grid.XAxisStyle.IsVisible = UseLogScale;


            tplot.Plot.Axes.SetLimitsX(0, 100);
            tplot.Plot.Axes.SetLimitsY(0, 10);
            tplot.Plot.Add.Signal(times);

            for (int i = 0; i < 100; i++)
            {
                times.Add(0);
            }

            //Initializing audio processor
            WaveFormat wf = new(rate: this.Samplerate, bits: 16, channels: Stereo ? 2 : 1);
            wvin = new WaveInEvent
            {
                DeviceNumber = DeviceNr,
                BufferMilliseconds = BufferMs,
                WaveFormat = wf
            };
            wvin.DataAvailable += OnDataAvailable;

            MainWindow.mw.infoLabel.Content = $"Block Allign: {wf.BlockAlign}   Encoding: {wf.Encoding}\nChannels: {wf.Channels}   Sample Rate: {wf.SampleRate}\nBipS: {wf.BitsPerSample}   Average BpS: {wf.AverageBytesPerSecond}";
        }

        public void Start()
        {
            Init();

            wvin!.StartRecording();
            timer!.Start();
        }

        public void Stop()
        {
            plot.Plot.Clear();
            timer.Stop();
            timer.Dispose();
            wvin.Dispose();
        }


        //Set self centering rate for deviation
        public bool SetAverage(int avg)
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