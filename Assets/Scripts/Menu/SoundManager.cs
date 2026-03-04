using System.Collections;
using UnityEngine;

public enum MusicTrack { None, Ingame, Credits, Victory, GameOver }

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Musiques")]
    [SerializeField] private AudioClip musicIngame;
    [SerializeField] private AudioClip musicCredits;
    [SerializeField] private AudioClip musicVictory;
    [SerializeField] private AudioClip musicGameOver;

    [Header("Musique au démarrage")]
    [SerializeField] private MusicTrack playOnStart = MusicTrack.Ingame;

    [Header("SFX – Bouteille")]
    [SerializeField] private AudioClip sfxBrokenBottle;
    [SerializeField] private AudioClip sfxChampagneOpen;
    [SerializeField] private AudioClip sfxShake;
    [SerializeField] private AudioClip sfxTakeBottle;
    [SerializeField] private AudioClip sfxBottleOver;

    [Header("SFX – UI")]
    [SerializeField] private AudioClip sfxCliquer;

    [Header("SFX – Roulette")]
    [SerializeField] private AudioClip sfxRouletteTick;

    [Header("SFX – Fin de partie")]
    [SerializeField] private AudioClip sfxHappyEnding;
    [SerializeField] private AudioClip sfxUnhappyEnding;

    [Header("SFX – Réactions (probabilité aléatoire)")]
    [SerializeField] private AudioClip sfxHappyMen;
    [SerializeField] private AudioClip sfxAngryMen;
    [SerializeField] private AudioClip sfxHappyWomen;
    [SerializeField] private AudioClip sfxAngryWomen;

    [Tooltip("Probabilité (0-1) qu'une réaction soit jouée lors d'une action.")]
    [Range(0f, 1f)]
    [SerializeField] private float reactionChance = 0.4f;

    [Header("Volumes")]
    [Range(0f, 1f)][SerializeField] private float musicVolume = 0.5f;
    [Range(0f, 1f)][SerializeField] private float sfxVolume   = 1.0f;

    private AudioSource _musicSource;
    private AudioSource _sfxSource;

    private const float MusicFadeDuration = 1.0f;
    private Coroutine _fadeCoroutine;
    private Coroutine _endingCoroutine;

    // ── Unity ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _musicSource = CreateSource("MusicSource", loop: true,  volume: musicVolume);
        _sfxSource   = CreateSource("SFXSource",   loop: false, volume: sfxVolume);
    }

    private void Start()
    {
        switch (playOnStart)
        {
            case MusicTrack.Ingame:   PlayMusicIngame();                       break;
            case MusicTrack.Credits:  PlayMusicCredits();                      break;
            case MusicTrack.Victory:  FadeToMusic(musicVictory,  loop: false); break;
            case MusicTrack.GameOver: FadeToMusic(musicGameOver, loop: false); break;
        }
    }

    // ── API Musique ───────────────────────────────────────────────────

    /// <summary>Joue la musique in-game avec fondu.</summary>
    public void PlayMusicIngame() => FadeToMusic(musicIngame, loop: true);

    /// <summary>Joue la musique des crédits avec fondu.</summary>
    public void PlayMusicCredits() => FadeToMusic(musicCredits, loop: true);

    /// <summary>Coupe la musique avec fondu.</summary>
    public void StopMusic() => FadeToMusic(null);

    /// <summary>Stoppe immédiatement tous les SFX en cours.</summary>
    public void StopAllSFX() => _sfxSource.Stop();

    // ── API Fins de partie ────────────────────────────────────────────

    /// <summary>Stoppe tout, joue HappyEndingSound puis Victory music (une fois).</summary>
    public void PlayVictorySequence()
    {
        StopAllSFX();
        if (_endingCoroutine != null) StopCoroutine(_endingCoroutine);
        _endingCoroutine = StartCoroutine(EndingSequenceCoroutine(sfxHappyEnding, musicVictory));
    }

    /// <summary>Stoppe tout, joue UnhappyEndingSound puis GameOver music (une fois).</summary>
    public void PlayGameOverSequence()
    {
        StopAllSFX();
        if (_endingCoroutine != null) StopCoroutine(_endingCoroutine);
        _endingCoroutine = StartCoroutine(EndingSequenceCoroutine(sfxUnhappyEnding, musicGameOver));
    }

    // ── API SFX Bouteille ─────────────────────────────────────────────

    /// <summary>Son de casse selon le type de bouteille.</summary>
    public void PlayBottleExplosion(BottleType bottleType)
    {
        if (bottleType == BottleType.Champagne) PlaySFX(sfxChampagneOpen);
        else PlaySFX(sfxBrokenBottle);
    }

    /// <summary>Son quand la bouteille passe à l'état Crack.</summary>
    public void PlayBottleOver() => PlaySFX(sfxBottleOver);

    /// <summary>Son de secouage.</summary>
    public void PlayShake() => PlaySFX(sfxShake);

    /// <summary>Son de passage de bouteille (fin de tour).</summary>
    public void PlayTakeBottle() => PlaySFX(sfxTakeBottle);

    // ── API SFX Roulette ──────────────────────────────────────────────

    /// <summary>Joue le tick sonore de la roulette à chaque déplacement de la flèche.</summary>
    public void PlayRouletteTick() => PlaySFX(sfxRouletteTick);

    // ── API SFX UI ────────────────────────────────────────────────────

    /// <summary>Son de clic UI.</summary>
    public void PlayClic() => PlaySFX(sfxCliquer);

    // ── API SFX Réactions ─────────────────────────────────────────────

    /// <summary>Joue une réaction aléatoire selon le personnage (probabilité reactionChance).</summary>
    public void PlayReaction(bool isFemale)
    {
        if (Random.value > reactionChance) return;

        AudioClip clip = isFemale
            ? (Random.value < 0.5f ? sfxHappyWomen : sfxAngryWomen)
            : (Random.value < 0.5f ? sfxHappyMen   : sfxAngryMen);

        PlaySFX(clip);
    }

    // ── API Volume ────────────────────────────────────────────────────

    /// <summary>Règle le volume de la musique (0-1).</summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        _musicSource.volume = musicVolume;
    }

    /// <summary>Règle le volume des SFX (0-1).</summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        _sfxSource.volume = sfxVolume;
    }

    // ── Internals ─────────────────────────────────────────────────────

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip, sfxVolume);
    }

    private void FadeToMusic(AudioClip newClip, bool loop = true)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeMusicCoroutine(newClip, loop));
    }

    private IEnumerator EndingSequenceCoroutine(AudioClip endingStinger, AudioClip endingMusic)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _musicSource.Stop();
        _musicSource.clip = null;

        if (endingStinger != null)
        {
            _sfxSource.PlayOneShot(endingStinger, sfxVolume);
            yield return new WaitForSecondsRealtime(endingStinger.length);
        }

        if (endingMusic != null)
        {
            _musicSource.loop   = false;
            _musicSource.clip   = endingMusic;
            _musicSource.volume = 0f;
            _musicSource.Play();

            float elapsed = 0f;
            while (elapsed < MusicFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / MusicFadeDuration);
                yield return null;
            }
            _musicSource.volume = musicVolume;
        }
    }

    private IEnumerator FadeMusicCoroutine(AudioClip newClip, bool loop)
    {
        if (_musicSource.isPlaying)
        {
            float startVolume = _musicSource.volume;
            float elapsed     = 0f;

            while (elapsed < MusicFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / MusicFadeDuration);
                yield return null;
            }

            _musicSource.Stop();
            _musicSource.clip = null;
        }

        if (newClip == null) yield break;

        _musicSource.loop   = loop;
        _musicSource.clip   = newClip;
        _musicSource.volume = 0f;
        _musicSource.Play();

        float elapsed2 = 0f;
        while (elapsed2 < MusicFadeDuration)
        {
            elapsed2 += Time.unscaledDeltaTime;
            _musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed2 / MusicFadeDuration);
            yield return null;
        }
        _musicSource.volume = musicVolume;
    }

    private AudioSource CreateSource(string sourceName, bool loop, float volume)
    {
        var go     = new GameObject(sourceName);
        go.transform.SetParent(transform);
        var source            = go.AddComponent<AudioSource>();
        source.loop           = loop;
        source.volume         = volume;
        source.playOnAwake    = false;
        source.spatialBlend   = 0f;
        return source;
    }
}
