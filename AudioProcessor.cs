using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NAudio;
using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;
using LiveCharts;

namespace AudioDashboard
{
    class AudioProcessor(int deviceNr, byte bufferMS = 20)
    {
        private WaveIn capture = new WaveIn { DeviceNumber=deviceNr, BufferMilliseconds = bufferMS, WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(192000,1)};
        //private WasapiCapture capture = new WasapiCapture();

        private BufferedWaveProvider bwp; public BufferedWaveProvider GetBufferedWaveProvider() { return bwp; }

        private readonly int BUFFERSIZE = (int)Math.Pow(2,13); public int getBufferSize() { return BUFFERSIZE; }


        private (string Info, float Volume, double Deviation) outData = ("", 0, 0); public (string Info, float Volume, double Deviation) getData() { return outData; }


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

        public void Start()
        {
            WaveFormat wf = capture.WaveFormat;
            outData.Info = $"Block Allign: {wf.BlockAlign}   Encoding: {wf.Encoding}\nChannels: {wf.Channels}   Sample Rate: {wf.SampleRate}\nBipS: {wf.BitsPerSample}   Average BpS: {wf.AverageBytesPerSecond}";

            capture.DataAvailable += (s, a) => Capture(s, a);
            bwp = new BufferedWaveProvider(capture.WaveFormat);
            bwp.BufferLength = BUFFERSIZE*2;

            bwp.DiscardOnBufferOverflow = true;
            capture.StartRecording();
        }

        public void Stop()
        {
            capture.StopRecording();
            capture.Dispose();
        }

        private List<double> volumeStack = new List<double>(capacity:100) { };

        public async Task Capture(object sender, WaveInEventArgs args)
        {
            bwp.AddSamples(args.Buffer, 0, args.BytesRecorded);

            float max = 0;
            float[] buffer = new WaveBuffer(args.Buffer).FloatBuffer;

            // interpret as 32 bit floating point audio
            for (int index = 0; index < args.BytesRecorded / 8; index++)
            {
                var sample = buffer[index];

                if (sample < 0) sample *= -1;

                // is this the max value?
                if (sample > max) max = sample;
            }

            double vsAvg = volumeStack.Sum() / volumeStack.Count;
            outData.Volume = max * 100;
            if (volumeStack.Count != 0) { outData.Deviation = outData.Volume - vsAvg; } 
            else outData.Deviation = 0;
            
            volumeStack.Add(outData.Volume);

            while (volumeStack.Count >= volumeStack.Capacity-1 ) volumeStack.RemoveAt(0);

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(()=> 
            {
                MainWindow.mw.Update(outData); 
            }));
        }
    }
}
