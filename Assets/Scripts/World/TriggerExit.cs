using UnityEngine;
using System.Collections;

public class TriggerExit : MonoBehaviour
{
    public float delay = 2f;

    public delegate void ExitAction();
    public static event ExitAction OnChunkExited;

    bool exited = false;

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (exited)
            return;

        exited = true;

        OnChunkExited?.Invoke();

        StartCoroutine(DisableLater());
    }

    IEnumerator DisableLater()
    {
        yield return new WaitForSeconds(delay);
        transform.root.gameObject.SetActive(false);
    }
}