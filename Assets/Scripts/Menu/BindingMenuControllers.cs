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

        timer += Time.unscaledDeltaTime;

        if (!rebinding)
            HandleNavigation();

        MoveGuidingStar();
        AnimateSelection();
    }

    void HandleNavigation()
    {
        // 🔥 UNIVERSAL BACK (Keyboard + Controller)
        bool backPressed =
            Keyboard.current.escapeKey.wasPressedThisFrame ||
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

        if (backPressed)
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

        // ⌨️ Keyboard (still allowed)
        if (Keyboard.current.wKey.isPressed) vertical += 1f;
        if (Keyboard.current.sKey.isPressed) vertical -= 1f;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            select = true;

        if (vertical > 0.5f) ChangeIndex(-1);
        if (vertical < -0.5f) ChangeIndex(1);

        if (select) Activate();
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

        guidingStar.position = Vector3.SmoothDamp(
     guidingStar.position,
     target,
     ref velocity,
     0.15f,
     Mathf.Infinity,
     Time.unscaledDeltaTime
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