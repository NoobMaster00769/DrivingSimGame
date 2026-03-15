using UnityEngine;
using System.Collections;

public class WorldAudioController : MonoBehaviour
{
    public WorldEventDirector director;
    public RoadState road;
    public Rigidbody carRB;

    [Header("Music Filters")]
    public AudioLowPassFilter filterA;
    public AudioLowPassFilter filterB;

    [Header("Music Sources")]
    public AudioSource musicA;
    public AudioSource musicB;

    [Header("Layers")]
    public AudioSource pianoLayer;
    public AudioSource cosmicAmbience;
    public AudioSource oceanAmbience;   // optional beach waves
    public AudioSource wind;
    public AudioSource engine;
    public AudioSource specialFX;

    [Header("Arc Music")]
    public AudioClip[] arcMusic;

    [Header("Global Mix")]
    [Range(0f, 1f)] public float masterVolume = 0.85f;
    [Range(0f, 1f)] public float bgmVolume = 1f;

    int currentArc = -1;
    bool usingA = true;

    float previousSpeed;

    void Start()
    {
        ChangeArcMusic(director.CurrentArcIndex);

        if (!pianoLayer.isPlaying) pianoLayer.Play();
        if (!cosmicAmbience.isPlaying) cosmicAmbience.Play();
        if (oceanAmbience && !oceanAmbience.isPlaying) oceanAmbience.Play();
        if (!wind.isPlaying) wind.Play();
        if (!engine.isPlaying) engine.Play();
    }

    void Update()
    {
        UpdateArcMusic();
        UpdateSpeedSounds();
        UpdateArcAtmosphere();
        UpdateMusicFiltering();
    }

    // --------------------------------------------------
    // ARC MUSIC CONTROL
    // --------------------------------------------------

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
        float targetVolume = GetArcMusicVolume(currentArc);

        while (t < 5f)
        {
            t += Time.deltaTime;
            float k = t / 5f;

            a.volume = Mathf.Lerp(targetVolume, 0f, k) * masterVolume * bgmVolume;
            b.volume = Mathf.Lerp(0f, targetVolume, k) * masterVolume * bgmVolume;

            yield return null;
        }

        a.Stop();
    }

    // slightly louder music but still background
    float GetArcMusicVolume(int arc)
    {
        switch (arc)
        {
            case 0: return 0.24f; // Calm
            case 1: return 0.28f; // Pulse
            case 2: return 0.28f; // Ribbon
            case 3: return 0.32f; // Chaotic
            case 4: return 0.20f; // Dream
            case 5: return 0.30f; // Surge
            case 6: return 0.25f; // Drift
        }

        return 0.26f;
    }

    // --------------------------------------------------
    // MOTION SOUNDS (speed dependent)
    // --------------------------------------------------

    void UpdateSpeedSounds()
    {
        float speed = carRB.velocity.magnitude;
        float normalized = Mathf.Clamp01(speed / 28f);

        float acceleration = (speed - previousSpeed) / Time.deltaTime;
        previousSpeed = speed;

        float accelFactor = Mathf.Clamp(acceleration * 0.03f, -0.2f, 0.25f);

        float basePitch = Mathf.Lerp(0.9f, 1.35f, normalized);

        engine.pitch = Mathf.Lerp(
            engine.pitch,
            basePitch + accelFactor,
            Time.deltaTime * 4f
        );

        engine.volume =
            Mathf.Lerp(0.22f, 0.55f, normalized) *
            masterVolume;

        wind.volume =
            Mathf.Lerp(0f, 0.20f, normalized) *
            masterVolume;
    }

    // --------------------------------------------------
    // ARC ATMOSPHERE
    // --------------------------------------------------

    void UpdateArcAtmosphere()
    {
        int arc = director.CurrentArcIndex;

        float pianoTarget = 0f;
        float cosmicTarget = 0f;
        float oceanTarget = 0f;

        switch (arc)
        {
            case 0: // Calm
                pianoTarget = 0.18f;
                cosmicTarget = 0.02f;
                oceanTarget = 0.10f;
                break;

            case 1: // Pulse
                pianoTarget = 0.05f;
                cosmicTarget = 0.03f;
                oceanTarget = 0.06f;
                break;

            case 2: // Ribbon
                pianoTarget = 0.10f;
                cosmicTarget = 0.03f;
                oceanTarget = 0.07f;
                break;

            case 3: // Chaotic
                pianoTarget = 0.0f;
                cosmicTarget = 0.04f;
                oceanTarget = 0.02f;
                break;

            case 4: // Dream
                pianoTarget = 0.22f;
                cosmicTarget = 0.015f;
                oceanTarget = 0.12f;
                break;

            case 5: // Surge
                pianoTarget = 0.06f;
                cosmicTarget = 0.03f;
                oceanTarget = 0.05f;
                break;

            case 6: // Drift
                pianoTarget = 0.12f;
                cosmicTarget = 0.025f;
                oceanTarget = 0.08f;
                break;
        }

        pianoLayer.volume =
            Mathf.Lerp(pianoLayer.volume, pianoTarget * masterVolume, Time.deltaTime * 1.5f);

        cosmicAmbience.volume =
            Mathf.Lerp(cosmicAmbience.volume, cosmicTarget * masterVolume, Time.deltaTime * 1.5f);

        if (oceanAmbience)
        {
            oceanAmbience.volume =
                Mathf.Lerp(oceanAmbience.volume, oceanTarget * masterVolume, Time.deltaTime * 1.5f);
        }
    }

    void UpdateMusicFiltering()
    {
        int arc = director.CurrentArcIndex;

        float cutoff = 16000f;

        switch (arc)
        {
            case 0: // Calm
                cutoff = 9000f;
                break;

            case 1: // Pulse
                cutoff = 14000f;
                break;

            case 2: // Ribbon
                cutoff = 12000f;
                break;

            case 3: // Chaotic
                cutoff = 18000f;
                break;

            case 4: // Dream
                cutoff = 7000f;
                break;

            case 5: // Surge
                cutoff = 17000f;
                break;

            case 6: // Drift
                cutoff = 11000f;
                break;
        }

        filterA.cutoffFrequency =
            Mathf.Lerp(filterA.cutoffFrequency, cutoff, Time.deltaTime * 1.2f);

        filterB.cutoffFrequency =
            Mathf.Lerp(filterB.cutoffFrequency, cutoff, Time.deltaTime * 1.2f);
    }
}