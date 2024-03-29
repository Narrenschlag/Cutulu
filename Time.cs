using System;
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

        public delegate void EmptyEvent();
        public static EmptyEvent onMinuteElapsed;
        public static EmptyEvent onHourElapsed;
        public static EmptyEvent onDayElapsed;

        public static void Setup(Node master) => OnMinuteElapsed(false, master);
        #endregion

        #region Private     ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private static void OnMinuteElapsed() => OnMinuteElapsed(true);
        private static void OnMinuteElapsed(bool callDelegate, Node masterNode = null)
        {
            ushort day = DayOfWeek;
            ushort hour = Hour;
            UpdateInternal();

            // Recall this function every 60s
            masterNode.QueueAction(OnMinuteElapsed, 60f - Seconds);

            // Call delegate voids
            if (callDelegate)
            {
                // The order of calling these delegates
                // simplifies the workflow a lot

                // Every day
                if (day != DayOfWeek && onDayElapsed != null) onDayElapsed();

                // Every hour
                if (hour != Hour && onHourElapsed != null) onHourElapsed();

                // Every minute
                onMinuteElapsed?.Invoke();
            }
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
