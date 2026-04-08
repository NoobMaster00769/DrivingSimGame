using UnityEngine;
using UnityEngine.InputSystem;

public class AudioMenuController : MonoBehaviour
{
    public Transform[] options;
    public StarHaloGenerator[] halos;
    public Transform guidingStar;
    public VehicleInputReader input;

    public float starHeight = 3f;

    public WorldAudioController audioController;

    public StarVolumeSlider masterSlider;
    public StarVolumeSlider bgmSlider;

    int index = 0;
    float inputCooldown = 0.25f;
    float timer;

    Vector3 velocity;

    void Start()
    {
        index = Mathf.Clamp(index, 0, options.Length - 1);

        if (halos.Length > index)
            halos[index].Highlight(true);
    }

    void Update()
    {
        if (!enabled) return;

        var manager = FindObjectOfType<MenuManager>();
        if (manager && manager.IsTransitioning())
            return;

        timer += Time.unscaledDeltaTime;

        HandleNavigation();
        MoveGuidingStar();
        AnimateSelection();
        UpdateSliders();
    }

    void HandleNavigation()
    {
        if (timer < inputCooldown) return;


        if (Keyboard.current.escapeKey.wasPressedThisFrame ||
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame))
        {
            TriggerBack();
            return;
        }

        float horizontal = 0f;
        float vertical = 0f;
        bool select = false;


        if (Gamepad.current != null)
        {
            var stick = Gamepad.current.leftStick.ReadValue();

            horizontal += stick.x;
            vertical += stick.y;

            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                select = true;
        }


        if (Keyboard.current.aKey.isPressed) horizontal -= 1f;
        if (Keyboard.current.dKey.isPressed) horizontal += 1f;

        if (Keyboard.current.wKey.isPressed) vertical += 1f;
        if (Keyboard.current.sKey.isPressed) vertical -= 1f;

        if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
            select = true;

        if (horizontal > 0.6f) ChangeIndex(1);
        if (horizontal < -0.6f) ChangeIndex(-1);

        if (vertical > 0.6f) IncreaseValue();
        if (vertical < -0.6f) DecreaseValue();

        if (select) ActivateOption();
    }

    void TriggerBack()
    {
        var state = GameStateController.Instance.currentState;

        if (state == GameState.Paused)
            FindObjectOfType<MenuManager>().OpenMenu(7);
        else
        {
            index = 1;
            ActivateOption();
        }
    }
    void ChangeIndex(int direction)
    {
        halos[index].Highlight(false);

        index += direction;

        if (index >= options.Length) index = 0;
        if (index < 0) index = options.Length - 1;

        halos[index].Highlight(true);

        guidingStar.GetComponent<GuidingStarFlash>()?.TriggerFlash();
        halos[index].Pulse();

        timer = 0;
    }

    void MoveGuidingStar()
    {
        Transform target = options[index];

        Vector3 targetPos =
            target.localPosition + Vector3.up * starHeight;

        guidingStar.localPosition = Vector3.SmoothDamp(
     guidingStar.localPosition,
     targetPos,
     ref velocity,
     0.18f,
     Mathf.Infinity,
     Time.unscaledDeltaTime
 );
    }

    void AnimateSelection()
    {
        for (int i = 0; i < options.Length; i++)
        {
            float scale = (i == index) ? 1.25f : 1f;

            options[i].localScale =
                Vector3.Lerp(
                    options[i].localScale,
                    Vector3.one * scale,
                    Time.unscaledDeltaTime * 7f
                );
        }
    }

    void IncreaseValue()
    {
        if (index == 0)
            audioController.masterVolume =
                Mathf.Clamp01(audioController.masterVolume + 0.05f);

        if (index == 2)
            audioController.bgmVolume =
                Mathf.Clamp01(audioController.bgmVolume + 0.05f);

        timer = 0;
    }

    void DecreaseValue()
    {
        if (index == 0)
            audioController.masterVolume =
                Mathf.Clamp01(audioController.masterVolume - 0.05f);

        if (index == 2)
            audioController.bgmVolume =
                Mathf.Clamp01(audioController.bgmVolume - 0.05f);

        timer = 0;
    }

    void ActivateOption()
    {

        if (index == 1)
        {
            FindObjectOfType<MenuManager>().OpenMenu(1);
        }

        timer = 0;
    }
    void OnEnable()
    {
        ResetSelection();
        FindObjectOfType<NavigationFooterUI>()
    .SetMenuType(NavigationFooterUI.MenuType.Slider);
    }
    void ResetSelection()
    {
        index = 0;

        for (int i = 0; i < halos.Length; i++)
            halos[i].Highlight(false);

        if (halos.Length > 0)
            halos[0].Highlight(true);

        timer = 0;
    }
    void UpdateSliders()
    {
        masterSlider.SetValue(audioController.masterVolume);
        bgmSlider.SetValue(audioController.bgmVolume);
    }
}