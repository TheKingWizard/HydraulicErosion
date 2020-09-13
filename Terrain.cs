using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System;

public class Terrain
{
    public UnityEngine.ComputeShader erosion;

    private static readonly float phi = (float)((1.0 + Math.Sqrt(5)) / 2.0);
    private static List<Vector3> initialVertices = new List<Vector3>();
    private static HashSet<Triangle> initialTriangles = new HashSet<Triangle>();
    public static List<Triangle> triangles = new List<Triangle>();
    private static Dictionary<Vector3, HashSet<Triangle>> mapping = new Dictionary<Vector3, HashSet<Triangle>>();

    public Dictionary<Vector3, List<Triangle>> triangleMapping = new Dictionary<Vector3, List<Triangle>>();
    public Dictionary<Vector3, ErosionRegion> regions = new Dictionary<Vector3, ErosionRegion>();

    public List<River> rivers = new List<River>();

    public Terrain(UnityEngine.ComputeShader eroder)
    {
        erosion = eroder;
        GenerateHexagon();
        //GenerateIcosahderon();

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        //SimulateErosion(1024 * 64);
        //SimulateErosionComputeShader(1024 * 64);
        stopwatch.Stop();
        UnityEngine.Debug.Log("Time Elapsed: " + stopwatch.ElapsedMilliseconds);

        return;
        List<Region> r = new List<Region>();
        foreach (var item in regions.Values)
        {
            r.Add(item.Region);
        }

        rivers = RiverGenerator.GenerateRivers(r);
    }

    public Terrain()
    {
        GenerateHexagon();
        //GenerateIcosahderon();

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        //SimulateErosion(100000);
        stopwatch.Stop();
        UnityEngine.Debug.Log("Time Elapsed: " + stopwatch.ElapsedMilliseconds);

        return;
        List<Region> r = new List<Region>();
        foreach (var item in regions.Values)
        {
            r.Add(item.Region);
        }

        rivers = RiverGenerator.GenerateRivers(r);
    }

    private void GenerateHexagon()
    {
        Initialize();
        Subdivide(8);
        MapTriangles();
        regions = CreateErosionRegions(CreateHexRegions());
        MapElevation();
    }

    void Initialize()
    {
        Vector3 top = new Vector3(0, 1, 0);
        Vector3 bottom = new Vector3(0, -1, 0);
        Vector3 left = new Vector3(-1, 0, 0);
        Vector3 right = new Vector3(1, 0, 0);
        Vector3 topRight = new Vector3(1, 1, 0);
        Vector3 topLeft = new Vector3(-1, 1, 0);
        Vector3 bottomRight = new Vector3(1, -1, 0);   
        Vector3 bottomLeft = new Vector3(-1, -1, 0);
        Vector3 center = new Vector3(0, 0, 0);

        Triangle a = new Triangle(center, topLeft, top);
        Triangle b = new Triangle(center, top, topRight);
        Triangle c = new Triangle(center, topRight, right);
        Triangle d = new Triangle(center, right, bottomRight);
        Triangle e = new Triangle(center, bottomRight, bottom);
        Triangle f = new Triangle(center, bottom, bottomLeft);
        Triangle g = new Triangle(center, bottomLeft, left);
        Triangle h = new Triangle(center, left, topLeft);

        Vector3 A = new Vector3(0, 0, 0);
        Vector3 B = new Vector3(1f, 0, 0);
        Vector3 C = new Vector3(1f / 2f, -(float)System.Math.Sqrt(3) / 2f, 0);
        Vector3 D = new Vector3(-1f / 2f, -(float)System.Math.Sqrt(3) / 2f, 0);
        Vector3 E = new Vector3(-1f, 0, 0);
        Vector3 F = new Vector3(-1f / 2f, (float)System.Math.Sqrt(3) / 2f, 0);
        Vector3 G = new Vector3(1f / 2f, (float)System.Math.Sqrt(3) / 2f, 0);

        Triangle one = new Triangle(A, B, C);
        Triangle two = new Triangle(A, C, D);
        Triangle three = new Triangle(A, D, E);
        Triangle four = new Triangle(A, E, F);
        Triangle five = new Triangle(A, F, G);
        Triangle six = new Triangle(A, G, B);

        triangles.Add(one);
        triangles.Add(two);
        triangles.Add(three);
        triangles.Add(four);
        triangles.Add(five);
        triangles.Add(six);
    }

    private void Subdivide(int numSubdivisions)
    {
        for (int i = 0; i < numSubdivisions; i++)
        {
            List<Triangle> newTrianlges = new List<Triangle>();
            foreach (Triangle triangle in triangles)
            {
                newTrianlges.AddRange(triangle.Subdivide());
            }
            triangles = newTrianlges;
        }
    }

    private void MapTriangles()
    {
        foreach (Triangle triangle in triangles)
        {
            foreach (Vector3 vertex in triangle.Vertices)
            {
                if (!triangleMapping.ContainsKey(vertex))
                    triangleMapping.Add(vertex, new List<Triangle>());
                triangleMapping[vertex].Add(triangle);
            }
        }
    }

    private List<Region> CreateHexRegions()
    {
        Dictionary<Vector3, Region> regionMapping = new Dictionary<Vector3, Region>();
        foreach (KeyValuePair<Vector3, List<Triangle>> kvp in triangleMapping)
        {
            regionMapping.Add(kvp.Key, new Region(kvp.Key, kvp.Value));
        }
        foreach (Region region in regionMapping.Values)
        {

            HashSet<Region> adjacentRegions = new HashSet<Region>();
            foreach (Triangle triangle in region.Triangles)
            {
                foreach(Vector3 vertex in triangle.Vertices)
                {
                    adjacentRegions.Add(regionMapping[vertex]);
                }
            }
            adjacentRegions.Remove(region);
            region.AdjacentRegions = adjacentRegions.ToList();
        }
        return regionMapping.Values.ToList();
    }

    private Dictionary<Vector3, ErosionRegion> CreateErosionRegions(List<Region> regions)
    {
        Dictionary<Vector3, ErosionRegion> regionMapping = new Dictionary<Vector3, ErosionRegion>();
        foreach (Region region in regions)
        {
            regionMapping.Add(region.Center, new ErosionRegion(region));
        }
        foreach (ErosionRegion region in regionMapping.Values)
        {
            foreach (Region innerRegion in region.Region.AdjacentRegions)
            {
                region.AdjacentRegions.Add(regionMapping[innerRegion.Center]);
            }
        }
        return regionMapping;
    }

    private void MapElevation()
    {
        Noise noise = new Noise();
        noise.SetParameters(8, 0.5f, 0.25f, 1f, 2f);
        foreach (ErosionRegion region in regions.Values)
        {
            region.Elevation = (noise.Evaluate(region.Center) + 1f) / 2f;
            //region.Elevation = 1;
        }
    }

    public void SimulateErosion(int numIterations)
    {
        ErosionRegion[] values = regions.Values.ToArray();
        Parallel.For(0, numIterations, new ParallelOptions { MaxDegreeOfParallelism = 32 }, (i) =>
        {
            System.Random random = new System.Random(i);
            WaterDroplet waterDroplet = new WaterDroplet(values[(random.Next(regions.Count))]);
            waterDroplet.Simulate();
        });

        /*for (int i = 0; i < numIterations; i++)
        {
            System.Random random = new System.Random(i);
            WaterDroplet waterDroplet = new WaterDroplet(regions.ElementAt(random.Next(regions.Count)).Value);
            waterDroplet.Simulate();
        }*/
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
        int numThreads = numIterations / 1024;
        ErosionRegion[] values = regions.Values.ToArray();
        Dictionary<ErosionRegion, int> regionMap = new Dictionary<ErosionRegion, int>();
        for (int i = 0; i < values.Length; i++)
        {
            regionMap.Add(values[i], i);
        }

        int[] randomRegionIndicies = new int[numIterations];
        Random random = new Random(0);
        for (int i = 0; i < numIterations; i++)
        {
            randomRegionIndicies[i] = random.Next(values.Length);
        }
        UnityEngine.ComputeBuffer randomBuffer = new UnityEngine.ComputeBuffer(numIterations, sizeof(int));
        randomBuffer.SetData(randomRegionIndicies);
        erosion.SetBuffer(0, "random", randomBuffer);

        UnityEngine.ComputeBuffer regionBuffer = new UnityEngine.ComputeBuffer(values.Length, sizeof(float) *4 + sizeof(int) *7);

        ErosionRegionCompute[] computeRegions = new ErosionRegionCompute[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            ErosionRegion region = values[i];
            ErosionRegionCompute r = new ErosionRegionCompute();
            r.position = region.Center;
            r.elevation = region.Elevation;
            for (int j = 0; j < 6; j++)
            {
                ErosionRegion erosionRegion = region;
                try
                {
                    erosionRegion = region.AdjacentRegions[j];
                }
                catch (Exception e)
                {
                    // Less than 6 adjacent regions
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

        erosion.SetInt("lifetime", 100);
        erosion.SetFloat("inertia", 0.5f);
        erosion.SetFloat("gravity", 4.0f);
        erosion.SetFloat("evaporation", 0.05f);
        erosion.SetFloat("capacity", 2.0f);
        erosion.SetFloat("erosion", 0.3f);
        erosion.SetFloat("deposition", 0.01f);
        erosion.SetFloat("minErosion", 0.01f);
        
        erosion.Dispatch(0, numThreads, 1, 1);

        regionBuffer.GetData(computeRegions);
        
        for (int i = 0; i < computeRegions.Length; i++)
        {
            values[i].Elevation = computeRegions[i].elevation;
        }

        regionBuffer.Release();
        randomBuffer.Release();

    }

    private ErosionRegion FindNextLocation(Vector3 newDirection, ErosionRegion region)
    {
        ErosionRegion newPosition = region;
        float maxCosTheta = float.MinValue;
        foreach (ErosionRegion adjacentRegion in region.AdjacentRegions)
        {
            Vector3 direction = Vector3.Normalize(adjacentRegion.Center - region.Center);
            float cosTheta = Vector3.Dot(direction, newDirection);
            if (cosTheta > maxCosTheta)
            {
                maxCosTheta = cosTheta;
                newPosition = adjacentRegion;
            }
        }
        return newPosition;
    }

    private Vector3 GetGradient(ErosionRegion region)
    {
        Dictionary<Vector3, float> heightDiffs = new Dictionary<Vector3, float>();
        float totalHeightDiff = 0;

        foreach (ErosionRegion adjacentNode in region.AdjacentRegions)
        {
            float heightDiff = adjacentNode.Elevation - region.Elevation;
            heightDiffs.Add(Vector3.Normalize(adjacentNode.Center - region.Center), heightDiff);
            totalHeightDiff += Math.Abs(heightDiff);
        }

        Vector3 gradient = new Vector3(0, 0, 0);
        foreach (Vector3 direction in heightDiffs.Keys)
        {
            gradient += direction * heightDiffs[direction] / totalHeightDiff;
        }

        if (gradient == Vector3.Zero || totalHeightDiff == 0)
        {
            return gradient;
        }

        return Vector3.Normalize(gradient);
    }

    public void GenerateIcosahderon()
    {
        Clear();
        InitializeVertices();
        CreateInitialTriangles();
        NormalizeTriangles();
        SubdivideIcosahedron(4);
        GenerateTriangleMapping();
        regions = CreateErosionRegions(CreateRegions().Values.ToList());
        UnityEngine.Debug.Log(regions.Count);
        MapElevation();
    }
    private static void Clear()
    {
        initialVertices.Clear();
        initialTriangles.Clear();
        triangles.Clear();
        mapping.Clear();
    }

    private static void InitializeVertices()
    {
        initialVertices.Add(new Vector3(0, 1, phi));
        initialVertices.Add(new Vector3(0, -1, phi));
        initialVertices.Add(new Vector3(0, 1, -phi));
        initialVertices.Add(new Vector3(0, -1, -phi));
        initialVertices.Add(new Vector3(1, phi, 0));
        initialVertices.Add(new Vector3(-1, phi, 0));
        initialVertices.Add(new Vector3(1, -phi, 0));
        initialVertices.Add(new Vector3(-1, -phi, 0));
        initialVertices.Add(new Vector3(phi, 0, 1));
        initialVertices.Add(new Vector3(-phi, 0, 1));
        initialVertices.Add(new Vector3(phi, 0, -1));
        initialVertices.Add(new Vector3(-phi, 0, -1));
    }

    private static void CreateInitialTriangles()
    {
        foreach (Vector3 vertex in initialVertices)
        {
            CreateTrianglesFromVertex(vertex);
        }
    }

    private static void CreateTrianglesFromVertex(Vector3 vertex)
    {
        List<Vector3> adjacentVertices = FindAdjacentVertices(vertex);
        foreach (Vector3 v1 in adjacentVertices)
        {
            foreach (Vector3 v2 in adjacentVertices)
            {
                if (AreVerticesAdjacent(v1, v2))
                    initialTriangles.Add(new Triangle(vertex, v1, v2));
            }
        }
    }

    private static List<Vector3> FindAdjacentVertices(Vector3 vertex)
    {
        List<Vector3> adjacentVertices = new List<Vector3>();
        foreach (Vector3 v in initialVertices)
        {
            if (AreVerticesAdjacent(vertex, v))
                adjacentVertices.Add(v);
        }
        return adjacentVertices;
    }

    private static bool AreVerticesAdjacent(Vector3 v1, Vector3 v2)
    {
        return Vector3.Distance(v1, v2) == 2.0f;
    }

    private static void NormalizeTriangles()
    {
        foreach (Triangle triangle in initialTriangles)
        {
            triangles.Add(triangle.Normalize());
        }
    }

    private static void SubdivideIcosahedron(int numSubdivisions)
    {
        for (int i = 0; i < numSubdivisions; i++)
        {
            Subdivide();
        }
    }

    private static void Subdivide()
    {
        List<Triangle> newTriangles = new List<Triangle>();
        triangles.ForEach(t => newTriangles.AddRange(t.Subdivide()));
        triangles = newTriangles;
    }

    private static Dictionary<Vector3, HashSet<Triangle>> GenerateTriangleMapping()
    {

        foreach (Triangle triangle in triangles)
        {
            foreach (Vector3 vertex in triangle.Vertices)
            {
                if (!mapping.ContainsKey(vertex))
                    mapping.Add(vertex, new HashSet<Triangle>());
                mapping[vertex].Add(triangle);
            }
        }
        return mapping;
    }

    private static Dictionary<Vector3, Region> CreateRegions()
    {
        Dictionary<Vector3, Region> regions = new Dictionary<Vector3, Region>();
        foreach (KeyValuePair<Vector3, HashSet<Triangle>> pair in mapping)
        {
            Vector3 center = pair.Key;
            HashSet<Triangle> triangles = pair.Value;
            /*IEnumerable<RegionEdge> matches = from t1 in triangles
                                              from t2 in triangles
                                              where (t1.vertices.Intersect(t2.vertices)).Count() == 2
                                              select new RegionEdge(Vector3.Normalize(t1.Circumcenter()),
                                                                    Vector3.Normalize(t2.Circumcenter()));
            List<RegionEdge> edges = matches.Distinct().ToList();*/
            regions.Add(center, new Region(center, triangles.ToList()));
        }
        AssignAdjacentRegions(regions);
        return regions;
    }

    private static void AssignAdjacentRegions(Dictionary<Vector3, Region> regions)
    {
        foreach (Region region in regions.Values)
        {
            foreach (Vector3 vector in GetAdjacentVectors(region.Center))
            {
                region.AdjacentRegions.Add(regions[vector]);
            }
        }
    }

    private static HashSet<Vector3> GetAdjacentVectors(Vector3 vector)
    {
        HashSet<Vector3> adjacents = new HashSet<Vector3>();
        foreach (Triangle triangle in mapping[vector])
        {
            foreach (Vector3 v in triangle.Vertices)
            {
                adjacents.Add(v);
            }
        }
        adjacents.Remove(vector);
        return adjacents;
    }


}
