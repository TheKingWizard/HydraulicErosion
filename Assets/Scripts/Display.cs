using System.Collections.Generic;
using UnityEngine;

public class Display : MonoBehaviour
{
    TerrainCompute terrain;
    public Mesh mesh;
    public Material terrainMaterial;

    void Start()
    {
        terrain = new TerrainCompute();
    }

    void Update()
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        if (mesh != null)
            Graphics.DrawMesh(mesh, Matrix4x4.identity, terrainMaterial, 0, Camera.main, 0, block, true, true);
    }

    public void Simulate(int numDrops, bool useGPU)
    {
        if (useGPU)
            terrain.SimulateErosionComputeShader(numDrops);
        else
            terrain.SimulateErosion(numDrops);

        GenerateMesh();
    }

    Mesh GenerateMesh()
    {
        mesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();
        int index = 0;
        Dictionary<Vector3, int> indexer = new Dictionary<Vector3, int>();

        List<Color> colors = new List<Color>();
        foreach (Triangle triangle in Terrain.triangles)
        {
            Vector3 a = new Vector3(triangle.A.X, triangle.A.Y, -terrain.erosionRegions[triangle.A].Elevation);
            Vector3 b = new Vector3(triangle.B.X, triangle.B.Y, -terrain.erosionRegions[triangle.B].Elevation);
            Vector3 c = new Vector3(triangle.C.X, triangle.C.Y, -terrain.erosionRegions[triangle.C].Elevation);

            Vector3 s1 = a - c;
            Vector3 s2 = b - c;
            Vector3 n = Vector3.Cross(s1, s2).normalized;
            Vector3 z = new Vector3(0, 0, -1);
            System.Numerics.Vector3 v = triangle.Circumcenter();
            float cosTheta = Vector3.Dot(n, z);

            if (!indexer.ContainsKey(a))
            {
                indexer.Add(a, index);
                vertices.Add(a);
                index++;
            }
            if (!indexer.ContainsKey(b))
            {
                indexer.Add(b, index);
                vertices.Add(b);
                index++;
            }
            if (!indexer.ContainsKey(c))
            {
                indexer.Add(c, index);
                vertices.Add(c);
                index++;
            }
            if (cosTheta > 0)
            {
                indices.Add(indexer[a]);
                indices.Add(indexer[b]);
                indices.Add(indexer[c]);
            }
            else
            {
                indices.Add(indexer[c]);
                indices.Add(indexer[b]);
                indices.Add(indexer[a]);
            }

        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(indices, 0);
        mesh.SetColors(colors);
        mesh.RecalculateNormals();

        return mesh;
    }
}
