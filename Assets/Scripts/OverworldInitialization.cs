using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OverworldInitialization : MonoBehaviour
{
    [SerializeField] private Fader fader;

    private void Awake()
    {

        OverworldDestroy();
    }

    private void Start()
    {
        fader.SteppedFadeOut();
    }

    private void OverworldDestroy()
    {
        foreach (string tag in PlayerStats.OverworldDestroyList)
        {
            foreach (GameObject obj in GameObject.FindGameObjectsWithTag(tag))
            {
                Destroy(obj);
            }
        }
    }
}
