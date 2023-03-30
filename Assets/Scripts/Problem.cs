using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefaultNamespace
{
    public class Problem
    {
        private List<Observation> observations;
        private List<Observatory> observatories;
        private float maxPoints;

        public List<Observation> Observations
        {
            get => observations;
            set => observations = value;
        }

        public List<Observatory> Observatories
        {
            get => observatories;
            set => observatories = value;
        }

        public Problem()
        {
            this.observatories = new List<Observatory>();
            this.observations = new List<Observation>();
        }

        public void AddObservatoryWithAngle(int angle)
        {
            observatories.Add(new Observatory(angle));
        }

        public void MakeObservatoryStatic(int index, float latitude, float longitude)
        {
            observatories[index].MakeStatic(latitude, longitude);
        }

        public float getMaxPoints()
        {
            if (maxPoints == 0)
            {
                for (int i = 0; i < observations[0].GETObservedObjects().Count; i++)
                {
                    maxPoints += observations[0].GETObservedObjectAt(i).Importance;
                }
            }

            return maxPoints;
        }

        public void turnOnNextObservatory(Vector3 location, float latitude, float longitude, float action1,
            float action2, int gridY, int gridX, bool isInvalidPlacement)
        {
            foreach (var observatory in observatories)
            {
                if (!observatory.IsUsed)
                {
                    observatory.turnOn(location, latitude, longitude, action1, action2, gridY,
                        gridX, isInvalidPlacement);
                    break;
                }
            }
        }

        public bool areAllObservatoriesOn()
        {
            return observatories.All(x => x.IsUsed);
        }
    }
}