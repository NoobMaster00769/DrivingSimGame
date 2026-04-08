using UnityEngine;
using System.Collections;

public class CameraDirector : MonoBehaviour
{
    public Camera menuCamera;        
    public Camera gameplayCamera;    

    public float transitionTime = 0.6f;

    bool isTransitioning;

    Vector3 menuStartPos;
    Quaternion menuStartRot;

    void Awake()
    {

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
        StartCoroutine(GameplayToMenu()); 
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

        menuT.position = targetPos;
        menuT.rotation = targetRot;


        menuCamera.gameObject.SetActive(false);
        gameplayCamera.gameObject.SetActive(true);

        isTransitioning = false;
    }

    IEnumerator GameplayToMenu()
    {
        isTransitioning = true;

        Transform menuT = menuCamera.transform;
        Transform gameT = gameplayCamera.transform;

        menuT.position = gameT.position;
        menuT.rotation = gameT.rotation;

        Vector3 startPos = menuT.position;
        Quaternion startRot = menuT.rotation;

        Vector3 targetPos = new Vector3(
            menuStartPos.x,
            menuStartPos.y,
            gameT.position.z   
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


        menuT.position = targetPos;
        menuT.rotation = targetRot;

        isTransitioning = false;
    }
}