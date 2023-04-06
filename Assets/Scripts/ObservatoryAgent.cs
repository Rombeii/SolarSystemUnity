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
    private Dictionary<string, GameObject> _observedObjects = new Dictionary<string, GameObject>();
    private Problem _problem;
    private int _previousSampleSize;
    private List<HeatmapWrapper> _invalidHeatmaps;
    private Dictionary<int, List<int>> _fullWhiteCells;
    private List<HeatmapWrapper> _yearlyRewardHeatmaps;
    private Dictionary<int, List<HeatmapWrapper>> _monthlyRewardHeatmaps;

    private const float EarthRadius = 0.006371f;
    private const int NumberOfCols = 36;
    private const int NumberOfRows = 18;
    private float _cellWidth;
    private float _cellHeight;

    private bool _useInvalidateHeatmaps;
    private bool _useRewardHeatmaps;
    private bool _useSolarElevation;
    private bool _useMinibatching;
    private bool _checkIfObjectsAreCovered;
    
    private float _minLat;
    private float _minLong;
    private float _maxLat;
    private float _maxLong;

    private float _minDistanceForTriangulation;
    private float _maxDistanceForTriangulation;

    private bool _createPointsInASphere;
    private int _numberOfSpheres;
    private float _sphereDistance;

    public override void Initialize()
    {
        _problem = GeneratedPositionUtil.GETProblemFromCsv();
        InitializeEnvironmentParameters();
        CreatePointsInASphere();
        InitializeObservedObjectDict();
        InitializeHeatmaps();
        ResetScene();
    }

    private void CreatePointsInASphere()
    {
        if (_createPointsInASphere)
        {
            List<Vector3> pts = MathUtil.GetEquidistantPointsOnSphere(_numberOfSpheres, _sphereDistance + EarthRadius);
            foreach (var observation in _problem.Observations)
            {
                for (int i = 0; i < pts.Count; i++)
                {
                    Vector3 point = pts[i];
                    observation.AddObservedObject(new ObservedObject("Sphere" + i, point.x, point.y, point.z,
                        0.0001f, 1));
                }
            } 
        }
    }

    private void InitializeEnvironmentParameters()
    {
        EnvironmentParameters parameters = Academy.Instance.EnvironmentParameters;
        _useInvalidateHeatmaps = parameters.GetWithDefault("use_invalidate_heatmaps", 0) == 1;
        _useRewardHeatmaps = parameters.GetWithDefault("use_reward_heatmaps", 0) == 1;
        _useSolarElevation = parameters.GetWithDefault("use_solar_elevation", 0) == 1;
        _useMinibatching = parameters.GetWithDefault("use_minibatching", 0) == 1;
        _checkIfObjectsAreCovered = parameters.GetWithDefault("check_if_objects_are_covered", 1) == 1;
        
        _createPointsInASphere = parameters.GetWithDefault("create_points_in_a_sphere", 0) == 1;
        _numberOfSpheres = (int) parameters.GetWithDefault("number_of_spheres", 16384);
        _sphereDistance = parameters.GetWithDefault("sphere_distance", 0.00012f);
        
        _minLat = parameters.GetWithDefault("min_lat", -90);
        _minLong = parameters.GetWithDefault("min_long", -180);
        _maxLat = parameters.GetWithDefault("max_lat", 90f);
        _maxLong = parameters.GetWithDefault("max_long", 180f);
        
        _minDistanceForTriangulation = parameters.GetWithDefault("min_distance_for_triangulation", 0f);
        _maxDistanceForTriangulation = parameters.GetWithDefault("max_distance_for_triangulation", Mathf.Infinity);
        
        _cellWidth = (_maxLong - _minLong) / NumberOfCols;
        _cellHeight = (_maxLat - _minLat) / NumberOfRows;
    }
    
    private void InitializeObservedObjectDict()
    {
        for (int i = 0; i < _problem.Observations[0].GETObservedObjects().Count; i++)
        {
            GameObject newObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            newObject.transform.parent = transform;
            newObject.transform.name = _problem.Observations[0].GETObservedObjectAt(i).Name;
            float diameter = _problem.Observations[0].GETObservedObjectAt(i).Diameter;
            newObject.transform.localScale = new Vector3(diameter, diameter, diameter);
            _observedObjects.Add(_problem.Observations[0].GETObservedObjectAt(i).Name, newObject);
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
        ResetNonStaticObservatories();
    }

    private void ResetNonStaticObservatories()
    {
        foreach (var observatory in _problem.Observatories)
        {
            if (!observatory.IsStatic)
            {
                observatory.Reset();    
            }
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

    bool CheckIfObservatoriesWithinDistance(List<Observatory> observatories, float minDistance, float maxDistance)
    {
        
        // Check if there are at least 3 Observatories in the list
        if (observatories.Count < 3) {
            return false;
        }
        
        // Loop through all possible combinations of 3 Observatories
        for (int i = 0; i < observatories.Count - 2; i++)
        {
            for (int j = i + 1; j < observatories.Count - 1; j++)
            {
                for (int k = j + 1; k < observatories.Count; k++)
                {
                    // Calculate the distances between the 3 Obervatories
                    float distance1 = Vector3.Distance(observatories[i].Location, observatories[j].Location);
                    float distance2 = Vector3.Distance(observatories[i].Location, observatories[k].Location);
                    float distance3 = Vector3.Distance(observatories[j].Location, observatories[k].Location);

                    // Check if all distances are between the minDistance and maxDistance
                    if (distance1 >= minDistance && distance1 <= maxDistance &&
                        distance2 >= minDistance && distance2 <= maxDistance &&
                        distance3 >= minDistance && distance3 <= maxDistance)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private bool UseTriangulation()
    {
        return _minDistanceForTriangulation != 0f || !float.IsPositiveInfinity(_maxDistanceForTriangulation);
    }
    
    private void CalculateRewardMultipliers()
    {
        if (!_useRewardHeatmaps)
        {
            return;
        }
        foreach (var observatory in _problem.Observatories)
        {
            if (!observatory.IsInvalidPlacement)
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
                            monthlyRewardHeatmap.GetMultiplierBasedOnGrayscale(observatory.Latitude,
                                observatory.Longitude));
                    }
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
            Dictionary<string, List<KeyValuePair<int, float>>> objectsSeen = new Dictionary<string, List<KeyValuePair<int, float>>>();
            List<ObservedObject> positions = _problem.Observations[index].GETObservedObjects();
            DateTime observationDate = _problem.Observations[index].GETObservationDate();
            SetObjectsToPosition(positions);
            foreach (var observatory in _problem.Observatories)
            {
                if (!observatory.IsInvalidPlacement && !IsSunUp(observationDate, observatory))
                {
                    List<String> objectsInCone = GetAllObjectsInCone(observatory, positions, observatory.Angle);
                    foreach (var observedObject in objectsInCone)
                    {
                        float observatoryMultiplier = observatory.GetOverallMultiplierForMonth(observationDate.Month);
                        if (!objectsSeen.ContainsKey(observedObject))
                        {
                            objectsSeen[observedObject] = new List<KeyValuePair<int, float>>();
                        }
                        objectsSeen[observedObject].Add(new KeyValuePair<int, float>(_problem.Observatories.IndexOf(observatory), observatoryMultiplier));
                    }
                }
            }
            
            objectsSeen.Remove("earth");
            foreach (var observedObject in objectsSeen)
            {
                if (!UseTriangulation() || CheckIfObservatoriesWithinDistance(
                    observedObject.Value.Select(pair => _problem.Observatories[pair.Key]).ToList(),
                    _minDistanceForTriangulation, _maxDistanceForTriangulation))
                {
                    reward += observedObject.Value.Max(pair => pair.Value) * positions.Find(p => p.Name == observedObject.Key).Importance;
                }
            }
        }
        return reward;
    }
    
    private void SetObjectsToPosition(List<ObservedObject> positions)
    {
        foreach (var position in positions)
        {
            _observedObjects[position.Name].transform.position = new Vector3(position.X, position.Y, position.Z);
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
    
    private List<String> GetAllObjectsInCone(Observatory observatory, List<ObservedObject> positions, int maxAngle)
    {
        Vector3 from = observatory.Location;
        List<String> objectsHit = new List<string>();

        foreach (var generatedPosition in positions)
        {
            if (generatedPosition.Importance != 0 && 
                MathUtil.IsPointInsideCone(generatedPosition.GETPosition(),from,
                    (from - Vector3.zero).normalized, maxAngle))
            {
                RaycastHit hit;
                if (!_checkIfObjectsAreCovered
                    || !Physics.Linecast(from, generatedPosition.GETPosition(), out hit)
                    || generatedPosition.Name.Equals(hit.collider.name))
                {
                    objectsHit.Add(generatedPosition.Name);
                }
            }
        }

        return objectsHit;
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
