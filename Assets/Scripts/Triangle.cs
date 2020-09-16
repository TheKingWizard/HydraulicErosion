using System.Linq;
using System.Numerics;
using System.Collections.Generic;

public class Triangle
{
    private HashSet<Vector3> vertices = new HashSet<Vector3>();
    public List<Vector3> Vertices { get { return vertices.ToList(); } }
    public Vector3 A { get { return Vertices[0]; } }
    public Vector3 B { get { return Vertices[1]; } }
    public Vector3 C { get { return Vertices[2]; } }

    public Triangle(Vector3 a, Vector3 b, Vector3 c)
    {
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
    }

    public List<Triangle> Subdivide()
    {
        List<Triangle> triangles = new List<Triangle>();
        Vector3 a = A;
        Vector3 b = B;
        Vector3 c = C;
        Vector3 d = (a + b) / 2.0f;
        Vector3 e = (a + c) / 2.0f;
        Vector3 f = (b + c) / 2.0f;
        triangles.Add(new Triangle(a, d, e));
        triangles.Add(new Triangle(b, d, f));
        triangles.Add(new Triangle(c, e, f));
        triangles.Add(new Triangle(d, e, f));
        return triangles;
    }

    public Triangle Normalize()
    {
        return new Triangle(Vector3.Normalize(A), Vector3.Normalize(B), Vector3.Normalize(C));
    }

    public Vector3 Circumcenter()
    {
        Vector3 ab = B - A;
        Vector3 ac = C - A;
        Vector3 abXac = Vector3.Cross(ab, ac);
        Vector3 acXab = Vector3.Cross(ac, ab);
        Vector3 zero = Vector3.Zero;
        Vector3 term1 = Vector3.Cross(Vector3.DistanceSquared(ac, zero) * abXac, ab);
        Vector3 term2 = Vector3.Cross(Vector3.DistanceSquared(ab, zero) * acXab, ac);

        Vector3 circumcenter = A + ((term1 + term2) / (2 * Vector3.DistanceSquared(abXac, zero)));
        return circumcenter;
    }

    public Vector3 Interpolate(Vector3 vector)
    {
        Vector3 vA = A - vector;
        Vector3 vB = B - vector;
        Vector3 vC = C - vector;
        Vector3 v = Vector3.Cross(A - B, A - C);
        Vector3 v1 = Vector3.Cross(vB, vC);
        Vector3 v2 = Vector3.Cross(vC, vA);
        Vector3 v3 = Vector3.Cross(vA, vB);
        float area = v.Length();
        float area1 = v1.Length() / area * System.Math.Sign(Vector3.Dot(v, v1));
        float area2 = v2.Length() / area * System.Math.Sign(Vector3.Dot(v, v2));
        float area3 = v3.Length() / area * System.Math.Sign(Vector3.Dot(v, v3));
        return new Vector3(area1, area2, area3);
    }

    public bool AdjacentTo(Triangle triangle)
    {
        return vertices.Intersect(triangle.vertices).Count() == 2;
    }

    public override bool Equals(object obj)
    {
        return obj is Triangle triangle &&
               vertices.SetEquals(triangle.vertices);
    }

    public override int GetHashCode()
    {
        int hashcode = 911371710;
        foreach (Vector3 vector in Vertices)
        {
            hashcode += vector.GetHashCode();
        }
        return hashcode;
    }
}
