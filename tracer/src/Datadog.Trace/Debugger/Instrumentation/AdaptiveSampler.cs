// <copyright file="AdaptiveSampler.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

[SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1028:Code should not contain trailing whitespace", Justification = "stuff")]
internal class AdaptiveSampler
{
    private readonly double _emaAlpha;
    private readonly int _samplesPerWindow;

    private Counts _countsRef;

    /*volatile*/
    private double _probability = 1;

    /*volatile*/
    private long _samplesBudget;

    private double _totalCountRunningAverage;
    private double _avgSamples;

    private int _budgetLookback;
    private double _budgetAlpha;

    private int _countsSlotIndex = 0;
    private Counts[] _countsSlots;

    private Timer _timer;
    private Action _rollWindowCallback;

    // std::mutex _rngMutex;
    // std::uniform_real_distribution<> _distribution;
    private ThreadLocal<Random> _rng;

    internal AdaptiveSampler(
        TimeSpan windowDuration,
        int samplesPerWindow,
        int averageLookback,
        int budgetLookback,
        Action rollWindowCallback) /*:
    _timer([this] { RollWindow(); }, windowDuration),
    _totalCountRunningAverage(0),
    _rollWindowCallback(std::move(rollWindowCallback)),
    _avgSamples(0),*/
    {
        _timer = new Timer(state => RollWindow(), state: null, windowDuration, windowDuration);
        _totalCountRunningAverage = 0;
        _rollWindowCallback = rollWindowCallback;
        _rng = new ThreadLocal<Random>(() => new Random());
        _avgSamples = 0;
        _countsSlots = new Counts[2] { new(), new() };
        if (averageLookback < 1)
        {
            // Error("AdaptiveSampler: 'averageLookback' argument must be at least 1");
            averageLookback = 1;
        }

        if (budgetLookback < 1)
        {
            // Log::Error("AdaptiveSampler: 'budgetLookback' argument must be at least 1");
            budgetLookback = 1;
        }

        _samplesPerWindow = samplesPerWindow;
        _budgetLookback = budgetLookback;

        _samplesBudget = samplesPerWindow + (budgetLookback * samplesPerWindow);
        _emaAlpha = ComputeIntervalAlpha(averageLookback);
        _budgetAlpha = ComputeIntervalAlpha(budgetLookback);

        _countsRef = _countsSlots[0];

        // Initialize RNG
        // std::random_device rd;
        // _rng = std::mt19937(rd());
        // _distribution = std::uniform_real_distribution<>(0.0, 1.0);

        if (windowDuration != TimeSpan.Zero)
        {
            _timer.Change(windowDuration, windowDuration);
        }
    }

    internal bool Sample()
    {
        _countsRef.AddTest();

        if (NextDouble() < _probability)
        {
            return _countsRef.AddSample(_samplesBudget);
        }

        return false;
    }

    internal bool Keep()
    {
        _countsRef.AddTest();
        _countsRef.AddSample();
        return true;
    }

    internal bool Drop()
    {
        _countsRef.AddTest();
        return false;
    }

    internal double NextDouble()
    {
        // std::lock_guard lock(_rngMutex);
        // return _distribution(_rng);
        return _rng.Value.NextDouble();
    }

    private double ComputeIntervalAlpha(int lookback)
    {
        return 1 - Math.Pow(lookback, -1.0 / lookback);
    }

    private long CalculateBudgetEma(long sampledCount)
    {
        _avgSamples = double.IsNaN(_avgSamples) || _budgetAlpha <= 0.0
                          ? sampledCount
                          : _avgSamples + (_budgetAlpha * (sampledCount - _avgSamples));

        double result = Math.Max(_samplesPerWindow - _avgSamples, 0.0) * _budgetLookback;
        return (long)result;
    }

    internal void RollWindow()
    {
        var counts = _countsSlots[_countsSlotIndex];

        /*
         * Semi-atomically replace the Counts instance such that sample requests during window maintenance will be
         * using the newly created counts instead of the ones currently processed by the maintenance routine.
         * We are ok with slightly racy outcome where totaCount and sampledCount may not be totally in sync
         * because it allows to avoid contention in the hot-path and the effect on the overall sample rate is minimal
         * and will get compensated in the long run.
         * Theoretically, a compensating system might be devised but it will always require introducing a single point
         * of contention and add a fair amount of complexity. Considering that we are ok with keeping the target sampling
         * rate within certain error margins and this data race is not breaking the margin it is better to keep the
         * code simple and reasonably fast.
         */

        _countsSlotIndex = (_countsSlotIndex + 1) % 2;
        _countsRef = _countsSlots[_countsSlotIndex];
        var totalCount = counts.TestCount();
        var sampledCount = counts.SampleCount();

        _samplesBudget = CalculateBudgetEma(sampledCount);

        if (_totalCountRunningAverage == 0 || _emaAlpha <= 0.0)
        {
            _totalCountRunningAverage = totalCount;
        }
        else
        {
            _totalCountRunningAverage = _totalCountRunningAverage + (_emaAlpha * (totalCount - _totalCountRunningAverage));
        }

        if (_totalCountRunningAverage <= 0)
        {
            _probability = 1;
        }
        else
        {
            _probability = Math.Min(_samplesBudget / _totalCountRunningAverage, val2: 1.0);
        }

        counts.Reset();

        if (_rollWindowCallback != null)
        {
            _rollWindowCallback();
        }
    }

    internal State GetInternalState()
    {
        var counts = _countsRef;

        return new State
        {
            TestCount = counts.TestCount(),
            SampleCount = counts.SampleCount(),
            Budget = _samplesBudget,
            Probability = _probability,
            TotalAverage = _totalCountRunningAverage
        };
    }

    private class Counts
    {
        private long _testCount;
        private long _sampleCount;

        internal void AddTest()
        {
            Interlocked.Increment(ref _testCount);
        }

        internal bool AddSample(long limit)
        {
            long previousValue;
            long newValue;

            do
            {
                previousValue = _sampleCount;
                newValue = Math.Min(previousValue + 1, limit);
            } 
            while (previousValue != Interlocked.CompareExchange(ref _sampleCount, previousValue, newValue));

            return newValue < limit;
        }

        internal void AddSample()
        {
            Interlocked.Increment(ref _sampleCount);
        }

        internal void Reset()
        {
            _testCount = 0;
            _sampleCount = 0;
        }

        internal long SampleCount()
        {
            return _sampleCount;
        }

        internal long TestCount()
        {
            return _testCount;
        }
    }

    internal class State
    {
        public long TestCount { get; init; }

        public long SampleCount { get; init; }

        public long Budget { get; init; }

        public double Probability { get; init; }

        public double TotalAverage { get; init; }
    }
}
