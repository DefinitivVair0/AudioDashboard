# AudioDashboard
>*current version: v0*

This program is written in C# using WPF and used to visualize and analyze an audio stream.

**Please note that this project is currently in development and very much unfinished.**


## How to use
1. Select yout audio device
2. Adjust buffer refresh rate (1-200ms) and data refresh multiplier (1-10x) -> Buffer 20ms + Multiplier 2x = Buffer refresh every 20ms and graph refresh every 40ms
3. Press the green "I" button on the right

You can change into fullscreen with F11.
To stop the audio capture, click the red "O" button on the right.
To change the audio device/Buffer/Multiplier change/select to the desired option and click the green "I" again.



## Used packages
- LiveCharts
- NAudio
- FftSharp
- ScottPlot
