// Copyright 2017 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.
using NodaTime.TimeZones;
using System;
using System.Diagnostics;
using System.Linq;

namespace NodaTime.Test
{
    class Program
    {
        public static void Main(string[] args)
        {
            var zones = TimeZoneInfo.GetSystemTimeZones().ToList();
            
            // Fetch the source outside the measurement time
            var source = TzdbDateTimeZoneSource.Default;
            // JIT compile before we start
            source.GuessZoneIdByTransitionsUncached(TimeZoneInfo.Local);
            
            var stopwatch = Stopwatch.StartNew();
            foreach (var zone in zones)
            {
                source.GuessZoneIdByTransitionsUncached(zone);
            }
            stopwatch.Stop();
            Console.WriteLine($"Zones tested: {zones.Count}");
            Console.WriteLine($"Elapsed time: {stopwatch.Elapsed}");
        }
    }
}
