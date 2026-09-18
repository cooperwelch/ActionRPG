using PixelCrushers.DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Attack : MonoBehaviour, IDamageable
{
	public float dmgValue = 4f;
	public int invulnerableDuration = 10;
	public MeleeController meleeWeaponController;
	public SecondaryWeaponController secondaryWeaponController;

	public Transform attackCheck;
	private Rigidbody2D rigidBody;
	public Animator animator;
	public bool canMeleeAttack = true;
	public bool isTimeToCheck = false;
	public GameObject cam;
	public SubzoneAudioManager audioManager;
	public Vector2 knockbackForce = Vector2.zero;
	public bool isAttacking
	{
		get
		{
			return !canMeleeAttack;
		}
	}

	/// <summary>True while in hit reaction / knockback arc (control locked).</summary>
	public bool isDamaged { get; private set; }

	/// <summary>True from hit until i-frames fully expire (blocks all damage sources).</summary>
	public bool isInvulnerable { get; private set; }

    [SerializeField] private SubzoneHUD subzoneHUD;

	private SpriteRenderer spriteRenderer;
    private PlayerMovement playerMovement;
    private CharacterController2D controller;
	private bool shouldCheckGroundedForKnockback = false;
    private bool playerWasKnockedBack = false;
    private bool dead = false;
    private const int PLAYER_COLLISION_LAYER = 1;
    private const int ENEMY_COLLISION_LAYER = 9;

    private string activeAttackTrigger;
    private int lastDialogueEndFrame = -1;
	private Coroutine invulnerabilityCoroutine;
	private Coroutine resumeControlCoroutine;

    private void Awake()
	{
		rigidBody = GetComponent<Rigidbody2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();
        playerMovement = gameObject.GetComponent<PlayerMovement>();
        controller = gameObject.GetComponent<CharacterController2D>();

        Physics2D.IgnoreLayerCollision(PLAYER_COLLISION_LAYER, ENEMY_COLLISION_LAYER, false);
    }

	private void BeginAttackAnim(string trigger)
	{
		activeAttackTrigger = trigger;
		animator.SetBool(trigger, true);
	}

	public void ClearAttackAnimation()
	{
		if (string.IsNullOrEmpty(activeAttackTrigger)) return;
		animator.SetBool(activeAttackTrigger, false);
		activeAttackTrigger = null;
	}

    // Update is called once per frame
    void Update()
    {
		// Update melee attack direction
		meleeWeaponController.playerDirection = transform.localScale.x > 0 ?
			MeleeController.PlayerDirection.Right : MeleeController.PlayerDirection.Left;
		meleeWeaponController.isCrouching = playerMovement.isCrouching;

		// Handle melee attack
		if (meleeWeaponController.HasWeapon && playerMovement.canMove && !GameplayUI.IsBlocking && Input.GetButtonDown("Fire1") && canMeleeAttack && Time.frameCount > lastDialogueEndFrame && Time.frameCount > GameplayUI.LastClosedFrame)
		{
			if (playerMovement.grounded)
			{
                playerMovement.StopForAttack();
            } else
			{
				playerMovement.StopAirControlForJumpAttack();
			}
            canMeleeAttack = false;
			BeginAttackAnim(meleeWeaponController.currentMeleeWeapon.attackAnimationTrigger);

            meleeWeaponController.Attack();

			StartCoroutine(MeleeAttackCooldown());
		}

		// Handle secondary weapon attack
		if (secondaryWeaponController.HasWeapon && secondaryWeaponController.CanAttack && playerMovement.canMove && !GameplayUI.IsBlocking && Input.GetButtonDown("Fire2") && canMeleeAttack && Time.frameCount > lastDialogueEndFrame && Time.frameCount > GameplayUI.LastClosedFrame)
		{
			if (playerMovement.grounded)
			{
				playerMovement.StopForAttack();
			} else
			{
				playerMovement.StopAirControlForJumpAttack();
			}
			canMeleeAttack = false;
			BeginAttackAnim(secondaryWeaponController.currentWeapon.attackAnimationTrigger);

			Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
			secondaryWeaponController.Attack(direction, playerMovement.isCrouching);

			StartCoroutine(SecondaryAttackCooldown());
		}

		// Handle knockback landing
		if (shouldCheckGroundedForKnockback && animator.GetBool("IsHit"))
		{
			CheckGroundedForKnockback();
        }
	}

	IEnumerator SecondaryAttackCooldown()
	{
		yield return new WaitForSeconds(secondaryWeaponController.currentWeapon.attackAnimationDuration);
		ClearAttackAnimation();
		canMeleeAttack = true;
		// Do not restore control mid-knockback; that damps / cancels the knockback arc.
		if (!isDamaged)
		{
			playerMovement.AllowMovementAfterAttackOrKnockback();
		}
	}

	IEnumerator MeleeAttackCooldown()
	{
		MeleeWeapon weapon = meleeWeaponController.currentMeleeWeapon;
        yield return new WaitForSeconds(
            playerMovement.isCrouching ? weapon.crouchAttackAnimationDuration : weapon.attackAnimationDuration
        );
        ClearAttackAnimation();
        canMeleeAttack = true;
		if (!isDamaged)
		{
			playerMovement.AllowMovementAfterAttackOrKnockback();
		}
    }

	private void StopHitCoroutines()
	{
		if (invulnerabilityCoroutine != null)
		{
			StopCoroutine(invulnerabilityCoroutine);
			invulnerabilityCoroutine = null;
		}
		if (resumeControlCoroutine != null)
		{
			StopCoroutine(resumeControlCoroutine);
			resumeControlCoroutine = null;
		}
		spriteRenderer.color = Color.white;
	}

	private void BeginInvulnerability()
	{
		isInvulnerable = true;
		Physics2D.IgnoreLayerCollision(PLAYER_COLLISION_LAYER, ENEMY_COLLISION_LAYER, true);
	}

	private void EndInvulnerability()
	{
		isInvulnerable = false;
		spriteRenderer.color = Color.white;
		Physics2D.IgnoreLayerCollision(PLAYER_COLLISION_LAYER, ENEMY_COLLISION_LAYER, false);
		invulnerabilityCoroutine = null;
	}

	/// <summary>
	/// Ground blink phase after landing. Invulnerability already began on hit (#1);
	/// this only handles the visual and ends i-frames when blink finishes.
	/// </summary>
    private IEnumerator InvulnerabilityBlink(int duration)
    {
        Color color = Color.clear;
		int durationCounter = 0;
        while (durationCounter <= duration)
        {
            spriteRenderer.color = color;
            yield return new WaitForSeconds(0.1f);
            color = (color == Color.clear) ? Color.white : Color.clear;
			durationCounter++;
        }
		EndInvulnerability();
    }

	private void CheckGroundedForKnockback()
	{
        if (!playerMovement.grounded)
        {
            playerWasKnockedBack = true;
        }
        else
        {
            if (playerWasKnockedBack)
            {
                shouldCheckGroundedForKnockback = false;
                playerWasKnockedBack = false;
				if (resumeControlCoroutine != null)
				{
					StopCoroutine(resumeControlCoroutine);
				}
                resumeControlCoroutine = StartCoroutine(ResumeControlAfterKnockback());
            }
        }
    }

    private IEnumerator ResumeControlAfterKnockback()
	{
        if (PlayerStats.Health <= 0f)
        {
            yield return StartCoroutine(Die());
        } else
		{
            animator.SetBool("IsHit", false);
            isDamaged = false;
			// Clear leftover knockback velocity before control restore / freeze handoff.
			rigidBody.velocity = Vector2.zero;
            playerMovement.AllowMovementAfterAttackOrKnockback();
            canMeleeAttack = true;

			// Full-opacity hit sprite ends; blink until remaining i-frames finish.
			// isInvulnerable stays true so sand balls / non-Enemy layers cannot damage during blink.
			if (invulnerabilityCoroutine != null)
			{
				StopCoroutine(invulnerabilityCoroutine);
			}
			// Ensure sprite is white before blink starts (hit phase was full opacity).
			spriteRenderer.color = Color.white;
            invulnerabilityCoroutine = StartCoroutine(InvulnerabilityBlink(invulnerableDuration));
        }
		resumeControlCoroutine = null;
    }

	// IDamageable
	public void Damage(int damage, float damageDirection)
	{
		if (dead) { return; }

		// Unified invuln gate for all sources (enemy contact, sand balls, boulders, etc.)
		if (isInvulnerable) { return; }

		// Fresh hit reaction: stop any leftover hit/i-frame coroutines before starting again.
		StopHitCoroutines();

		isDamaged = true;
		BeginInvulnerability();

		subzoneHUD.ReducePlayerHealthMeter(damage);
		audioManager.PlayDamage();
		PlayerStats.ApplyDamage(damage);

		playerMovement.StopForKnockback();
		// Lock physics to no-friction so slope max-friction cannot cancel the knock arc.
		controller.SetNoFrictionForKnockback();
		canMeleeAttack = false;
		animator.SetBool("IsHit", true);
		// Full opacity during air hit reaction (blink starts only after land).
		spriteRenderer.color = Color.white;

		// Set velocity directly for a consistent arc (mass-independent Impulse equivalent for our setup).
		rigidBody.velocity = new Vector2(knockbackForce.x * damageDirection, knockbackForce.y);

		shouldCheckGroundedForKnockback = true;
		// If already airborne, count as in-arc so first land restores control.
		playerWasKnockedBack = !playerMovement.grounded;
	}

    private IEnumerator Die()
	{
        playerMovement.Stop();
        audioManager.PlayGameOver();
        dead = true;
		isInvulnerable = true;
		isDamaged = false;
		shouldCheckGroundedForKnockback = false;
        animator.SetBool("IsDead", true);
		yield return new WaitForSeconds(4);
        GameManager.sharedInstance.ShowGameOver(SceneManager.GetActiveScene().name);
	}

    void OnConversationEnd(Transform actor)
    {
        lastDialogueEndFrame = Time.frameCount;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryPickupMeleeWeapon(collision);
        TryPickupSecondaryWeapon(collision);
        TryPickupCraftingHammer(collision);
    }

    private void TryPickupMeleeWeapon(Collider2D collision)
    {
        MeleeWeaponPickup weaponPickup = collision.GetComponentInChildren<MeleeWeaponPickup>();
        if (weaponPickup == null) return;

        meleeWeaponController.PickUpMeleeWeapon(weaponPickup.weapon);
        RunPickupDialogueIfPresent(collision);
        Destroy(collision.gameObject);
    }

    private void TryPickupSecondaryWeapon(Collider2D collision)
    {
        SecondaryWeaponPickup secondaryPickup = collision.GetComponentInChildren<SecondaryWeaponPickup>();
        if (secondaryPickup == null) return;

        secondaryWeaponController.AcquireSecondaryWeapon(secondaryPickup.weapon);
        RunPickupDialogueIfPresent(collision);
        Destroy(collision.gameObject);
    }

    private void TryPickupCraftingHammer(Collider2D collision)
    {
        CraftingHammerPickup hammerPickup = collision.GetComponentInChildren<CraftingHammerPickup>();
        if (hammerPickup == null) return;

        PlayerStats.AcquireCraftingHammer();
        RunPickupDialogueIfPresent(collision);
        Destroy(collision.gameObject);
    }

    private void RunPickupDialogueIfPresent(Collider2D collision)
    {
        DialogueSystemTrigger dialogueTrigger = collision.GetComponent<DialogueSystemTrigger>();
        if (dialogueTrigger == null)
        {
            dialogueTrigger = collision.GetComponentInParent<DialogueSystemTrigger>();
        }

        if (dialogueTrigger == null) return;

        dialogueTrigger.conversationConversant = dialogueTrigger.transform;
        dialogueTrigger.OnUse(transform);
    }
}
