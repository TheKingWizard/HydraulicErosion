using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class Terrain
{
    public static List<Triangle> triangles = new List<Triangle>();

    public Dictionary<Vector3, List<Triangle>> triangleMapping = new Dictionary<Vector3, List<Triangle>>();
    public Dictionary<Vector3, ErosionRegion> erosionRegions = new Dictionary<Vector3, ErosionRegion>();

    public Terrain()
    {
        Initialize();
    }

    protected void GenerateHexagon()
    {
        erosionRegions = CreateErosionRegions(CreateRegions());
        MapElevation();
    }

    void Initialize()
    {
        /* Hexagon Setup
         *    F --- G
         *   / \ 5 / \
         *  / 4 \ / 6 \
         * E --- A --- B
         *  \ 3 / \ 1 /
         *   \ / 2 \ /
         *    D --- C
         */

        float phi = (float)System.Math.Sqrt(3) / 2f;

        Vector3 A = new Vector3(0, 0, 0);
        Vector3 B = new Vector3(1f, 0, 0);
        Vector3 C = new Vector3(0.5f, -phi, 0);
        Vector3 D = new Vector3(-0.5f, -phi, 0);
        Vector3 E = new Vector3(-1f, 0, 0);
        Vector3 F = new Vector3(-0.5f, phi, 0);
        Vector3 G = new Vector3(0.5f, phi, 0);

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

        Subdivide(6);
        MapTriangles();
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

    private List<Region> CreateRegions()
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
        foreach (ErosionRegion region in erosionRegions.Values)
        {
            region.Elevation = (noise.Evaluate(region.Center) + 1f) / 2f;
        }
    }

    public void SimulateErosion(int numIterations)
    {
        GenerateHexagon();

        if (numIterations == 0) return;

        ErosionRegion[] values = erosionRegions.Values.ToArray();
        Parallel.For(0, numIterations, new ParallelOptions { MaxDegreeOfParallelism = 32 }, (i) =>
        {
            System.Random random = new System.Random(i);
            WaterDroplet waterDroplet = new WaterDroplet(values[(random.Next(erosionRegions.Count))]);
            waterDroplet.Simulate();
        });

        /*for (int i = 0; i < numIterations; i++)
        {
            System.Random random = new System.Random(i);
            WaterDroplet waterDroplet = new WaterDroplet(regions.ElementAt(random.Next(regions.Count)).Value);
            waterDroplet.Simulate();
        }*/
    }
}
