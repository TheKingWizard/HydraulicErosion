using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class Terrain
{
    public List<Triangle> triangles = new List<Triangle>();
    public Dictionary<Vector3, float> elevationMapping = new Dictionary<Vector3, float>();
    public Dictionary<Vector3, List<Triangle>> triangleMapping = new Dictionary<Vector3, List<Triangle>>();

    private float inertia = 0.05f;
    private float gravity = 4.0f;
    private float evaporation = 0.05f;
    private float capacity = 8.0f;
    private float erosion = 0.3f;
    private float deposition = 0.01f;
    private float minErosion = 0.01f;
    private int lifetime = 50;

    private int erosionRadius = 2;

    public List<List<Vector3>> paths = new List<List<Vector3>>();
    public List<Vector3> path = new List<Vector3>();
    public List<float> erosits = new List<float>();

    float minV = float.MaxValue;
    float maxV = float.MinValue;

    public Terrain()
    {
        Initialize();
        Subdivide(8);
        MapTriangles();
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

        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
        triangles.Add(d);
        triangles.Add(e);
        triangles.Add(f);
        triangles.Add(g);
        triangles.Add(h);
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

    private void MapElevation()
    {
        Noise noise = new Noise();
        noise.SetParameters(8, 0.75f, 0.25f, 1f, 2f);
        foreach (Vector3 vector in triangleMapping.Keys)
        {
            elevationMapping.Add(vector, (noise.Evaluate(vector) + 1f) / 2f);
            //elevationMapping.Add(vector, 1);
        }
    }

    private void SimulateErosion(int numIterations)
    {
        UnityEngine.Debug.Log("SIMULATING EROSION");
        for (int i = 0; i < numIterations; i++)
        {
            SimulateDroplet();
        }
        UnityEngine.Debug.Log("SIMULATION COMPLETE");
    }

    private void SimulateDroplet()
    {
        path = new List<Vector3>();

        erosits.Clear();

        Vector3 initialDirection = new Vector3();
        initialDirection.X = UnityEngine.Random.Range(-1.0f, 1.0f);
        initialDirection.Y = UnityEngine.Random.Range(-1.0f, 1.0f);
        initialDirection = Vector3.Normalize(initialDirection);

        WaterDrop drop = new WaterDrop
        {
            volume = 1,
            sediment = 0,
            velocity = 1,
            position = triangleMapping.ElementAt(UnityEngine.Random.Range(0, triangleMapping.Count)).Key,
            direction = initialDirection
        };
        for (int i = 0; i < lifetime; i++)
        {
            MoveDroplet(ref drop);
            if (!IsDropAlive(drop))
                break;
        }
        //Deposit(ref drop, 0, float.MaxValue);
        //paths.Add(path);
    }

    private bool IsDropAlive(WaterDrop drop)
    {
        if(float.IsNaN(drop.velocity) || drop.volume < 0.001)
        {
            Deposit(ref drop, 0, float.MaxValue);
            return false;
        }
        return true;
    }

    private void Erode(ref WaterDrop drop, float dropSedimentCapacity, float heightDiff)
    {
        if (drop.volume == 1)
            return;
        float erosit = System.Math.Min((dropSedimentCapacity - drop.sediment) * erosion, -heightDiff);
        //float erosit = System.Math.Min((dropSedimentCapacity) * erosion, -heightDiff);
        foreach (KeyValuePair<Vector3, float> kvp in GetNodeErosionWeights(drop.position))
        {
            Vector3 location = kvp.Key;
            float weight = kvp.Value;

            elevationMapping[location] -= erosit * weight;
        }
        //elevationMapping[drop.position] -= erosit;
        drop.sediment += erosit;
        erosits.Add(erosit);

        Dictionary<Vector3, float> GetNodeErosionWeights(Vector3 center)
        {
            HashSet<Vector3> nodes = GetNodesWithinRadius(center);

            Dictionary<Vector3, float> weights = new Dictionary<Vector3, float>();
            float totalWeight = 0;
            float minDist = float.MaxValue;
            float maxDist = float.MinValue;
            float totalDist = 0;
            foreach (Vector3 node in nodes)
            {
                if (node == center)
                    continue;
                float distance = Vector3.Distance(node, center);
                maxDist = System.Math.Max(distance, maxDist);
                minDist = System.Math.Min(distance, minDist);
                totalDist += distance;
            }
            //float sigma = (maxDist + minDist) / 2;
            //float sigma = erosionRadius + 1;
            //float sigma = erosionRadius * minDist;
            float sigma = maxDist / 3;
            //float sigma = minDist;
            //float sigma = totalDist / (nodes.Count - 1);
            foreach (Vector3 node in nodes)
            {
                float distance = Vector3.Distance(node, center);
                float weight = (float)(1 / (System.Math.Sqrt(2 * System.Math.PI) * sigma) * System.Math.Exp(-(System.Math.Pow(distance, 2) / (2 * System.Math.Pow(sigma, 2)))));
                weights.Add(node, weight);
                totalWeight += weight;
            }

            //totalWeight *= 2;

            foreach (Vector3 node in nodes)
            {
                weights[node] /= totalWeight;
            }

            /*if(erosionRadius == 0)
                weights.Add(center, 1f);
            else
                weights.Add(center, 0.5f);*/


            return weights;

            HashSet<Vector3> GetNodesWithinRadius(Vector3 centerNode)
            {
                HashSet<Vector3> seenNodes = new HashSet<Vector3>();
                seenNodes.Add(centerNode);
                List<Vector3> nodesToVisit = new List<Vector3>();
                nodesToVisit.Add(centerNode);

                for (int i = 0; i < erosionRadius; i++)
                {
                    List<Vector3> nextNodesToVisit = new List<Vector3>();
                    foreach (Vector3 node in nodesToVisit)
                    {
                        foreach (Vector3 adjacentNode in GetAdjacentNodes(node))
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

    private void Deposit(ref WaterDrop drop, float dropSedimentCapacity, float heightDiff)
    {
        float deposit = (heightDiff > 0) ? System.Math.Min(heightDiff, drop.sediment) : (drop.sediment - dropSedimentCapacity) * deposition;
        drop.sediment -= deposit;
        elevationMapping[drop.position] += deposit;
    }

    private void MoveDroplet(ref WaterDrop drop)
    {
        path.Add(drop.position);
        Vector3 gradient = GetGradient(drop.position);
        Vector3 newDirection = Vector3.Normalize(drop.direction * inertia - gradient * (1f - inertia));
        //Vector3 newDirection = -gradient;
        Vector3 newPosition = new Vector3();
        float maxCosTheta = float.MinValue;
        List<Vector3> adjacentNodes = GetAdjacentNodes(drop.position);
        Vector3 bestDirection = newDirection;
        foreach (Vector3 adjacentPosition in adjacentNodes)
        {
            Vector3 direction = Vector3.Normalize(adjacentPosition - drop.position);
            float cosTheta = Vector3.Dot(direction, newDirection);
            if (cosTheta > maxCosTheta)
            {
                maxCosTheta = cosTheta;
                newPosition = adjacentPosition;
                bestDirection = direction;
            }
        }

        float heightDiff = elevationMapping[newPosition] - elevationMapping[drop.position];
        float dropSedimentCapacity = System.Math.Max(-heightDiff * drop.velocity * drop.volume * capacity, minErosion);
        if (drop.sediment > dropSedimentCapacity || heightDiff > 0)
        {
            Deposit(ref drop, dropSedimentCapacity, heightDiff);
        }
        else
        {
            Erode(ref drop, dropSedimentCapacity, heightDiff);
        }

        float newVelocity = (float)System.Math.Sqrt(System.Math.Pow(drop.velocity, 2) - heightDiff * gravity);
        float newVolume = drop.volume * (1 - evaporation);
        //drop.direction = (newDirection  + bestDirection) / 2;
        //drop.direction = bestDirection;
        drop.direction = newDirection;
        drop.position = newPosition;
        drop.velocity = newVelocity;
        drop.volume = newVolume;

    }

    Vector3 GetGradient(Vector3 position)
    {
        List<Vector3> adjacentNodes = GetAdjacentNodes(position);
        Dictionary<Vector3, float> heightDiffs = new Dictionary<Vector3, float>();
        float totalHeightDiff = 0;
        bool inPit = true;
        foreach (Vector3 adjacentNode in adjacentNodes)
        {
            float heightDiff = elevationMapping[adjacentNode] - elevationMapping[position];
            heightDiffs.Add(Vector3.Normalize(adjacentNode - position), heightDiff);
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
            /*gradient.X = UnityEngine.Random.value * 2 - 1;
            gradient.Y = UnityEngine.Random.value * 2 - 1;
            gradient = Vector3.Normalize(gradient);*/
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

    List<Vector3> GetAdjacentNodes(Vector3 node)
    {
        HashSet<Vector3> adjacentNodes = new HashSet<Vector3>();
        foreach (Triangle triangle in triangleMapping[node])
        {
            foreach (Vector3 vertex in triangle.Vertices)
            {
                adjacentNodes.Add(vertex);
            }
        }
        adjacentNodes.Remove(node);
        return adjacentNodes.ToList();
    }

    struct WaterDrop
    {
        public float volume;
        public float sediment;
        public Vector3 position;
        public Vector3 direction;
        public float velocity;
    }
}
