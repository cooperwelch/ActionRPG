using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager sharedInstance;

    [SerializeField] private GameObject pauseScreenPrefab;
    [SerializeField] private GameObject gameOverScreenPrefab;
    private GameObject _pauseScreen;
    private GameObject _gameOverScreen;
    private bool _isPaused = false;
    private bool _isGameOver = false;
    public bool IsPaused => _isPaused;
    public bool IsGameOver => _isGameOver;
    private string _continueLevelName;

    private void Awake()
    {
        if (sharedInstance != null && sharedInstance != this)
        {
            Destroy(this);
            return;
        } else
        {
            sharedInstance = this;
            DontDestroyOnLoad(gameObject);
        }

        InitializeMenuPrefabs();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Cancel")
            && !_isGameOver
            && !GameplayUI.IsBlocking
            && Time.frameCount > GameplayUI.LastClosedFrame)
        {
            _pauseScreen.SetActive(!_isPaused);
            _isPaused = _pauseScreen.activeInHierarchy;
            TogglePlayerMovement(_isPaused);
            Time.timeScale = _isPaused ? 0f : 1f;
        }
        if (Input.GetButtonDown("Fire2") && (_isPaused || _isGameOver))
        {
            QuitGame();
        }
    }

    public void ShowGameOver(string levelName)
    {
        _continueLevelName = levelName;
        _gameOverScreen.SetActive(true);
        _isGameOver = true;
        TogglePlayerMovement(true);
    }

    public void InitializeMenuPrefabs()
    {
        _pauseScreen = GameObject.Instantiate(pauseScreenPrefab);
        DontDestroyOnLoad(_pauseScreen);
        _pauseScreen.SetActive(false);
        _gameOverScreen = GameObject.Instantiate(gameOverScreenPrefab);
        DontDestroyOnLoad(_gameOverScreen);
        _gameOverScreen.SetActive(false);
    }

    public void ContinueFromGameOver()
    {
        Time.timeScale = 1f;
        PlayerStats.ResetHealthForContinue();
        SceneManager.LoadScene(_continueLevelName);
        SceneManager.sceneLoaded += DisableGameOverScreen; // subscribe a delegate function to disable the game over screen after the scene load is finished
    }

    public void QuitGame()
    {
#if UNITY_STANDALONE
        Application.Quit();
#endif
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void DisableGameOverScreen(Scene scene, LoadSceneMode mode)
    {
        _gameOverScreen.SetActive(false);
        _isGameOver = false;

        SceneManager.sceneLoaded -= DisableGameOverScreen; // unsubscribe this function after removing the game over screen
    }

    private void TogglePlayerMovement(bool shouldStop)
    {
        PlayerMovement playerMovement = GameObject.FindAnyObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            if (shouldStop)
            {
                playerMovement.Stop();
            }
            else
            {
                playerMovement.AllowMovement();
            }
        }
        else
        {
            TopDownMovement overworldMovement = GameObject.FindAnyObjectByType<TopDownMovement>();
            if (overworldMovement != null)
            {
                if (shouldStop)
                {
                    overworldMovement.StopMovement();
                }
                else
                {
                    overworldMovement.AllowMovement();
                }
            }
        }
    }
}
