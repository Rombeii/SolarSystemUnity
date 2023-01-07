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
        public bool ShouldTerminateBasedOnGrayScale(float latitude, float longitude)
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
            var x = (longitude + 180f) / 360f * tex.width;
            var y = tex.height / 2f - latitude * tex.height / 180f;
            //Texture coordinates start at lower left corner.
            y = tex.height - y;
            return tex.GetPixel((int) x, (int) y);
        }
    }
}