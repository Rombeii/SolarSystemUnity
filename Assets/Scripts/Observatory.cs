using UnityEngine;

namespace DefaultNamespace
{
    public class Observatory
    {
        private Vector3 location;
        private bool isUsed;
        private int angle;
        private float latitude;
        private float latitudeAction;
        private float longitude;
        private float longitudeAction;

        public Observatory(int angle)
        {
            this.angle = angle;
            this.location = Vector3.zero;
            this.isUsed = false;
        }

        public void turnOn(Vector3 location, float latitude, float longitude, float latitudeAction,
            float longitudeAction)
        {
            this.location = location;
            this.isUsed = true;
            this.latitude = latitude;
            this.longitude = longitude;
            this.latitudeAction = latitudeAction;
            this.longitudeAction = longitudeAction;
        }

        public void Reset()
        {
            this.location = Vector3.zero;
            this.isUsed = false;
            this.latitude = 0;
            this.longitude = 0;
        }

        public bool IsUsed
        {
            get => isUsed;
            set => isUsed = value;
        }

        public float Latitude
        {
            get => latitude;
            set => latitude = value;
        }

        public float Longitude
        {
            get => longitude;
            set => longitude = value;
        }

        public Vector3 Location
        {
            get => location;
            set => location = value;
        }

        public int Angle
        {
            get => angle;
            set => angle = value;
        }
        
        public float LongitudeAction
        {
            get => longitudeAction;
            set => longitudeAction = value;
        }

        public float LatitudeAction
        {
            get => latitudeAction;
            set => latitudeAction = value;
        }
    }
}