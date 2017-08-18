// Copyright 2012 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NodaTime.TimeZones;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NodaTime.Test.TimeZones
{
    public class TzdbDateTimeZoneSourceTest
    {
        private static readonly List<TimeZoneInfo> SystemTimeZones = TimeZoneInfo.GetSystemTimeZones().ToList();

        // We should be able to use TestCaseSource to call TimeZoneInfo.GetSystemTimeZones directly,
        // but that appears to fail under Mono.
        [Test]
        [TestCaseSource(nameof(SystemTimeZones))]
        public void GuessZoneIdByTransitionsUncached(TimeZoneInfo bclZone)
        {
            // As of March 6th 2017, the Windows time zone database hasn't caught up to
            // 2017a which reflected the change that Mongolia no longer observes DST.
            if (bclZone.Id == "Ulaanbaatar Standard Time" || bclZone.Id == "W. Mongolia Standard Time")
            {
                return;
            }

            string id = TzdbDateTimeZoneSource.Default.GuessZoneIdByTransitionsUncached(bclZone);

            // Unmappable zones may not be mapped, or may be mapped to something reasonably accurate.
            // We don't mind either way.
            if (!TzdbDateTimeZoneSource.Default.WindowsMapping.PrimaryMapping.ContainsKey(bclZone.Id))
            {
                return;
            }

            Assert.IsNotNull(id, $"Unable to guess time zone for {bclZone.Id}");
            var tzdbZone = TzdbDateTimeZoneSource.Default.ForId(id);

            var thisYear = SystemClock.Instance.GetCurrentInstant().InUtc().Year;
            LocalDate? lastIncorrectDate = null;
            Offset? lastIncorrectBclOffset = null;
            Offset? lastIncorrectTzdbOffset = null;

            int total = 0;
            int correct = 0;
            // From the start of this year to the end of next year, we should have an 80% hit rate or better.
            // That's stronger than the 70% we limit to in the code, because if it starts going between 70% and 80% we
            // should have another look at the algorithm. (And this is dealing with 80% of days, not 80% of transitions,
            // so it's not quite equivalent anyway.)
            for (var date = new LocalDate(thisYear, 1, 1); date.Year < thisYear + 2; date = date.PlusDays(1))
            {
                Instant startOfUtcDay = date.AtMidnight().InUtc().ToInstant();
                Offset tzdbOffset = tzdbZone.GetUtcOffset(startOfUtcDay);
                Offset bclOffset = Offset.FromTimeSpan(bclZone.GetUtcOffset(startOfUtcDay.ToDateTimeOffset()));
                if (tzdbOffset == bclOffset)
                {
                    correct++;
                }
                else
                {
                    // Useful for debugging (by having somewhere to put a breakpoint) as well as for the message.
                    lastIncorrectDate = date;
                    lastIncorrectBclOffset = bclOffset;
                    lastIncorrectTzdbOffset = tzdbOffset;
                }
                total++;
            }
            Assert.That(correct * 100.0 / total, Is.GreaterThanOrEqualTo(75.0),
                "Last incorrect date for {0} vs {1}: {2} (BCL: {3}; TZDB: {4})",
                bclZone.Id,
                id,
                lastIncorrectDate, lastIncorrectBclOffset, lastIncorrectTzdbOffset);
        }
    }
}
