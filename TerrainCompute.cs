using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TerrainCompute : Terrain
{
    public ComputeShader erosion;

    public TerrainCompute(ComputeShader eroder)
    {
        erosion = eroder;
    }

    struct ErosionRegionCompute
    {
        public Vector3 position;

        public float elevation;

        public int numAdjRegions;

        public int adjacentRegion1;
        public int adjacentRegion2;
        public int adjacentRegion3;
        public int adjacentRegion4;
        public int adjacentRegion5;
        public int adjacentRegion6;
    };

    public void SimulateErosionComputeShader(int numIterations)
    {
        GenerateHexagon();

        int numThreads = System.Math.Max(numIterations / 1024, 1);
        ErosionRegion[] values = erosionRegions.Values.ToArray();
        Dictionary<ErosionRegion, int> regionMap = new Dictionary<ErosionRegion, int>();
        for (int i = 0; i < values.Length; i++)
        {
            regionMap.Add(values[i], i);
        }

        int[] randomRegionIndicies = new int[numIterations];
        Random.InitState(0);
        for (int i = 0; i < numIterations; i++)
        {
            randomRegionIndicies[i] = Random.Range(0, values.Length);
        }
        ComputeBuffer randomBuffer = new ComputeBuffer(numIterations, sizeof(int));
        randomBuffer.SetData(randomRegionIndicies);
        erosion.SetBuffer(0, "random", randomBuffer);

        ComputeBuffer regionBuffer = new ComputeBuffer(values.Length, sizeof(float) * 4 + sizeof(int) * 7);

        ErosionRegionCompute[] computeRegions = new ErosionRegionCompute[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            ErosionRegion region = values[i];
            ErosionRegionCompute r = new ErosionRegionCompute
            {
                position = new Vector3(region.Center.X, region.Center.Y, region.Center.Z),
                elevation = region.Elevation
            };
            for (int j = 0; j < 6; j++)
            {
                ErosionRegion erosionRegion = region;
                try
                {
                    erosionRegion = region.AdjacentRegions[j];
                }
                catch (System.Exception)
                {
                    // Less than 6 adjacent regions, ignore
                }

                int index = regionMap[erosionRegion];
                switch (j)
                {
                    case 0: r.adjacentRegion1 = index; break;
                    case 1: r.adjacentRegion2 = index; break;
                    case 2: r.adjacentRegion3 = index; break;
                    case 3: r.adjacentRegion4 = index; break;
                    case 4: r.adjacentRegion5 = index; break;
                    case 5: r.adjacentRegion6 = index; break;
                }
            }
            r.numAdjRegions = region.AdjacentRegions.Count;
            computeRegions[i] = r;
        }

        regionBuffer.SetData(computeRegions);
        erosion.SetBuffer(0, "regions", regionBuffer);

        erosion.SetInt("lifetime", WaterDroplet.Settings.lifetime);
        erosion.SetFloat("inertia", WaterDroplet.Settings.inertia);
        erosion.SetFloat("gravity", WaterDroplet.Settings.gravity);
        erosion.SetFloat("evaporation", WaterDroplet.Settings.evaporation);
        erosion.SetFloat("capacity", WaterDroplet.Settings.capacity);
        erosion.SetFloat("erosion", WaterDroplet.Settings.erosion);
        erosion.SetFloat("deposition", WaterDroplet.Settings.deposition);
        erosion.SetFloat("minErosion", WaterDroplet.Settings.minErosion);

        erosion.Dispatch(0, numThreads, 1, 1);

        regionBuffer.GetData(computeRegions);

        for (int i = 0; i < computeRegions.Length; i++)
        {
            values[i].Elevation = computeRegions[i].elevation;
        }

        regionBuffer.Release();
        randomBuffer.Release();
    }
}
