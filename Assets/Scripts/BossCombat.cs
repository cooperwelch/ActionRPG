using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossCombat : MonoBehaviour
{
    public SubzoneAudioManager audioManager;
    private Rigidbody2D rigidBody;
    private SpriteRenderer spriteRenderer;
    private BossFightManager bossFightManager;
    private bool canTakeDamage = false;

    private void Awake()
    {

        rigidBody = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bossFightManager = GetComponent<BossFightManager>();
    }

    public void InitialBossPhaseDidBegin()
    {
        canTakeDamage = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!canTakeDamage) { return; }
        if (collision.gameObject.CompareTag("SubzoneHeroProjectile"))
        {
            audioManager.PlayDamage();
            bossFightManager.ApplyDamage(PlayerStats.Attack);

            StartCoroutine(TakeDamage());
        }
    }

    private IEnumerator TakeDamage()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }
}
