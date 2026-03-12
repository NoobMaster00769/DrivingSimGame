using UnityEngine;
using System.Collections;

public class WorldAudioController : MonoBehaviour
{
    public WorldEventDirector director;
    public RoadState road;
    public Rigidbody carRB;

    [Header("Music")]
    public AudioSource musicA;
    public AudioSource musicB;

    [Header("Layers")]
    public AudioSource pianoLayer;
    public AudioSource cosmicAmbience;
    public AudioSource wind;
    public AudioSource engine;
    public AudioSource specialFX;

    [Header("Arc Music")]
    public AudioClip[] arcMusic;

    int currentArc = -1;
    bool usingA = true;

    void Start()
    {
        ChangeArcMusic(director.CurrentArcIndex);
    }

    void Update()
    {
        UpdateSpeedSounds();
        UpdateEmotionLayers();
        UpdateArcMusic();
    }

    void UpdateArcMusic()
    {
        if (director.CurrentArcIndex != currentArc)
        {
            ChangeArcMusic(director.CurrentArcIndex);
        }
    }

    void ChangeArcMusic(int arc)
    {
        currentArc = arc;

        AudioSource active = usingA ? musicA : musicB;
        AudioSource next = usingA ? musicB : musicA;

        next.clip = arcMusic[arc];
        next.volume = 0;
        next.Play();

        StartCoroutine(Crossfade(active, next));

        usingA = !usingA;
    }

    IEnumerator Crossfade(AudioSource a, AudioSource b)
    {
        float t = 0;

        while (t < 5f)
        {
            t += Time.deltaTime;
            float k = t / 5f;

            a.volume = Mathf.Lerp(1, 0, k);
            b.volume = Mathf.Lerp(0, 1, k);

            yield return null;
        }

        a.Stop();
    }

    void UpdateSpeedSounds()
    {
        float speed = carRB.velocity.magnitude;

        float normalized = Mathf.Clamp01(speed / 30f);

        engine.pitch = Mathf.Lerp(0.7f, 1.4f, normalized);
        engine.volume = Mathf.Lerp(0.25f, 0.7f, normalized);

        wind.volume = Mathf.Lerp(0f, 0.8f, normalized);
    }

    void UpdateEmotionLayers()
    {
        pianoLayer.volume = Mathf.Lerp(0f, 0.6f, road.serenity);

        cosmicAmbience.volume = Mathf.Lerp(0.4f, 0.1f, road.tempest);
    }
}