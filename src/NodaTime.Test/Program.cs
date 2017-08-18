// Copyright 2017 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.
using NodaTime.Extensions;
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
            var zones = DateTimeZoneProviders.Tzdb.GetAllZones()
                .Select(zone => zone is CachedDateTimeZone cached ? cached.TimeZone : zone)
                .ToList();
            var start = Instant.FromUtc(2000, 1, 1, 0, 0);
            var gap = Duration.FromDays(1);
            var instants = Enumerable
                .Range(0, 50000)
                .Select(index => start + gap * index)
                .ToList();
            
            var stopwatch = Stopwatch.StartNew();
            foreach (var zone in zones)
            {
                foreach (var instant in instants)
                {
                    zone.GetUtcOffset(instant);
                }
            }
            stopwatch.Stop();
            Console.WriteLine($"Elapsed time: {stopwatch.Elapsed}");
        }
    }
}
