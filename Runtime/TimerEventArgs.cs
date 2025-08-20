using System;

namespace Aventra.Nugget.Utility.ChickenTimers
{
    public sealed class TimerEventArgs : EventArgs
    {
        public TimeSpan Current { get; set; }
        public TimeSpan Elapsed { get; set; }
        public TimeSpan Remaning { get; set; }
        /// <summary>Ilerleme [0..1], anlamliysa.</summary
        public double Progress { get; set; }
        public object? Owner { get; set; }
        public  TimerState State { get; set; }
        /// <summary>Bu event bir loop donusunde mi ateslendi?</summary>
        public bool IsLooping { get; set; }
    }
}