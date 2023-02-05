using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DefaultNamespace;
using Innovative.Geometry;
using Innovative.SolarCalculator;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = System.Random;

public class ObservatoryAgent : Agent
{
    private Dictionary<string, GameObject> _planets = new Dictionary<string, GameObject>();
    private Problem _problem;
    private int _previousSampleSize;
    private List<HeatmapWrapper> _invalidHeatmaps;
    private List<HeatmapWrapper> _yearlyRewardHeatmaps;
    private Dictionary<int, List<HeatmapWrapper>> _monthlyRewardHeatmaps;

    public override void Initialize()
    {
        _problem = GeneratedPositionUtil.GETProblemFromCsv();
        InitializePlanetDict();
        InitializeHeatmaps();

        ResetScene();
    }
    
    private void InitializePlanetDict()
    {
        for (int i = 0; i < _problem.GeneratedPositions[0].Count; i++)
        {
            GameObject newPlanet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            newPlanet.transform.parent = transform;
            newPlanet.transform.name = _problem.GeneratedPositions[0][i].Name;
            _planets.Add(_problem.GeneratedPositions[0][i].Name, newPlanet);
        }
    }

    private void InitializeHeatmaps()
    {
        _invalidHeatmaps = new List<HeatmapWrapper>();
        DirectoryInfo invalidDirectory = new DirectoryInfo(Application.dataPath + "/Resources/InvalidHeatmap/");
        FileInfo[] invalidFiles = invalidDirectory.GetFiles("*.png");
        foreach (var file in invalidFiles)
        {
            _invalidHeatmaps.Add(new HeatmapWrapper(file.FullName));
        }
        
        _yearlyRewardHeatmaps = new List<HeatmapWrapper>();
        DirectoryInfo yearlyRewardDirectory = new DirectoryInfo(Application.dataPath + "/Resources/RewardHeatmap/Yearly");
        FileInfo[] yearlyRewardFiles = yearlyRewardDirectory.GetFiles("*.png");
        foreach (var file in yearlyRewardFiles)
        {
            _yearlyRewardHeatmaps.Add(new HeatmapWrapper(file.FullName));
        }

        _monthlyRewardHeatmaps = new Dictionary<int, List<HeatmapWrapper>>();
        for (int i = 1; i <= 12; i++)
        {
            DirectoryInfo monthlyRewardDirectory = new DirectoryInfo(Application.dataPath + "/Resources/RewardHeatmap/Monthly/" + i);
            FileInfo[] monthlyRewardFiles = monthlyRewardDirectory.GetFiles("*.png");
            List<HeatmapWrapper> monthlyHeatmaps = new List<HeatmapWrapper>();
            foreach (var file in monthlyRewardFiles)
            {
                monthlyHeatmaps.Add(new HeatmapWrapper(file.FullName));
            }
            _monthlyRewardHeatmaps.Add(i, monthlyHeatmaps);
        }
    }
    
    private void ResetScene()
    {
        ResetObservatories();
    }

    private void ResetObservatories()
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
        
        int gridY = actions.DiscreteActions[0];
        int gridX = actions.DiscreteActions[1];
        
        float plusLatitude = MathUtil.MapBetweenValues(-1, 1, 0, 10, action1);
        float plusLongitude = MathUtil.MapBetweenValues(-1, 1, 0, 10, action2);

        float latitude = 90 - gridY * 10 - plusLatitude;
        float longitude = -180 + gridX * 10 + plusLongitude;

        bool isInvalidPlacement = false;
        foreach (var invalidHeatmap in _invalidHeatmaps)
        {
            if (invalidHeatmap.isInvalidPlacementBasedOnGrayScale(latitude, longitude))
            {
                isInvalidPlacement = true;
                break;
            }
        }
        
        _problem.turnOnNextObservatory(MathUtil.LatLonToECEF(latitude, longitude),
            latitude, longitude, action1, action2, gridY, gridX, isInvalidPlacement);
        if (_problem.areAllObservatoriesOn())
        {
            CalculateRewardMultipliers();
            int sampleSize = _previousSampleSize == _problem.GeneratedPositions.Count
                ? _previousSampleSize
                : MathUtil.CalculateSampleSize(CompletedEpisodes, _problem.GeneratedPositions.Count);
            // float reward = CalculateReward() / _problem.GeneratedPositions.Count / _problem.getMaxPoints();
            float reward = CalculateReward(sampleSize) / sampleSize / _problem.getMaxPoints();
            // AddReward(_problem.GeneratedPositions.Count, reward);
            AddReward(sampleSize, reward);
            _previousSampleSize = sampleSize;
            EndEpisode();
        }
    }

    private void CalculateRewardMultipliers()
    {
        foreach (var observatory in _problem.Observatories)
        {
            foreach (var yearlyRewardHeatmap in _yearlyRewardHeatmaps)
            {
                observatory.YearlyRewardMultiplier *=
                    yearlyRewardHeatmap.GetMultiplierBasedOnGrayscale(observatory.Latitude, observatory.Longitude);
            }

            foreach (var rewardHeatmapsByMonth in _monthlyRewardHeatmaps)
            {
                foreach (var monthlyRewardHeatmap in rewardHeatmapsByMonth.Value)
                {
                    observatory.MultiplyMonthlyMultiplier(rewardHeatmapsByMonth.Key,
                        monthlyRewardHeatmap.GetMultiplierBasedOnGrayscale(observatory.Latitude, observatory.Longitude));
                }
            }
        }
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
                if (!observatory.IsInvalidPlacement)
                {
                    List<String> planetsInCone = GetAllPlanetsInCone(observatory, _problem.GeneratedPositions[index],
                        observatory.Angle);
                    distinctPlanetsSeen.AddRange(planetsInCone.Except(distinctPlanetsSeen));
                }
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
            Dictionary<string, float> distinctPlanetsSeen = new Dictionary<string, float>();
            List<ObservedPlanet> positions = _problem.GeneratedPositions[indices[index]];
            DateTime observationDate = positions[0].ObservationDate;
            SetPlanetsToPosition(positions);
            foreach (var observatory in _problem.Observatories)
            {
                if (!observatory.IsInvalidPlacement && new SolarTimes(observationDate, 0, observatory.Latitude, observatory.Longitude).SolarElevation <= new Angle(0))
                {
                    List<String> planetsInCone = GetAllPlanetsInCone(observatory, _problem.GeneratedPositions[indices[index]],
                        observatory.Angle);
                    foreach (var planet in planetsInCone)
                    {
                        float observatoryMultiplier = observatory.GetOverallMultiplierForMonth(observationDate.Month);
                        if (!distinctPlanetsSeen.ContainsKey(planet)|| distinctPlanetsSeen[planet] < observatoryMultiplier)
                        {
                            distinctPlanetsSeen[planet] = observatoryMultiplier;
                        } 
                    }
                }
            }
            
            distinctPlanetsSeen.Remove("earth");
            foreach (var planet in distinctPlanetsSeen)
            {
                reward += planet.Value;
            }
        }
        return reward;
    }
    
    private void SetPlanetsToPosition(List<ObservedPlanet> positions)
    {
        foreach (var position in positions)
        {
            _planets[position.Name].transform.position = new Vector3(position.X, position.Y, position.Z);
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
    
    private List<String> GetAllPlanetsInCone(Observatory observatory, List<ObservedPlanet> positions, int maxAngle)
    {
        Vector3 from = observatory.Location;
        List<String> planetsHit = new List<string>();

        foreach (var generatedPosition in positions)
        {
            if (generatedPosition.Importance != 0 && 
                IsPointInsideCone(generatedPosition.GETPosition(),from, (from - Vector3.zero).normalized, maxAngle))
            {
                RaycastHit hit;
                if (!Physics.Linecast(from, generatedPosition.GETPosition(), out hit)
                    || generatedPosition.Name.Equals(hit.collider.name))
                {
                    planetsHit.Add(generatedPosition.Name);
                }
            }
        }

        return planetsHit;
    }
    
    private static bool IsPointInsideCone(Vector3 point, Vector3 coneOrigin, Vector3 coneDirection, int maxAngle)
    {
        Vector3 pointDirection = (point - coneOrigin).normalized;
        var angle = Vector3.Angle(coneDirection, pointDirection);
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
            sensor.AddObservation(observatory.LatitudeAction);    
            sensor.AddObservation(observatory.LongitudeAction);
            sensor.AddObservation(observatory.GridY);
            sensor.AddObservation(observatory.GridX);
            sensor.AddObservation(observatory.IsInvalidPlacement);
        }
    }

    public override void OnEpisodeBegin()
    {
        ResetScene();
        base.OnEpisodeBegin();
    }
}
