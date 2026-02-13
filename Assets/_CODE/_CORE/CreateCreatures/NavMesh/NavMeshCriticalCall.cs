using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEngine;

public class NavMeshCriticalCall : MonoBehaviour
{
    void Awake()
    {
        var surface = GetComponent<NavMeshSurface>();
        surface.AddData();
    }
    private void Start()
    {
        Debug.Log("AI vertices length: " + NavMesh.CalculateTriangulation().vertices.Length);
    }
}