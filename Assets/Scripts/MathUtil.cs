using System;
using UnityEngine;

namespace DefaultNamespace
{
    public static class MathUtil
    {
        public static float MapBetweenValues(float fromMin, float fromMax, float toMin, float toMax, float value)
        {
            float t = Mathf.InverseLerp(fromMin, fromMax, value);
            return Mathf.Lerp(toMin, toMax, t);
        }
        
        public static int CalculateSampleSize(int k, float N)
        {
            float sigma_max = 40f;
            float T0 = 5;
            float alpha = 0.99997f;
            float eps = 0.00009f;
            float sigma_max_pow = (float) Math.Pow(sigma_max, 2);

            float numerator = N * sigma_max_pow;
            float sigma_k = T0 * (float) Math.Pow(alpha, k) * (float) Math.Pow(1 - eps, k);
            float denominator = (N - 1) * (float) Math.Pow(sigma_k, 2) + sigma_max_pow;

            return (int)(Math.Ceiling(numerator / denominator) + 0.5);
        }
        
        public static Vector3 LatLonToECEF(float lat, float lon)
        {
            lat = Mathf.Deg2Rad * lat;
            lon = Mathf.Deg2Rad * lon;

            float a = 6378137.0f / 1000000; // semi-major axis
            float b = 6356752.3142f / 1000000; // semi-minor axis
            float f = (a - b) / a; // flattening
            float e = Mathf.Sqrt(f * (2 - f)); // first eccentricity
            float sinLat = Mathf.Sin(lat);
            float cosLat = Mathf.Cos(lat);
            float sinLon = Mathf.Sin(lon);
            float cosLon = Mathf.Cos(lon);

            float N = a / Mathf.Sqrt(1 - e * e * sinLat * sinLat);

            float x = N * cosLat * cosLon;
            float y = N * cosLat * sinLon;
            float z = N * sinLat;

            //Y points up in unity
            return new Vector3(x, z, y);
        }
    }
    
    
}