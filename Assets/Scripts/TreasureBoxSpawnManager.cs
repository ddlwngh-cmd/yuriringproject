using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TreasureBoxSpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField, InspectorName("TreasureBoxPrefab")] private TreasureBoxController treasureBoxPrefab;
    [SerializeField, InspectorName("SpawnCooldown"), Min(0.01f)] private float spawnCooldown = 30f;
    [SerializeField, InspectorName("InitialDelay"), Min(0f)] private float initialDelay = 10f;
    [SerializeField, InspectorName("MaxActiveBoxes"), Min(0)] private int maxActiveBoxes = 3;

    [Header("Spawn Area")]
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Camera targetCamera;
    [SerializeField, Min(0f)] private float outsideViewPadding = 2f;
    [SerializeField, Min(0f)] private float randomDistanceRange = 5f;

    private readonly List<TreasureBoxController> activeBoxes = new();
    private float spawnTimer;

    public TreasureBoxController TreasureBoxPrefab
    {
        get => treasureBoxPrefab;
        set => treasureBoxPrefab = value;
    }

    public float SpawnCooldown
    {
        get => spawnCooldown;
        set => spawnCooldown = Mathf.Max(0.01f, value);
    }

    public float InitialDelay
    {
        get => initialDelay;
        set => initialDelay = Mathf.Max(0f, value);
    }

    public int MaxActiveBoxes
    {
        get => maxActiveBoxes;
        set => maxActiveBoxes = Mathf.Max(0, value);
    }

    public int ActiveBoxCount
    {
        get
        {
            RemoveDestroyedBoxes();
            return activeBoxes.Count;
        }
    }

    private void Start()
    {
        ResolveReferences();
        RegisterExistingBoxes();
        spawnTimer = initialDelay;

        if (playerTarget == null || targetCamera == null)
        {
            Debug.LogError("TreasureBoxSpawnManager requires both a player target and a camera.", this);
            enabled = false;
        }
    }

    private void OnValidate()
    {
        spawnCooldown = Mathf.Max(0.01f, spawnCooldown);
        initialDelay = Mathf.Max(0f, initialDelay);
        maxActiveBoxes = Mathf.Max(0, maxActiveBoxes);
        outsideViewPadding = Mathf.Max(0f, outsideViewPadding);
        randomDistanceRange = Mathf.Max(0f, randomDistanceRange);
    }

    private void Update()
    {
        if (GamePauseState.IsGameplayPaused)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }

        spawnTimer = spawnCooldown;
        RemoveDestroyedBoxes();

        if (treasureBoxPrefab == null || activeBoxes.Count >= maxActiveBoxes)
        {
            return;
        }

        SpawnTreasureBox();
    }

    private void ResolveReferences()
    {
        if (playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void RegisterExistingBoxes()
    {
        TreasureBoxController[] existingBoxes = FindObjectsByType<TreasureBoxController>(FindObjectsSortMode.None);
        for (int i = 0; i < existingBoxes.Length; i++)
        {
            TreasureBoxController box = existingBoxes[i];
            if (box != null && box.gameObject.activeInHierarchy && !activeBoxes.Contains(box))
            {
                activeBoxes.Add(box);
            }
        }
    }

    private void SpawnTreasureBox()
    {
        Vector3 spawnPosition = GetRandomPositionOutsideCamera();
        TreasureBoxController spawnedBox = Instantiate(treasureBoxPrefab, spawnPosition, Quaternion.identity);
        activeBoxes.Add(spawnedBox);
    }

    private Vector3 GetRandomPositionOutsideCamera()
    {
        Vector3 playerPosition = playerTarget.position;
        float planeDistance = Mathf.Abs(targetCamera.transform.position.z - playerPosition.z);
        Vector3[] viewportCorners =
        {
            targetCamera.ViewportToWorldPoint(new Vector3(0f, 0f, planeDistance)),
            targetCamera.ViewportToWorldPoint(new Vector3(0f, 1f, planeDistance)),
            targetCamera.ViewportToWorldPoint(new Vector3(1f, 0f, planeDistance)),
            targetCamera.ViewportToWorldPoint(new Vector3(1f, 1f, planeDistance))
        };

        float outsideRadius = 0f;
        for (int i = 0; i < viewportCorners.Length; i++)
        {
            outsideRadius = Mathf.Max(outsideRadius, Vector2.Distance(playerPosition, viewportCorners[i]));
        }

        float angleRadians = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        Vector2 direction = new(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
        float distance = outsideRadius + outsideViewPadding + UnityEngine.Random.Range(0f, randomDistanceRange);
        Vector2 position = (Vector2)playerPosition + direction * distance;
        return new Vector3(position.x, position.y, playerPosition.z);
    }

    private void RemoveDestroyedBoxes()
    {
        activeBoxes.RemoveAll(box => box == null || !box.gameObject.activeInHierarchy);
    }
}
