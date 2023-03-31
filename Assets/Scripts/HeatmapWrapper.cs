using System;
using System.IO;
using UnityEngine;
using Color = UnityEngine.Color;

namespace DefaultNamespace
{
    public class HeatmapWrapper
    {
        private Texture2D _tex;
        private float _minLat;
        private float _minLong;
        private float _maxLat;
        private float _maxLong;
        
        
        public HeatmapWrapper(String path, float minLat, float minLong, float maxLat, float maxLong)
        {
            if (File.Exists(path))     {
                byte[] fileData = File.ReadAllBytes(path);
                _tex = new Texture2D(2, 2);
                _tex.LoadImage(fileData);
                _minLat = minLat;
                _minLong = minLong;
                _maxLat = maxLat;
                _maxLong = maxLong;
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
            var x = MathUtil.MapBetweenValues(_minLong, _maxLong, 0f, _tex.width, longitude);
            var y = MathUtil.MapBetweenValues(_minLat, _maxLat, 0f, _tex.height, latitude);
            return _tex.GetPixel((int) x, (int) y);
        }
    }
}