using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFi.Utilities
{
    public static class DateTimeExtensions
    {
        public static DateTime GetClosestWeekDay(this DateTime date, bool precedingDay)
        {
            if(date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday) 
                return date;
            int dayOfWeek = date.DayOfWeek == DayOfWeek.Sunday ? 7 : 6;
            return date.AddDays((dayOfWeek - (precedingDay ? 5 : 8)) * (precedingDay ? -1 : 1));
        }
    }
}
