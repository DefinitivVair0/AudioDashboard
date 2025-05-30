# AudioDashboard
>*current version: v0*

This program is written in C# using WPF and used to visualize and analyze an audio stream.

**Please note that this project is currently in development and very much unfinished.**


## How to use
1. Select yout audio device
2. Adjust **options**
3. Press the green "I" button on the right

You can change into fullscreen with F11.
To stop the audio capture, click the red "O" button on the right.
To change the audio device/Buffer/Multiplier change/select to the desired option and click the green "I" again.

## Options
<details>
	<summary> Stereo </summary>
	Toggles the seperation of data into left and right channel.
	
	-> Allows the volume bars to change in relation to the corrosponding channel but at a higher performance impact
</details>
<details>
	<summary> FFT Window </summary>
	Toggles the use of a window (Hanning) when calculating the FFT-Spectrum.
</details>
<details>
	<summary> Log FFT </summary>
	Toggles the use of a logarithmic scaling of the FFT-Spectrum.
</details>
<details>
	<summary> Buffer ms </summary>
	Selects the buffer update intervall.
	
	-> Range 1ms - 200ms

	-> Smaller values = lower FFT resolution but higher refresh rate (might cause artifacts and stuttering if to low)
 	-> Higher values = higher FFT resolution but lower refresh rate
</details>
<details>
	<summary> Multiplier </summary>
	Selects the UI update rate depending on the buffer update intervall.
	(values below 1 are only used to make the fft graph on high buffer ms a bit more enjoyable and probably cause some loss of information)

	-> Range 0.?x - 10x


	-> 1x = every buffer refresh causes one UI update 
	-> 10x = every ten buffer refreshes cause one UI update
	-> 0,1x = every buffer refresh causes ten UI updates

	! ATTENTION !	-	floating point numbers use "," instead of "." as the decimal seperator

	-> Allows the reduction of stuttering by slowing down the UI update while leaving the data gathering rate and FFT resolution unchanged
</details>
<details>
	<summary> Sample Rate </summary>
	The combo box selects the sample rate used for audio processing.
	

	-> The "Add SR" button can be used to add custom sample rates.

	-> There is no security mechanisms in place yet so the program might crash on to high/low custom sample rates
</details>


## Used packages
- LiveCharts
- NAudio
- FftSharp
- ScottPlot
