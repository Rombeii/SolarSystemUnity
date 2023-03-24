using System;
using System.Collections.Generic;

namespace DefaultNamespace
{
    public class Observation
    {
        private List<ObservedObject> _observedObjects;
        private DateTime _observationDate;

        public Observation(List<ObservedObject> observedObjects, DateTime observationDate)
        {
            _observedObjects = observedObjects;
            _observationDate = observationDate;
        }

        public List<ObservedObject> GETObservedObjects()
        {
            return _observedObjects;
        }

        public ObservedObject GETObservedObjectAt(int index)
        {
            return _observedObjects[index];
        }

        public DateTime GETObservationDate()
        {
            return _observationDate;
        }
    }
    
}