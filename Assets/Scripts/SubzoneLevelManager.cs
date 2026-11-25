using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubzoneLevelManager : MonoBehaviour
{
    public GameObject Player;
    private Rigidbody2D rigidBody;
    private PlayerMovement movement;
    // Captured in Awake before IntroWreckedShipDialogue.Start marks the intro as shown.
    private bool skipAutoWalkForWreckedShipIntro;

    private void Awake()
    {
        movement = Player.GetComponent<PlayerMovement>();
        skipAutoWalkForWreckedShipIntro =
            GetComponent<IntroWreckedShipDialogue>() != null
            && !OverworldSubzoneContainer.HasShownWreckedShipIntro;
    }

    private void Start()
    {
        if (OverworldSubzoneContainer.UseSubzoneLevelStartPosition)
        {
            OverworldSubzoneContainer.UseSubzoneLevelStartPosition = false;

            rigidBody = Player.GetComponent<Rigidbody2D>();
            rigidBody.transform.position = OverworldSubzoneContainer.SubzoneLevelStartPosition;

            movement.SetDirection(OverworldSubzoneContainer.SubzoneLevelStartDirection);
        }

        // Keep controls locked until StandUp() / conversation end on first visit.
        if (skipAutoWalkForWreckedShipIntro)
        {
            return;
        }

        StartCoroutine(movement.AutoWalk(0.8f, OverworldSubzoneContainer.SubzoneLevelStartDirection));
    }

    // Update is called once per frame
    void Update()
    {

    }
}
