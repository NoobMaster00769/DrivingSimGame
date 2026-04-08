using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System;

public class ControllerRebindItem : MonoBehaviour
{
    [Header("Binding")]
    public string actionName;
    public string compositePartName; 

    [Header("UI")]
    public TMP_Text keyText;
    public TMP_Text labelText;

    public bool isBack;

    InputAction action;
    int currentIndex = -1;

    string previousPath; 
    InputActionRebindingExtensions.RebindingOperation currentOp; 


    bool initialized = false;

    public void EnsureInitialized(VehicleInputActions actions)
    {
        if (initialized) return;

        var a = actions.FindAction(actionName);

        if (a == null)
        {
            Debug.LogError($"Action NOT FOUND: {actionName}");
            return;
        }

        Initialize(a);
        initialized = true;
    }
    public void Initialize(InputAction a)
    {
        action = a;

        if (action == null)
        {
            Debug.LogError($"[INIT] Action NULL on {gameObject.name}");
            return;
        }

        UpdateDisplay();
    }



    public void StartRebind(Action onComplete)
    {
        if (isBack) return;

        if (action == null)
        {
            Debug.LogError($"[Rebind] Action NULL on {gameObject.name}");
            return;
        }

        int index = FindBindingIndex();

        if (index == -1)
        {
            Debug.LogError($"[Rebind] No valid binding found for {actionName}");
            return;
        }

        currentIndex = index;


        previousPath = action.bindings[index].effectivePath;
        if (string.IsNullOrEmpty(previousPath))
            previousPath = action.bindings[index].path;

        keyText.text = "PRESS KEY";

        action.Disable();

        currentOp = action.PerformInteractiveRebinding(index)
            .WithControlsExcluding("Mouse")
            .OnCancel(op => Cancel(op, onComplete))
            .OnComplete(op => Complete(op, onComplete));

        currentOp.Start();
    }



    void Update()
    {

        if (currentOp != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            currentOp.Cancel();
        }
    }



    int FindBindingIndex()
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var b = action.bindings[i];


            if (!string.IsNullOrEmpty(compositePartName))
            {
                if (!b.isPartOfComposite) continue;
                if (!string.Equals(b.name, compositePartName, StringComparison.OrdinalIgnoreCase)) continue;
            }
            else
            {
                if (b.isComposite || b.isPartOfComposite) continue;
            }

            string path = b.effectivePath;
            if (string.IsNullOrEmpty(path))
                path = b.path;

            if (string.IsNullOrEmpty(path))
                continue;


            if (path.Contains("Gamepad") || path.Contains("XInput") || path.Contains("DualShock"))
            {
                return i;
            }
        }

        return -1;
    }



    void Complete(InputActionRebindingExtensions.RebindingOperation op, Action onComplete)
    {
        op.Dispose();
        currentOp = null;

        action.Enable();

        string newPath = action.bindings[currentIndex].effectivePath;

        if (string.IsNullOrEmpty(newPath))
            newPath = action.bindings[currentIndex].path;


        if (IsKeyAlreadyUsed(newPath))
        {
            Debug.Log("Key already bound to another action");

            action.ApplyBindingOverride(currentIndex, previousPath);

            UpdateDisplay();

            currentIndex = -1;

            onComplete?.Invoke();
            return;
        }

        UpdateDisplay();

        currentIndex = -1;

        onComplete?.Invoke();
    }

    void Cancel(InputActionRebindingExtensions.RebindingOperation op, Action onComplete)
    {
        op.Dispose();
        currentOp = null;

        action.Enable();

        UpdateDisplay();

        currentIndex = -1;

        onComplete?.Invoke();
    }



    bool IsKeyAlreadyUsed(string newPath)
    {
        var asset = action.actionMap.asset;

        foreach (var map in asset.actionMaps)
        {
            foreach (var a in map.actions)
            {
                for (int i = 0; i < a.bindings.Count; i++)
                {
                    var b = a.bindings[i];

                    string path = b.effectivePath;
                    if (string.IsNullOrEmpty(path))
                        path = b.path;

                    if (string.IsNullOrEmpty(path))
                        continue;

                    if (a == action && i == currentIndex)
                        continue;

                    if (path == newPath)
                        return true;
                }
            }
        }

        return false;
    }


    public void UpdateDisplay()
    {
        if (action == null) return;

        int index = FindBindingIndex();

        if (index == -1)
        {
            keyText.text = "-";
            return;
        }

        var binding = action.bindings[index];

        string path = binding.effectivePath;
        if (string.IsNullOrEmpty(path))
            path = binding.path;

        if (string.IsNullOrEmpty(path))
        {
            keyText.text = "-";
            return;
        }

        string readable =
            InputControlPath.ToHumanReadableString(
                path,
                InputControlPath.HumanReadableStringOptions.OmitDevice
            );

        readable = readable.ToLower();

        bool isPlayStation = false;

        if (Gamepad.current != null)
        {
            string name = Gamepad.current.displayName.ToLower();

            if (name.Contains("dualshock") ||
                name.Contains("dualsense") ||
                name.Contains("playstation"))
            {
                isPlayStation = true;
            }
        }


        readable = readable
            .Replace("button south", isPlayStation ? "Cross" : "A")
            .Replace("button east", isPlayStation ? "Circle" : "B")
            .Replace("button west", isPlayStation ? "Square" : "X")
            .Replace("button north", isPlayStation ? "Triangle" : "Y");


        readable = readable
            .Replace("left shoulder", isPlayStation ? "L1" : "LB")
            .Replace("right shoulder", isPlayStation ? "R1" : "RB")
            .Replace("left trigger", isPlayStation ? "L2" : "LT")
            .Replace("right trigger", isPlayStation ? "R2" : "RT");


        readable = readable
            .Replace("left stick/x", "Left Stick → Right")
            .Replace("left stick/y", "Left Stick → Up")
            .Replace("right stick/x", "Right Stick → Right")
            .Replace("right stick/y", "Right Stick → Up")
            .Replace("left stick", "Left Stick")
            .Replace("right stick", "Right Stick");


        readable = readable
            .Replace("dpad/up", "D-Pad Up")
            .Replace("dpad/down", "D-Pad Down")
            .Replace("dpad/left", "D-Pad Left")
            .Replace("dpad/right", "D-Pad Right");

        keyText.text = readable.ToUpper();
    }



    public void SetSelected(bool active)
    {
        float scale = active ? 1.2f : 1f;

        transform.localScale =
            Vector3.Lerp(
                transform.localScale,
                Vector3.one * scale,
                Time.unscaledDeltaTime * 10f
            );
    }
}