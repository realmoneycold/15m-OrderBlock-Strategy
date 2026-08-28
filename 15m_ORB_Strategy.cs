// Quantower runs on Windows only – suppress cross-platform warnings
#pragma warning disable CA1416

// ============================================================
//  15-Minute Opening Range Breakout (ORB) - AMP Quantower
//
//  HOW SESSION TIMES WORK (Eastern Time - ET):
//    All times are evaluated in Eastern Standard Time (EST/EDT)
//    By default, it tracks the first 15 minutes of the NY Open:
//    09:30 – 09:45 ET.
//
//  HOW IT WORKS:
//    - During the ORB window (09:30-09:45), it records the High and Low.
//    - After the window (09:45-16:00), it plots those lines as 
//      Support (Low) and Resistance (High) for breakout trading.
// ============================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace OpeningRangeBreakout
{
    public class OrbIndicator : Indicator
    {
        // ---- User-adjustable settings ----

        [InputParameter("ORB Start Time (ET HH:MM)", 10)]
        public string OrbStartET = "09:30";

        [InputParameter("ORB Duration (Minutes)", 20)]
        public int OrbDurationMinutes = 15;

        [InputParameter("Session End Time (ET HH:MM)", 30)]
        public string SessionEndET = "16:00";

        [InputParameter("Draw ORB Box (Fill)", 40)]
        public bool DrawFill = true;

        [InputParameter("ORB High Line Color", 50)]
        public Color OrbHighColor = Color.LimeGreen;

        [InputParameter("ORB Low Line Color", 60)]
        public Color OrbLowColor = Color.Red;
        
        [InputParameter("ORB Fill Color (Opacity 10%)", 70)]
        public Color OrbFillColor = Color.LightSlateGray;

        // ---- Internal State ----
        private static readonly TimeZoneInfo _easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        private class DailyOrb
        {
            public double High = double.MinValue;
            public double Low = double.MaxValue;
            public bool IsActive = false; // Is there any data yet?
        }

        private Dictionary<DateTime, DailyOrb> _dailyOrbs = new Dictionary<DateTime, DailyOrb>();
        private TimeSpan _tStart;
        private TimeSpan _tEnd;
        private TimeSpan _sessionEnd;

        // Line indices
        private const int ORB_HIGH = 0;
        private const int ORB_LOW = 1;

        public OrbIndicator() : base()
        {
            Name = "15m ORB (Opening Range Breakout)";
            Description = "Plots the Opening Range Breakout High and Low lines based on Eastern Time.";
            SeparateWindow = false;

            AddLineSeries("ORB High", OrbHighColor, 2, LineStyle.Solid);
            AddLineSeries("ORB Low", OrbLowColor, 2, LineStyle.Solid);
        }

        protected override void OnInit()
        {
            _dailyOrbs.Clear();

            if (!TimeSpan.TryParse(OrbStartET, out _tStart))
                _tStart = new TimeSpan(9, 30, 0);

            _tEnd = _tStart.Add(TimeSpan.FromMinutes(OrbDurationMinutes));

            if (!TimeSpan.TryParse(SessionEndET, out _sessionEnd))
                _sessionEnd = new TimeSpan(16, 0, 0);
        }

        protected override void OnClear()
        {
            _dailyOrbs.Clear();
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            if (Count == 0) return;

            // Get bar time
            DateTime barTimeUTC = Time();
            if (barTimeUTC.Kind == DateTimeKind.Unspecified)
                barTimeUTC = DateTime.SpecifyKind(barTimeUTC, DateTimeKind.Utc);

            // Convert to Eastern Time
            DateTime etTime = TimeZoneInfo.ConvertTimeFromUtc(barTimeUTC, _easternZone);
            DateTime tradingDay = etTime.Date;
            TimeSpan tod = etTime.TimeOfDay;

            // Initialize dictionary entry for the day
            if (!_dailyOrbs.ContainsKey(tradingDay))
            {
                _dailyOrbs[tradingDay] = new DailyOrb();
            }

            var orb = _dailyOrbs[tradingDay];

            // 1. ARE WE INSIDE THE ORB WINDOW? (e.g. 09:30 to 09:45)
            if (tod >= _tStart && tod < _tEnd)
            {
                orb.High = Math.Max(orb.High, High());
                orb.Low = Math.Min(orb.Low, Low());
                orb.IsActive = true;

                // Optionally draw the lines as they form
                SetValue(orb.High, ORB_HIGH);
                SetValue(orb.Low, ORB_LOW);
            }
            // 2. ARE WE AFTER THE ORB WINDOW BUT INSIDE THE TRADING SESSION? (e.g. 09:45 to 16:00)
            else if (tod >= _tEnd && tod <= _sessionEnd)
            {
                if (orb.IsActive)
                {
                    // Draw the established lines
                    SetValue(orb.High, ORB_HIGH);
                    SetValue(orb.Low, ORB_LOW);
                }
            }
            // 3. OUTSIDE TRADING SESSION
            else
            {
                // Leave lines blank (double.NaN) so they don't stretch into the overnight session
                SetValue(double.NaN, ORB_HIGH);
                SetValue(double.NaN, ORB_LOW);
            }
        }
        
    }
}
