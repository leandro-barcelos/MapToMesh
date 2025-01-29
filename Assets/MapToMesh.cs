using System.Collections;
using System.Collections.Generic;
using UnityEditor.Compilation;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MapToMesh : MonoBehaviour
{
    [Header("Starting Position")]
    public double startLat;
    public double startLon;

    [Header("Ending Position")]
    public double endLat;
    public double endLon;

    [Header("Settings")] public int resolution;

    private async void OnEnable()
    {
        OpenElevationWrap openElevationWrap = new();
        for (var i = 0; i < resolution; i++)
        {
            var lat = startLat + (endLat - startLat) * i / resolution;
            for (var j = 0; j < resolution; j++)
            {
                var lon = startLon + (endLon - startLon) * j / resolution;
                openElevationWrap.AddLocation(lat, lon);
            }
        }

        var response = await openElevationWrap.GetElevationData();

        GenerateMesh(response);
    }

    void GenerateMesh(OpenElevationWrap.Response response)
    {
        Mesh mesh = new Mesh { name = "Map Mesh" };
        var size = GetSize();

        // Initialize arrays
        Vector3[] vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        Vector2[] uvs = new Vector2[vertices.Length];

        // Generate vertices & UVs
        int indexVertex = 0;
        for (var i = 0; i < resolution; i++)
        {
            for (var j = 0; j < resolution; j++)
            {
                var elevation = response.results[indexVertex].elevation;
                vertices[indexVertex] = new Vector3(
                    (float)i / (resolution - 1) * size.x,
                    (float)elevation / 1000f,
                    (float)j / (resolution - 1) * size.y
                );

                uvs[indexVertex] = new Vector2((float)i / (resolution - 1), (float)j / (resolution - 1));
                indexVertex++;
            }
        }

        // Generate triangles
        int indexTriangle = 0;
        for (var i = 0; i < resolution - 1; i++)
        {
            for (var j = 0; j < resolution - 1; j++)
            {
                int topLeft = i * resolution + j;
                int topRight = topLeft + 1;
                int bottomLeft = topLeft + resolution;
                int bottomRight = bottomLeft + 1;

                // First triangle
                triangles[indexTriangle++] = topLeft;
                triangles[indexTriangle++] = topRight;
                triangles[indexTriangle++] = bottomLeft;

                // Second triangle
                triangles[indexTriangle++] = topRight;
                triangles[indexTriangle++] = bottomRight;
                triangles[indexTriangle++] = bottomLeft;
            }
        }

        // Assign mesh data
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        // Recalculate normals & bounds
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        GetComponent<MeshFilter>().mesh = mesh;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private Vector2 GetSize()
    {
        var width = Mesure(startLat, startLon, startLat, endLon);
        var height = Mesure(startLat, startLon, endLat, startLon);
        return new Vector2(width, height);
    }

    // Returns the distance between to points in KM
    private float Mesure(double lat1, double lon1, double lat2, double lon2)
    {
        const float r = 6378.137f; // Radius of earth in KM
        var distanceLat = (float)(lat2 - lat1) * (Mathf.PI / 180);
        var distanceLon = (float)(lon2 - lon1) * (Mathf.PI / 180);
        var a = Mathf.Sin(distanceLat / 2) * Mathf.Sin(distanceLat / 2) +
                Mathf.Cos((float)lat1 * (Mathf.PI / 180)) * Mathf.Cos((float)lat2 * (Mathf.PI / 180)) *
                Mathf.Sin(distanceLon / 2) * Mathf.Sin(distanceLon / 2);
        var c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        return r * c;
    }
}
