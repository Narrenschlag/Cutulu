using System;
using System.Threading.Tasks;
using Godot;

namespace Cutulu
{
    /// <summary>
    /// Has to be setup once provided with a root node it is working with
    /// </summary>
    public static class Time
    {
        #region Public      ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Updated every minute
        /// </summary>
        public static DateTime Now { get; private set; }

        /// <summary>
        /// Updated every minute.
        /// <br/>Format: ##.####
        /// </summary>
        public static float Seconds { get; private set; }

        /// <summary>
        /// Updated every minute
        /// </summary>
        public static ushort TotalMinute { get; private set; }

        /// <summary>
        /// Updated every minute
        /// </summary>
        public static ushort Minute { get; private set; }

        /// <summary>
        /// Updated every minute
        /// </summary>
        public static ushort Hour { get; private set; }

        /// <summary>
        /// Updated every minute
        /// </summary>
        public static ushort DayOfWeek { get; private set; }

        /// <summary>
        /// Updated every minute
        /// </summary>
        public static ushort DayOfMonth { get; private set; }

        /// <summary>
        /// Updated every minute
        /// </summary>
        public static ushort DayOfYear { get; private set; }

        /// <summary>
        /// Updated every minute
        /// </summary>
        public static ushort WeekOfYear { get; private set; }

        /// <summary>
        /// Updated every minute
        /// </summary>
        public static ushort MonthOfYear { get; private set; }

        /// <summary>
        /// Updated every minute
        /// </summary>
        public static ushort Year { get; private set; }

        public static Action MinuteElapsed { get; set; }
        public static Action HourElapsed { get; set; }
        public static Action DayElapsed { get; set; }

        static Time()
        {
            OnMinuteElapsed();
        }
        #endregion

        #region Private     ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private static async void OnMinuteElapsed()
        {
            var day = DayOfWeek;
            var hour = Hour;
            UpdateInternal();

            // The order of calling these delegates
            // simplifies the workflow a lot

            // Every day
            if (day != DayOfWeek) DayElapsed?.Invoke();

            // Every hour
            if (hour != Hour) HourElapsed?.Invoke();

            // Every minute
            MinuteElapsed?.Invoke();

            await Task.Delay(Mathf.RoundToInt((60f - Seconds) * 1000));

            // Recall this function every 60s
            OnMinuteElapsed();
        }

        private static void UpdateInternal()
        {
            Now = DateTime.Now;

            Seconds = Now.Second + (float)Now.Millisecond / 1000;
            Minute = (ushort)Now.Minute;
            Hour = (ushort)Now.Hour;

            TotalMinute = (ushort)(Minute + 60 * Hour);

            DayOfWeek = (ushort)Now.DayOfWeek;
            DayOfYear = (ushort)Now.DayOfYear;
            DayOfMonth = (ushort)Now.Day;

            WeekOfYear = (ushort)Mathf.FloorToInt((float)DayOfYear / 7);
            MonthOfYear = (ushort)Now.Month;
            Year = (ushort)Now.Year;
        }

        public static double DaysSince(DateTime date) => (Now - date).TotalDays;
        #endregion
    }
}
