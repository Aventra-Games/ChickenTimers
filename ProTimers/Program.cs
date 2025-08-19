using Aventra.Test;

Console.WriteLine("Hello Wrold!");

using var hb = new HeartbeatLogger("hb.log");

// Programin hemen kapanmamasi icin beklet
Console.WriteLine("Press any key to exit...");
Console.ReadKey();