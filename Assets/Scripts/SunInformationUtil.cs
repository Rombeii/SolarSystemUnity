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
            TimeZoneInfo tzInfo = TZConvert.GetTimeZoneInfo(tz);
            double offset = tzInfo.BaseUtcOffset.TotalHours;
            Coordinate coordinate = new Coordinate(latitude, longitude, observationDate.AddHours(offset));
            coordinate.Offset = offset;
            return coordinate.CelestialInfo.IsSunUp;
        }
    }
}