# 🐔 Chicken Timer

Chicken Timer is a flexible, developer-friendly timer framework for .NET that provides high-level abstractions for creating, controlling, and customizing timers. Whether you need simple countdowns, repeating intervals, or advanced timer states with pause/resume functionality, Chicken Timer is designed to make it easy.

## ✨ Features
- ⏱️ CountUp & CountDown modes
- 🎛️ Flexible configuration using TimerOptions
- 🛑 Pause, Resume, Cancel support
- 🔔 Event-driven API (OnTick, OnCompleted, OnCanceled)
- 🚦 Multiple states (Idle, Running, Paused, Completed, Canceled)
- ⚡ Thread-safe and efficient
- 🔧 Extensible design – build your own timer logic on top

---
## 🚀 Usage
### Example: Countdown Timer with Pause/Resume
```cs
using ProTimers;
using System;

class Program
{
    static void Main()
    {
        var timer = new SmartTimer(new TimerOptions
        {
            Duration = TimeSpan.FromSeconds(10), // 10 second countdown
        });

        timer.OnTick += (sender, elapsed) =>
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write($"Time Left: {timer.RemainingTime.TotalSeconds:F1}s ");
        };

        timer.OnCompleted += (sender, _) =>
        {
            Console.WriteLine("\nTimer Completed ✅");
        };

        timer.Start();

        while (true)
        {
            var key = Console.ReadKey(true).Key;
            if (key == ConsoleKey.P) timer.Pause();
            if (key == ConsoleKey.R) timer.Resume();
            if (key == ConsoleKey.C) timer.Cancel();
        }
    }
}
```
---
### 🔍 Timer Modes
- **Count Up** -> starts from 0 and counts upward until a given duration
- **Count Down** -> starts from duration counts downward until 0.
  Mode is automatically inferred from `TimerOptions`.

### 🏗️ Architecture
- **`TimerOptions`** -> Configuration for timer creation
- **`SmartTimer`** -> High-level timer implementation
- **`TimerMode`** -> Enum for `CountUp` or `CountDown`
- **`TimerState`** -> Enum for current state of timer
---
### 📜 License
MIT License – Free for personal and commercial use.
























  
