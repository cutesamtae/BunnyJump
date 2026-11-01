/*****************************************************************************
 // File Name:         BunnyGameplayChecks.cs
 // Author:            Hongseok Kim (with OpenAI Codex assistance)
 // Creation Date:     Nov 1, 2026
 // Brief Description: Runs integration checks on the active Play mode scene.
 *****************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Checks gameplay using the real scene components and physics.
/// </summary>
public static class BunnyGameplayChecks
{
    /// <summary>
    /// Tests wrapping, camera, scoring, platform cleanup, collisions, and game
    /// over.
    /// </summary>
    [MenuItem("Bunny Jump/Check Running Game")]
    public static void CheckGame()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("Enter Play mode before running checks.");
            return;
        }

        BunnyGameController controller =
            UnityEngine.Object.FindAnyObjectByType<BunnyGameController>();
        if (controller.IsGameOver)
        {
            controller.RestartGame();
            Debug.LogWarning("Restarted. Run checks again.");
            return;
        }

        SimulationMode2D oldMode = Physics2D.simulationMode;
        try
        {
            Physics2D.simulationMode = SimulationMode2D.Script;
            BunnyPlayer player = controller.Player;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            body.interpolation = RigidbodyInterpolation2D.None;
            Camera camera = controller.GameCamera;
            BunnyCamera follow = camera.GetComponent<BunnyCamera>();
            BunnyPlatformSpawner spawner =
                UnityEngine.Object.FindAnyObjectByType<BunnyPlatformSpawner>();
            float half = camera.orthographicSize * camera.aspect;
            body.position = new Vector2(half + 0.2f, 1f);
            player.transform.position = new Vector3(body.position.x,
                body.position.y, 0f);
            player.SendMessage("FixedUpdate");
            Require(Mathf.Abs(body.position.x - (-half + 0.2f)) < 0.05f,
                "right-to-left wrapping");
            body.position = new Vector2(-half - 0.2f, 1f);
            player.transform.position = new Vector3(body.position.x,
                body.position.y, 0f);
            player.SendMessage("FixedUpdate");
            Require(Mathf.Abs(body.position.x - (half - 0.2f)) < 0.05f,
                "left-to-right wrapping");
            body.position = new Vector2(0f, 50f);
            player.transform.position = new Vector3(body.position.x,
                body.position.y, 0f);
            Physics2D.SyncTransforms();
            follow.SendMessage("LateUpdate");
            float highCamera = camera.transform.position.y;
            Require(Mathf.Abs(highCamera - 49.5f) < 0.1f,
                "camera follows upward");
            controller.SendMessage("Update");
            string highScore = controller.ScoreText.text;
            body.position = new Vector2(0f, 48f);
            player.transform.position = new Vector3(body.position.x,
                body.position.y, 0f);
            Physics2D.SyncTransforms();
            follow.SendMessage("LateUpdate");
            controller.SendMessage("Update");
            Require(camera.transform.position.y == highCamera,
                "camera never follows downward");
            Require(controller.ScoreText.text == highScore,
                "score does not decrease on a fall");
            FieldInfo listField = typeof(BunnyPlatformSpawner).GetField(
                "platforms", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int height = 50; height <= 1000; height += 50)
            {
                camera.transform.position = new Vector3(0f, height, -10f);
                spawner.SendMessage("Update");
                List<BunnyPlatform> platforms = (
                    List<BunnyPlatform>)listField.GetValue(spawner);
                Require(platforms.Count > 0 && platforms.Count < 20,
                    "bounded platform List at height " + height);
                float bottom = height - camera.orthographicSize - 2f;
                Require(platforms.TrueForAll(p => p != null &&
                    p.transform.position.y >= bottom),
                    "old platforms removed at height " + height);
            }

            Require(controller.Music.clip != null &&
                controller.Music.isPlaying && controller.Music.loop,
                "looping background music");
            Require(controller.BounceClip != null &&
                controller.GameOverClip != null,
                "both required sound effects assigned");
            // Isolate the landing prefab so unrelated platforms cannot affect
            // collision checks.
            foreach (
                BunnyPlatform existing in UnityEngine.Object
                .FindObjectsByType<BunnyPlatform>(FindObjectsSortMode.None))
            {
                existing.GetComponent<Collider2D>().enabled = false;
            }

            BunnyPlatform testPlatform = UnityEngine.Object.Instantiate(
                spawner.PlatformPrefab, new Vector3(0f, 1000f, 0f),
                Quaternion.identity);
            body.position = new Vector2(0f, 1001.2f);
            player.transform.position = new Vector3(body.position.x,
                body.position.y, 0f);
            body.linearVelocity = new Vector2(0f, -2f);
            Physics2D.SyncTransforms();
            bool bounced = false;
            for (int i = 0; i < 30; i++)
            {
                player.SendMessage("FixedUpdate");
                Physics2D.Simulate(0.02f);
                if (body.linearVelocity.y > 8f)
                {
                    bounced = true;
                    break;
                }
            }

            Require(bounced, "falling onto platform top causes " +
                "automatic bounce");
            body.position = new Vector2(0f, 999f);
            player.transform.position = new Vector3(body.position.x,
                body.position.y, 0f);
            body.linearVelocity = new Vector2(0f, 10f);
            Physics2D.SyncTransforms();
            bool falseBounce = false;
            for (int i = 0; i < 15; i++)
            {
                player.SendMessage("FixedUpdate");
                Physics2D.Simulate(0.02f);
                if (body.linearVelocity.y > 10.1f)
                {
                    falseBounce = true;
                }
            }

            Require(!falseBounce && body.position.y > 1000.3f,
                "passes upward through platform without underside bounce");
            body.position = new Vector2(1.15f, 999.95f);
            player.transform.position = new Vector3(body.position.x,
                body.position.y, 0f);
            body.linearVelocity = new Vector2(0f, -1f);
            Physics2D.SyncTransforms();
            falseBounce = false;
            for (int i = 0; i < 10; i++)
            {
                player.SendMessage("FixedUpdate");
                Physics2D.Simulate(0.02f);
                if (body.linearVelocity.y > 5f)
                {
                    falseBounce = true;
                }
            }

            Require(!falseBounce, "side contact does not cause bounce");
            body.position = new Vector2(0f,
                camera.transform.position.y - camera.orthographicSize - 2f);
            player.transform.position = new Vector3(body.position.x,
                body.position.y, 0f);
            Physics2D.SyncTransforms();
            controller.SendMessage("Update");
            Require(controller.IsGameOver && Time.timeScale == 0f &&
                controller.GameOverPanel.activeSelf,
                "fall ends and pauses the game with visible overlay");
            Debug.Log("BUNNY QA: ALL CHECKS PASSED");
        }
        catch (Exception exception)
        {
            Debug.LogError("BUNNY QA: FAILED " + exception);
        }
        finally
        {
            Physics2D.simulationMode = oldMode;
            Time.timeScale = 1f;
            controller.RestartGame();
        }
    }

    /// <summary>
    /// Reports a failed expectation and stops the test run.
    /// </summary>
    /// <param name="condition">Whether the expectation was satisfied.</param>
    /// <param name="description">Behavior being checked.</param>
    private static void Require(bool condition, string description)
    {
        if (!condition)
        {
            throw new Exception(description);
        }

        Debug.Log("BUNNY QA: PASS " + description);
    }
}
