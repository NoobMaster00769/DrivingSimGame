using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public MonoBehaviour[] menuControllers;

    public float slideDistance = 60f;
    public float slideDuration = 0.35f;

    int currentIndex = 0;

    bool transitioning;

    MonoBehaviour currentMenu;
    MonoBehaviour nextMenu;

    Transform currentT;
    Transform nextT;

    float timer;
    int direction;

    void Start()
    {

        foreach (var m in menuControllers)
        {
            m.gameObject.SetActive(false);
        }


        currentMenu = menuControllers[0];
        currentT = currentMenu.transform;

        currentMenu.gameObject.SetActive(true);
        currentMenu.enabled = true;
        currentT.localPosition = Vector3.zero;
    }

    public void OpenMenuInstant(int index)
    {
        foreach (var m in menuControllers)
            m.gameObject.SetActive(false);

        currentIndex = index;

        currentMenu = menuControllers[index];
        currentT = currentMenu.transform;

        currentMenu.gameObject.SetActive(true);
        currentMenu.enabled = true;

        currentT.localPosition = Vector3.zero;
    }

    public void OpenMenu(int index)
    {
        if (transitioning) return;
        if (index == currentIndex) return;

        transitioning = true;

        direction = index > currentIndex ? 1 : -1;

        nextMenu = menuControllers[index];
        nextT = nextMenu.transform;


        nextMenu.gameObject.SetActive(true);
        nextMenu.enabled = false;

        nextT.localPosition = new Vector3(direction * slideDistance, 0, 0);

        timer = 0;

        currentIndex = index;
    }

    void Update()
    {
        if (!transitioning) return;

        timer += Time.unscaledDeltaTime;

        float t = Mathf.Clamp01(timer / slideDuration);
        float curveHeight = 10f;
        float stretch = 1.15f;

        Vector3 currentStart = Vector3.zero;
        Vector3 currentEnd = new Vector3(-direction * slideDistance, 0, 0);

        Vector3 nextStart = new Vector3(direction * slideDistance, 0, 0);
        Vector3 nextEnd = Vector3.zero;


        Vector3 currentPos =
            Vector3.Lerp(currentStart, currentEnd, t)
            + Vector3.up * Mathf.Sin(t * Mathf.PI) * curveHeight;

        Vector3 nextPos =
            Vector3.Lerp(nextStart, nextEnd, t)
            + Vector3.up * Mathf.Sin(t * Mathf.PI) * curveHeight;

        currentT.localPosition = currentPos;
        nextT.localPosition = nextPos;


        float scaleWarp = 1 + Mathf.Sin(t * Mathf.PI) * (stretch - 1);

        currentT.localScale = Vector3.one * scaleWarp;
        nextT.localScale = Vector3.one * scaleWarp;
        if (t >= 1f)
        {
            FinishTransition();
        }
    }

    void FinishTransition()
    {
        currentT.localScale = Vector3.one;

        nextT.localScale = Vector3.one;


        var halos = currentMenu.GetComponentsInChildren<StarHaloGenerator>();

        foreach (var h in halos)
        {
            h.Pulse();
        }

        currentMenu.gameObject.SetActive(false);


        nextMenu.enabled = true;

        currentMenu = nextMenu;
        currentT = nextT;

        currentT.localPosition = Vector3.zero;

        transitioning = false;
    }

    public bool IsTransitioning()
    {
        return transitioning;
    }
    public void SetMenusActive(bool active)
    {
        foreach (var m in menuControllers)
            m.gameObject.SetActive(active);
    }

}