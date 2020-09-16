using System.Collections.Generic;
using System.Numerics;

public class ErosionRegion
{
    private readonly object Lock = new object();
    private static int erosionRadius = 2;

    public static void SetErosionRadius(int radius)
    {
        erosionRadius = radius;
    }

    public float Elevation
    {
        get
        {
            lock (Lock)
            {
                return Region.GeographicalProperties.Elevation;
            }
        }

        set
        {
            lock (Lock)
            {
                Region.GeographicalProperties.Elevation = value;
            }
        }
    }

    public Vector3 Center 
    {
        get
        {
            return Region.Center;
        } 
    }
    public List<ErosionRegion> AdjacentRegions { get; set; } = new List<ErosionRegion>();

    public Region Region { get; }

    public Dictionary<ErosionRegion, float> ErosionWeights
    {
        get
        {
            if (erosionWeights == null)
                erosionWeights = GetNodeErosionWeights();
            
            return erosionWeights;
        }
    }

    private Dictionary<ErosionRegion, float> erosionWeights = null;

    public ErosionRegion(Region region)
    {
        Region = region;
        Elevation = region.GeographicalProperties.Elevation;
    }

    Dictionary<ErosionRegion, float> GetNodeErosionWeights()
    {
        HashSet<ErosionRegion> nodes = GetNodesWithinRadius();

        Dictionary<ErosionRegion, float> weights = new Dictionary<ErosionRegion, float>();
        float totalWeight = 0;
        float maxDist = float.MinValue;
        float totalDist = 0;
        foreach (ErosionRegion node in nodes)
        {
            if (node == this)
                continue;
            float distance = Vector3.Distance(node.Center, Center);
            maxDist = System.Math.Max(distance, maxDist);
            totalDist += distance;
        }
        float sigma = maxDist / 3;
        foreach (ErosionRegion node in nodes)
        {
            float distance = Vector3.Distance(node.Center, Center);
            float weight = (float)(1 / (System.Math.Sqrt(2 * System.Math.PI) * sigma) * System.Math.Exp(-(System.Math.Pow(distance, 2) / (2 * System.Math.Pow(sigma, 2)))));
            weights.Add(node, weight);
            totalWeight += weight;
        }

        foreach (ErosionRegion node in nodes)
        {
            weights[node] /= totalWeight;
        }

        return weights;

        HashSet<ErosionRegion> GetNodesWithinRadius()
        {
            HashSet<ErosionRegion> seenNodes = new HashSet<ErosionRegion>{ this };
            List<ErosionRegion> nodesToVisit = new List<ErosionRegion>{ this };

            for (int i = 0; i < erosionRadius; i++)
            {
                List<ErosionRegion> nextNodesToVisit = new List<ErosionRegion>();
                foreach (ErosionRegion node in nodesToVisit)
                {
                    foreach (ErosionRegion adjacentNode in node.AdjacentRegions)
                    {
                        if (!seenNodes.Contains(adjacentNode))
                        {
                            seenNodes.Add(adjacentNode);
                            nextNodesToVisit.Add(adjacentNode);
                        }
                    }
                }
                nodesToVisit = nextNodesToVisit;
            }

            return seenNodes;
        }
    }
}