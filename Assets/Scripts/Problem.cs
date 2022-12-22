using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefaultNamespace
{
    public class Problem
    {
        private List<List<Planet>> generatedPositions;
        private List<Observatory> observatories;
        private float maxPoints;

        public List<List<Planet>> GeneratedPositions
        {
            get => generatedPositions;
            set => generatedPositions = value;
        }

        public List<Observatory> Observatories
        {
            get => observatories;
            set => observatories = value;
        }

        public Problem()
        {
            this.observatories = new List<Observatory>();
            this.generatedPositions = new List<List<Planet>>();
        }

        public void AddObservatoryWithAngle(int angle)
        {
            observatories.Add(new Observatory(angle));
        }

        public float getMaxPoints()
        {
            if (maxPoints == 0)
            {
                for (int i = 0; i < generatedPositions[0].Count; i++)
                {
                    maxPoints += generatedPositions[0][i].importance;
                }
            }

            return maxPoints;
        }

        public void turnOnNextObservatory(Vector3 location, float latitude, float longitude, float action1, float action2)
        {
            foreach (var observatory in observatories)
            {
                if (!observatory.IsUsed)
                {
                    observatory.turnOn(location, latitude, longitude, action1, action2);
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