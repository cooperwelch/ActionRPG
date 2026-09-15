using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopDownMovement : MonoBehaviour
{
    public float moveSpeed;
    private Animator animator;
    private Rigidbody2D rigidBody;
    private bool stopMovement = false;
    private Vector2 movement;
    private bool endAnimationOverride = false;

    public bool CanMove => !stopMovement && !endAnimationOverride;

    private void Awake()
    {
        // Set position to last encounter
        rigidBody = gameObject.GetComponent<Rigidbody2D>();
        rigidBody.transform.position = new Vector3(
            OverworldSubzoneContainer.LastEncounterPosition.Item1,
            OverworldSubzoneContainer.LastEncounterPosition.Item2
        );

        animator = gameObject.GetComponent<Animator>();
        animator.SetFloat("speed", 1);

        switch (OverworldSubzoneContainer.LastEncounterDirection)
        {
            case OverworldSubzoneContainer.PlayerDirection.Up:
                animator.SetFloat("vertical", 1);
                break;
            case OverworldSubzoneContainer.PlayerDirection.Down:
                animator.SetFloat("vertical", -1);
                break;
            case OverworldSubzoneContainer.PlayerDirection.Left:
                animator.SetFloat("horizontal", -1);
                break;
            case OverworldSubzoneContainer.PlayerDirection.Right:
                animator.SetFloat("horizontal", 1);
                break;
        }
    }

    void Update()
    {
        if (endAnimationOverride)
        {
            movement = new Vector2(1f, 0f);
            UpdateAnimation(movement);
            return;
        }
        if (stopMovement) {
            movement = Vector2.zero;
            return;
        }

        movement.x = Input.GetAxisRaw("Horizontal") + Input.GetAxisRaw("DPadX");
        movement.y = Input.GetAxisRaw("Vertical") + Input.GetAxisRaw("DPadY");

        if (Mathf.Abs(movement.x) >= 1f)
        {
            movement.y = 0f;
            UpdateAnimation(movement);
        }
        if (Mathf.Abs(movement.y) >= 1f)
        {
            movement.x = 0f;
            UpdateAnimation(movement);
        }

        // Enforce single direction movement on gamepad
        if (Mathf.Abs(movement.x) < 1f)
        {
            movement.x = 0f;
        }
        if (Mathf.Abs(movement.y) < 1f)
        {
            movement.y = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (endAnimationOverride)
        {
            return;
        }
        rigidBody.velocity = movement * moveSpeed;
    }

    private void UpdateAnimation(Vector2 currentMovement)
    {
        animator.SetFloat("horizontal", currentMovement.x);
        animator.SetFloat("vertical", currentMovement.y);
    }

    public void StopMovement()
    {
        stopMovement = true;
        animator.speed = 0;
    }

    public void AllowMovement()
    {
        stopMovement = false;
        animator.speed = 1;
    }

    // TODO: Temp demo end
    public void AnimateWalkingRight()
    {
        rigidBody.velocity = Vector2.zero;
        endAnimationOverride = true;
    }
}
