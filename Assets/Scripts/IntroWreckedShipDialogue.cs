using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelCrushers.DialogueSystem;
using Cinemachine;

public class IntroWreckedShipDialogue : MonoBehaviour
{
    public GameObject Player;
    [SerializeField] private DialogueSystemTrigger dialogueTrigger;
    private Animator animator;
    private PlayerMovement movement;

    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private Fader fader;

    private void Awake()
    {
        movement = Player.GetComponent<PlayerMovement>();
        animator = Player.GetComponent<Animator>();

        if (OverworldSubzoneContainer.HasShownWreckedShipIntro)
        {
            TargetVirtualCameraOnPlayer();
            fader.doSteppedFade = true;
        }
        else
        {
            PlayerStats.SetHealth(3);
        }
    }

    void Start()
    {
        if (!OverworldSubzoneContainer.HasShownWreckedShipIntro)
        {
            OverworldSubzoneContainer.HasShownWreckedShipIntro = true;
            movement.Stop();
            animator.SetBool("IsCollapsed", true);
            StartCoroutine(StartSequence());
        }
    }

    private IEnumerator StartSequence()
    {
        yield return StartCoroutine(DoInitialCameraPan());

        TargetVirtualCameraOnPlayer();

        yield return StartCoroutine(GetUpToCrochIdle());

        yield return new WaitForSeconds(1f);
        // Now do dialogue
        dialogueTrigger.OnUse(transform);
    }
    private IEnumerator GetUpToCrochIdle()
    {
        yield return new WaitForSeconds(3);
        animator.SetBool("IsCollapsed", false);
        animator.SetBool("IsGettingUp", true);
        yield return new WaitForSeconds(2.0f);
        animator.SetBool("IsCrouchIdling", true);
    }

    // Called by `DialogueSystemTrigger.OnConversationEnd`
    public void StandUp()
    {
        animator.SetBool("IsStandingUp", true);
        movement.AllowMovement();
    }

    // used for targeting the virtual camera
    private void TargetVirtualCameraOnPlayer()
    {
        virtualCamera.PreviousStateIsValid = false;
        virtualCamera.Follow = Player.transform;
    }

    private IEnumerator DoInitialCameraPan()
    {
        float duration = 12.0f;
        float elapsedTime = 0;
        float cameraStartingXPosition = virtualCamera.transform.position.x;

        while (Mathf.Abs(Player.transform.position.x - virtualCamera.transform.position.x) > 0.01f)
        {
            float newCameraXPosition = Mathf.Lerp(cameraStartingXPosition, Player.transform.position.x, SineEaseOut(elapsedTime / duration));
            virtualCamera.transform.position = new Vector3(newCameraXPosition, virtualCamera.transform.position.y, virtualCamera.transform.position.z);
            elapsedTime += Time.deltaTime;

            yield return null;
        }
        yield return null;
    }

    // based on Robert Penner's easing functions,
    // takes the current lerp time value and interpolates it to a quartic ease out curve
    private float SineEaseOut(float t)
    {
        return Mathf.Sin((t * Mathf.PI) / 2);
    }

}
