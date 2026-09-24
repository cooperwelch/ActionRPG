using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SubzoneEntrance : MonoBehaviour
{
    public string subzone;
    public Vector3 positionOverride;
    public bool usePositionOverride;

    public Vector3 subzoneLevelStartPositionOverride;
    public bool usesubzoneLevelStartPositionOverride;
    public OverworldSubzoneContainer.PlayerDirection subzoneLevelStartDirectionOverride = OverworldSubzoneContainer.PlayerDirection.Left;

    [SerializeField] private Fader fader;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("OverworldHero"))
        {
            // stop hero movement and animation
            TopDownMovement heroMovement = collision.GetComponent<TopDownMovement>();
            heroMovement.StopMovement();
            heroMovement.StopAnimation();

            Vector3 encounterPosition = usePositionOverride ?
                positionOverride : gameObject.transform.position;
            OverworldSubzoneContainer.AddEncounter(
                encounterPosition.x,
                encounterPosition.y - 1, // Offset player from last encounter so we don't auto-collide again.
                subzone,
                OverworldSubzoneContainer.PlayerDirection.Down
            );

            if (usesubzoneLevelStartPositionOverride)
            {
                OverworldSubzoneContainer.AddSubzoneStartPosition(
                    subzoneLevelStartPositionOverride.x,
                    subzoneLevelStartPositionOverride.y,
                    subzoneLevelStartDirectionOverride
                );
            }

            StartCoroutine(DoSceneExit());
        }
    }

    // contains effect for fading to black and call to load the next scene
    private IEnumerator DoSceneExit()
    {
        yield return StartCoroutine(fader.DoSteppedFadeIn());

        SceneManager.LoadScene(subzone);

        yield return null;
    }
}
