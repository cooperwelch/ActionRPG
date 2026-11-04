using System.Collections;
using UnityEngine;

public class HUDHealthMeter : MonoBehaviour
{
    public bool isPlayer = false;
    [SerializeField] private GameObject[] healthMeterFilled;
    [SerializeField] private GameObject[] healthMeterEmpty;
    [SerializeField] private SubzoneAudioManager subzoneAudioManager;

    private void Awake()
    {
        // Set to empty health and let level managers / PlayerStats update
        for (int i = healthMeterFilled.Length; i-- > 0;)
        {
            healthMeterFilled[i].SetActive(false);
        }
    }

    private void Start()
    {
        if (isPlayer)
        {
            RefreshFromPlayerStats();
        }
    }

    public void RefreshFromPlayerStats()
    {
        if (!isPlayer)
        {
            return;
        }

        SyncCapacitySlots(PlayerStats.HealthCapacity);
        SetHealth(PlayerStats.Health);
    }

    public void FillMeter()
    {
        StartCoroutine(Fill());
    }

    public void SetHealth(int health)
    {
        int capacity = GetActiveCapacity();
        health = Mathf.Clamp(health, 0, capacity);
        for (int i = 0; i < healthMeterFilled.Length; i++)
        {
            healthMeterFilled[i].SetActive(i < health);
        }
    }

    private void SyncCapacitySlots(int capacity)
    {
        capacity = Mathf.Clamp(capacity, 0, healthMeterFilled.Length);

        if (healthMeterEmpty != null)
        {
            for (int i = 0; i < healthMeterEmpty.Length; i++)
            {
                if (healthMeterEmpty[i] != null)
                {
                    healthMeterEmpty[i].SetActive(i < capacity);
                }
            }
        }

        for (int i = capacity; i < healthMeterFilled.Length; i++)
        {
            healthMeterFilled[i].SetActive(false);
        }
    }

    private int GetActiveCapacity()
    {
        if (isPlayer)
        {
            return Mathf.Min(PlayerStats.HealthCapacity, healthMeterFilled.Length);
        }

        return healthMeterFilled.Length;
    }

    private IEnumerator Fill()
    {
        int capacity = GetActiveCapacity();
        for (int i = 0; i < capacity; i++)
        {
            GameObject bar = healthMeterFilled[i];
            if (bar.activeInHierarchy == false)
            {
                yield return StartCoroutine(FillOne(bar));
            }
        }
    }

    private IEnumerator FillOne(GameObject bar)
    {
        yield return new WaitForSeconds(0.15f);
        if (!bar.activeInHierarchy)
        {
            if (subzoneAudioManager != null)
            {
                subzoneAudioManager.PlayArcadeJump();
            }
            bar.SetActive(true);
        }
    }

    public void Increment(int number)
    {
        int capacity = GetActiveCapacity();
        for (int i = 0; i < capacity; i++)
        {
            if (healthMeterFilled[i].activeInHierarchy == false)
            {
                healthMeterFilled[i].SetActive(true);
                number--;
                if (number == 0) { return; }
            }
        }
    }

    public void Decrement(int number)
    {
        int capacity = GetActiveCapacity();
        for (int i = capacity; i-- > 0;)
        {
            if (healthMeterFilled[i].activeInHierarchy == true)
            {
                healthMeterFilled[i].SetActive(false);
                number--;
                if (number == 0) { return; }
            }
        }
    }
}
