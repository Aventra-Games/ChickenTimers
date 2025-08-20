using System;
using System.Threading;
using Aventra.Nugget.ChickenTimers;

namespace Aventra.Nugget.ChickenTimers
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Saniye cinsinden süreyi gir: ");
            if (!int.TryParse(Console.ReadLine(), out int seconds))
            {
                Console.WriteLine("Geçersiz sayı girdin!");
                return;
            }

            // Timer Options ayarla
            var options = new TimerOptions
            {
                Duration = TimeSpan.FromSeconds(seconds),
                Mode = TimerMode.CountDown
            };

            // Timer Olustur
            var timer = new SmartTimer(options);

            // Event'lere abone ol
            timer.OnTick((t, e) =>
            {
                //Console.Clear();
                //Console.WriteLine($"Kalan süre: {timer.Remaning}");
                PrintTime(timer.Remaning);
            }).OnCompleted((t, e) =>
            {
                Console.WriteLine("Süre Doldu!");
            });

            // Timer calistir
            timer.Start();

            Console.WriteLine("Timer başladı!");
            Console.WriteLine("p = pause, r = resume, q = çıkış");

            // Kullanicidan kontrol tuslarini al
            while(true)
            {
                var key = Console.ReadKey(true).Key;

                if (key == ConsoleKey.P)
                {
                    timer.Pause();
                    Console.WriteLine("⏸ Timer duraklatıldı.");
                }
                else if (key == ConsoleKey.R)
                {
                    timer.Resume();
                    Console.WriteLine("▶ Timer devam ediyor.");
                }
                else if (key == ConsoleKey.Q)
                {
                    timer.Cancel();
                    Console.WriteLine("🛑 Timer iptal edildi.");
                    break;
                }

                Thread.Sleep(100); // CPU'yu boş yere yorma.
            }


        }

        static void PrintTime(TimeSpan remaningTime)
        {
            Console.SetCursorPosition(0, 10); // 2. Satira git (0'dan baslar

            Console.Write(new string(' ', Console.WindowWidth));

            // Tekrar basa don
            Console.SetCursorPosition(0, 10);

            // Guncel süreyi yaz
            Console.Write($"Kalan Süre: {(int)remaningTime.TotalSeconds}");

        }
    }
}