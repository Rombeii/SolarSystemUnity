using System;
using System.IO;
using UnityEngine;
using Color = UnityEngine.Color;

namespace DefaultNamespace
{
    public class HeatmapWrapper
    {
        private Texture2D tex;
        
        public HeatmapWrapper(String path)
        {
            if (File.Exists(path))     {
                byte[] fileData = File.ReadAllBytes(path);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData);
            }
        }

        //white -> terminate
        public bool isInvalidPlacementBasedOnGrayScale(float latitude, float longitude)
        {
            return GETPixelAt(latitude, longitude).grayscale >= 0.5;
        }

        //whiter -> lower multiplier 
        public float GetMultiplierBasedOnGrayscale(float latitude, float longitude)
        {
            return 1 - GETPixelAt(latitude, longitude).grayscale;
        }

        private Color GETPixelAt(float latitude, float longitude)
        {
            var x = MathUtil.MapBetweenValues(-180f, 180f, 0f, tex.width, longitude);
            var y = MathUtil.MapBetweenValues(-90f, 90f, 0f, tex.height, latitude);
            return tex.GetPixel((int) x, (int) y);
        }
    }
}