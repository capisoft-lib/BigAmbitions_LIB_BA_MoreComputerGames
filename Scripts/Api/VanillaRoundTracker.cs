using System;

namespace Capisoft.Lib.BaComputerGames
{
    // Observe the menu before a start, so attaching halfway through a run cannot fabricate a duration.
    internal sealed class VanillaRoundTracker
    {
        private bool _armed, _tracking, _modified;
        private double _started, _active;
        private DateTime _utc;
        internal void Reset() { _armed = _tracking = _modified = false; _active = 0; }
        internal ComputerGameResult Observe(bool menu, int lives, int score, int pending, int level,
            double now, double activeDelta, DateTime utc, bool modified, string version, string profile)
        {
            if (menu) { Reset(); _armed = true; return null; }
            if (!_tracking) {
                if (!_armed || lives <= 0) return null;
                _armed = false; _tracking = true; _started = now; _utc = utc;
            }
            if (!double.IsNaN(activeDelta) && !double.IsInfinity(activeDelta)) _active += Math.Max(0, activeDelta);
            _modified |= modified;
            if (lives > 0) return null;
            var elapsed = Math.Max(0.001, now - _started);
            var result = new ComputerGameResult(ComputerGames.VanillaBrickBreakerId, version, ComputerGames.VanillaBrickBreakerRuleset,
                Math.Max(0, (long)score + pending), Math.Max(1, level + 1), _utc, _utc.AddSeconds(elapsed),
                Math.Min(elapsed, _active), elapsed, _modified, profile);
            Reset(); return result;
        }
    }
}
