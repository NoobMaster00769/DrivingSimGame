using UnityEngine;
using UnityEngine.InputSystem;

public class BindingListMenuController : MonoBehaviour
{
    public Transform[] options;
    public Transform guidingStar;
    public VehicleInputReader input;

    public float starOffsetX = -8f;

    int index;
    float timer;
    float cooldown = 0.25f;

    bool rebinding;
    bool initialized;

    Vector3 velocity;

    void Update()
    {

        if (!enabled) return;

        var manager = FindObjectOfType<MenuManager>();
        if (manager && manager.IsTransitioning()) return;

        timer += Time.deltaTime;

        if (!rebinding)
            HandleNavigation();

        MoveGuidingStar();
        AnimateSelection();
    }

    void HandleNavigation()
    {
        // 🔥 ESC handling FIRST
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (rebinding)
            {
                rebinding = false;
                return;
            }
            else
            {
                FindObjectOfType<MenuManager>().OpenMenu(3);
                return;
            }
        }

        // ✅ IMPORTANT — ADD THIS BACK
        if (timer < cooldown) return;

        float vertical = 0f;
        bool select = false;

        // 🎮 Controller
        if (Gamepad.current != null)
        {
            vertical += Gamepad.current.leftStick.y.ReadValue();

            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                select = true;
        }

        // ⌨️ Keyboard
        vertical += input.Throttle;

        if (input.Brake > 0.5f)
            select = true;

        // ✅ Movement WITH cooldown reset
        if (vertical > 0.5f)
        {
            ChangeIndex(-1);
            timer = 0;
        }
        else if (vertical < -0.5f)
        {
            ChangeIndex(1);
            timer = 0;
        }

        if (select)
        {
            Activate();
            timer = 0;
        }
    }
    void OnEnable()
    {
        index = 0;
        timer = 0;
        FindObjectOfType<NavigationFooterUI>()
    .SetMenuType(NavigationFooterUI.MenuType.Vertical);
    }

    void ChangeIndex(int dir)
    {
        index += dir;

        if (index >= options.Length)
            index = 0;

        if (index < 0)
            index = options.Length - 1;

        timer = 0;
    }

    void Activate()
    {
        var item = options[index].GetComponent<ControlRebindItem>();

        if (item.isBack)
        {
            FindObjectOfType<MenuManager>().OpenMenu(3);
            return;
        }

        rebinding = true;

        item.StartRebind(() =>
        {
            rebinding = false;

            var actions = input.GetActions();
            PlayerPrefs.SetString("rebinds",
                actions.asset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        });

        timer = 0;
    }

    void MoveGuidingStar()
    {

        Vector3 target =
            options[index].position +
            Vector3.left * starOffsetX;

        guidingStar.position =
            Vector3.SmoothDamp(
                guidingStar.position,
                target,
                ref velocity,
                0.15f
            );
    }

    void AnimateSelection()
    {
        for (int i = 0; i < options.Length; i++)
        {
            var item = options[i].GetComponent<ControlRebindItem>();

            item.SetSelected(i == index);
        }
    }
}