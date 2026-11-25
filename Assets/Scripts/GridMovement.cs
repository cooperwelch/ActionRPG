using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridMovement : MonoBehaviour
{
    public float moveSpeed;
    private Transform movePoint;
    private Animator animator;
    private bool isMovingTarget = false;
    private Rigidbody2D rigidBody;
    private Vector3 lastValidPosition;
    private bool stopMovement = false;

    private void Awake()
    {
        // Set position to last encounter
        rigidBody = gameObject.GetComponent<Rigidbody2D>();
        // Offset player from last encounter so we don't auto-collide again.
        rigidBody.transform.position = OverworldSubzoneContainer.LastEncounterPosition + Vector2.down;

        movePoint = transform.Find("MovePoint");
        movePoint.parent = null;

        animator = gameObject.GetComponent<Animator>();
        animator.SetFloat("speed", 1);
    }

    void Update()
    {
        if (stopMovement) { return; }



        float distanceToMovePoint = Vector3.Distance(
            transform.position,
            movePoint.position
        );
        if (distanceToMovePoint == 0)
        {
            isMovingTarget = false;
            lastValidPosition = transform.position;
        }

        if (!isMovingTarget)
        {
/*            bool isRight = (Input.GetAxis("DPadX") == 1f) ? true : false;
            bool isLeft = (Input.GetAxis("DPadX") == -1f) ? true : false;
            bool isDown = (Input.GetAxis("DPadY") == -1f) ? true : false;
            bool isUp = (Input.GetAxis("DPadY") == 1f) ? true : false;*/

            float horizontal = 0;
            float vertical = 0;
            if (Input.GetKeyDown("w") || Input.GetKeyDown("up"))// || isUp)
            {
                vertical = 1;
            }
            else if (Input.GetKeyDown("s") || Input.GetKeyDown("down"))// || isDown)
            {
                vertical = -1;
            }
            else if (Input.GetKeyDown("a") || Input.GetKeyDown("left"))// || isLeft)
            {
                horizontal = -1;
            }
            else if (Input.GetKeyDown("d") || Input.GetKeyDown("right"))// || isRight)
            {
                horizontal = 1;
            }

            Vector2 movement = new Vector2(
                horizontal,
                vertical
            );

            if (movement.x != 0 || movement.y != 0)
            {
                UpdateAnimation(movement);
            }


            movePoint.position += new Vector3(
                movement.x,
                movement.y, 0.0f
            );
            isMovingTarget = true;
        }
        else
        {
            rigidBody.transform.position = Vector3.MoveTowards(
                transform.position,
                movePoint.position,
                moveSpeed * Time.deltaTime
            );
        }
    }

    private void UpdateAnimation(Vector2 currentMovement)
    {
        animator.SetFloat("horizontal", currentMovement.x);
        animator.SetFloat("vertical", currentMovement.y);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("OverworldTile-Rock") ||
            collision.gameObject.CompareTag("OverworldTile-Water"))
        {
            Debug.Log("Handle Tile Collision");
            transform.position = lastValidPosition;
            movePoint.position = lastValidPosition;
        }
    }

    public void StopMovement()
    {
        stopMovement = true;
    }
}
