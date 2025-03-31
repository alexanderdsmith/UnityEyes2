using UnityEngine;
using System.Collections.Generic;

public class EyeVisualizationManager : MonoBehaviour
{
    [Header("Visualization Settings")]
    public bool visualizationEnabled = false;
    public float pointSize = 0.1f;
    public float lineWidth = 0.5f;
    
    [Header("Colors")]
    public Color interiorMarginColor = new Color(1f, 0.2f, 0.2f, 0.8f);
    public Color caruncleColor = new Color(0.2f, 1f, 0.2f, 0.8f);
    public Color irisColor = new Color(0.2f, 0.2f, 1f, 0.8f);
    public Color gazeVectorColor = new Color(0.8f, 0.2f, 0.2f, 1.0f);
    
    [Header("References")]
    public SynthesEyesServer synthesEyesServer;
    
    // Point cloud containers
    private List<GameObject> visualizationObjects = new List<GameObject>();
    private GameObject visualizationRoot;
    
    private void Start()
    {
        if (synthesEyesServer == null)
        {
            synthesEyesServer = FindFirstObjectByType<SynthesEyesServer>();
        }
        
        visualizationRoot = new GameObject("Visualization_Root");
        visualizationRoot.transform.SetParent(transform);
    }
    
    public void ToggleVisualization()
    {
        visualizationEnabled = !visualizationEnabled;
        visualizationRoot.SetActive(visualizationEnabled);
        
        if (visualizationEnabled)
        {
            UpdateVisualizations();
        }
    }
    
    public void ClearVisualizations()
    {
        foreach (var obj in visualizationObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        
        visualizationObjects.Clear();
    }
    
    public void UpdateVisualizations()
    {
        if (!visualizationEnabled) return;
        
        ClearVisualizations();
        
        // Get references to meshes and transforms from SynthesEyesServer
        var eyeRegionMesh = synthesEyesServer.eyeRegionObj.GetComponent<MeshFilter>().mesh;
        var eyeRegionTransform = synthesEyesServer.eyeRegionObj.transform;
        
        var eyeballMesh = synthesEyesServer.eyeballObj.GetComponent<MeshFilter>().mesh;
        var eyeballTransform = synthesEyesServer.eyeballObj.transform;
        var eyeballController = synthesEyesServer.eyeballObj.GetComponent<EyeballController>();
        
        // Create visualizations
        CreatePointCloudVisualization(eyeRegionMesh, eyeRegionTransform, EyeRegionTopology.interior_margin_idxs, interiorMarginColor, "InteriorMargin");
        CreatePointCloudVisualization(eyeRegionMesh, eyeRegionTransform, EyeRegionTopology.caruncle_idxs, caruncleColor, "Caruncle");
        CreatePointCloudVisualization(eyeballMesh, eyeballTransform, EyeRegionTopology.iris_idxs, irisColor, "Iris");
        
        // Create gaze vector visualization
        var gazeVector = eyeballController.GetGazeVector();
        Vector3 eyeCenter = eyeballTransform.position;
        CreateGazeVectorVisualization(eyeballController.GetPupilCenter(), eyeballController.GetEyeLookVector(), gazeVectorColor, "GazeVector");
    }
    
    private Vector3 JsonToVector3(SimpleJSON.JSONNode node)
    {
        try
        {
            // First, debug the actual string we're receiving
            Debug.Log($"Parsing vector string: {node.Value}");
            
            // Clean the string more thoroughly
            string vectorStr = node.Value;
            vectorStr = vectorStr.Replace("(", "").Replace(")", "").Replace(" ", "");
            string[] components = vectorStr.Split(',');
            
            // Add error checking for number of components
            if (components.Length < 3)
            {
                Debug.LogError($"Vector string doesn't have enough components: {node.Value}");
                return Vector3.zero;
            }
            
            // Use TryParse instead of Parse for better error handling
            float x, y, z;
            if (!float.TryParse(components[0], out x))
            {
                Debug.LogError($"Failed to parse X component: {components[0]}");
                x = 0;
            }
            
            if (!float.TryParse(components[1], out y))
            {
                Debug.LogError($"Failed to parse Y component: {components[1]}");
                y = 0;
            }
            
            if (!float.TryParse(components[2], out z))
            {
                Debug.LogError($"Failed to parse Z component: {components[2]}");
                z = 0;
            }
            
            return new Vector3(x, y, z);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing vector: {e.Message}");
            return Vector3.zero;
        }
    }
    
    private void CreatePointCloudVisualization(Mesh sourceMesh, Transform sourceTransform, int[] indices, Color color, string name)
    {
        GameObject pointsRoot = new GameObject($"Visualization_{name}");
        pointsRoot.transform.SetParent(visualizationRoot.transform);
        visualizationObjects.Add(pointsRoot);
        
        foreach (var idx in indices)
        {
            Vector3 localPos = sourceMesh.vertices[idx];
            Vector3 worldPos = sourceTransform.TransformPoint(localPos);
            
            GameObject point = CreateSpherePoint(worldPos, color);
            point.transform.SetParent(pointsRoot.transform);
        }
    }
    
    private void CreateGazeVectorVisualization(Vector3 origin, Vector3 direction, Color color, string name)
    {
        GameObject arrowRoot = new GameObject($"Visualization_{name}");
        arrowRoot.transform.SetParent(visualizationRoot.transform);
        visualizationObjects.Add(arrowRoot);
        
        // Calculate the normalized direction and desired length
        Vector3 normalizedDir = direction.normalized;
        float arrowLength = 1f; // Total length of the arrow - smaller value
        float cylinderLength = arrowLength * 1f; // Length of the shaft
        float cylinderRadius = lineWidth * 0.3f; // Thickness of the shaft
        float coneHeight = arrowLength * 0.4f; // Height of the arrowhead
        float coneRadius = lineWidth * 0.3f; // Width of the arrowhead
        
        // Create the shaft (cylinder)
        GameObject shaftObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaftObj.name = "Arrow_Shaft";
        shaftObj.transform.SetParent(arrowRoot.transform);
        
        // Position and orient the shaft
        shaftObj.transform.position = origin + normalizedDir * (cylinderLength / 2f);
        shaftObj.transform.up = normalizedDir;
        shaftObj.transform.localScale = new Vector3(cylinderRadius, cylinderLength / 2f, cylinderRadius);
        
        // Create the arrowhead (using a scaled capsule since Unity doesn't have a cone primitive)
        GameObject coneObj = new GameObject("Arrow_Head");
        coneObj.transform.SetParent(arrowRoot.transform);
        coneObj.transform.position = origin + normalizedDir * (cylinderLength + coneHeight / 2f);
        coneObj.transform.up = normalizedDir;
        
        // Add mesh components to create a cone
        MeshFilter meshFilter = coneObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = coneObj.AddComponent<MeshRenderer>();
        
        // Create the cone mesh
        meshFilter.mesh = CreateConeMesh(coneRadius, coneHeight);
        
        // Apply materials to both objects
        Material arrowMaterial = new Material(Shader.Find("Standard"));
        arrowMaterial.color = color;
        arrowMaterial.SetFloat("_Glossiness", 0f);
        
        shaftObj.GetComponent<Renderer>().material = arrowMaterial;
        meshRenderer.material = arrowMaterial;
        
        // Remove colliders
        Destroy(shaftObj.GetComponent<Collider>());
    }

    private Mesh CreateConeMesh(float radius, float height)
    {
        Mesh mesh = new Mesh();
        
        // Define vertices
        int segments = 16;
        Vector3[] vertices = new Vector3[segments + 2];
        
        // Apex of the cone
        vertices[0] = Vector3.up * height * 0.5f;
        
        // Center of the base
        vertices[segments + 1] = Vector3.up * height * -0.5f;
        
        // Vertices around the base
        float angleStep = 2f * Mathf.PI / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            vertices[i + 1] = new Vector3(x, height * -0.5f, z);
        }
        
        // Define triangles
        int[] triangles = new int[segments * 6];
        
        // Triangles for the base and sides with correct winding order
        for (int i = 0; i < segments; i++)
        {
            int current = i + 1;
            int next = (i + 1) % segments + 1;
            
            // Triangle for bottom face (clock-wise when looking from outside/below)
            triangles[i * 3] = segments + 1; // Center of base
            triangles[i * 3 + 1] = current;
            triangles[i * 3 + 2] = next;
            
            // Triangle for side face (clock-wise when looking from outside)
            triangles[segments * 3 + i * 3] = 0; // Apex
            triangles[segments * 3 + i * 3 + 1] = next;
            triangles[segments * 3 + i * 3 + 2] = current;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        return mesh;
    }
    
    private GameObject CreateSpherePoint(Vector3 position, Color color)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * pointSize;
        
        // Configure renderer
        Renderer renderer = sphere.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = color;
        
        // Remove colliders to avoid physics interactions
        Collider collider = sphere.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
            
        return sphere;
    }
}