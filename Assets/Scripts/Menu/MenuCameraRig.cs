using UnityEngine;

public class MenuCameraRig : MonoBehaviour
{
    public Camera menuCamera;

    public GameObject celestialMenu;

    public Camera gameplayCamera;

    public Transform gameplayCameraTransform;

    public float transitionSpeed = 2f;

    bool transitioning = false;

    void Start()
    {
        gameplayCamera.enabled = false;
    }

    void Update()
    {
        if (!transitioning)
            return;

        transform.position =
            Vector3.Lerp(
                transform.position,
                gameplayCameraTransform.position,
                Time.deltaTime * transitionSpeed
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                gameplayCameraTransform.rotation,
                Time.deltaTime * transitionSpeed
            );

        if (Vector3.Distance(transform.position, gameplayCameraTransform.position) < 0.1f)
        {
            gameplayCamera.enabled = true;
            menuCamera.enabled = false;

            celestialMenu.SetActive(false);

            GameStateController.Instance.SetState(GameState.Driving);
        }
    }

    public void StartTransition()
    {
        transitioning = true;
    }

    public void SwitchToMenuInstant()
    {
        transitioning = false;

        menuCamera.enabled = true;
        gameplayCamera.enabled = false;

        celestialMenu.SetActive(true);
    }

}