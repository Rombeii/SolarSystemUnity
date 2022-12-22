using UnityEngine;

namespace DefaultNamespace
{
    public class Planet
    {
        public string name { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
        public float diameter { get; set; }
        public float importance { get; set; }

        public Planet(string name, float x, float y, float z, float diameter, float importance)
        {
            this.name = name;
            this.x = x;
            this.y = y;
            this.z = z;
            this.diameter = diameter;
            this.importance = importance;
        }

        public Vector3 getVector()
        {
            return new Vector3(x, y, z);
        }
    }
}