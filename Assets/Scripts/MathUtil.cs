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
        
        //lat -> -90 --- 90
        //long -> -180 --- 180
        public static Vector3 LatLongToXYZ(float latitude, float longitude, float radius)
        {
            return Quaternion.AngleAxis(longitude, -Vector3.up)
                   * Quaternion.AngleAxis(latitude, -Vector3.right)
                   * new Vector3(0,0,radius);
        }
        
        public static Vector2 XYZtoLatLong(Vector3 position, float radius){
            float lat = Mathf.Acos(position.y / radius); //theta
            float lon = Mathf.Atan2(position.x, position.z); //phi
            lat *= Mathf.Rad2Deg;
            lon *= Mathf.Rad2Deg;
            return new Vector2(lat, lon);
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
    }
    
    
}