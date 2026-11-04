using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PowerupType { Attack, Defense, Health };
public class Powerup : MonoBehaviour
{
    public SubzoneAudioManager audioManager;
    public PowerupType powerupType;
    public string uniqueId;

    private void Awake()
    {
        foreach (string tag in PlayerStats.PowerupDestroy)
        {
            Debug.Log("power up destroy: " + tag);
        }
        if (PlayerStats.PowerupDestroy.Contains(uniqueId))
        {
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (powerupType == PowerupType.Attack)
        {
            transform.Rotate(0f, 0f, 300f * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            gameObject.SetActive(false);
            audioManager.PlayPowerup();
            switch (powerupType)
            {
                case PowerupType.Attack:
                    PlayerStats.UpgradetAttack(uniqueId);
                    break;
                case PowerupType.Defense:
                    PlayerStats.UpgradeDefense(uniqueId);
                    break;
                case PowerupType.Health:
                    PlayerStats.UpgradeHealth(uniqueId);
                    SubzoneHUD hud = FindObjectOfType<SubzoneHUD>();
                    if (hud != null)
                    {
                        hud.RefreshPlayerHealthMeter();
                    }
                    break;
            }
        }
    }
}