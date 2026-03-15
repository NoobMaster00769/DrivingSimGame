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
        // Disable ALL menus first
        foreach (var m in menuControllers)
        {
            m.gameObject.SetActive(false);
        }

        // Activate main menu
        currentMenu = menuControllers[0];
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

        // prepare next menu
        nextMenu.gameObject.SetActive(true);
        nextMenu.enabled = false;

        nextT.localPosition = new Vector3(direction * slideDistance, 0, 0);

        timer = 0;

        currentIndex = index;
    }

    void Update()
    {
        if (!transitioning) return;

        timer += Time.deltaTime;

        float t = Mathf.Clamp01(timer / slideDuration);

        Vector3 currentTarget = new Vector3(-direction * slideDistance, 0, 0);
        Vector3 nextTarget = Vector3.zero;

        currentT.localPosition =
            Vector3.Lerp(Vector3.zero, currentTarget, t);

        nextT.localPosition =
            Vector3.Lerp(new Vector3(direction * slideDistance, 0, 0), nextTarget, t);

        if (t >= 1f)
        {
            FinishTransition();
        }
    }

    void FinishTransition()
    {
        // hide previous menu completely
        currentMenu.gameObject.SetActive(false);

        // activate new menu
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
}