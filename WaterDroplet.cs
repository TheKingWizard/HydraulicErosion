using System.Collections.Generic;
using System.Numerics;

public class WaterDroplet
{
    private static readonly int lifetime = 50;
    private static readonly int erosionRadius = 2;
    private static readonly float inertia = 0.1f;
    private static readonly float gravity = 4.0f;
    private static readonly float evaporation = 0.05f;
    private static readonly float capacity = 2.0f;
    private static readonly float erosion = 0.3f;
    private static readonly float deposition = 0.01f;
    private static readonly float minErosion = 0.01f;

    private ErosionRegion position;
    private Vector3 direction = Vector3.Normalize(new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), 0));
    private float velocity = 1;
    private float volume = 1;
    private float sediment = 0;

    private bool IsAlive
    {
        get 
        {
            if (float.IsNaN(velocity))
                return false;
            else
                return true;
        }
    }

    public WaterDroplet(ErosionRegion region)
    {
        position = region;
    }

    public void Simulate()
    {
        for (int i = 0; i < lifetime; i++)
        {
            if(IsAlive)
                SimulationStep();
            else
                break;
        }
    }

    private void SimulationStep()
    {
        Vector3 gradient = GetGradient(position);
        Vector3 newDirection = Vector3.Normalize(direction * inertia - gradient * (1f - inertia));

        ErosionRegion newPosition = FindNextLocation(newDirection);

        float heightDiff = newPosition.Elevation - position.Elevation;
        SimulateHydraulicAction(heightDiff);

        float newVelocity = (float)System.Math.Sqrt(System.Math.Pow(velocity, 2) - heightDiff * gravity);
        float newVolume = volume * (1 - evaporation);

        UpdateValues(newPosition, newDirection, newVelocity, newVolume);
    }

    private Vector3 GetGradient(ErosionRegion region)
    {
        Dictionary<Vector3, float> heightDiffs = new Dictionary<Vector3, float>();
        float totalHeightDiff = 0;
        bool inPit = true;
        foreach (ErosionRegion adjacentNode in region.AdjacentRegions)
        {
            float heightDiff = adjacentNode.Elevation - position.Elevation;
            heightDiffs.Add(Vector3.Normalize(adjacentNode.Center - position.Center), heightDiff);
            totalHeightDiff += System.Math.Abs(heightDiff);
            if (heightDiff < 0)
                inPit = false;
        }
        Vector3 gradient = new Vector3(0, 0, 0);
        if (totalHeightDiff == 0)
        {
            gradient.X = UnityEngine.Random.value * 2 - 1;
            gradient.Y = UnityEngine.Random.value * 2 - 1;
            gradient = Vector3.Normalize(gradient) * 0.5f * inertia / (1 - inertia);
            return gradient;
        }
        else if (inPit)
        {
            return gradient;
        }
        else
        {
            foreach (Vector3 direction in heightDiffs.Keys)
            {
                gradient += direction * heightDiffs[direction] / totalHeightDiff;
            }
            if (gradient == Vector3.Zero)
            {
                gradient.X = UnityEngine.Random.value * 2 - 1;
                gradient.Y = UnityEngine.Random.value * 2 - 1;
                gradient = Vector3.Normalize(gradient) * inertia / (1 - inertia);
                return gradient;
            }
            gradient = Vector3.Normalize(gradient);
            if (UnityEngine.Random.value > 0.5)
            {
                Vector3 jitter = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.5f, 0.5f), 0);
                jitter = Vector3.Normalize(jitter);
                gradient = Vector3.Normalize(gradient + 0.5f * jitter);
            }

        }

        return gradient;
    }

    private ErosionRegion FindNextLocation(Vector3 newDirection)
    {
        ErosionRegion newPosition = new ErosionRegion(null);
        float maxCosTheta = float.MinValue;
        foreach (ErosionRegion adjacentRegion in position.AdjacentRegions)
        {
            Vector3 direction = Vector3.Normalize(adjacentRegion.Center - position.Center);
            float cosTheta = Vector3.Dot(direction, newDirection);
            if (cosTheta > maxCosTheta)
            {
                maxCosTheta = cosTheta;
                newPosition = adjacentRegion;
            }
        }
        return newPosition;
    }

    private void SimulateHydraulicAction(float heightDiff)
    {
        float dropSedimentCapacity = System.Math.Max(-heightDiff * velocity * volume * capacity, minErosion);
        if (sediment > dropSedimentCapacity || heightDiff > 0)
        {
            Deposit(dropSedimentCapacity, heightDiff);
        }
        else
        {
            Erode(dropSedimentCapacity, heightDiff);
        }
    }

    private void Deposit(float dropSedimentCapacity, float heightDiff)
    {
        float deposit = (heightDiff > 0) ? System.Math.Min(heightDiff, sediment) : (sediment - dropSedimentCapacity) * deposition;
        sediment -= deposit;
        position.Elevation += deposit;
    }

    private void Erode(float dropSedimentCapacity, float heightDiff)
    {
        float erosit = System.Math.Min((dropSedimentCapacity - sediment) * erosion, -heightDiff);
        foreach (KeyValuePair<ErosionRegion, float> kvp in GetNodeErosionWeights(position))
        {
            ErosionRegion location = kvp.Key;
            float weight = kvp.Value;

            location.Elevation -= erosit * weight;
        }
        sediment += erosit;

        Dictionary<ErosionRegion, float> GetNodeErosionWeights(ErosionRegion centerRegion)
        {
            HashSet<ErosionRegion> nodes = GetNodesWithinRadius(centerRegion);

            Dictionary<ErosionRegion, float> weights = new Dictionary<ErosionRegion, float>();
            float totalWeight = 0;
            float minDist = float.MaxValue;
            float maxDist = float.MinValue;
            float totalDist = 0;
            foreach (ErosionRegion node in nodes)
            {
                if (node == centerRegion)
                    continue;
                float distance = Vector3.Distance(node.Center, centerRegion.Center);
                maxDist = System.Math.Max(distance, maxDist);
                minDist = System.Math.Min(distance, minDist);
                totalDist += distance;
            }
            float sigma = maxDist / 3;
            foreach (ErosionRegion node in nodes)
            {
                float distance = Vector3.Distance(node.Center, centerRegion.Center);
                float weight = (float)(1 / (System.Math.Sqrt(2 * System.Math.PI) * sigma) * System.Math.Exp(-(System.Math.Pow(distance, 2) / (2 * System.Math.Pow(sigma, 2)))));
                weights.Add(node, weight);
                totalWeight += weight;
            }

            foreach (ErosionRegion node in nodes)
            {
                weights[node] /= totalWeight;
            }

            return weights;

            HashSet<ErosionRegion> GetNodesWithinRadius(ErosionRegion centerNode)
            {
                HashSet<ErosionRegion> seenNodes = new HashSet<ErosionRegion>();
                seenNodes.Add(centerNode);
                List<ErosionRegion> nodesToVisit = new List<ErosionRegion>();
                nodesToVisit.Add(centerNode);

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

    private void UpdateValues(ErosionRegion newPosition, Vector3 newDirection, float newVelocity, float newVolume)
    {
        position = newPosition;
        direction = newDirection;
        velocity = newVelocity;
        volume = newVolume;
    }
}
