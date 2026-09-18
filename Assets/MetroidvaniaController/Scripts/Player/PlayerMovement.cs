using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

	public CharacterController2D controller;
	public Animator animator;

	[SerializeField] private float runSpeed = 40f;

	public float horizontalMove = 0f;
	bool jump = false;
	bool dash = false;
	public bool canMove = true;
	public bool grounded { get; private set; }
	private bool StopFixedUpdate = false;
    private Attack attack;
    private bool stopOverrideAttack = false;
    private SpriteRenderer spriteRenderer;
	public bool isAttacking
	{
		get
		{
			return attack.isAttacking;
		}
	}
	public bool isDamaged
    {
        get
        {
            return attack.isDamaged;
        }
    }
	public bool isCrouching = false;

    // Auto Walk
    private bool isAutowalking = false;
    private float autowalk;

    //bool dashAxis = false;

    private void Awake()
    {
        attack = GetComponent<Attack>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        grounded = controller.m_Grounded;

        if (!canMove) { return; }

        if (!isCrouching)
        {
            float inputHorizontal = Input.GetAxisRaw("Horizontal") + Input.GetAxisRaw("DPadX");
            horizontalMove = inputHorizontal * runSpeed;
            animator.SetFloat("Speed", Mathf.Abs(inputHorizontal));
        } else
        {
            horizontalMove = 0f;
            animator.SetFloat("Speed", 0f);
        }

		if (Input.GetButtonDown("Jump"))
		{
            UnCrouch();
			jump = true;
		}

        float inputVertical = Input.GetAxisRaw("Vertical") + Input.GetAxisRaw("DPadY");

        if (grounded)
		{
			if (inputVertical == -1f)
			{
                Crouch();
            } else
			{
                UnCrouch();
			}
		} else
        {
            UnCrouch();
        }
	}

	private void Crouch()
	{
        horizontalMove = 0f;
        animator.SetBool("IsCrouching", true);
		isCrouching = true;
    }

    private void UnCrouch()
    {
        animator.SetBool("IsCrouching", false);
        isCrouching = false;
    }

    public void DoJump()
	{
		jump = true;
	}

	public void OnFall()
	{
		animator.SetBool("IsJumping", true);
	}

	public void OnLanding()
	{
		animator.SetBool("IsJumping", false);
	}

	void FixedUpdate()
	{
		if (StopFixedUpdate) { return; }
        if (isAutowalking)
        {
            controller.Move(autowalk * Time.fixedDeltaTime, false, false);
            return;
        }

        controller.Move(horizontalMove * Time.fixedDeltaTime, jump, dash);
		jump = false;
		dash = false;
	}

	public void Stop(bool stopInFlightAnimations = true, bool overrideAttackCooldown = false)
	{
        stopOverrideAttack = overrideAttackCooldown;
        horizontalMove = 0f;
        canMove = false;
		StopFixedUpdate = true;
        GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        UnCrouch();
        animator.SetFloat("Speed", 0f);
        if (stopInFlightAnimations)
        {
            animator.SetBool("IsJumping", false);
            attack.ClearAttackAnimation();
        }
	}

	public void StopForAttack()
	{
        horizontalMove = 0f;
        canMove = false;
        GetComponent<Rigidbody2D>().velocity = Vector2.zero;
    }

    public void AllowMovementAfterAttackOrKnockback()
    {
		// Attack cooldown must not re-enable Move() during knockback (damps the arc).
		if (isDamaged) return;

		// Knockback finished, but something else still owns control (cyclops death Stop,
		// or menu/shop/crafting). Kill residual slide velocity without restoring input.
		if (stopOverrideAttack || GameplayUI.IsBlocking)
		{
			GetComponent<Rigidbody2D>().velocity = Vector2.zero;
			// Menu/shop freeze: match StopForDialogue (Move(0) still runs).
			// Hard Stop() freeze (cyclops death): keep StopFixedUpdate true.
			if (!stopOverrideAttack)
				StopFixedUpdate = false;
			return;
		}

        AllowMovement();
    }

    public void StopForKnockback()
    {
        horizontalMove = 0f;
        canMove = false;
        StopFixedUpdate = true;
		UnCrouch();
        animator.SetBool("IsJumping", false);
        animator.SetFloat("Speed", 0f);
        attack.ClearAttackAnimation();
        GetComponent<Rigidbody2D>().velocity = Vector2.zero;
    }

    public void StopAirControlForJumpAttack()
    {
        canMove = false;
    }

    public void AllowMovement()
	{
        stopOverrideAttack = false;
        canMove = true;
        StopFixedUpdate = false;
    }

	public void StopForDialogue()
	{
		canMove = false;
		horizontalMove = 0f;
        UnCrouch();
        animator.SetBool("IsJumping", false);
		animator.SetFloat("Speed", 0f);
		attack.ClearAttackAnimation();
		GetComponent<Rigidbody2D>().velocity = Vector2.zero;
	}

	public void FreezeWalking()
	{
        canMove = false;
        horizontalMove = 0f;
        UnCrouch();
        animator.SetBool("IsJumping", false);
        animator.SetFloat("Speed", 1f);
        attack.ClearAttackAnimation();
    }

    public void BeginEnteringDoor()
    {
        canMove = false;
        horizontalMove = 0f;
        UnCrouch();
        animator.SetBool("IsJumping", false);
        animator.SetFloat("Speed", 0f);
        attack.ClearAttackAnimation();
        animator.SetBool("IsEnteringDoor", true);
        GetComponent<Rigidbody2D>().velocity = Vector2.zero;
    }

    // Called by an animation event on the enter-door clips.
    public void BeginDoorSpriteFade(float duration)
    {
        if (spriteRenderer == null || duration <= 0f) return;

        StartCoroutine(FadeSpriteOut(duration));
    }

    private IEnumerator FadeSpriteOut(float duration)
    {
        float elapsedTime = 0f;
        Color color = spriteRenderer.color;
        float startingAlpha = color.a;

        while (elapsedTime < duration)
        {
            color.a = Mathf.Lerp(startingAlpha, 0f, elapsedTime / duration);
            spriteRenderer.color = color;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        color.a = 0f;
        spriteRenderer.color = color;
    }

    public IEnumerator AutoWalk(float duration, OverworldSubzoneContainer.PlayerDirection direction)
    {
        // Trigger animator
        FreezeWalking();

        // Calculate move
        float autowalkSpeed = runSpeed / 1.5f;
        autowalk = direction switch
        {
            OverworldSubzoneContainer.PlayerDirection.Left => -1 * autowalkSpeed,
            OverworldSubzoneContainer.PlayerDirection.Right => autowalkSpeed,
            _ => autowalkSpeed,
        };

        // Start walking
        isAutowalking = true;

        yield return new WaitForSeconds(duration);

        // Stop Auto Walk
        isAutowalking = false;
        autowalk = 0f;
        AllowMovement();
        yield return null;
    }

	public void SetDirection(OverworldSubzoneContainer.PlayerDirection direction)
	{
        switch (direction)
        {
            case OverworldSubzoneContainer.PlayerDirection.Up:
                break;
            case OverworldSubzoneContainer.PlayerDirection.Down:
                break;
            case OverworldSubzoneContainer.PlayerDirection.Left:
				controller.Flip();
                break;
            case OverworldSubzoneContainer.PlayerDirection.Right:
                break;
        }
    }
}
