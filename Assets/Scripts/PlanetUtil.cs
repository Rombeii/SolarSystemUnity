using System;
using System.Collections.Generic;

namespace DefaultNamespace
{
    public static class PlanetUtil
    {
        private const float SCALE = 1000000;
        
        public static Dictionary<String, float> planetSizes = new Dictionary<string, float>()
        {
            {"sun", 695700.0f * 2/ SCALE},
            {"mercury", 2439.7f * 2 / SCALE},
            {"venus", 6051.8f * 2 / SCALE},
            {"earth", 6371.0f * 2 / SCALE},
            {"moon", 1737.4f * 2 / SCALE},
            {"mars", 3389.5f * 2 / SCALE},
            {"jupiter", 69911.0f * 2 / SCALE},
            {"saturn", 58232.0f * 2 / SCALE},
            {"uranus", 25362.0f * 2 / SCALE},
            {"neptune", 24622.0f * 2 / SCALE},
            {"pluto", 1188.3f * 2 / SCALE},
        };
    }
}