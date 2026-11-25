using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TitleScreenUI : MonoBehaviour
{

    [SerializeField] private Image menuCursor;
    [SerializeField] private List<GameObject> menuCursorPositions = new List<GameObject>();
    [SerializeField] private List<TextMeshProUGUI> menuItemsText = new List<TextMeshProUGUI>();
    [SerializeField] private Color inactiveMenuTextColor;
    [SerializeField] private Color activeMenuTextColor;

    [SerializeField] public string nextSceneName;
    [SerializeField] public float delay = 5f;

    [SerializeField] GameObject faderObject;
    [SerializeField] private Image controlsScreen;
    [SerializeField] private TextMeshProUGUI backKeyTextLabel;
    [SerializeField] private Image backKeyGraphic;

    private Image image;
    private bool isAdvancing = false;

    private List<Action> menuActions = new List<Action>(); // a list of delegate functions for menu actions

    private int currentMenuItemIndex = 0;

    private float axisInputDelayDuration = 0.5f; // add a delay of .5 seconds between switching menu items when holding a direction
    private bool acceptingAxisInputUp = true;
    private bool acceptingAxisInputDown = true;
    private Coroutine delayDownCoroutine;
    private Coroutine delayUpCoroutine;

    private enum ScreenState
    {
        Title,
        Controls
    }
    private ScreenState screenState;

    [SerializeField] private KeyCode backKey = KeyCode.E;


    void Start()
    {
        screenState = ScreenState.Title;

        if (faderObject != null)
        {
            image = faderObject.GetComponent<Image>();
            image.color = new Color(image.color.r, image.color.g, image.color.b, 0);
        }

        // set starting position of the menu cursor to first menu item
        menuCursor.transform.position = menuCursorPositions[0].transform.position;

        // Add functions to the menu actions list
        menuActions.Add(() => StartCoroutine(StartGame()));
        menuActions.Add(() => ShowControlsScreen());
        menuActions.Add(() => GameManager.sharedInstance.QuitGame());
    }

    void Update()
    {

        switch (screenState)
        {
            case ScreenState.Title:

                float inputVertical = Input.GetAxisRaw("Vertical") + Input.GetAxisRaw("DPadY");

                // axis input down
                if (inputVertical < 0 && acceptingAxisInputDown == true)
                {
                    acceptingAxisInputDown = false;
                    acceptingAxisInputUp = true; // should immediately be able to move up after pressing down

                    if (delayDownCoroutine != null)
                    {
                        StopCoroutine(delayDownCoroutine);
                    }
                    delayDownCoroutine = StartCoroutine(DelayAxisInputDown());

                    currentMenuItemIndex++;
                    // if it goes past the last menu item, then start back at the beginning
                    if (currentMenuItemIndex > (menuActions.Count - 1))
                    {
                        currentMenuItemIndex = 0;
                    }

                    // move cursor to the next menu item
                    menuCursor.transform.position = new Vector3(menuCursorPositions[currentMenuItemIndex].transform.position.x, menuCursorPositions[currentMenuItemIndex].transform.position.y, menuCursorPositions[currentMenuItemIndex].transform.position.z);
                    menuCursor.gameObject.GetComponent<Animator>().Play("Menu Cursor", -1, 0f);

                    // update all menu item text colors
                    UpdateMenuItemsTextColors();

                }

                // axis input up
                else if (inputVertical > 0 && acceptingAxisInputUp == true)
                {
                    acceptingAxisInputUp = false;
                    acceptingAxisInputDown = true;

                    if (delayUpCoroutine != null)
                    {
                        StopCoroutine(delayUpCoroutine);
                    }
                    delayUpCoroutine = StartCoroutine(DelayAxisInputUp());

                    currentMenuItemIndex--;
                    // if it goes below the first menu item, then start at the top
                    if (currentMenuItemIndex < 0)
                    {
                        currentMenuItemIndex = menuActions.Count - 1;
                    }

                    // move cursor to the next menu item
                    menuCursor.transform.position = new Vector3(menuCursorPositions[currentMenuItemIndex].transform.position.x, menuCursorPositions[currentMenuItemIndex].transform.position.y, menuCursorPositions[currentMenuItemIndex].transform.position.z);
                    menuCursor.gameObject.GetComponent<Animator>().Play("Menu Cursor", -1, 0f);

                    // update all menu item text colors
                    UpdateMenuItemsTextColors();
                }

                // confirm button
                if (!isAdvancing && Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    //do menu action based on currently selected menu item
                    menuActions[currentMenuItemIndex]();
                }

                // reset rate limit when releasing a direction
                if (inputVertical == 0)
                {
                    acceptingAxisInputDown = true;
                    acceptingAxisInputUp = true;
                }

                break;

            case ScreenState.Controls:
                if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(backKey))
                {
                    // hide controls screen
                    HideControlsScreen();
                }
                break;


        }
        

    }

    private void ShowControlsScreen()
    {
        controlsScreen.color = new Color(controlsScreen.color.r, controlsScreen.color.g, controlsScreen.color.b, 1.0f);
        backKeyTextLabel.color = new Color(backKeyTextLabel.color.r, backKeyTextLabel.color.g, backKeyTextLabel.color.b, 1.0f);
        backKeyGraphic.color = new Color(backKeyGraphic.color.r, backKeyGraphic.color.g, backKeyGraphic.color.b, 1.0f);

        screenState = ScreenState.Controls;
    }

    private void HideControlsScreen()
    {
        controlsScreen.color = new Color(controlsScreen.color.r, controlsScreen.color.g, controlsScreen.color.b, 0);
        backKeyTextLabel.color = new Color(backKeyTextLabel.color.r, backKeyTextLabel.color.g, backKeyTextLabel.color.b, 0);
        backKeyGraphic.color = new Color(backKeyGraphic.color.r, backKeyGraphic.color.g, backKeyGraphic.color.b, 0);

        screenState = ScreenState.Title;
    }

    private void UpdateMenuItemsTextColors()
    {
        for (int i = 0; i < menuItemsText.Count; i++)
        {
            if (i == currentMenuItemIndex)
            {
                menuItemsText[i].color = activeMenuTextColor;
            }
            else
            {
                menuItemsText[i].color = inactiveMenuTextColor;
            }
        }
    }

    private IEnumerator DelayAxisInputDown()
    {
        float elapsedTime = 0f;

        while (elapsedTime < axisInputDelayDuration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        acceptingAxisInputDown = true;
        delayDownCoroutine = null;
    }

    private IEnumerator DelayAxisInputUp()
    {
        float elapsedTime = 0f;

        while (elapsedTime < axisInputDelayDuration)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        acceptingAxisInputUp = true;
        delayUpCoroutine = null;
    }

    // based on Robert Penner's easing functions,
    // takes the current lerp time value and interpolates it to a quartic ease out curve
    private float QuarticEaseOut(float t)
    {
        float f = (t - 1);
        return f * f * f * (1 - t) + 1;
    }

    private IEnumerator StartGame()
    {
        isAdvancing = true;
        //FindObjectOfType<AudioManager>().Play("MenuClick");

        // do a fade out if the image exists
        if (image != null)
        {
            while (image.color.a < 1.0f)
            {
                image.color = new Color(image.color.r, image.color.g, image.color.b, image.color.a + 0.2f);
                yield return new WaitForSeconds(0.08f);
            }
            yield return new WaitForSeconds(2.0f); // short delay after fade out before advancing
        }
        else
        {
            print("No fader object specified on AdvanceScene script");
        }

        // Fresh run: wipe any stats/progress left over from a previous playthrough this session.
        GameState.NewGame();
        SceneManager.LoadScene(nextSceneName);

        yield return null;
    }

}
