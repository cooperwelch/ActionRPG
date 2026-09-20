using UnityEngine;

/// <summary>
/// Temporary debug helper: logs joystick names, button presses, and axis values.
/// Attach to any GameObject in a playable scene, enter Play mode, press controller
/// inputs, and watch the Console (or on-screen overlay).
/// Remove this component/script when mapping is finished.
/// </summary>
public class GamepadInputDebugger : MonoBehaviour
{
    [SerializeField] private bool logJoystickNamesOnStart = true;
    [SerializeField] private bool drawOnScreenOverlay = true;
    [SerializeField] private int maxButtonsToPoll = 20;
    [SerializeField] private int maxAxesToPoll = 7;
    [SerializeField] private float axisLogThreshold = 0.5f;

    private string lastEvent = "(none yet)";
    private string joystickNamesSummary = "(none)";
    private readonly bool[] axisWasActive = new bool[8]; // index by JoystickAxis 1..7
    private readonly System.Collections.Generic.Dictionary<string, bool> namedAxisWasActive =
        new System.Collections.Generic.Dictionary<string, bool>();

    private void Start()
    {
        RefreshJoystickNames();
        if (logJoystickNamesOnStart)
        {
            Debug.Log($"[GamepadInputDebugger] Joysticks: {joystickNamesSummary}");
        }
    }

    private void Update()
    {
        for (int i = 0; i < maxButtonsToPoll; i++)
        {
            string buttonName = "joystick button " + i;
            if (Input.GetKeyDown(buttonName))
            {
                lastEvent = $"DOWN  {buttonName}";
                Debug.Log($"[GamepadInputDebugger] {lastEvent}");
            }
            else if (Input.GetKeyUp(buttonName))
            {
                lastEvent = $"UP    {buttonName}";
                Debug.Log($"[GamepadInputDebugger] {lastEvent}");
            }
        }

        for (int axis = 1; axis <= maxAxesToPoll; axis++)
        {
            string axisName = "JoystickAxis" + axis;
            float value = Input.GetAxisRaw(axisName);
            bool active = Mathf.Abs(value) > axisLogThreshold;
            if (active)
            {
                lastEvent = $"{axisName} = {value:F2}";
                if (!axisWasActive[axis])
                {
                    Debug.Log($"[GamepadInputDebugger] {lastEvent}");
                }
            }
            axisWasActive[axis] = active;
        }

        // Also report the named axes your game already uses.
        LogNamedAxisEdge("Horizontal");
        LogNamedAxisEdge("Vertical");
        LogNamedAxisEdge("DPadX");
        LogNamedAxisEdge("DPadY");
    }

    private void LogNamedAxisEdge(string axisName)
    {
        float value = Input.GetAxisRaw(axisName);
        bool active = Mathf.Abs(value) > axisLogThreshold;
        namedAxisWasActive.TryGetValue(axisName, out bool wasActive);

        if (active)
        {
            lastEvent = $"{axisName} = {value:F2}";
            if (!wasActive)
            {
                Debug.Log($"[GamepadInputDebugger] {lastEvent}");
            }
        }

        namedAxisWasActive[axisName] = active;
    }

    private void RefreshJoystickNames()
    {
        string[] names = Input.GetJoystickNames();
        if (names == null || names.Length == 0)
        {
            joystickNamesSummary = "(none detected — check Bluetooth pairing / 8BitDo mode)";
            return;
        }

        joystickNamesSummary = string.Join(" | ", names);
    }

    private void OnGUI()
    {
        if (!drawOnScreenOverlay) return;

        const int pad = 12;
        GUI.Box(new Rect(pad, pad, 520, 140), "Gamepad Input Debugger");
        GUI.Label(new Rect(pad + 10, pad + 28, 500, 40), $"Joysticks: {joystickNamesSummary}");
        GUI.Label(new Rect(pad + 10, pad + 68, 500, 20), $"Last event: {lastEvent}");
        GUI.Label(new Rect(pad + 10, pad + 92, 500, 40),
            "Press face buttons / d-pad / sticks. Button presses log to Console.\nAxes show here while held past threshold.");
    }
}
