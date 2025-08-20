using System;
using System.Threading;

namespace Aventra.Nugget.Utility.ChickenTimers
{
    /// <summary>
    /// Olusturmayi kolaylastiran secenekler kumesi.
    /// Eksik alanlara gore CountUp/CountDown otomatik hesaplanir.
    /// </summary>
    public sealed class TimerOptions
    {
        public static double ONE_SECOND = 1000;

        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }

        /// <summary>Olusturulur olusturulmaz Start() cagrilsin mi?</summary>
        public bool PlayOnStart { get; set; } = false;

        /// <summary>
        /// Tick event'inin hedef periyodu. Yani 2 tik arasindaki sure
        /// (AutoTick = true icin kullanilir)
        /// </summary>
        public TimeSpan TickInterval { get; set; } = TimeSpan.FromMilliseconds(ONE_SECOND);

        /// <summary>Zaman olcegi (1.0 = gercek zaman). 0''dan buyuk olmali.</summary>
        public double TimeScale { get; set; } = 1.0;

        /// <summary>Hedefe varinca basa sar?</summary>
        public bool Loop { get; set; } = false;

        /// <summary>Iceriden System.Timers.Timer ile otomatik tiklensin mi?</summary>
        public bool AutoTick { get; set; } = true;

        /// <summary>Zorla mod? (null ise start/end'e gore hesaplanir)</summary>
        public TimerMode? Mode { get; set; }

        /// <summary>Event'lerin post edilecegini senk. baglami. (UI/Unity main thread icin yakalanabilir)</summary>
        public SynchronizationContext? EventContext { get; set; }
    }
}