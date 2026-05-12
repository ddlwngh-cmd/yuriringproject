using System.Collections.Generic;
using UnityEngine;

public class InfiniteTilemapStreamer : MonoBehaviour
{
    [SerializeField] private GameObject defaultTilemapPrefab;
    [SerializeField] private Transform cameraTarget;
    [SerializeField, Min(0)] private int preloadRadius = 1;

    private readonly Dictionary<Vector2Int, GameObject> spawnedChunks = new();
    private Vector2 chunkSize;

    private void Start()
    {
        if (defaultTilemapPrefab == null)
        {
            Debug.LogError("InfiniteTilemapStreamer requires a defaultTilemapPrefab.");
            enabled = false;
            return;
        }

        if (cameraTarget == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTarget = mainCamera.transform;
            }
        }

        if (cameraTarget == null)
        {
            Debug.LogError("InfiniteTilemapStreamer requires a cameraTarget or a Main Camera.");
            enabled = false;
            return;
        }

        chunkSize = ResolveChunkSize(defaultTilemapPrefab);
        if (chunkSize.x <= 0f || chunkSize.y <= 0f)
        {
            Debug.LogError("InfiniteTilemapStreamer could not resolve a valid tilemap chunk size.");
            enabled = false;
            return;
        }

        RefreshChunks();
    }

    private void LateUpdate()
    {
        RefreshChunks();
    }

    private void RefreshChunks()
    {
        Vector2Int cameraChunk = WorldToChunk(cameraTarget.position);
        Vector2 halfView = GetCameraWorldHalfExtents();

        int horizontalCoverage = Mathf.CeilToInt(halfView.x / chunkSize.x) + preloadRadius;
        int verticalCoverage = Mathf.CeilToInt(halfView.y / chunkSize.y) + preloadRadius;

        for (int y = -verticalCoverage; y <= verticalCoverage; y++)
        {
            for (int x = -horizontalCoverage; x <= horizontalCoverage; x++)
            {
                Vector2Int chunkCoord = new(cameraChunk.x + x, cameraChunk.y + y);
                EnsureChunk(chunkCoord);
            }
        }
    }

    private void EnsureChunk(Vector2Int chunkCoord)
    {
        if (spawnedChunks.ContainsKey(chunkCoord))
        {
            return;
        }

        Vector3 spawnPosition = new(chunkCoord.x * chunkSize.x, chunkCoord.y * chunkSize.y, 0f);
        GameObject chunk = Instantiate(defaultTilemapPrefab, spawnPosition, Quaternion.identity, transform);
        chunk.name = $"Tilemap1_{chunkCoord.x}_{chunkCoord.y}";
        spawnedChunks.Add(chunkCoord, chunk);
    }

    private Vector2Int WorldToChunk(Vector3 worldPosition)
    {
        int chunkX = Mathf.RoundToInt(worldPosition.x / chunkSize.x);
        int chunkY = Mathf.RoundToInt(worldPosition.y / chunkSize.y);
        return new Vector2Int(chunkX, chunkY);
    }

    private Vector2 GetCameraWorldHalfExtents()
    {
        if (Camera.main == null || !Camera.main.orthographic)
        {
            return new Vector2(chunkSize.x, chunkSize.y);
        }

        float vertical = Camera.main.orthographicSize;
        float horizontal = vertical * Camera.main.aspect;
        return new Vector2(horizontal, vertical);
    }

    private Vector2 ResolveChunkSize(GameObject prefab)
    {
        GameObject tempChunk = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        tempChunk.hideFlags = HideFlags.HideAndDontSave;

        Renderer[] renderers = tempChunk.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

        foreach (Renderer renderer in renderers)
        {
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        Destroy(tempChunk);

        if (!hasBounds)
        {
            return Vector2.zero;
        }

        return bounds.size;
    }
}
