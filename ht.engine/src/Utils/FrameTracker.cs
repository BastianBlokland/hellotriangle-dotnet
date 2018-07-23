using System.Diagnostics;

namespace HT.Engine.Utils
{
    public sealed class FrameTracker
    {
        // Used in the smoothed fps calculation, between 0 and 1. Higher numbers discounts older 
        //fps observations faster 
        private const float FPS_SMOOTHING_WEIGHTING_COEFFICIENT = .03f;

        //Public properties
        public int FrameNumber => frameNumber;
        public double ElapsedTime => stopwatch.Elapsed.TotalSeconds;
        public float TimeInThisFrame => (float)(ElapsedTime - frameStartTime);
        public float DeltaTime => lastFrameDuration; //Alias for 'LastFrameDuration'
        public float LastFps => lastFps;
        public float SmoothedFps => smoothedFps;

        //Data
        private double frameStartTime;
        private float lastFrameDuration;
        private float lastFps;
        private float smoothedFps = -1;
        private int frameNumber;
        private Stopwatch stopwatch;

        public FrameTracker()
        {
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        public void TrackFrame()
        {
            //Track the time that the last frame took to execute (in seconds)
            lastFrameDuration = TimeInThisFrame;
            //Also store that in frames per second
            lastFps = 1f / lastFrameDuration;
            //Calculated a smoothed fps using 'Exponential moving average'
            //Wiki: https://en.wikipedia.org/wiki/Moving_average#Exponential_moving_average
            smoothedFps = smoothedFps < 0
                ? lastFps 
                : smoothedFps + (lastFps - smoothedFps) * FPS_SMOOTHING_WEIGHTING_COEFFICIENT;
            //Track when the new frame started
            frameStartTime = ElapsedTime;
            //Increment the frame number
            frameNumber++;
        }
    }
}