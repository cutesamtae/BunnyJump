/*****************************************************************************
 // File Name:         BunnyPlatformSpawner.cs
 // Author:            Hongseok Kim
 // Creation Date:     Nov 1, 2026
 //
 // Brief Description: Generates reachable endless platforms and removes old
 //                    ones from a List.
 *****************************************************************************/
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns ahead of the camera using height rather than a timer.
/// </summary>
public class BunnyPlatformSpawner : MonoBehaviour
{
    /// <summary>
    /// Distance below the camera before removing a platform.
    /// </summary>
    private const float CleanupMargin = 2f;

    /// <summary>
    /// Distance above the camera to keep populated.
    /// </summary>
    private const float SpawnMargin = 3f;

    /// <summary>
    /// Keeps each platform inside the horizontal view.
    /// </summary>
    private const float PlatformEdgeMargin = 1.25f;

    /// <summary>
    /// Largest horizontal step in the reachable route.
    /// </summary>
    private const float MaximumHorizontalGap = 3.2f;

    [Tooltip("One-way platform prefab to create.")]
    [SerializeField]
    private BunnyPlatform platformPrefab;

    [Tooltip("Camera that determines spawning and cleanup bounds.")]
    [SerializeField]
    private Camera gameCamera;

    [Tooltip("Controller used to stop spawning after the run ends.")]
    [SerializeField]
    private BunnyGameController controller;

    [Tooltip("Smallest vertical gap between platforms.")]
    [SerializeField]
    private float minimumGap = 1.15f;

    [Tooltip("Largest vertical gap; stays below the " +
        "bunny's maximum jump height.")]
    [SerializeField]
    private float maximumGap = 1.75f;

    /// <summary>
    /// The landing prefab used to verify isolated collisions.
    /// </summary>
    public BunnyPlatform PlatformPrefab
    {
        get
        {
            return platformPrefab;
        }
    }

    /// <summary>
    /// All live platforms, including the initial landing platform.
    /// </summary>
    private List<BunnyPlatform> platforms = new List<BunnyPlatform>();

    /// <summary>
    /// Position of the last generated platform.
    /// </summary>
    private Vector2 lastPosition = new Vector2(0f, -3.5f);

    /// <summary>
    /// Creates a safe starting platform and fills the first screen.
    /// </summary>
    private void Start()
    {
        CreatePlatform(lastPosition);
        SpawnAhead();
    }

    /// <summary>
    /// Extends the route and removes platforms below the view.
    /// </summary>
    private void Update()
    {
        if (controller.IsGameOver)
        {
            return;
        }

        SpawnAhead();
        float bottom =
            gameCamera.transform.position.y - gameCamera.orthographicSize -
            CleanupMargin;
        // Walk backward so removal cannot skip the next list entry.
        for (int i = platforms.Count - 1; i >= 0; i--)
        {
            if (platforms[i].transform.position.y < bottom)
            {
                Destroy(platforms[i].gameObject);
                platforms.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Builds platforms above the visible area with reachable horizontal gaps.
    /// </summary>
    private void SpawnAhead()
    {
        float top = gameCamera.transform.position.y +
            gameCamera.orthographicSize + SpawnMargin;
        float edge =
            gameCamera.orthographicSize * gameCamera.aspect -
            PlatformEdgeMargin;
        while (lastPosition.y < top)
        {
            lastPosition.y += Random.Range(minimumGap, maximumGap);
            // Limit consecutive horizontal gaps so there is always a reachable
            // route.
            lastPosition.x = Random.Range(Mathf.Max(-edge,
                lastPosition.x - MaximumHorizontalGap), Mathf.Min(edge,
                lastPosition.x + MaximumHorizontalGap));
            CreatePlatform(lastPosition);
        }
    }

    /// <summary>
    /// Instantiates a platform and records it in the management list.
    /// </summary>
    /// <param name="position">World position of the new platform.</param>
    private void CreatePlatform(Vector2 position)
    {
        BunnyPlatform platform = Instantiate(platformPrefab, position,
            Quaternion.identity, transform);
        platforms.Add(platform);
    }
}
