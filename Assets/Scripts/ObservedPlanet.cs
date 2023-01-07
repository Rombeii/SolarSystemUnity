using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class ObservedPlanet
    {
        private string _name;
        private float _x;
        private float _y;
        private float _z;
        private float _diameter;
        private float _importance;
        private DateTime _observationDate;

        public ObservedPlanet(string name, float x, float y, float z, float diameter, float importance, DateTime observationDate)
        {
            _name = name;
            _x = x;
            _y = y;
            _z = z;
            _diameter = diameter;
            _importance = importance;
            _observationDate = observationDate;
        }

        public Vector3 GETPosition()
        {
            return new Vector3(_x, _y, _z);
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public float X
        {
            get => _x;
            set => _x = value;
        }

        public float Y
        {
            get => _y;
            set => _y = value;
        }

        public float Z
        {
            get => _z;
            set => _z = value;
        }

        public float Diameter
        {
            get => _diameter;
            set => _diameter = value;
        }

        public float Importance
        {
            get => _importance;
            set => _importance = value;
        }
    }
}