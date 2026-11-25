using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverworldItemFlash : MonoBehaviour
{
    [SerializeField] private SecondaryWeapon weapon;

    private void Awake()
    {
        if (weapon != null && PlayerStats.SecondaryWeapons.Contains(weapon.name))
        {
            gameObject.SetActive(false);
        }
    }

}
