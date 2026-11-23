using System.Collections.Generic;
using UnityEngine;

public class ShapeMesh : MonoBehaviour
{
    public enum ShapeType { Cube, Pyramid, Sphere }

    [SerializeField] private ShapeType shapeType = ShapeType.Cube;
    [Range(6, 32)][SerializeField] private int sphereSegments = 10;

    [SerializeField] private Material lineMat;
    [SerializeField] private Vector3 position = Vector3.zero;
    [SerializeField] private float scale = 1f;
    [SerializeField] private float xRotation = 0f;
    [SerializeField] private float yRotation = 0f;
    [SerializeField] private Vector2 rotationSpeed = new Vector2(45f, 60f);
    [Range(1, 500)][SerializeField] private float focalLength = 20f;

    private Vector3[] verts;
    private int[][] faces;
    private readonly List<int> visibleEdges = new();
    private readonly List<Vector3> actualPosition = new();
    private Mesh mesh;

    private void Awake()
    {
        mesh = new Mesh();
        gameObject.AddComponent<MeshFilter>().mesh = mesh;
        gameObject.AddComponent<MeshRenderer>().material = lineMat;

        BuildShape();
    }

    private void BuildShape()
    {
        switch (shapeType)
        {
            case ShapeType.Cube:
                BuildCube();
                break;
            case ShapeType.Pyramid:
                BuildPyramid();
                break;
            case ShapeType.Sphere:
                BuildSphere(sphereSegments);
                break;
        }
    }

    #region Shape Generators

    private void BuildCube()
    {
        verts = new Vector3[]
        {
            new(-1, -1, -1), new(1, -1, -1), // 0, 1
            new(1, 1, -1), new(-1, 1, -1), // 2, 3
            new(-1, -1, 1), new(1, -1, 1), // 4, 5
            new(1, 1, 1), new(-1, 1, 1) // 6, 7
        };

        faces = new int[][]
        {
            new[] { 0, 1, 2, 3 }, new[] { 5, 4, 7, 6 }, new[] { 4, 0, 3, 7 }, // counter-clockwise
            new[] { 1, 5, 6, 2 }, new[] { 3, 2, 6, 7 }, new[] { 0, 4, 5, 1 } 
        };
    }

    private void BuildPyramid()
    {
        verts = new Vector3[]
        {
            new(-1, -1, -1),
            new(1, -1, -1),
            new(1, -1, 1),
            new(-1, -1, 1),
            new(0, 1, 0)    
        };

        faces = new int[][]
        {
            new[] { 0, 1, 4 },
            new[] { 1, 2, 4 },
            new[] { 2, 3, 4 },
            new[] { 3, 0, 4 },
            new[] { 0, 3, 2, 1 }
        };
    }

    private void BuildSphere(int segments)
    {
        int rings = segments;
        int sectors = segments;

        List<Vector3> vertList = new();
        List<int[]> faceList = new();

        // Generate UV Sphere vertices
        for (int r = 0; r <= rings; r++)
        {
            float v = (float)r / rings;
            float phi = v * Mathf.PI;

            for (int s = 0; s <= sectors; s++)
            {
                float u = (float)s / sectors;
                float theta = u * Mathf.PI * 2f;

                float x = Mathf.Sin(phi) * Mathf.Cos(theta);
                float y = Mathf.Cos(phi);
                float z = Mathf.Sin(phi) * Mathf.Sin(theta);

                vertList.Add(new Vector3(x, y, z));
            }
        }

        verts = vertList.ToArray();

        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < sectors; s++)
            {
                int current = r * (sectors + 1) + s;
                int next = current + sectors + 1;

                if (r == 0)
                {
                    faceList.Add(new[] { current, next, next + 1 });
                }
                else if (r == rings - 1)
                {
                    faceList.Add(new[] { current, next, current + 1 });
                }
                else
                {
                    faceList.Add(new[] { current, next, next + 1, current + 1 });
                }
            }
        }

        faces = faceList.ToArray();
    }

    #endregion

    private void Update()
    {
        xRotation += rotationSpeed.x * Time.deltaTime;
        yRotation += rotationSpeed.y * Time.deltaTime;
    }

    private void LateUpdate()
    {
        if (verts == null || faces == null) return;

        actualPosition.Clear();
        foreach (var point in verts)
        {
            actualPosition.Add(Project(point));
        }

        mesh.Clear();
        mesh.vertices = actualPosition.ToArray();
        visibleEdges.Clear();

        foreach (var f in faces)
        {
            if (!FacesCamera(actualPosition[f[0]], actualPosition[f[1]], actualPosition[f[2]])) continue;

            for (int i = 0; i < f.Length; i++)
            {
                visibleEdges.Add(f[i]);
                visibleEdges.Add(f[(i + 1) % f.Length]);
            }
        }

        mesh.SetIndices(visibleEdges, MeshTopology.Lines, 0);
        mesh.RecalculateBounds();
    }

    #region Projection & Culling Helpers

    private Vector2 Project(Vector3 point)
    {
        point *= 0.5f;

        //X axis
        float radX = xRotation * Mathf.Deg2Rad;
        float yRot = point.y * Mathf.Cos(radX) - point.z * Mathf.Sin(radX);
        float zRotX = point.y * Mathf.Sin(radX) + point.z * Mathf.Cos(radX);
        point = new Vector3(point.x, yRot, zRotX);

        //Y axis
        float radY = yRotation * Mathf.Deg2Rad;
        float xRot = point.x * Mathf.Cos(radY) - point.z * Mathf.Sin(radY);
        float zRotY = point.x * Mathf.Sin(radY) + point.z * Mathf.Cos(radY);
        point = new Vector3(xRot, point.y, zRotY);

        point += position;

        float p = focalLength / (focalLength + point.z);
        return new Vector2(point.x, point.y) * scale * p;
    }

    private bool FacesCamera(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x) > 0;
    }

    #endregion
}