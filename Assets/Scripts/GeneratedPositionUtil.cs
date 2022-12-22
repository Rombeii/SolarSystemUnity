using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DefaultNamespace
{
    public class GeneratedPositionUtil
    {
        private static Problem problem = new Problem();
        
        public static Problem getProblemFromCsv()
        {
            if (problem.Observatories.Count == 0)
            {
                getGeneratedPositionsFromCsv();
            }

            return problem;
        }
        
        
        private static void getGeneratedPositionsFromCsv()
        {
            using(var reader = new StreamReader(Application.dataPath + "/Resources/planetPositions.csv"))
            {
                int lineNumber = 0;
                List<float> diameters = new List<float>();
                List<float> importances = new List<float>();
                while (!reader.EndOfStream)
                {
                    List<Planet> positionsForTheDay = new List<Planet>();
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
                        for (int i = 0; i < values.Length; i++)
                        {
                            positionsForTheDay.Add(generatePlanetFromCell(values[i].Split('/'),
                                diameters[i], i.ToString(), importances[i]));
                        }
                    
                        problem.GeneratedPositions.Add(positionsForTheDay);
                    }
                    lineNumber++;
                }
            }
        }

        private static Planet generatePlanetFromCell(string[] position, float diameter, string name, float importance)
        {
            return new Planet(name,
                float.Parse(position[0].Trim().Replace('.', ',')),
                float.Parse(position[1].Trim().Replace('.', ',')),
                float.Parse(position[2].Trim().Replace('.', ',')), diameter, importance);
        }
    }
}