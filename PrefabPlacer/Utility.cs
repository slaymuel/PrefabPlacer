using System;
using UnityEngine;

namespace PrefabPlacer
{
    class Utility
    {
        /*                                 MAP ONTO ALL                                  */
        static Action<GameObject> lambda;
        public static void MapOntoAll(GameObject parent, Action<GameObject> l)
        {
            lambda = l;
            RecursiveMap(parent);
        }

        private static void RecursiveMap(GameObject parent)
        {
            lambda(parent);
            foreach (Transform t in parent.transform)
            {
                RecursiveMap(t.gameObject);
            }
        }
        //////////////////////////////////////////////////////////////////////////////////////

        /*                                 RANDOM VECTOR3                                  */
        public static Vector3 RandomVector(Vector2 xRange, Vector2 yRange, Vector2 zRange, Vector2 aspectXZRange, Vector2 aspectXYRange)
        {
            float x;
            float y;
            float z;

            //x has priority
            x = UnityEngine.Random.Range(xRange[0], xRange[1]);

            if (aspectXZRange[1] == 0f && aspectXZRange[0] == 0f)
            {
                z = UnityEngine.Random.Range(zRange[0], zRange[1]);
            }
            else
            {
                z = UnityEngine.Random.Range(x / aspectXZRange[1], x / aspectXZRange[0]);
            }

            if (aspectXYRange[1] == 0f && aspectXYRange[0] == 0f)
            {
                y = UnityEngine.Random.Range(yRange[0], yRange[1]);
            }
            else
            {
                y = UnityEngine.Random.Range(z / aspectXYRange[1], x / aspectXYRange[0]);
            }

            return new Vector3(x, y, z);
        }
        //////////////////////////////////////////////////////////////////////////////////////
    }
}
