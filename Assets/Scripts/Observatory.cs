using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public class Observatory
    {
        private Vector3 _location;
        private bool _isUsed;
        private bool _isInvalidPlacement;
        private int _angle;
        private float _latitude;
        private float _latitudeAction;
        private float _longitude;
        private float _longitudeAction;
        private float _yearlyRewardMultiplier;
        private Dictionary<int, float> _monthlyRewardMultiplier;

        public Observatory(int angle)
        {
            _angle = angle;
            _location = Vector3.zero;
            _isUsed = false;
            _yearlyRewardMultiplier = 1;
            _monthlyRewardMultiplier = new Dictionary<int, float>();
            ResetMonthlyRewardMultiplier();
        }

        public void turnOn(Vector3 location, float latitude, float longitude, float latitudeAction,
            float longitudeAction, bool isInvalidPlacement)
        {
            _location = location;
            _isUsed = true;
            _latitude = latitude;
            _longitude = longitude;
            _latitudeAction = latitudeAction;
            _longitudeAction = longitudeAction;
            _isInvalidPlacement = isInvalidPlacement;
        }

        public void Reset()
        {
            _location = Vector3.zero;
            _isUsed = false;
            _latitude = 0;
            _longitude = 0;
            _yearlyRewardMultiplier = 1;
            _isInvalidPlacement = true;
            ResetMonthlyRewardMultiplier();
        }

        private void ResetMonthlyRewardMultiplier()
        {
            for (int i = 1; i <= 12; i++)
            {
                _monthlyRewardMultiplier[i] = 1;
            }
        }

        public bool IsUsed
        {
            get => _isUsed;
            set => _isUsed = value;
        }

        public float Latitude
        {
            get => _latitude;
            set => _latitude = value;
        }

        public float Longitude
        {
            get => _longitude;
            set => _longitude = value;
        }

        public Vector3 Location
        {
            get => _location;
            set => _location = value;
        }

        public int Angle
        {
            get => _angle;
            set => _angle = value;
        }
        
        public float LongitudeAction
        {
            get => _longitudeAction;
            set => _longitudeAction = value;
        }

        public float LatitudeAction
        {
            get => _latitudeAction;
            set => _latitudeAction = value;
        }

        public bool IsInvalidPlacement
        {
            get => _isInvalidPlacement;
            set => _isInvalidPlacement = value;
        }
    }
}