using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioDashboard
{
    class FFTCalculator(AudioProcessor ap, int SAMPLE_RESOLUTION, int BYTES_PER_POINT)
    {
        public (double[] FFT_Magnitude, double[] FFT_Power, int FFT_Length)? GetFFT()
        {
            int frameSize = ap.getBufferSize();
            var frames = new byte[frameSize];
            ap.GetBufferedWaveProvider().Read(frames, 0, frameSize);

            if (frames.Length == 0) return null;

            MainWindow.mw.timer.Enabled = false;

            double[] vals = new double[frames.Length / BYTES_PER_POINT];

            for (int i = 0; i < vals.Length; i++)
            {
                vals[i] = frames[i];
            }

            var window = new FftSharp.Windows.Hanning();
            window.ApplyInPlace(vals);

            System.Numerics.Complex[] spectrum = FftSharp.FFT.Forward(vals);

            double[] magnitude = FftSharp.FFT.Magnitude(spectrum);
            double[] power = FftSharp.FFT.Power(spectrum);

            return (magnitude,power,magnitude.Length);
        }
    }
}
