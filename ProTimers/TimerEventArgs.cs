namespace Aventra.Nugget.ProTimers
{
    public sealed class TimerEventArgs : EventArgs
    {
        public required TimeSpan Current { get; init; }
        public required TimeSpan Elapsed { get; init; }
        public required TimeSpan Remaning { get; init; }
        /// <summary>Ilerleme [0..1], anlamliysa.</summary
        public required double Progress { get; init; }
        public object? Owner { get; init; }
        public required TimerState State { get; init; }
        /// <summary>Bu event bir loop donusunde mi ateslendi?</summary>
        public bool IsLooping { get; init; }
    }
}