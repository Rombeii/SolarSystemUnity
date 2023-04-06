using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public class DistanceCache
    {
        private float?[,] distances;
        private List<Observatory> observatories;

        public DistanceCache(List<Observatory> obsList)
        {
            observatories = obsList;
            distances = new float?[observatories.Count, observatories.Count];
        }

        public void ResetCache()
        {
            distances = new float?[observatories.Count, observatories.Count];
        }

        public float GetDistance(Observatory obs1, Observatory obs2)
        {
            int index1 = observatories.IndexOf(obs1);
            int index2 = observatories.IndexOf(obs2);

            float? distance = distances[index1, index2];

            if (distance == null)
            {
                Vector3 loc1 = obs1.Location;
                Vector3 loc2 = obs2.Location;
                float dist = Vector3.Distance(loc1, loc2);
                distances[index1, index2] = dist;
                distances[index2, index1] = dist;
                distance = dist;
            }

            return (float)distance;
        }
    }
}