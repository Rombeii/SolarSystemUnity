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
    private Dictionary<int, List<int>> _fullWhiteCells;
    private List<HeatmapWrapper> _yearlyRewardHeatmaps;
    private Dictionary<int, List<HeatmapWrapper>> _monthlyRewardHeatmaps;

    private const int NumberOfCols = 36;
    private const int NumberOfRows = 18;
    private float _cellWidth;
    private float _cellHeight;

    private bool _useInvalidateHeatmaps;
    private bool _useRewardHeatmaps;
    private bool _useSolarElevation;
    private bool _useMinibatching;
    private float _minLat;
    private float _minLong;
    private float _maxLat;
    private float _maxLong;

    public override void Initialize()
    {
        _problem = GeneratedPositionUtil.GETProblemFromCsv();
        InitializeEnvironmentParameters();
        InitializePlanetDict();
        InitializeHeatmaps();
        
        List<Vector3> pts = MathUtil.GetEquidistantPointsOnSphere(16384, 0.00012f);

        ResetScene();
    }

    private void InitializeEnvironmentParameters()
    {
        EnvironmentParameters parameters = Academy.Instance.EnvironmentParameters;
        _useInvalidateHeatmaps = parameters.GetWithDefault("use_invalidate_heatmaps", 0) == 1;
        _useRewardHeatmaps = parameters.GetWithDefault("use_reward_heatmaps", 0) == 1;
        _useSolarElevation = parameters.GetWithDefault("use_solar_elevation", 0) == 1;
        _useMinibatching = parameters.GetWithDefault("use_minibatching", 0) == 1;
        
        _minLat = parameters.GetWithDefault("min_lat", -90);
        _minLong = parameters.GetWithDefault("min_long", -180);
        _maxLat = parameters.GetWithDefault("max_lat", 90f);
        _maxLong = parameters.GetWithDefault("max_long", 180f);
        
        _cellWidth = (_maxLong - _minLong) / NumberOfCols;
        _cellHeight = (_maxLat - _minLat) / NumberOfRows;
    }
    
    private void InitializePlanetDict()
    {
        for (int i = 0; i < _problem.Observations[0].GETObservedObjects().Count; i++)
        {
            GameObject newObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            newObject.transform.parent = transform;
            newObject.transform.name = _problem.Observations[0].GETObservedObjectAt(i).Name;
            float diameter = _problem.Observations[0].GETObservedObjectAt(i).Diameter;
            newObject.transform.localScale = new Vector3(diameter, diameter, diameter);
            _planets.Add(_problem.Observations[0].GETObservedObjectAt(i).Name, newObject);
        }
    }

    private void InitializeHeatmaps()
    {
        InitializeInvalidHeatmaps();
        InitializeRewardHeatmaps();
    }

    private void InitializeInvalidHeatmaps()
    {
        _fullWhiteCells = new Dictionary<int, List<int>>();
        for (int rowIndex = 0; rowIndex < NumberOfRows; rowIndex++)
        {
            _fullWhiteCells[rowIndex] = new List<int>();
        }
        
        _invalidHeatmaps = new List<HeatmapWrapper>();
        if(_useInvalidateHeatmaps)
        {
            DirectoryInfo invalidDirectory = new DirectoryInfo(Application.dataPath + "/Resources/InvalidHeatmap/");
            FileInfo[] invalidFiles = invalidDirectory.GetFiles("*.png");
            foreach (var file in invalidFiles)
            {
                _invalidHeatmaps.Add(new HeatmapWrapper(file.FullName, _minLat, _minLong, _maxLat, _maxLong));
            }
        }

        for (int rowNum = 0; rowNum < NumberOfRows; rowNum++)
        {
            for (int colNum = 0; colNum < NumberOfCols; colNum++)
            {
                foreach (var invalidHeatmap in _invalidHeatmaps)
                {
                    if (invalidHeatmap.AreAllPixelsWhiteInCell(rowNum, colNum, NumberOfRows, NumberOfCols))
                    {
                        _fullWhiteCells[rowNum].Add(colNum);
                        break;
                    }          
                }
            }
        }
    }
    
    private void InitializeRewardHeatmaps()
    {
        _yearlyRewardHeatmaps = new List<HeatmapWrapper>();
        _monthlyRewardHeatmaps = new Dictionary<int, List<HeatmapWrapper>>();
        
        if (_useRewardHeatmaps)
        {
            DirectoryInfo yearlyRewardDirectory = new DirectoryInfo(Application.dataPath + "/Resources/RewardHeatmap/Yearly");
            FileInfo[] yearlyRewardFiles = yearlyRewardDirectory.GetFiles("*.png");
            foreach (var file in yearlyRewardFiles)
            {
                _yearlyRewardHeatmaps.Add(new HeatmapWrapper(file.FullName, _minLat, _minLong, _maxLat, _maxLong));
            }

            for (int i = 1; i <= 12; i++)
            {
                DirectoryInfo monthlyRewardDirectory = new DirectoryInfo(Application.dataPath + "/Resources/RewardHeatmap/Monthly/" + i);
                FileInfo[] monthlyRewardFiles = monthlyRewardDirectory.GetFiles("*.png");
                List<HeatmapWrapper> monthlyHeatmaps = new List<HeatmapWrapper>();
                foreach (var file in monthlyRewardFiles)
                {
                    monthlyHeatmaps.Add(new HeatmapWrapper(file.FullName, _minLat, _minLong, _maxLat, _maxLong));
                }
                _monthlyRewardHeatmaps.Add(i, monthlyHeatmaps);
            }
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
        float latitudeAction = actions.ContinuousActions[0];
        float longitudeAction = actions.ContinuousActions[1];
        
        int gridY = actions.DiscreteActions[0];
        int gridX = actions.DiscreteActions[1];
        
        float plusLatitude = MathUtil.MapBetweenValues(-1, 1, 0, _cellHeight, latitudeAction);
        float plusLongitude = MathUtil.MapBetweenValues(-1, 1, 0, _cellWidth, longitudeAction);

        float latitude = _maxLat - gridY * _cellHeight - plusLatitude;
        float longitude = _minLong + gridX * _cellWidth + plusLongitude;

        bool isInvalidPlacement = isInvalidPlacementBasedOnHeatmaps(latitude, longitude, gridX, gridY);
        _problem.turnOnNextObservatory(MathUtil.LatLonToECEF(latitude, longitude),
            latitude, longitude, latitudeAction, longitudeAction, gridY, gridX, isInvalidPlacement);
        
        if (_problem.areAllObservatoriesOn())
        {
            CalculateRewardMultipliers();
            CalculateReward();
            EndEpisode();
        }
    }

    private bool isInvalidPlacementBasedOnHeatmaps(float latitude, float longitude, int gridX, int gridY)
    {
        if (!_useInvalidateHeatmaps)
        {
            return false;
        }

        if (_fullWhiteCells[gridY].Contains(gridX))
        {
            return true;
        }
        
        bool isInvalidPlacement = false;
        foreach (var invalidHeatmap in _invalidHeatmaps)
        {
            if (invalidHeatmap.isInvalidPlacementBasedOnGrayScale(latitude, longitude))
            {
                isInvalidPlacement = true;
                break;
            }
        }

        return isInvalidPlacement;
    }
    
    private void CalculateRewardMultipliers()
    {
        if (!_useRewardHeatmaps)
        {
            return;
        }
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
    
    private void CalculateReward()
    {
        int sampleSize = CalculateSampleSizeIfNeeded();
        float reward;
        if (sampleSize == _problem.Observations.Count)
        {
            reward = CalculateRewardBasedOnIndices(Enumerable.Range(0, _problem.Observations.Count).ToList());
        }
        else
        {
            List<int> indices = GetRandomNumber(0, _problem.Observations.Count, sampleSize);
            reward = CalculateRewardBasedOnIndices(indices);
        }
        AddReward(sampleSize, reward / sampleSize / _problem.getMaxPoints());
        _previousSampleSize = sampleSize;
    }

    private int CalculateSampleSizeIfNeeded()
    {
        if (!_useMinibatching || _previousSampleSize == _problem.Observations.Count)
        {
            return _problem.Observations.Count;
        }
        else
        {
            return MathUtil.CalculateSampleSize(CompletedEpisodes, _problem.Observations.Count);
        }
    }
    
    private float CalculateRewardBasedOnIndices(List<int> indices)
    {
        float reward = 0;
        foreach (var index in indices)
        {
            Dictionary<string, float> distinctPlanetsSeen = new Dictionary<string, float>();
            List<ObservedObject> positions = _problem.Observations[index].GETObservedObjects();
            DateTime observationDate = _problem.Observations[index].GETObservationDate();
            SetPlanetsToPosition(positions);
            foreach (var observatory in _problem.Observatories)
            {
                if (!observatory.IsInvalidPlacement && !IsSunUp(observationDate, observatory))
                {
                    List<String> planetsInCone = GetAllPlanetsInCone(observatory, positions, observatory.Angle);
                    foreach (var planet in planetsInCone)
                    {
                        float observatoryMultiplier = observatory.GetOverallMultiplierForMonth(observationDate.Month);
                        if (!distinctPlanetsSeen.ContainsKey(planet)
                            || _useRewardHeatmaps && distinctPlanetsSeen[planet] < observatoryMultiplier)
                        {
                            distinctPlanetsSeen[planet] = observatoryMultiplier;
                        } 
                    }
                }
            }
            
            distinctPlanetsSeen.Remove("earth");
            foreach (var planet in distinctPlanetsSeen)
            {
                reward += planet.Value * positions.Find(p => p.Name == planet.Key).Importance;
            }
        }
        return reward;
    }
    
    private void SetPlanetsToPosition(List<ObservedObject> positions)
    {
        foreach (var position in positions)
        {
            _planets[position.Name].transform.position = new Vector3(position.X, position.Y, position.Z);
        }
    }

    private bool IsSunUp(DateTime observationDate, Observatory observatory)
    {
        SolarTimes solarTimes = new SolarTimes(observationDate, 0, observatory.Latitude, observatory.Longitude);
        return _useSolarElevation && solarTimes.SolarElevation + solarTimes.AtmosphericRefraction >= new Angle(0);
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
    
    private List<String> GetAllPlanetsInCone(Observatory observatory, List<ObservedObject> positions, int maxAngle)
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
