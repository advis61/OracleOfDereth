using System;
using System.Collections.Generic;
using System.Globalization;

namespace OracleOfDereth
{
    // Independent of Decal and the UI. Feed cumulative earned counters, or positive
    // award amounts via AddGain; balance-based currencies need their own source adapter
    // so spending, banking and transfers are not mistaken for income.
    public sealed class IncomeMetric
    {
        private readonly long?[] samples = new long?[6];
        private int position;
        private long? previous;
        public string Name { get; }
        public long Total { get; private set; }
        public IncomeMetric(string name) { Name = name; }

        public void ObserveCounter(long value)
        {
            if (previous.HasValue && value > previous.Value) AddGain(value - previous.Value);
            previous = value; // A counter reset establishes a new baseline.
        }

        public void AddGain(long amount)
        {
            if (amount > 0) Total = checked(Total + amount);
        }

        internal void Reset()
        {
            Total = 0;
            previous = null;
            ClearHistory();
        }

        public void ClearHistory()
        {
            Array.Clear(samples, 0, samples.Length);
            position = 0;
            samples[0] = Total;
        }

        internal void SampleMinute()
        {
            position = (position + 1) % samples.Length;
            samples[position] = Total;
        }

        public double? MinutePerHour => Rate(1, 60);
        public double? FiveMinutePerHour => Rate(5, 12);
        private double? Rate(int minutes, int multiplier)
        {
            long? baseline = samples[(position + samples.Length - minutes) % samples.Length];
            return baseline.HasValue ? (double)(samples[position].Value - baseline.Value) * multiplier : (double?)null;
        }
        public double SessionPerHour(TimeSpan elapsed) => elapsed.TotalHours > 0 ? Total / elapsed.TotalHours : 0;

        public string Report(TimeSpan elapsed)
        {
            string duration = string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}",
                (long)elapsed.TotalHours, elapsed.Minutes, elapsed.Seconds);
            bool pet = Name == "Kills (pet)";
            bool kills = pet || Name == "Kills";
            string unit = kills ? "kills" : Name == "Experience" ? "XP" : Name;
            string earned = kills
                ? "You've killed " + Total.ToString("N0", CultureInfo.InvariantCulture) + " creatures" + (pet ? " with your pet" : "")
                : "You've made " + Total.ToString("N0", CultureInfo.InvariantCulture) + " " + unit;
            string report = earned + " in " + duration + " for "
                + SessionPerHour(elapsed).ToString("N0", CultureInfo.InvariantCulture) + " " + unit + "/hour";
            if (FiveMinutePerHour.HasValue)
                report += " (5min " + FiveMinutePerHour.Value.ToString("N0", CultureInfo.InvariantCulture) + " " + unit + "/hour)";
            return report + ".";
        }
    }

    public sealed class IncomeSession
    {
        private readonly List<IncomeMetric> metrics = new List<IncomeMetric>();
        public IReadOnlyList<IncomeMetric> Metrics => metrics;
        public DateTime StartedAt { get; private set; }
        public bool Started { get; private set; }
        public IncomeMetric Register(string name)
        {
            var metric = new IncomeMetric(name);
            metric.Reset();
            metrics.Add(metric);
            return metric;
        }
        public void Reset(DateTime now)
        {
            StartedAt = now;
            Started = true;
            foreach (var metric in metrics) metric.Reset();
        }
        public TimeSpan Elapsed(DateTime now) => Started && now > StartedAt ? now - StartedAt : TimeSpan.Zero;
        public void SampleMinute() { foreach (var metric in metrics) metric.SampleMinute(); }
    }
}
