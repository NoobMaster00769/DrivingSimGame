using UnityEngine;
using System.Collections;

public class CameraDirector : MonoBehaviour
{
    public Camera menuCamera;        // static menu cam
    public Camera gameplayCamera;    // main camera (with CarFollowCamera)

    public float transitionTime = 0.6f;

    bool isTransitioning;

    Vector3 menuStartPos;
    Quaternion menuStartRot;

    void Awake()
    {
        // Store menu camera original position
        menuStartPos = menuCamera.transform.position;
        menuStartRot = menuCamera.transform.rotation;
    }

    public void SwitchToGameplay()
    {
        if (isTransitioning) return;

        StopAllCoroutines();
        StartCoroutine(MenuToGameplay());
    }

    public void SwitchToMenu()
    {
        if (isTransitioning) return;

        StopAllCoroutines();
        StartCoroutine(GameplayToMenu()); // 🔥 FIX: use correct coroutine
    }

    IEnumerator MenuToGameplay()
    {
        isTransitioning = true;

        Transform menuT = menuCamera.transform;
        Transform gameT = gameplayCamera.transform;

        Vector3 startPos = menuT.position;
        Quaternion startRot = menuT.rotation;

        Vector3 targetPos = gameT.position;
        Quaternion targetRot = gameT.rotation;

        menuCamera.gameObject.SetActive(true);
        gameplayCamera.gameObject.SetActive(false);

        float t = 0;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / transitionTime;

            menuT.position = Vector3.Lerp(startPos, targetPos, t);
            menuT.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        // snap
        menuT.position = targetPos;
        menuT.rotation = targetRot;

        // switch
        menuCamera.gameObject.SetActive(false);
        gameplayCamera.gameObject.SetActive(true);

        isTransitioning = false;
    }

    IEnumerator GameplayToMenu()
    {
        isTransitioning = true;

        Transform menuT = menuCamera.transform;
        Transform gameT = gameplayCamera.transform;

        // 🔥 start from gameplay camera position
        menuT.position = gameT.position;
        menuT.rotation = gameT.rotation;

        Vector3 startPos = menuT.position;
        Quaternion startRot = menuT.rotation;

        Vector3 targetPos = new Vector3(
            menuStartPos.x,
            menuStartPos.y,
            gameT.position.z   // 🔥 FOLLOW CAR FORWARD
        );

        Quaternion targetRot = menuStartRot;

        menuCamera.gameObject.SetActive(true);
        gameplayCamera.gameObject.SetActive(false);

        float t = 0;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / transitionTime;

            menuT.position = Vector3.Lerp(startPos, targetPos, t);
            menuT.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return null;
        }

        // snap back to original menu position
        menuT.position = targetPos;
        menuT.rotation = targetRot;

        isTransitioning = false;
    }
}