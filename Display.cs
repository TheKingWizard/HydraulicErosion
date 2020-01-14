using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Display : MonoBehaviour
{

    Terrain terrain;
    public Mesh mesh;
    public Material material;
    public Material lineMat;

    // Start is called before the first frame update
    void Start()
    {
        terrain = new Terrain();
        mesh = GenerateMesh();
    }

    // Update is called once per frame
    void Update()
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        if (mesh != null)
        {
            Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 0, Camera.main, 0, block, true, true);
        }
    }

    void OnPostRender()
    {
        /*if (!mat)
        {
            Debug.LogError("Please Assign a material on the inspector");
            return;
        }*/
        //GL.LoadIdentity();
        //GL.PushMatrix();
        lineMat.SetPass(0);
        //GL.LoadOrtho();
        /*foreach (List<System.Numerics.Vector3> path in terrain.paths)
        {
            GL.Begin(GL.LINE_STRIP);
            GL.Color(Color.green);
            foreach (System.Numerics.Vector3 vector in path)
            {
                GL.Vertex(new Vector3(vector.X, vector.Y, -(terrain.elevationMapping[vector])));
                GL.Color(Color.red);
            }
            GL.End();
        }*/
        

        //GL.PopMatrix();
    }

    Mesh GenerateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();
        int index = 0;
        List<Color> colors = new List<Color>();
        foreach (Triangle triangle in terrain.triangles)
        {
            Vector3 a = new Vector3(triangle.A.X, triangle.A.Y, -terrain.regions[triangle.A].elevation);
            Vector3 b = new Vector3(triangle.B.X, triangle.B.Y, -terrain.regions[triangle.B].elevation);
            Vector3 c = new Vector3(triangle.C.X, triangle.C.Y, -terrain.regions[triangle.C].elevation);

            Vector3 s1 = a - c;
            Vector3 s2 = b - c;
            Vector3 n = Vector3.Cross(s1, s2).normalized;
            Vector3 z = new Vector3(0, 0, -1);
            float cosTheta = Vector3.Dot(n, z);

            if (cosTheta > 0)
            {
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
            }
            else
            {
                vertices.Add(c);
                vertices.Add(b);
                vertices.Add(a);
            }
                
        }
        for (int i = 0; i < vertices.Count; i++)
        {
            indices.Add(i);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(indices, 0);
        mesh.SetColors(colors);
        mesh.RecalculateNormals();

        return mesh;
    }
}
