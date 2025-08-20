using System.Diagnostics;
using System.Timers;

namespace Aventra.Nugget.ChickenTimers
{
    public sealed class SmartTimer : IDisposable
    {
        private readonly Stopwatch _stopWatch = new Stopwatch();
        private System.Timers.Timer? _timer;
        private WeakReference<object> _owner;

        private TimeSpan _start;
        private TimeSpan _end;
        private TimeSpan _current;
        private TimeSpan _duration;
        private int _direction; // +1 up, -1 down
        private double _timeScale;
        private bool _loop;
        private bool _autoTick;
        private TimeSpan _tickInterval;
        private readonly SynchronizationContext? _eventContext;

        private TimerState _state = TimerState.Idle;

        public object? Owner => _owner != null && _owner.TryGetTarget(out var o) ? o : null;
        public TimerMode Mode { get; private set; }
        public TimerState State => _state;


        /// <summary>Su anki zaman degeri.</summary>
        public TimeSpan Current => _current;

        /// <summary>Toplam Sure (|end-start|).</summary>
        public TimeSpan Duration => _duration;

        /// <summary>Baslangictan beri gecen zaman (moda gore anlam degismez).</summary>
        public TimeSpan Elapsed => _direction > 0 ? _current - _start : _start - _current;

        /// <summary>Hedefe kalan sure.</summary>
        public TimeSpan Remaning => Duration - Elapsed;

        /// <summary>Ilerleme 0..1 (Duration>0 ise).</summary>
        public double Progress => Duration.TotalSeconds > 0
            ? Math.Clamp(Elapsed.TotalSeconds / Duration.TotalSeconds, 0.0, 1.0)
            : 1.0f;

        public bool IsRunning => _state == TimerState.Running;

        #region Events
        public event EventHandler<TimerEventArgs>? Started;
        public event EventHandler<TimerEventArgs>? Paused;
        public event EventHandler<TimerEventArgs>? Resumed;
        public event EventHandler<TimerEventArgs>? Tick;
        public event EventHandler<TimerEventArgs>? Completed;
        public event EventHandler<TimerEventArgs>? ResetEvent;
        public event EventHandler<TimerEventArgs>? Canceled;
        #endregion

        #region Constructors & factories

        public SmartTimer(TimerOptions options, object? owner = null)
        {
            if (options.TimeScale <= 0) throw new ArgumentOutOfRangeException(nameof(options.TimeScale), "TimeScale > 0 olmali.");

            _timeScale = options.TimeScale;
            _loop = options.Loop;
            _autoTick = options.AutoTick;
            _tickInterval = options.TickInterval <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(TimerOptions.ONE_SECOND) : options.TickInterval;
            _eventContext = options.EventContext ?? SynchronizationContext.Current;

            if (owner != null)
                _owner = new WeakReference<object>(owner);

            // otomatik mod/baslangic/bitis hesaplama
            ComputeEndPoints(options, out _start, out _end, out var mode);
            Mode = options.Mode ?? mode;
            _direction = Mode == TimerMode.CountUp ? +1 : -1;
            _duration = Abs(_end - _start);
            _current = _start;

            if (_autoTick)
            {
                _timer = new System.Timers.Timer(_tickInterval.TotalMilliseconds)
                {
                    AutoReset = true,
                    Enabled = false
                };
                _timer.Elapsed += OnTimerElapsed;
            }

            if (options.PlayOnStart)
            {
                Start();
            }
        }

        public SmartTimer(TimeSpan start, TimeSpan end, bool playOnStart = false,
                        TimeSpan? tickInterval = null, bool loop = false, double timeScale = 1.0,
                        bool autoTick = true, SynchronizationContext? eventContext = null, object? owner = null)
        : this(new TimerOptions
        {
            StartTime = start,
            EndTime = end,
            PlayOnStart = playOnStart,
            TickInterval = tickInterval ?? TimeSpan.FromMilliseconds(TimerOptions.ONE_SECOND),
            Loop = loop,
            TimeScale = timeScale,
            AutoTick = autoTick,
            EventContext = eventContext
        }, owner)
        { }

        /// <summary>CountDown: duration > 0</summary>
        public static SmartTimer CreateCountDown(TimeSpan duration, bool playOnStart = false,
                                                TimeSpan? tickInterval = null, bool loop = false,
                                                double timeScale = 1.0, bool autoTick = true,
                                                SynchronizationContext? eventContext = null,
                                                object? owner = null)
            => new SmartTimer(new TimerOptions
            {
                Duration = duration,
                Mode = TimerMode.CountDown,
                PlayOnStart = playOnStart,
                TickInterval = tickInterval ?? TimeSpan.FromMilliseconds(TimerOptions.ONE_SECOND),
                Loop = loop,
                TimeScale = timeScale,
                AutoTick = autoTick,
                EventContext = eventContext
            }, owner);


        /// <summary>CountUp: 0 > duration</summary>
        public static SmartTimer CreateCountUp(TimeSpan duration, bool playOnStart = false,
                                                TimeSpan? tickInterval = null, bool loop = false,
                                                double timeScale = 1.0, bool autoTick = true,
                                                SynchronizationContext? eventContext = null,
                                                object? owner = null)
            => new SmartTimer(new TimerOptions
            {
                Duration = duration,
                Mode = TimerMode.CountDown,
                PlayOnStart = playOnStart,
                TickInterval = tickInterval ?? TimeSpan.FromMilliseconds(TimerOptions.ONE_SECOND),
                Loop = loop,
                TimeScale = timeScale,
                AutoTick = autoTick,
                EventContext = eventContext
            }, owner);
        #endregion

        #region Public API (control)

        /// <summary>
        /// Timer'i baslatir. (Idle/Completed/Canceled/Paused durumlarindan)
        /// </summary>
        public SmartTimer Start()
        {
            switch (_state)
            {
                case TimerState.Running:
                    return this;
                case TimerState.Paused:
                    return Resume();
                default: _current = _start;
                    _state = TimerState.Running;
                    _stopWatch.Restart();
                    SetAutoTimerEnabled(true);
                    Raise(Started);
                    return this;
            }
        }

        /// <summary>O anki degerde duraklat.</summary>
        public SmartTimer Pause()
        {
            if (_state != TimerState.Running)
                return this;

            SyncStopwatchIntoCurrent();
            _state = TimerState.Paused;
            _stopWatch.Stop();
            SetAutoTimerEnabled(false);
            Raise(Paused);
            return this;
        }

        /// <summary>Duraklatilmis timer'i devam ettir.</summary>
        public SmartTimer Resume()
        {
            if (_state != TimerState.Paused) 
                return this;

            _state = TimerState.Running;
            _stopWatch.Restart();
            SetAutoTimerEnabled(true);
            Raise(Resumed);
            return this;
        }

        /// <summary>Bastan baslayarak tekrar baslat.</summary>
        public SmartTimer Restart()
        {
            _current = _start;
            _state = TimerState.Running;
            _stopWatch.Restart();
            SetAutoTimerEnabled(true);
            Raise(ResetEvent);
            Raise(Started);
            return this;
        }

        /// <summary>Basa sar, Idle'a al</summary>
        public SmartTimer Reset()
        {
            _current = _start;
            _state = TimerState.Idle;
            _stopWatch.Reset();
            SetAutoTimerEnabled(false);
            Raise(ResetEvent);
            return this;
        }

        /// <summary>Iptal et, ilerlemeyi koru.</summary>
        public SmartTimer Cancel()
        {
            if (_state == TimerState.Canceled)
                return this;

            SyncStopwatchIntoCurrent();
            _state = TimerState.Canceled;
            _stopWatch.Reset();
            SetAutoTimerEnabled(false);
            Raise(Canceled);
            return this;
        }

        /// <summary>Manuel surmek icin: belirtilen dt kadakr ilerlet.</summary>
        public void Advance(TimeSpan dt)
        {
            if (_state != TimerState.Running)
                return;
            if (dt <= TimeSpan.Zero)
                return;

            var scaled = TimeSpan.FromTicks((long)(dt.Ticks * _timeScale));
            Step(scaled);
        }

        /// <summary>Su anki zamani ayarla.</summary>
        public SmartTimer SetTime(TimeSpan t, bool clamp = true)
        {
            _current = clamp ? ClampToRange(t) : t;
            PumpTick(isLooping: false);
            return this;
        }

        public SmartTimer BindOwner(object owner)
        {
            _owner = new WeakReference<object>(owner);
            return this;
        }

        public SmartTimer SetTimeScale(double scale)
        {
            if (scale <= 0) 
                throw new ArgumentOutOfRangeException(nameof(scale));

            _timeScale = scale;
            return this;
        }

        public SmartTimer EnableLoop(bool loop)
        {
            _loop = loop;
            return this;
        }

        #endregion Public API (control) END


        #region Fluent event binding

        public SmartTimer OnStarted(Action<SmartTimer, TimerEventArgs> handler) { Started += Wrap(handler); return this; }
        public SmartTimer OnPaused(Action<SmartTimer, TimerEventArgs> handler) { Paused += Wrap(handler); return this; }
        public SmartTimer OnResumed(Action<SmartTimer, TimerEventArgs> handler) { Resumed += Wrap(handler); return this; }
        public SmartTimer OnTick(Action<SmartTimer, TimerEventArgs> handler) { Tick += Wrap(handler); return this; }
        public SmartTimer OnCompleted(Action<SmartTimer, TimerEventArgs> handler) { Completed += Wrap(handler); return this; }
        public SmartTimer OnReset(Action<SmartTimer, TimerEventArgs> handler) { ResetEvent += Wrap(handler); return this; }
        public SmartTimer OnCanceled(Action<SmartTimer, TimerEventArgs> handler) { Canceled += Wrap(handler); return this; }

        private static EventHandler<TimerEventArgs> Wrap(Action<SmartTimer, TimerEventArgs> a)
            => (s, e) => a((SmartTimer)s!, e);

        #endregion Fluent event binding END



        #region Internals

        private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_state != TimerState.Running)
                return;

            // Stopwatch'tan dt al
            var dt = _stopWatch.Elapsed;
            if (dt <= TimeSpan.Zero)
                return;

            _stopWatch.Restart();
            var scaled = TimeSpan.FromTicks((long)(dt.Ticks * _timeScale));
            Step(scaled);
        }

        private void Step(TimeSpan delta)
        {
            var before = _current;

            // Yonlu ilerleme
            var next = _direction > 0 ? _current + delta : _current - delta;

            bool reached = _direction > 0 ? next >= _end : next <= _end;

            if (reached)
            {
                // Tam hedefo gor
                next = _end;
                _current = next;
                PumpTick(isLooping: false);
                Raise(Completed);

                if (_loop && _duration > TimeSpan.Zero)
                {
                    // basa sarip fazlasini devret
                    var overflow = _direction > 0 ? (before - delta) - _end : _end - (before - delta);
                    // overflow pozitife donustur
                    var wrapped = overflow;
                    while (wrapped > TimeSpan.Zero)
                    {
                        _current = _start;
                        PumpTick(isLooping: true);
                        var take = TimeSpan.FromTicks(Math.Min(wrapped.Ticks, _duration.Ticks));
                        _current = _direction > 0 ? _start + take : _start - take;
                        PumpTick(isLooping: true);
                        wrapped -= take;

                        if ((_direction > 0 && _current >= _end) || (_direction < 0 && _current <= _end))
                        {
                            Raise(Completed);
                        }
                    }

                    // Kosuya Devam:
                    _state = TimerState.Running;
                }
                else
                {
                    // Bitti, Dur!
                    _state = TimerState.Completed;
                    _stopWatch.Reset();
                    SetAutoTimerEnabled(false);
                }
            }
            else
            {
                _current = next;
                PumpTick(isLooping: false);
            }
        }

        private void PumpTick(bool isLooping)
        {
            Raise(Tick, isLooping);
        }

        private void Raise(EventHandler<TimerEventArgs>? evt, bool isLooping = false)
        {
            if (evt is null)
                return;

            var args = new TimerEventArgs
            {
                Current = _current,
                Elapsed = Elapsed,
                Remaning = Remaning,
                Progress = Progress,
                Owner = Owner,
                State = State,
                IsLooping = isLooping
            };

            if (_eventContext != null)
            {
                _eventContext.Post(_ => evt?.Invoke(this, args), null);
            }
            else
            {
                evt.Invoke(this, args);
            }
        }

        private void SetAutoTimerEnabled(bool enabled)
        {
            if (_timer == null)
                return;

            _timer.Enabled = enabled;
        }

        private void SyncStopwatchIntoCurrent()
        {
            if (!_stopWatch.IsRunning) return;
            var dt = _stopWatch.Elapsed;
            _stopWatch.Restart();
            var scaled = TimeSpan.FromTicks((long)(dt.Ticks * _timeScale));
            Step(scaled);
        }

        private static void ComputeEndPoints(TimerOptions options, out TimeSpan start, out TimeSpan end, out TimerMode mode)
        {
            // Oncelik: Mode verilmisse onu uygula
            if (options.Mode.HasValue)
            {
                if (options.Mode == TimerMode.CountUp)
                {
                    if (options.StartTime is null && options.EndTime is null && options.Duration is { } d1)
                    {
                        start = TimeSpan.Zero;
                        end = d1;
                    }
                    else
                    {
                        start = options.StartTime ?? TimeSpan.Zero;
                        if (options.EndTime is { } e) end = e;
                        else if (options.Duration is { } d) end = start + d;
                        else end = start; // 0 sure
                    }
                    mode = TimerMode.CountUp;
                    return;
                }
                else
                {
                    // Count Down
                    if (options.StartTime is null && options.EndTime is null && options.Duration is { } d2)
                    {
                        start = d2;
                        end = TimeSpan.Zero;
                    }
                    else
                    {
                        end = options.EndTime ?? TimeSpan.Zero;
                        if (options.StartTime is { } s) start = s;
                        else if (options.Duration is { } d) start = end + d;
                        else start = end; // sure 0
                    }
                    mode = TimerMode.CountDown;
                    return;
                }
            }

            // Mod belirtilmediyse, start/end'e bakarak cikar.
            var s0 = options.StartTime ?? TimeSpan.Zero;

            if (options.EndTime is { } e0)
            {
                start = s0;
                end = e0;
                mode = e0 >= s0 ? TimerMode.CountUp : TimerMode.CountDown;
                return;
            }

            if (options.Duration is { } d0)
            {
                // duration varsa ve end yoksa: eger start 0'sa Count Up, degilse Count Down (start -> 0) kabul edilir.
                if (s0 == TimeSpan.Zero)
                {
                    start = TimeSpan.Zero;
                    end = d0;
                    mode = TimerMode.CountUp;
                }
                else
                {
                    start = s0;
                    end = TimeSpan.Zero;
                    mode = TimerMode.CountDown;
                }
                return;
            }

            // Hic biri yoksa 0 -> 0 (tamamlanmis kabul edilir.
            start = TimeSpan.Zero;
            end = TimeSpan.Zero;
            mode = TimerMode.CountUp;
        }

        private static TimeSpan Abs(TimeSpan t) => t >= TimeSpan.Zero ? t : -t;

        private TimeSpan ClampToRange(TimeSpan t)
        {
            if (_direction > 0)
            {
                if (t < _start) return _start;
                if (t > _end) return _end;
            }
            else
            {
                if (t > _start) return _start;
                if (t < _end) return _end;
            }
            return t;
        }


        public void Dispose()
        {
            _timer?.Dispose();
        }

        #endregion Internals END
    }
}