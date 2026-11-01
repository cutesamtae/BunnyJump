/*****************************************************************************
 // File Name:         BunnyCamera.cs
 // Author:            Hongseok Kim
 // Creation Date:     Nov 1, 2026
 //
 // Brief Description: Follows the bunny upward without moving downward.
 *****************************************************************************/
using UnityEngine;

/// <summary>
/// Keeps the highest camera position reached during the run.
/// </summary>
public class BunnyCamera : MonoBehaviour
{
    [Tooltip("Bunny that the camera follows.")]
    [SerializeField]
    private Transform player;

    [Tooltip("Game controller used to stop following after game over.")]
    [SerializeField]
    private BunnyGameController controller;

    [Tooltip("Distance the bunny can rise above camera" +
        " center before following.")]
    [SerializeField]
    private float followOffset = 0.5f;

    /// <summary>
    /// Moves upward only, so missed platforms can cause a fall off screen.
    /// </summary>
    private void LateUpdate()
    {
        if (controller.IsGameOver)
        {
            return;
        }

        Vector3 position = transform.position;
        position.y = Mathf.Max(position.y, player.position.y - followOffset);
        transform.position = position;
    }
}
