using UnityEngine;

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

        timer += Time.deltaTime;

        HandleNavigation();
        MoveGuidingStar();
        AnimateSelection();
        UpdateSliders();
    }

    void HandleNavigation()
    {
        if (timer < inputCooldown) return;

        // A / D navigation
        if (input.Steering > 0.6f) ChangeIndex(1);
        if (input.Steering < -0.6f) ChangeIndex(-1);

        // W / S volume control (Throttle axis)
        if (input.Throttle > 0.6f) IncreaseValue();
        if (input.Throttle < -0.6f) DecreaseValue();

        // SPACE = select
        if (input.Brake > 0.5f) ActivateOption();
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

        guidingStar.localPosition =
            Vector3.SmoothDamp(
                guidingStar.localPosition,
                targetPos,
                ref velocity,
                0.18f
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
                    Time.deltaTime * 7f
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
        // Back → Settings
        if (index == 1)
        {
            FindObjectOfType<MenuManager>().OpenMenu(1);
        }

        timer = 0;
    }

    void UpdateSliders()
    {
        masterSlider.SetValue(audioController.masterVolume);
        bgmSlider.SetValue(audioController.bgmVolume);
    }
}