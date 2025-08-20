namespace Aventra.Nugget.ChickenTimers
{
    /// <summary>
    /// Olusturmayi kolaylastiran secenekler kumesi.
    /// Eksik alanlara gore CountUp/CountDown otomatik hesaplanir.
    /// </summary>
    public sealed class TimerOptions
    {
        public static double ONE_SECOND = 1000;

        public TimeSpan? StartTime { get; init; }
        public TimeSpan? EndTime { get; init; }
        public TimeSpan? Duration { get; init; }

        /// <summary>Olusturulur olusturulmaz Start() cagrilsin mi?</summary>
        public bool PlayOnStart { get; init; } = false;

        /// <summary>
        /// Tick event'inin hedef periyodu. Yani 2 tik arasindaki sure
        /// (AutoTick = true icin kullanilir)
        /// </summary>
        public TimeSpan TickInterval { get; init; } = TimeSpan.FromMilliseconds(ONE_SECOND);

        /// <summary>Zaman olcegi (1.0 = gercek zaman). 0''dan buyuk olmali.</summary>
        public double TimeScale { get; init; } = 1.0;

        /// <summary>Hedefe varinca basa sar?</summary>
        public bool Loop { get; init; } = false;

        /// <summary>Iceriden System.Timers.Timer ile otomatik tiklensin mi?</summary>
        public bool AutoTick { get; init; } = true;

        /// <summary>Zorla mod? (null ise start/end'e gore hesaplanir)</summary>
        public TimerMode? Mode { get; init; }

        /// <summary>Event'lerin post edilecegini senk. baglami. (UI/Unity main thread icin yakalanabilir)</summary>
        public SynchronizationContext? EventContext { get; init; }
    }
}