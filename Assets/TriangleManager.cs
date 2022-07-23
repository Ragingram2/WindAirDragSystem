using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class triangle
{
    public (int idx, Vector3 point) v1 { get; set; }

    public (int idx, Vector3 point) v2 { get; set; }

    public (int idx, Vector3 point) v3 { get; set; }
    public Vector3 side1 { get; set; }
    public Vector3 side2 { get; set; }
    public Vector3 side3 { get; set; }
    public Vector3 normal { get; set; }
    public Vector3 center { get; set; }
    public Vector3 inCenter { get; set; }
    public List<(Vector3 position, Vector3 force)> dragForces { get; set; }

    public triangle() { }

    public float airFlowArea(Vector3 airNormal) => Vector3.Magnitude(Vector3.Cross(side1 - (airNormal * Vector3.Dot(side1, airNormal)), side2 - (airNormal * Vector3.Dot(side2, airNormal)))) * .5f;
    public Vector3 closestVertexToPoint(Vector3 point)
    {
        Vector3 closestPoint = point;
        float maxDistance = float.MaxValue;
        float distance = Vector3.Distance(point, v1.point);
        if (distance < maxDistance)
        {
            maxDistance = distance;
            closestPoint = v1.point;
        }

        distance = Vector3.Distance(point, v2.point);
        if (distance < maxDistance)
        {
            maxDistance = distance;
            closestPoint = v2.point;
        }

        distance = Vector3.Distance(point, v3.point);
        if (distance < maxDistance)
        {
            maxDistance = distance;
            closestPoint = v3.point;
        }

        return closestPoint;
    }

    public override string ToString()
    {
        return $"V1: {v1.point}\n" +
                   $"V2: {v2.point}\n" +
                   $"V3: {v3.point}\n" +
                   $"Normal: {normal}";
    }
}

public class TriangleManager : MonoBehaviour
{
    [SerializeField] private bool pause = true;
    private List<triangle> triangles = new List<triangle>();
    public List<triangle> Triangles => triangles;
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] tris;

    // Start is called before the first frame update
    void Start()
    {
        InitMesh();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMesh();

        if (Input.GetKeyUp(KeyCode.E))
            TogglePause();
    }

    public void InitMesh()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;
        tris = mesh.GetTriangles(0);
        for (int i = 0; i < tris.Length;)
        {
            var point1 = transform.localToWorldMatrix * vertices[tris[i + 0]];
            var point2 = transform.localToWorldMatrix * vertices[tris[i + 1]];
            var point3 = transform.localToWorldMatrix * vertices[tris[i + 2]];
            triangles.Add(new triangle
            {
                v1 = (i + 0, point1),
                v2 = (i + 1, point2),
                v3 = (i + 2, point3),
                side1 = point2 - point1,
                side2 = point3 - point2,
                side3 = point1 - point3,
                normal = Vector3.Normalize(Vector3.Cross(point3 - point1, point2 - point1)),
                center = ((point1 + point2 + point3) / 3.0f)
            });
            i += 3;
        }
    }

    public void UpdateMesh()
    {
        if (pause)
            return;

        for (int i = 0; i < tris.Length;)
        {
            var point1 = transform.localToWorldMatrix * vertices[tris[i + 0]];
            var point2 = transform.localToWorldMatrix * vertices[tris[i + 1]];
            var point3 = transform.localToWorldMatrix * vertices[tris[i + 2]];
            triangles[i / 3].v1 = (i + 0, point1);
            triangles[i / 3].v2 = (i + 1, point2);
            triangles[i / 3].v3 = (i + 2, point3);
            triangles[i / 3].side1 = point2 - point1;
            triangles[i / 3].side2 = point3 - point2;
            triangles[i / 3].side3 = point1 - point3;
            triangles[i / 3].normal = Vector3.Normalize(Vector3.Cross(point3 - point1, point2 - point1));
            triangles[i / 3].center = ((point1 + point2 + point3) / 3.0f);
            i += 3;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        foreach (var tri in triangles)
        {
            Gizmos.DrawLine(transform.position + tri.v1.point, transform.position + tri.v2.point);
            Gizmos.DrawLine(transform.position + tri.v2.point, transform.position + tri.v3.point);
            Gizmos.DrawLine(transform.position + tri.v3.point, transform.position + tri.v1.point);
        }
    }

    public bool IsPaused() => pause;
    public void Play() => pause = false;
    public void Pause() => pause = true;
    public void TogglePause() => pause = !pause;
}
