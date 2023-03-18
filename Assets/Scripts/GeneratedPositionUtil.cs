using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace DefaultNamespace
{
    public class GeneratedPositionUtil
    {
        private static Problem problem = new Problem();
        
        public static Problem GETProblemFromCsv()
        {
            if (problem.Observatories.Count == 0)
            {
                GETGeneratedPositionsFromCsv();
            }

            return problem;
        }
        
        
        private static void GETGeneratedPositionsFromCsv()
        {
            using(var reader = new StreamReader(Application.dataPath + "/Resources/planetPositions.csv"))
            {
                int lineNumber = 0;
                List<float> diameters = new List<float>();
                List<float> importances = new List<float>();
                while (!reader.EndOfStream)
                {
                    List<ObservedPlanet> positionsForTheDay = new List<ObservedPlanet>();
                    var line = reader.ReadLine();
                    var values = line.Split(';');
                    if (lineNumber == 0)
                    {
                        for (int i = 0; i < values.Length; i++)
                        {
                            problem.AddObservatoryWithAngle(int.Parse(values[i].Trim()));
                        }
                    } else if (lineNumber == 1)
                    {
                        for (int i = 0; i < values.Length; i++)
                        {
                            diameters.Add(float.Parse(values[i].Trim().Replace('.', ',')));
                        }
                    } else if (lineNumber == 2)
                    {
                        for (int i = 0; i < values.Length; i++)
                        {
                            importances.Add(float.Parse(values[i].Trim().Replace('.', ',')));
                        }
                    }
                    else
                    {
                        string[] dateTimeValues = values[0].Split('-');
                        DateTime dateTime = new DateTime(int.Parse(dateTimeValues[0]), int.Parse(dateTimeValues[1]),
                            int.Parse(dateTimeValues[2]), int.Parse(dateTimeValues[3]), int.Parse(dateTimeValues[4]),
                            int.Parse(dateTimeValues[5]));
                        for (int i = 1; i < values.Length; i++)
                        {
                            int planetIndex = i - 1;
                            positionsForTheDay.Add(GenerateObservedPlanetFromCell(values[i].Split('/'),
                                diameters[planetIndex], i.ToString(), importances[planetIndex], dateTime));
                        }
                    
                        problem.GeneratedPositions.Add(positionsForTheDay);
                    }
                    lineNumber++;
                }
            }
        }

        private static ObservedPlanet GenerateObservedPlanetFromCell(string[] position, float diameter, string name,
            float importance, DateTime observationDate)
        {
            return new ObservedPlanet(name,
                float.Parse(position[0], CultureInfo.InvariantCulture),
                float.Parse(position[1], CultureInfo.InvariantCulture),
                float.Parse(position[2], CultureInfo.InvariantCulture), diameter, importance,
                observationDate);
        }
    }
}