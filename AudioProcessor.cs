using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NAudio;
using NAudio.CoreAudioApi;
using NAudio.Dsp;
using NAudio.Wave;

namespace AudioDashboard
{
    class AudioProcessor(MMDevice mmDevice)
    {
        private WasapiCapture capture = new WasapiCapture(mmDevice);

        private (string Info, float Volume, double Deviation) outData = ("",0, 0);
        public (string Info, float Volume, double Deviation) getData() { return outData; }

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
            capture.DataAvailable += (s, a) => Capture(s, a);
            var wf = capture.WaveFormat;
            outData.Info = $"Channels: {wf.Channels}\nEncoding: {wf.Encoding}\nBipS: {wf.BitsPerSample}\nSample Rate: {wf.SampleRate}\nExtra Size: {wf.ExtraSize}\nBlock Allign: {wf.BlockAlign}\nAverage BpS: {wf.AverageBytesPerSecond}";
            capture.StartRecording();
        }

        public void Stop()
        {
            capture.StopRecording();
            capture.Dispose();
            mmDevice.Dispose();
        }

        private List<double> volumeStack = new List<double>(capacity:100) { };

        public async Task Capture(object sender, WaveInEventArgs args)
        {
            float max = 0;
            var buffer = new WaveBuffer(args.Buffer);

            // interpret as 32 bit floating point audio
            for (int index = 0; index < args.BytesRecorded / 4; index++)
            {
                var sample = buffer.FloatBuffer[index];

                // absolute value 
                if (sample < 0) sample = -sample;
                // is this the max value?
                if (sample > max) max = sample;
            }

            double vsSum = volumeStack.Sum();

            outData.Volume = max * 100;

            if (volumeStack.Count != 0) { outData.Deviation = outData.Volume - vsSum / (volumeStack.Count); } 
            else outData.Deviation = 0;
            
            volumeStack.Add(outData.Volume);
            while (volumeStack.Count >= volumeStack.Capacity-1 ) volumeStack.RemoveAt(0);

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(()=> { MainWindow.mw.Update(outData); }));
        }
    }
}
