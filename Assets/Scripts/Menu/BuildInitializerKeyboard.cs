using UnityEngine;
using UnityEngine.InputSystem;

public class BindingInitializerKeyboard : MonoBehaviour
{
    public VehicleInputReader input;
    public ControlRebindItem[] items;

    void Start()
    {
        var actions = input.GetActions();

        foreach (var item in items)
        {
            if (item.isBack) continue;

            var action = actions.FindAction(item.actionName);

            if (action == null)
            {
                Debug.LogError($"Action NOT FOUND: {item.actionName}");
                continue;
            }

            item.Initialize(action);
        }
    }
}