using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefaultNamespace
{
    public class DistanceCache
    {
        private List<ObservatoryDistance> distances;

        public DistanceCache()
        {
            ResetCache();
        }

        public void ResetCache()
        {
            distances = new List<ObservatoryDistance>();
        }

        public float GetDistance(Observatory obs1, Observatory obs2)
        {
            ObservatoryDistance observatoryDistance = GetOrCreateObservatoryDistance(obs1, obs2);
            return observatoryDistance.GetDistance();
        }

        private ObservatoryDistance GetOrCreateObservatoryDistance(Observatory obs1, Observatory obs2)
        {
            var existingObservatoryDistance = distances.FirstOrDefault(dist => dist.IsDistanceBetween(obs1, obs2));
            if (existingObservatoryDistance != null)
            {
                return existingObservatoryDistance;
            }
            
            var newObservatoryDistance = new ObservatoryDistance(obs1, obs2);
            distances.Add(newObservatoryDistance);
            return newObservatoryDistance;
        }

        private class ObservatoryDistance
        {
            private readonly Observatory observatory1;
            private readonly Observatory observatory2;
            private readonly float distance;

            public ObservatoryDistance(Observatory obs1, Observatory obs2)
            {
                observatory1 = obs1;
                observatory2 = obs2;
                distance = Vector3.Distance(obs1.Location, obs2.Location);
            }

            public bool IsDistanceBetween(Observatory obs1, Observatory obs2)
            {
                return (observatory1 == obs1 && observatory2 == obs2) || (observatory1 == obs2 && observatory2 == obs1);
            }

            public float GetDistance()
            {
                return distance;
            }
        }
    }
}