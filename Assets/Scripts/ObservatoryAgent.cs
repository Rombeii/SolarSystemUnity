using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = System.Random;

public class ObservatoryAgent : Agent
{
    private Dictionary<string, GameObject> _planets = new Dictionary<string, GameObject>();
    private Problem _problem;
    private float _earthRadius;
    private int _previousSampleSize;

    public override void Initialize()
    {
        _earthRadius = PlanetUtil.planetSizes["earth"] / 2;
        _problem = GeneratedPositionUtil.getProblemFromCsv();
        InitializePlanetDict();

        ResetScene();
    }
    
    private void InitializePlanetDict()
    {
        for (int i = 0; i < _problem.GeneratedPositions[0].Count; i++)
        {
            GameObject newPlanet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            newPlanet.transform.parent = transform;
            newPlanet.transform.name = _problem.GeneratedPositions[0][i].name;
            _planets.Add(_problem.GeneratedPositions[0][i].name, newPlanet);
        }
    }
    
    private void ResetScene()
    {
        ResetObservatoryPositions();
    }

    private void ResetObservatoryPositions()
    {
        foreach (var observatory in _problem.Observatories)
        {
            observatory.Reset();
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float action1 = actions.ContinuousActions[0];
        float action2 = actions.ContinuousActions[1];
        float latitude = mapBetweenValues(-1, 1, -90, 90, action1);
        float longitude = mapBetweenValues(-1, 1, -180, 180, action2);
        _problem.turnOnNextObservatory(LatLongToXYZ(latitude, longitude), latitude, longitude, action1, action2);
        if (_problem.areAllObservatoriesOn())
        {
            int sampleSize = _previousSampleSize == _problem.GeneratedPositions.Count
                ? _previousSampleSize
                : CalculateSampleSize(CompletedEpisodes);
            // float reward = CalculateReward() / problem.GeneratedPositions.Count / problem.getMaxPoints();
            float reward = CalculateReward(sampleSize) / sampleSize / _problem.getMaxPoints();
            // AddReward(problem.GeneratedPositions.Count, reward);
            AddReward(sampleSize, reward);
            _previousSampleSize = sampleSize;
            EndEpisode();
        }
    }

    private float mapBetweenValues(float fromMin, float fromMax, float toMin, float toMax, float value)
    {
        float t = Mathf.InverseLerp(fromMin, fromMax, value);
        return Mathf.Lerp(toMin, toMax, t);
    }
    
    //lat -> -90 --- 90
    //long -> -180 --- 180
    private Vector3 LatLongToXYZ(float latitude, float longitude)
    {
        return Quaternion.AngleAxis(longitude, -Vector3.up)
               * Quaternion.AngleAxis(latitude, -Vector3.right)
               * new Vector3(0,0,_earthRadius);
    }

    private int CalculateSampleSize(int k)
    {
        float N = _problem.GeneratedPositions.Count;
        float sigma_max = 40f;
        float T0 = 5;
        float alpha = 0.99997f;
        float eps = 0.0000009f;
        float sigma_max_pow = (float) Math.Pow(sigma_max, 2);

        float numerator = N * sigma_max_pow;
        float sigma_k = T0 * (float) Math.Pow(alpha, k) * (float) Math.Pow(1 - eps, k);
        float denominator = (N - 1) * (float) Math.Pow(sigma_k, 2) + sigma_max_pow;

        return (int)(Math.Ceiling(numerator / denominator) + 0.5);
    }
    
    private float CalculateReward()
    {
        float reward = 0;
        for (int index = 0; index < _problem.GeneratedPositions.Count; index++)
        {
            List<String> distinctPlanetsSeen = new List<string>();
            SetPlanetsToPosition(_problem.GeneratedPositions[index]);
            foreach (var observatory in _problem.Observatories)
            {
                List<String> planetsInCone = GetAllPlanetsInCone(observatory, _problem.GeneratedPositions[index],
                    observatory.Angle);
                distinctPlanetsSeen.AddRange(planetsInCone.Except(distinctPlanetsSeen));
            }
            
            distinctPlanetsSeen.Remove("earth");
            reward += distinctPlanetsSeen.Count;
        }
        return reward;
    }
    
    private float CalculateReward(int sampleSize)
    {
        float reward = 0;
        List<int> indices = GetRandomNumber(0, _problem.GeneratedPositions.Count, sampleSize);
        for (int index = 0; index < indices.Count; index++)
        {
            List<String> distinctPlanetsSeen = new List<string>();
            SetPlanetsToPosition(_problem.GeneratedPositions[indices[index]]);
            foreach (var observatory in _problem.Observatories)
            {
                List<String> planetsInCone = GetAllPlanetsInCone(observatory, _problem.GeneratedPositions[indices[index]],
                    observatory.Angle);
                distinctPlanetsSeen.AddRange(planetsInCone.Except(distinctPlanetsSeen));
            }
            
            distinctPlanetsSeen.Remove("earth");
            reward += distinctPlanetsSeen.Count;
        }
        return reward;
    }
    
    private void SetPlanetsToPosition(List<Planet> positions)
    {
        foreach (var position in positions)
        {
            _planets[position.name].transform.position = new Vector3(position.x, position.y, position.z);
        }
    }

    private List<int> GetRandomNumber(int from,int to,int numberOfElement)
    {
        var random = new Random();
        HashSet<int> numbers = new HashSet<int>();
        while (numbers.Count < numberOfElement)
        {
            numbers.Add(random.Next(from, to));
        }
        return numbers.ToList();
    }
    
    private List<String> GetAllPlanetsInCone(Observatory observatory, List<Planet> positions, int maxAngle)
    {
        Vector3 from = observatory.Location;
        List<String> planetsHit = new List<string>();

        foreach (var generatedPosition in positions)
        {
            if (generatedPosition.importance != 0 && 
                IsPointInsideCone(from, generatedPosition.getVector(), -from, maxAngle))
            {
                RaycastHit hit;
                if (!Physics.Linecast(from, generatedPosition.getVector(), out hit)
                    || generatedPosition.name.Equals(hit.collider.name))
                {
                    planetsHit.Add(generatedPosition.name);
                }
            }
        }

        return planetsHit;
    }
    
    private static bool IsPointInsideCone(Vector3 point, Vector3 coneOrigin, Vector3 coneDirection, int maxAngle)
    {
        var pointDirection = point - coneOrigin;
        var angle = Vector3.Angle ( coneDirection, pointDirection );
        return angle < maxAngle;
    }
    
    private void AddReward(int sampleSize, float reward)
    {
        AddReward(reward);
        AddStats(sampleSize);
    }

    private void AddStats(int sampleSize)
    {
        Academy.Instance.StatsRecorder.Add("SampleSize", sampleSize);
        Academy.Instance.StatsRecorder.Add("EpisodeCount", CompletedEpisodes);

        for (int index = 0; index < _problem.Observatories.Count; index++)
        {
            Academy.Instance.StatsRecorder.Add("Lat" + index, _problem.Observatories[index].Latitude);
            Academy.Instance.StatsRecorder.Add("Long" + index, _problem.Observatories[index].Longitude);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        foreach (var observatory in _problem.Observatories)
        {
            sensor.AddObservation(observatory.Location.normalized);    
        }
    }

    public override void OnEpisodeBegin()
    {
        ResetScene();
        base.OnEpisodeBegin();
    }

    
    public static Vector2 XYZtoLatLong(Vector3 position, float sphereRadius){
        float lat = Mathf.Acos(position.y / sphereRadius); //theta
        float lon = Mathf.Atan2(position.x, position.z); //phi
        lat *= Mathf.Rad2Deg;
        lon *= Mathf.Rad2Deg;
        return new Vector2(lat, lon);
    }

   
}
