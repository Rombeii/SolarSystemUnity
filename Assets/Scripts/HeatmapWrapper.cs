using System;
using System.IO;
using UnityEngine;
using Color = UnityEngine.Color;

namespace DefaultNamespace
{
    public class HeatmapWrapper
    {
        private Texture2D _tex;
        private float _min_lat;
        private float _min_long;
        private float _max_lat;
        private float _max_long;
        
        
        public HeatmapWrapper(String path, float min_lat, float min_long, float max_lat, float max_long)
        {
            if (File.Exists(path))     {
                byte[] fileData = File.ReadAllBytes(path);
                _tex = new Texture2D(2, 2);
                _tex.LoadImage(fileData);
            }
        }

        //white -> terminate
        public bool isInvalidPlacementBasedOnGrayScale(float latitude, float longitude, float threshold = 0.5f)
        {
            return GetMultiplierBasedOnGrayscale(latitude, longitude) <= threshold;
        }


        //whiter -> lower multiplier 

        public float GetMultiplierBasedOnGrayscale(float latitude, float longitude)
        {
            return 1 - GETPixelAt(latitude, longitude).grayscale;
        }

        public bool AreAllPixelsWhiteInCell(int rowNum, int colNum, int numberOfRows, int numberOfCols)
        {
            int cellWidth = _tex.width / numberOfCols;
            int cellHeight = _tex.height / numberOfRows;
            for (int rowIndex = rowNum * cellWidth; rowIndex < (rowNum + 1) * cellWidth - 1; rowIndex++)
            {
                for (int colIndex = colNum * cellHeight; colIndex < (colNum + 1) * cellHeight - 1; colIndex++)
                {
                    if (_tex.GetPixel(colIndex, _tex.height - rowIndex - 1).Equals(Color.black))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private Color GETPixelAt(float latitude, float longitude)
        {
            var x = MathUtil.MapBetweenValues(_min_long, _max_long, 0f, _tex.width, longitude);
            var y = MathUtil.MapBetweenValues(_min_lat, _max_lat, 0f, _tex.height, latitude);
            return _tex.GetPixel((int) x, (int) y);
        }
    }
}