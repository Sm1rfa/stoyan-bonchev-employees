using System;
using System.Globalization;

namespace Employees.Desktop.Helpers
{
    public static class Utils
    {
        public static DateTime DateParse(string date)
        {
            DateTime dateValue;
            // to handle the "today" state
            if (date.Equals("NULL"))
            {
                return DateTime.Today;
            }
            // to handle eventual user input case, with different culture
            if (DateTime.TryParse(date, CultureInfo.CurrentCulture, DateTimeStyles.None, out dateValue))
            {
                return dateValue;
            }

            // to handle if formatting and parsing are done from various cultures
            dateValue = DateTime.Parse(date, CultureInfo.InvariantCulture);

            return dateValue;
        }

        public static bool CheckIfDatesOverlap(DateTime startA, DateTime endA, DateTime startB, DateTime endB)
        {
            return startA < endB && startB < endA;
        }

        public static int CalculateDaysTogether(DateTime startA, DateTime endA, DateTime startB, DateTime endB)
        {
            if (startA < startB && endA < endB)
            {
                return Math.Abs((startB - endA).Days);
            }
            if (startA < startB && endA > endB)
            {
                return Math.Abs((startB - endB).Days);
            }
            if (startA > startB && endA < endB)
            {
                return Math.Abs((startA - endA).Days);
            }
            if (startA > startB && endA > endB)
            {
                return Math.Abs((startB - endB).Days);
            }
            if (startA < startB && endA == endB) 
            {
                return Math.Abs((startB - endA).Days);
            }
            if (startA > startB && endA == endB) 
            {
                return Math.Abs((startA - endA).Days);
            }
            if (startA == startB && endA > endB) 
            {
                return Math.Abs((startA - endB).Days);
            }
            if (startA == startB && endA < endB) 
            {
                return Math.Abs((startA - endA).Days);
            }

            return 1;
        }
    }
}
