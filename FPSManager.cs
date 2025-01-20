using System.Diagnostics;

namespace ConsoleGame
{
    internal class FPSManager
    {
        public int CurrentFPS;
        private DateTime LastDT;
        private int LastFrame;
        public float deltaTime;
        public float previousTime;
        Stopwatch stopwatch;
        public FPSManager()
        {
            LastDT = DateTime.Now;
            previousTime = 0f;
            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        public void Update()
        {
            float currentTime = (float)stopwatch.Elapsed.TotalSeconds;
            deltaTime = currentTime - previousTime;
            LastFrame++;
            if (DateTime.Now >= LastDT.AddSeconds(1))
            {
                LastDT = DateTime.Now;
                CurrentFPS = LastFrame;
                LastFrame = 0;
            }
            previousTime = currentTime;
        }


    }
}
