using System;
using CoordinateSharp;
using GeoTimeZone;
using TimeZoneConverter;

namespace DefaultNamespace
{
    public class SunInformationUtil
    {
        public static bool IsSunUp(float latitude, float longitude, DateTime observationDate)
        {
            string tz = TimeZoneLookup.GetTimeZone(latitude, longitude).Result;
            double offset; 
            //Antarctica/Troll is not handled
            if (tz.Equals("Antarctica/Troll"))
            {
                offset = 2;
            }
            else
            {
                TimeZoneInfo tzInfo = TZConvert.GetTimeZoneInfo(tz);
                offset = tzInfo.BaseUtcOffset.TotalHours;
            }
            Coordinate coordinate = new Coordinate(latitude, longitude, observationDate.AddHours(offset));
            coordinate.Offset = offset;
            return coordinate.CelestialInfo.IsSunUp;
        }
    }
}