// Copyright 2011 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NodaTime.Properties;
using NodaTime.Text;
using NUnit.Framework;
using NodaTime.Test.Calendars;

namespace NodaTime.Test.Text
{
    public class LocalDatePatternTest : PatternTestBase<LocalDate>
    {
        private static readonly LocalDate SampleLocalDate = new LocalDate(1976, 6, 19);

        internal static readonly Data[] InvalidPatternData = {
            new Data { Pattern = "!", Message = Messages.Parse_UnknownStandardFormat, Parameters = {'!', typeof(LocalDate).FullName }},
            new Data { Pattern = "%", Message = Messages.Parse_UnknownStandardFormat, Parameters = { '%', typeof(LocalDate).FullName } },
            new Data { Pattern = "\\", Message = Messages.Parse_UnknownStandardFormat, Parameters = { '\\', typeof(LocalDate).FullName } },
            new Data { Pattern = "%%", Message = Messages.Parse_PercentDoubled },
            new Data { Pattern = "%\\", Message = Messages.Parse_EscapeAtEndOfString },
            new Data { Pattern = "MMMMM", Message = Messages.Parse_RepeatCountExceeded, Parameters = { 'M', 4 } },
            new Data { Pattern = "ddddd", Message = Messages.Parse_RepeatCountExceeded, Parameters = { 'd', 4 } },
            new Data { Pattern = "M%", Message = Messages.Parse_PercentAtEndOfString },
            new Data { Pattern = "yyyyy", Message = Messages.Parse_RepeatCountExceeded, Parameters = { 'y', 4 } },
            new Data { Pattern = "uuuuu", Message = Messages.Parse_RepeatCountExceeded, Parameters = { 'u', 4 } },
            new Data { Pattern = "ggg", Message = Messages.Parse_RepeatCountExceeded, Parameters = { 'g', 2 } },
            new Data { Pattern = "'qwe", Message = Messages.Parse_MissingEndQuote, Parameters = { '\'' } },
            new Data { Pattern = "'qwe\\", Message = Messages.Parse_EscapeAtEndOfString },
            new Data { Pattern = "'qwe\\'", Message = Messages.Parse_MissingEndQuote, Parameters = { '\'' } },
            // Note incorrect use of "u" (year) instead of "y" (year of era)
            new Data { Pattern = "dd MM uuuu gg", Message = Messages.Parse_EraWithoutYearOfEra },
            // Era specifier and calendar specifier in the same pattern.
            new Data { Pattern = "dd MM yyyy gg c", Message = Messages.Parse_CalendarAndEra },

            // Invalid patterns directly after the yyyy specifier. This will detect the issue early, but then
            // continue and reject it in the normal path.
            new Data { Pattern = "yyyy'", Message = Messages.Parse_MissingEndQuote, Parameters = { '\'' } },
            new Data { Pattern = "yyyy\\", Message = Messages.Parse_EscapeAtEndOfString },

            // Common typo, which is caught in 2.0...
            new Data { Pattern = "yyyy-mm-dd", Message = Messages.Parse_UnquotedLiteral, Parameters = { 'm' } },
            // T isn't valid in a date pattern
            new Data { Pattern = "yyyy-MM-ddT00:00:00", Message = Messages.Parse_UnquotedLiteral, Parameters = { 'T' } },

            // These became invalid in v2.0, when we decided that y and yyy weren't sensible.
            new Data { Pattern = "y M d", Message = Messages.Parse_InvalidRepeatCount, Parameters = { 'y', 1 } },
            new Data { Pattern = "yyy M d", Message = Messages.Parse_InvalidRepeatCount, Parameters = { 'y', 3 } },
        };

        internal static Data[] ParseFailureData = {
            new Data { Pattern = "yyyy gg", Text = "2011 NodaEra", Message = Messages.Parse_MismatchedText, Parameters = {'g'} },
            new Data { Pattern = "yyyy uuuu gg", Text = "0010 0009 B.C.", Message = Messages.Parse_InconsistentValues2, Parameters = {'g', 'u', typeof(LocalDate)} },
            new Data { Pattern = "yyyy MM dd dddd", Text = "2011 10 09 Saturday", Message = Messages.Parse_InconsistentDayOfWeekTextValue },
            new Data { Pattern = "yyyy MM dd ddd", Text = "2011 10 09 Sat", Message = Messages.Parse_InconsistentDayOfWeekTextValue },
            new Data { Pattern = "yyyy MM dd ddd", Text = "2011 10 09 FooBar", Message = Messages.Parse_MismatchedText, Parameters = {'d'} },
            new Data { Pattern = "yyyy MM dd dddd", Text = "2011 10 09 FooBar", Message = Messages.Parse_MismatchedText, Parameters = {'d'} },
            new Data { Pattern = "yyyy/MM/dd", Text = "2011/02-29", Message = Messages.Parse_DateSeparatorMismatch },
            // Don't match a short name against a long pattern
            new Data { Pattern = "yyyy MMMM dd", Text = "2011 Oct 09", Message = Messages.Parse_MismatchedText, Parameters = {'M'} },
            // Or vice versa... although this time we match the "Oct" and then fail as we're expecting a space
            new Data { Pattern = "yyyy MMM dd", Text = "2011 October 09", Message = Messages.Parse_MismatchedCharacter, Parameters = {' '}},

            // Invalid year, year-of-era, month, day
            new Data { Pattern = "yyyy MM dd", Text = "0000 01 01", Message = Messages.Parse_FieldValueOutOfRange, Parameters = { 0, 'y', typeof(LocalDate) } },
            new Data { Pattern = "yyyy MM dd", Text = "2011 15 29", Message = Messages.Parse_MonthOutOfRange, Parameters = { 15, 2011 } },
            new Data { Pattern = "yyyy MM dd", Text = "2011 02 35", Message = Messages.Parse_DayOfMonthOutOfRange, Parameters = { 35, 2, 2011 } },
            // Year of era can't be negative...
            new Data { Pattern = "yyyy MM dd", Text = "-15 01 01", Message = Messages.Parse_UnexpectedNegative },

            // Invalid leap years
            new Data { Pattern = "yyyy MM dd", Text = "2011 02 29", Message = Messages.Parse_DayOfMonthOutOfRange, Parameters = { 29, 2, 2011 } },
            new Data { Pattern = "yyyy MM dd", Text = "1900 02 29", Message = Messages.Parse_DayOfMonthOutOfRange, Parameters = { 29, 2, 1900 } },

            // Year of era and two-digit year, but they don't match
            new Data { Pattern = "uuuu yy", Text = "2011 10", Message = Messages.Parse_InconsistentValues2, Parameters = { 'y', 'u', typeof(LocalDate) } },

            // Invalid calendar name
            new Data { Pattern = "c yyyy MM dd", Text = "2015 01 01", Message = Messages.Parse_NoMatchingCalendarSystem },

            // Invalid year
            new Data { Template = new LocalDate(1, 1, 1, CalendarSystem.IslamicBcl), Pattern = "uuuu", Text = "9999", Message = Messages.Parse_FieldValueOutOfRange, Parameters = { 9999, 'u', typeof(LocalDate) } },
            new Data { Template = new LocalDate(1, 1, 1, CalendarSystem.IslamicBcl), Pattern = "yyyy", Text = "9999", Message = Messages.Parse_YearOfEraOutOfRange, Parameters = { 9999, "EH", "Hijri" } },

            // https://github.com/nodatime/nodatime/issues/414
            new Data { Pattern = "yyyy-MM-dd", Text = "1984-00-15", Message = Messages.Parse_FieldValueOutOfRange, Parameters = { 0, 'M', typeof(LocalDate) } },
            new Data { Pattern = "M/d/yyyy", Text = "00/15/1984", Message = Messages.Parse_FieldValueOutOfRange, Parameters = { 0, 'M', typeof(LocalDate) } },

            // Calendar ID parsing is now ordinal, case-sensitive
            new Data(2011, 10, 9) { Pattern = "yyyy MM dd c", Text = "2011 10 09 iso", Message = Messages.Parse_NoMatchingCalendarSystem },
        };

        internal static Data[] ParseOnlyData = {
            // Alternative era names
            new Data(0, 10, 3) { Pattern = "yyyy MM dd gg", Text = "0001 10 03 BCE" },

            // Valid leap years
            new Data(2000, 2, 29) { Pattern = "yyyy MM dd", Text = "2000 02 29" },
            new Data(2004, 2, 29) { Pattern = "yyyy MM dd", Text = "2004 02 29" },

            // Month parsing should be case-insensitive
            new Data(2011, 10, 3) { Pattern = "yyyy MMM dd", Text = "2011 OcT 03" },
            new Data(2011, 10, 3) { Pattern = "yyyy MMMM dd", Text = "2011 OcToBeR 03" },
            // Day-of-week parsing should be case-insensitive
            new Data(2011, 10, 9) { Pattern = "yyyy MM dd ddd", Text = "2011 10 09 sUN" },
            new Data(2011, 10, 9) { Pattern = "yyyy MM dd dddd", Text = "2011 10 09 SuNDaY" },

            // Genitive name is an extension of the non-genitive name; parse longer first.
            new Data(2011, 1, 10) { Pattern = "yyyy MMMM dd", Text = "2011 MonthName-Genitive 10", Culture = Cultures.GenitiveNameTestCultureWithLeadingNames },
            new Data(2011, 1, 10) { Pattern = "yyyy MMMM dd", Text = "2011 MonthName 10", Culture = Cultures.GenitiveNameTestCultureWithLeadingNames },
            new Data(2011, 1, 10) { Pattern = "yyyy MMM dd", Text = "2011 MN-Gen 10", Culture = Cultures.GenitiveNameTestCultureWithLeadingNames },
            new Data(2011, 1, 10) { Pattern = "yyyy MMM dd", Text = "2011 MN 10", Culture = Cultures.GenitiveNameTestCultureWithLeadingNames },
        };

        internal static Data[] FormatOnlyData = {
            // Would parse back to 2011
            new Data(1811, 7, 3) { Pattern = "yy M d", Text = "11 7 3" },
            // Tests for the documented 2-digit formatting of BC years
            // (Less of an issue since yy became "year of era")
            new Data(-94, 7, 3) { Pattern = "yy M d", Text = "95 7 3" },
            new Data(-93, 7, 3) { Pattern = "yy M d", Text = "94 7 3" },
        };

        internal static Data[] FormatAndParseData = {
            // Standard patterns
            // Invariant culture uses the crazy MM/dd/yyyy format. Blech.
            new Data(2011, 10, 20) { Pattern = "d", Text = "10/20/2011" },
            new Data(2011, 10, 20) { Pattern = "D", Text = "Thursday, 20 October 2011" },

            // Custom patterns
            new Data(2011, 10, 3) { Pattern = "yyyy/MM/dd", Text = "2011/10/03" },
            new Data(2011, 10, 3) { Pattern = "yyyy/MM/dd", Text = "2011-10-03", Culture = Cultures.FrCa },
            new Data(2011, 10, 3) { Pattern = "yyyyMMdd", Text = "20111003" },
            new Data(2001, 7, 3) { Pattern = "yy M d", Text = "01 7 3" },
            new Data(2011, 7, 3) { Pattern = "yy M d", Text = "11 7 3" },
            new Data(2030, 7, 3) { Pattern = "yy M d", Text = "30 7 3" },
            // Cutoff defaults to 30 (at the moment...)
            new Data(1931, 7, 3) { Pattern = "yy M d", Text = "31 7 3" },
            new Data(1976, 7, 3) { Pattern = "yy M d", Text = "76 7 3" },

            // In the first century, we don't skip back a century for "high" two-digit year numbers.
            new Data(25, 7, 3) { Pattern = "yy M d", Text = "25 7 3", Template = new LocalDate(50, 1, 1) },
            new Data(35, 7, 3) { Pattern = "yy M d", Text = "35 7 3", Template = new LocalDate(50, 1, 1) },

            new Data(2000, 10, 3) { Pattern = "MM/dd", Text = "10/03"},
            new Data(1885, 10, 3) { Pattern = "MM/dd", Text = "10/03", Template = new LocalDate(1885, 10, 3) },

            // When we parse in all of the below tests, we'll use the month and day-of-month if it's provided;
            // the template value is specified to allow simple roundtripping. (Day of week doesn't affect what value is parsed; it just validates.)
            // Non-genitive month name when there's no "day of month", even if there's a "day of week"
            new Data(2011, 1, 3) { Pattern = "MMMM", Text = "FullNonGenName", Culture = Cultures.GenitiveNameTestCulture, Template = new LocalDate(2011, 5, 3)},
            new Data(2011, 1, 3) { Pattern = "MMMM dddd", Text = "FullNonGenName Monday", Culture = Cultures.GenitiveNameTestCulture, Template = new LocalDate(2011, 5, 3) },
            new Data(2011, 1, 3) { Pattern = "MMM", Text = "AbbrNonGenName", Culture = Cultures.GenitiveNameTestCulture, Template = new LocalDate(2011, 5, 3) },
            new Data(2011, 1, 3) { Pattern = "MMM ddd", Text = "AbbrNonGenName Mon", Culture = Cultures.GenitiveNameTestCulture, Template = new LocalDate(2011, 5, 3) },
            // Genitive month name when the pattern includes "day of month"
            new Data(2011, 1, 3) { Pattern = "MMMM dd", Text = "FullGenName 03", Culture = Cultures.GenitiveNameTestCulture, Template = new LocalDate(2011, 5, 3) },
            // TODO: Check whether or not this is actually appropriate
            new Data(2011, 1, 3) { Pattern = "MMM dd", Text = "AbbrGenName 03", Culture = Cultures.GenitiveNameTestCulture, Template = new LocalDate(2011, 5, 3) },

            // Era handling
            new Data(2011, 1, 3) { Pattern = "yyyy MM dd gg", Text = "2011 01 03 A.D." },
            new Data(2011, 1, 3) { Pattern = "uuuu yyyy MM dd gg", Text = "2011 2011 01 03 A.D." },
            new Data(-1, 1, 3) { Pattern = "yyyy MM dd gg", Text = "0002 01 03 B.C." },

            // Day of week handling
            new Data(2011, 10, 9) { Pattern = "yyyy MM dd dddd", Text = "2011 10 09 Sunday" },
            new Data(2011, 10, 9) { Pattern = "yyyy MM dd ddd", Text = "2011 10 09 Sun" },

            // Month handling
            new Data(2011, 10, 9) { Pattern = "yyyy MMMM dd", Text = "2011 October 09" },
            new Data(2011, 10, 9) { Pattern = "yyyy MMM dd", Text = "2011 Oct 09" },

            // Year and two-digit year-of-era in the same format. Note that the year
            // gives the full year information, so we're not stuck in the 20th/21st century
            new Data(1825, 10, 9) { Pattern = "uuuu yy MM/dd", Text = "1825 25 10/09" },

            // Negative years
            new Data(-43, 3, 15) { Pattern = "uuuu MM dd", Text = "-0043 03 15"},

            // Calendar handling
            new Data(2011, 10, 9) { Pattern = "c yyyy MM dd", Text = "ISO 2011 10 09" },
            new Data(2011, 10, 9) { Pattern = "yyyy MM dd c", Text = "2011 10 09 ISO" },
            new Data(2011, 10, 9, CalendarSystem.Coptic) { Pattern = "c uuuu MM dd", Text = "Coptic 2011 10 09" },
            new Data(2011, 10, 9, CalendarSystem.Coptic) { Pattern = "uuuu MM dd c", Text = "2011 10 09 Coptic" },

            // Awkward day-of-week handling
            // December 14th 2012 was a Friday. Friday is "Foo" or "FooBar" in AwkwardDayOfWeekCulture.
            new Data(2012, 12, 14) { Pattern = "ddd yyyy MM dd", Text = "Foo 2012 12 14", Culture = Cultures.AwkwardDayOfWeekCulture },
            new Data(2012, 12, 14) { Pattern = "dddd yyyy MM dd", Text = "FooBar 2012 12 14", Culture = Cultures.AwkwardDayOfWeekCulture },
            // December 13th 2012 was a Thursday. Friday is "FooBaz" or "FooBa" in AwkwardDayOfWeekCulture.
            new Data(2012, 12, 13) { Pattern = "ddd yyyy MM dd", Text = "FooBaz 2012 12 13", Culture = Cultures.AwkwardDayOfWeekCulture },
            new Data(2012, 12, 13) { Pattern = "dddd yyyy MM dd", Text = "FooBa 2012 12 13", Culture = Cultures.AwkwardDayOfWeekCulture },
        };

        internal static IEnumerable<Data> ParseData = ParseOnlyData.Concat(FormatAndParseData);
        internal static IEnumerable<Data> FormatData = FormatOnlyData.Concat(FormatAndParseData);
        
        [Test]
        [TestCaseSource(typeof(Cultures), nameof(Cultures.AllCultures))]
        public void BclLongDatePatternGivesSameResultsInNoda(CultureInfo culture)
        {
            AssertBclNodaEquality(culture, culture.DateTimeFormat.LongDatePattern);
        }

        [Test]
        [TestCaseSource(typeof(Cultures), nameof(Cultures.AllCultures))]
        public void BclShortDatePatternGivesSameResultsInNoda(CultureInfo culture)
        {
            AssertBclNodaEquality(culture, culture.DateTimeFormat.ShortDatePattern);
        }

        [Test]
        public void WithCalendar()
        {
            var pattern = LocalDatePattern.Iso.WithCalendar(CalendarSystem.Coptic);
            var value = pattern.Parse("0284-08-29").Value;
            Assert.AreEqual(new LocalDate(284, 8, 29, CalendarSystem.Coptic), value);
        }

        private void AssertBclNodaEquality(CultureInfo culture, string patternText)
        {
            // The BCL never seems to use abbreviated month genitive names.
            // I think it's reasonable that we do. Hmm.
            // See https://github.com/nodatime/nodatime/issues/377
            if (patternText.Contains("MMM") && !patternText.Contains("MMMM") &&
                culture.DateTimeFormat.AbbreviatedMonthGenitiveNames[SampleLocalDate.Month - 1] != culture.DateTimeFormat.AbbreviatedMonthNames[SampleLocalDate.Month - 1])
            {
                return;
            }

            var pattern = LocalDatePattern.Create(patternText, culture);
            var calendarSystem = BclCalendars.CalendarSystemForCalendar(culture.Calendar);
            if (calendarSystem == null)
            {
                // We can't map this calendar system correctly yet; the test would be invalid.
                return;
            }

            var sampleDateInCalendar = SampleLocalDate.WithCalendar(calendarSystem);
            // To construct a DateTime, we need a time... let's give a non-midnight one to catch
            // any unexpected uses of time within the date patterns.
            DateTime sampleDateTime = (SampleLocalDate + new LocalTime(2, 3, 5)).ToDateTimeUnspecified();
            Assert.AreEqual(sampleDateTime.ToString(patternText, culture), pattern.Format(sampleDateInCalendar));
        }

        public sealed class Data : PatternTestData<LocalDate>
        {
            // Default to the start of the year 2000.
            protected override LocalDate DefaultTemplate => LocalDatePattern.DefaultTemplateValue;

            /// <summary>
            /// Initializes a new instance of the <see cref="Data" /> class.
            /// </summary>
            /// <param name="value">The value.</param>
            public Data(LocalDate value) : base(value)
            {
            }

            public Data(int year, int month, int day) : this(new LocalDate(year, month, day))
            {
            }

            public Data(int year, int month, int day, CalendarSystem calendar)
                : this(new LocalDate(year, month, day, calendar))
            {
            }

            public Data() : this(LocalDatePattern.DefaultTemplateValue)
            {
            }

            internal override IPattern<LocalDate> CreatePattern() =>
                LocalDatePattern.CreateWithInvariantCulture(Pattern)
                    .WithTemplateValue(Template)
                    .WithCulture(Culture);
        }
    }
}
