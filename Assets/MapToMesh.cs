using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MapToMesh : MonoBehaviour
{
    [Header("Map Elevation")]
    public Texture2D elevationData;
    [Range(0f, 1f)] public float quality;

    private float prevQuality;
    private UnityMeshSimplifier.MeshSimplifier meshSimplifierAPI;
    private MeshFilter meshFilter;

    private void OnEnable()
    {
        meshSimplifierAPI = new();
        prevQuality = quality;

        meshFilter = GetComponent<MeshFilter>();

        GenerateMesh();
    }

    private void Update()
    {
        if (prevQuality != quality)
        {
            prevQuality = quality;
            meshSimplifierAPI.SimplifyMesh(quality);
            meshFilter.mesh = meshSimplifierAPI.ToMesh();
        }
    }

    void GenerateMesh()
    {
        if (elevationData.width != elevationData.height)
        {
            Debug.LogError("Elevation data texture must be square.");
            return;
        }

        Mesh mesh = new() { name = "Map Mesh" };

        var resolution = Mathf.Max(elevationData.height, elevationData.width);

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
                var elevation = elevationData.GetPixel(i, j).r;
                vertices[indexVertex] = new Vector3(
                    i * 0.03f,
                    (float)elevation,
                    j * 0.03f
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

        meshSimplifierAPI.Initialize(mesh);
        meshSimplifierAPI.SimplifyMesh(quality);
        mesh = meshSimplifierAPI.ToMesh();

        meshFilter.mesh = mesh;
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
