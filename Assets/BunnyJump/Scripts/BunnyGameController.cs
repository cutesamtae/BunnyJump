/*****************************************************************************
 // File Name:         BunnyGameController.cs
 // Author:            Hongseok Kim
 // Creation Date:     Nov 1, 2026
 //
 // Brief Description: Tracks highest height, UI, audio, game over, restart, and
 //                    quit.
 *****************************************************************************/
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the endless run and keeps the score from decreasing.
/// </summary>
public class BunnyGameController : MonoBehaviour
{
    /// <summary>
    /// Score earned per world unit above the starting height.
    /// </summary>
    private const float PointsPerUnit = 100f;

    /// <summary>
    /// Allows the full bunny to leave the view before losing.
    /// </summary>
    private const float FallMargin = 0.7f;

    /// <summary>
    /// Volume of the landing sound effect.
    /// </summary>
    private const float BounceVolume = 0.6f;

    /// <summary>
    /// Volume of the losing sound effect.
    /// </summary>
    private const float GameOverVolume = 0.8f;

    [Tooltip("Player used to measure height and detect" +
        " falling off screen.")]
    [SerializeField]
    private BunnyPlayer player;

    [Tooltip("Camera used to find the bottom of the visible area.")]
    [SerializeField]
    private Camera gameCamera;

    [Tooltip("Text showing the highest score.")]
    [SerializeField]
    private Text scoreText;

    [Tooltip("Overlay displayed when the run ends.")]
    [SerializeField]
    private GameObject gameOverPanel;

    [Tooltip("Text showing the final score.")]
    [SerializeField]
    private Text finalScoreText;

    [Tooltip("Audio source used for bounce and game-over effects.")]
    [SerializeField]
    private AudioSource effects;

    [Tooltip("Effect played when the bunny lands.")]
    [SerializeField]
    private AudioClip bounceClip;

    [Tooltip("Effect played when the bunny falls off screen.")]
    [SerializeField]
    private AudioClip gameOverClip;

    [Tooltip("Looping background music source.")]
    [SerializeField]
    private AudioSource music;

    /// <summary>
    /// The bunny whose motion is inspected by gameplay checks.
    /// </summary>
    public BunnyPlayer Player
    {
        get
        {
            return player;
        }
    }

    /// <summary>
    /// The camera used to inspect visible gameplay bounds.
    /// </summary>
    public Camera GameCamera
    {
        get
        {
            return gameCamera;
        }
    }

    /// <summary>
    /// The score label inspected to verify height scoring.
    /// </summary>
    public Text ScoreText
    {
        get
        {
            return scoreText;
        }
    }

    /// <summary>
    /// The overlay inspected to verify the losing state.
    /// </summary>
    public GameObject GameOverPanel
    {
        get
        {
            return gameOverPanel;
        }
    }

    /// <summary>
    /// The effects source configured by the scene builder.
    /// </summary>
    public AudioSource Effects
    {
        get
        {
            return effects;
        }
    }

    /// <summary>
    /// The landing clip inspected by the gameplay checks.
    /// </summary>
    public AudioClip BounceClip
    {
        get
        {
            return bounceClip;
        }
    }

    /// <summary>
    /// The losing clip inspected by the gameplay checks.
    /// </summary>
    public AudioClip GameOverClip
    {
        get
        {
            return gameOverClip;
        }
    }

    /// <summary>
    /// The music source configured and checked in the editor.
    /// </summary>
    public AudioSource Music
    {
        get
        {
            return music;
        }
    }

    /// <summary>
    /// The label used to display the final height after losing.
    /// </summary>
    public Text FinalScoreText
    {
        get
        {
            return finalScoreText;
        }
    }

    /// <summary>
    /// Whether gameplay has stopped after losing.
    /// </summary>
    private bool isGameOver;

    /// <summary>
    /// The highest Y position reached, measured relative to the start.
    /// </summary>
    private float highestHeight;

    /// <summary>
    /// The initial player height used as the score origin.
    /// </summary>
    private float startingHeight;

    /// <summary>
    /// Whether the current run has ended.
    /// </summary>
    public bool IsGameOver
    {
        get
        {
            return isGameOver;
        }
    }

    /// <summary>
    /// Starts the run immediately when the scene loads.
    /// </summary>
    private void Start()
    {
        Time.timeScale = 1f;
        startingHeight = player.transform.position.y;
        gameOverPanel.SetActive(false);
        scoreText.text = "HEIGHT  0";
    }

    /// <summary>
    /// Handles shortcuts, highest-height scoring, and the fail condition.
    /// </summary>
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.rKey.wasPressedThisFrame)
            {
                RestartGame();
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Application.Quit();
            }
        }

        if (isGameOver)
        {
            return;
        }

        highestHeight = Mathf.Max(highestHeight,
            player.transform.position.y - startingHeight);
        scoreText.text = "HEIGHT  " + Mathf.FloorToInt(
            highestHeight * PointsPerUnit);
        float bottom =
            gameCamera.transform.position.y - gameCamera.orthographicSize;
        if (player.transform.position.y < bottom - FallMargin)
        {
            EndGame();
        }
    }

    /// <summary>
    /// Plays landing feedback while the game is active.
    /// </summary>
    public void PlayBounce()
    {
        if (!isGameOver)
        {
            effects.PlayOneShot(bounceClip, BounceVolume);
        }
    }

    /// <summary>
    /// Stops physics and music, plays feedback, and shows the final score.
    /// </summary>
    private void EndGame()
    {
        isGameOver = true;
        music.Stop();
        effects.PlayOneShot(gameOverClip, GameOverVolume);
        finalScoreText.text = "HEIGHT  " + Mathf.FloorToInt(
            highestHeight * PointsPerUnit);
        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Reloads this scene and restores time after game over.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Restores normal time when leaving the scene or stopping Play mode.
    /// </summary>
    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
