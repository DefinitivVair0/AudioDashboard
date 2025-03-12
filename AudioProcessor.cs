using FftSharp;
using NAudio.Wave;
using ScottPlot;
using ScottPlot.TickGenerators;
using ScottPlot.WPF;
using System.Timers;
using System.Windows;
using System.Windows.Threading;

namespace AudioDashboard
{
    public class AudioProcessor
    {
        //Audio processor
        public int volReduction = 200;

        private readonly System.Timers.Timer timer;

        private readonly bool stereo, useFftWindow, useLogScale;
        private readonly int samplerate;

        private readonly WaveInEvent wvin;
        private readonly WpfPlot plot, tplot;

        private readonly System.Diagnostics.Stopwatch watch = new();

        //FFT
        private double[]? lastBuffer, lastBufferRight, SignalData;

        private readonly FftSharp.Windows.Hanning? window;

        private readonly List<double> volumeStack = new(capacity: 100) { };
        private readonly List<long> times = new(capacity: 100);

        //Output
        (double VolumeL, double VolumeR, double Volume, double Deviation) outData;


        public AudioProcessor(int deviceNr = 0, int bufferMs = 20, int samplerate = 48000, int updateMul = 1, bool stereo = false, bool useFftWindow = true, bool useLogScale = true)
        {
            //Variable initialization
            timer = new() { AutoReset = true, Interval = bufferMs * updateMul };
            timer.Elapsed += Update;

            this.stereo = stereo;
            this.useFftWindow = useFftWindow;
            this.useLogScale = useLogScale;
            this.samplerate = samplerate;

            plot = MainWindow.mw.fftPlot;
            tplot = MainWindow.mw.execTimePlot;
            window = useFftWindow ? new() : null;

            //Configure Plots
            plot.Plot.Axes.SetLimitsY(0,5000);
            plot.Plot.Axes.SetLimitsX(useLogScale ? 1.4 : 0, useLogScale ? Math.Log10(samplerate / 2) : samplerate / 2);

            static string LogTickLabelFormatter(double x) => $"{Math.Pow(10, x):N0}";
            plot.Plot.Axes.Bottom.TickGenerator = useLogScale ? new LabeledLogTickGenerator()
            {
                MinorTickGenerator = new LogMinorTickGenerator(),
                IntegerTicksOnly = true,
                LabelFormatter = LogTickLabelFormatter
            } : new NumericAutomatic();
            plot.Plot.Grid.XAxisStyle.IsVisible = useLogScale;


            tplot.Plot.Axes.SetLimitsX(0, 100);
            tplot.Plot.Axes.SetLimitsY(0, 10);
            tplot.Plot.Add.Signal(times);

            for (int i = 0; i<100;i++)
            {
                times.Add(0);
            }

            //Initializing audio processor
            WaveFormat wf = new(rate: this.samplerate, bits: 16, channels: stereo ? 2 : 1);
            wvin = new WaveInEvent
            {
                DeviceNumber = deviceNr,
                BufferMilliseconds = bufferMs,
                WaveFormat = wf
            };
            wvin.DataAvailable += OnDataAvailable;

            MainWindow.mw.infoLabel.Content = $"Block Allign: {wf.BlockAlign}   Encoding: {wf.Encoding}\nChannels: {wf.Channels}   Sample Rate: {wf.SampleRate}\nBipS: {wf.BitsPerSample}   Average BpS: {wf.AverageBytesPerSecond}"; ;
        }


        private void OnDataAvailable(object? sender, WaveInEventArgs args) //Called by WaveIn
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


        public void Update(object? sender, ElapsedEventArgs e) //Called by timer
        {
            if (lastBuffer is null) return;

            watch.Restart();

            timer.Stop();

            if (stereo)
            {
                //Volume Stereo
                outData.VolumeL = lastBuffer.Max() / volReduction;
                outData.VolumeR = lastBufferRight.Max() / volReduction;

                outData.Volume = Math.Clamp((outData.VolumeL + outData.VolumeR) / 2, 0, 100);
            }
            else
            {
                //Volume Mono
                outData.VolumeR = outData.VolumeL = outData.Volume = Math.Clamp(lastBuffer.Max() / volReduction,0,100);
            }
            
            outData.Deviation = Math.Clamp(volumeStack.Count != 0 ? outData.Volume - volumeStack.Sum() / volumeStack.Count : 0,-100,100);
            volumeStack.Add(outData.Volume);
            if (volumeStack.Count >= volumeStack.Capacity - 1) volumeStack.RemoveAt(0);


            //Send output to MainWindow for update
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => { MainWindow.mw.Update(outData); }));


            //FFT
            if (useFftWindow) window.ApplyInPlace(lastBuffer);
            System.Numerics.Complex[] spectrum = FFT.Forward(Pad.ZeroPad(lastBuffer));

            var fftValue = FFT.Magnitude(spectrum);
            double[] fftFreq = FFT.FrequencyScale(fftValue.Length, samplerate);

            if (!plot.Plot.GetPlottables().Any())
            {
                SignalData = fftValue;

                plot.Plot.Add.SignalXY(useLogScale ? [.. fftFreq.Select(Math.Log10)] : fftFreq, SignalData, Color.FromHex("#00DDFF"));

            }
            else Array.Copy(fftValue, SignalData, fftValue.Length);

            plot.Refresh();
            tplot.Refresh();

            watch.Stop();
            times.Add(watch.ElapsedMilliseconds);
            times.RemoveAt(0);

            try { timer.Start(); } catch (Exception ex) { MessageBox.Show(ex.Message); }
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