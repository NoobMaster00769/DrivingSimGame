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
        if (timer < cooldown) return;

        // W = up
        if (input.Throttle > 0.6f)
            ChangeIndex(-1);

        // S = down
        if (input.Throttle < -0.6f)
            ChangeIndex(1);

        // SPACE = select
        if (input.Brake > 0.5f)
            Activate();

        // ESC cancel rebind
        if (Keyboard.current.escapeKey.wasPressedThisFrame && rebinding)
        {
            rebinding = false;
        }
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