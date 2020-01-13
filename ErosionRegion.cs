using System.Collections.Generic;
using System.Numerics;

public class ErosionRegion
{
    public Vector3 Center 
    {
        get
        {
            return Region.Center;
        } 
    }
    public List<ErosionRegion> AdjacentRegions { get; set; } = new List<ErosionRegion>();

    public float Elevation 
    { 
        get
        {
            return Region.GeographicalProperties.Elevation;
        }
        set
        {
            Region.GeographicalProperties.Elevation = value;
        } 
    }

    public Region Region { get; }

    public ErosionRegion(Region region)
    {
        Region = region;
    }
}