using System.Collections.Generic;
using System.Numerics;

public class Region
{
    public Vector3 Center { get; }
    public List<Region> AdjacentRegions { get; set; } = new List<Region>();
    public List<Triangle> Triangles { get; }
    //public Biome Biome { get; set; }

    public GeographicalProperties GeographicalProperties { get; set; }

    public Region(Vector3 center, List<Triangle> triangles)
    {
        Center = center;
        Triangles = triangles;
        GeographicalProperties = new GeographicalProperties(0);
    }
}
