using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class Terrain
{
    public List<Triangle> triangles = new List<Triangle>();
    public Dictionary<Vector3, List<Triangle>> triangleMapping = new Dictionary<Vector3, List<Triangle>>();
    public Dictionary<Vector3, ErosionRegion> regions = new Dictionary<Vector3, ErosionRegion>();

    public Terrain()
    {
        Initialize();
        Subdivide(8);
        MapTriangles();
        regions = CreateErosionRegions(CreateRegions());
        MapElevation();
        SimulateErosion(300000);
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
        noise.SetParameters(8, 0.75f, 0.25f, 1f, 2f);
        foreach (ErosionRegion region in regions.Values)
        {
            region.elevation = (noise.Evaluate(region.Center) + 1f) / 2f;
        }
    }

    private void SimulateErosion(int numIterations)
    {
        Parallel.For(0, numIterations, (i) =>
        {
            System.Random random = new System.Random(i);
            WaterDroplet waterDroplet = new WaterDroplet(regions.ElementAt(random.Next(regions.Count)).Value);
            waterDroplet.Simulate();
        });

        /*for (int i = 0; i < numIterations; i++)
        {
            System.Random random = new System.Random(i);
            WaterDroplet waterDroplet = new WaterDroplet(regions.ElementAt(random.Next(regions.Count)).Value);
            waterDroplet.Simulate();
        }*/
     
        SyncElevation();
    }

    private void SyncElevation()
    {
        foreach (ErosionRegion region in regions.Values)
        {
            region.SyncElevation();
        }
    }
}
