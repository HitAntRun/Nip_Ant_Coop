using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance { get; private set; }

    public const string BgmVolumeKey = "Audio.BGM";
    public const string SfxVolumeKey = "Audio.SFX";
    private const string BgmParam = "BGMVolume";
    private const string SfxParam = "SFXVolume";

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("BGM")]
    [SerializeField] private BgmDatabase bgmDatabase;
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private string[] keepBgmScenes = { SceneRouter.LoadingScene };
    private int lastClaimFrame = -1;
    public const string BgmNone = "none";
    
    [Header("SFX")]
    [SerializeField] private SfxLibrary sfxLibrary;
    [SerializeField] private int sfxSourceCount = 8;

    [Header("Typing SFX")] 
    [SerializeField] private AudioClip typingClip;
    [SerializeField, Range(0f, 1f)] private float typingVolume = 0.3f;
    [SerializeField] private float typingPitchJitter = 0.08f;
    [SerializeField] private float typingMinInterval = 0.055f;
    
    private AudioSource bgmSource;
    private AudioSource[] sfxSources;
    private AudioSource typingSource;
    private float lastTypingTime = -999f;

    private BgmPlaylist currentPlaylist;
    private ShuffleBag<AudioClip> bag;
    private Coroutine bgmRoutine;
    private Coroutine stopRoutine;
    private bool bgmPaused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void BootStrap() => EnsureExists();

    public static void EnsureExists()
    {
        if (instance != null) return;
        var prefab = Resources.Load<GameObject>("SoundManager");
        Instantiate(prefab);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.loop = false;
        bgmSource.spatialBlend = 0f;
        bgmSource.outputAudioMixerGroup = bgmGroup;

        sfxSources = new AudioSource[Mathf.Max(1, sfxSourceCount)];
        typingSource = gameObject.AddComponent<AudioSource>();
        typingSource.playOnAwake = false;
        typingSource.spatialBlend = 0f;
        typingSource.outputAudioMixerGroup = sfxGroup;
        
        for (int i = 0; i < sfxSources.Length; i++)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.outputAudioMixerGroup = sfxGroup;
            sfxSources[i] = s;
        }

        SetBgmVolume(PlayerPrefs.GetFloat(BgmVolumeKey, 0.8f));
        SetSfxVolume(PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f));

        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (System.Array.IndexOf(keepBgmScenes, scene.name) >= 0) return;
        StartCoroutine(StopBgmIfUnclaimed());
    }

    private IEnumerator StopBgmIfUnclaimed()
    {
        int frameAtLoad = Time.frameCount;
        yield return null;

        if (lastClaimFrame >= frameAtLoad) yield break;
        if (bgmRoutine == null && !bgmSource.isPlaying) yield break;
        
        StopBgm();
    }

    public void PlayBgm(BgmPlaylist playlist, bool restartIfSame = false)
    {
        lastClaimFrame = Time.frameCount;
        if (stopRoutine != null) { StopCoroutine(stopRoutine); stopRoutine = null; }
        
        if (playlist == null || playlist.Clips == null || playlist.Clips.Length == 0)
        {
            StopBgm();
            return;
        }

        if (!restartIfSame && currentPlaylist == playlist && bgmRoutine != null) return;

        currentPlaylist = playlist;
        bag = new ShuffleBag<AudioClip>(playlist.Clips, playlist.avoidRepeatOnRefill);
        
        if(bgmRoutine != null) StopCoroutine(bgmRoutine);
        bgmRoutine = StartCoroutine(BgmLoop());
    }
    
    public void PlayBgmById(string id, bool restartIfSame = false)
    {
        if (string.IsNullOrEmpty(id)) { KeepBgm(); return; }

        lastClaimFrame = Time.frameCount;

        if (id == BgmNone) { StopBgm(); return; }

        if (bgmDatabase == null)
        {
            Debug.LogWarning("[SoundManager] BgmDatabase가 연결되지 않음");
            return;
        }

        var playlist = bgmDatabase.Get(id);
        if (playlist == null)
        {
            Debug.LogWarning($"[SoundManager] BGM '{id}' 를 DB에서 못 찾음");
            return;
        }

        PlayBgm(playlist, restartIfSame);
    }

    public void StopBgm(bool fade = true)
    {
        if (bgmRoutine != null) { StopCoroutine(bgmRoutine); bgmRoutine = null; }
        currentPlaylist = null;

        if (stopRoutine != null) StopCoroutine(stopRoutine);
        if (fade) stopRoutine = StartCoroutine(FadeOutAndStop());
        else bgmSource.Stop();
    }

    public void KeepBgm() => lastClaimFrame = Time.frameCount;

    public void SkipBgm()
    {
        if (currentPlaylist != null) bgmSource.Stop();
    }

    public void SetBgmPaused(bool paused)
    {
        bgmPaused = paused;
        if(paused) bgmSource.Pause();
        else bgmSource.UnPause();
    }

    private IEnumerator BgmLoop()
    {
        if(bgmSource.isPlaying) yield return FadeTo(0f, fadeDuration);
        bgmSource.Stop();

        if (currentPlaylist.Clips.Length == 1)
        {
            bgmSource.clip = currentPlaylist.Clips[0];
            bgmSource.loop = true;
            bgmSource.volume = 0f;
            bgmSource.Play();
            yield return FadeTo(currentPlaylist.volume, fadeDuration);
            yield break;
        }
        
        bgmSource.loop = false;

        while (true)
        {
            AudioClip clip = bag.Next();
            if (clip == null) yield break;

            bgmSource.clip = clip;
            bgmSource.volume = 0f;
            bgmSource.Play();
            yield return FadeTo(currentPlaylist.volume, fadeDuration);

            while (bgmSource.isPlaying || bgmPaused)
                yield return null;

            float gap = currentPlaylist.gapBetweenTracks;
            if (gap > 0f) yield return new WaitForSecondsRealtime(gap);
        }
    }

    public void PlayTyping()
    {
        if (typingClip == null || typingSource == null) return;
        if (Time.unscaledTime - lastTypingTime < typingMinInterval) return;
        lastTypingTime = Time.unscaledTime;

        typingSource.pitch = 1f + Random.Range(-typingPitchJitter, typingPitchJitter);
        typingSource.PlayOneShot(typingClip, typingVolume);
    }

    public void StopTyping()
    {
        if (typingSource != null) typingSource.Stop();
    }
    
    private IEnumerator FadeTo(float target, float dur)
    {
        float start = bgmSource.volume;
        if (dur <= 0f) { bgmSource.volume = target; yield break; }

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(start, target, t / dur);
            yield return null;
        }
        bgmSource.volume = target;
    }
    
    private IEnumerator FadeOutAndStop()
    {
        yield return FadeTo(0f, fadeDuration);
        bgmSource.Stop();
    }
    
    public void PlaySfx(string id)
    {
        if (sfxLibrary == null) return;
        var e = sfxLibrary.Find(id);
        if (e == null || e.clips == null || e.clips.Length == 0) return;

        var clip = e.clips[Random.Range(0, e.clips.Length)];
        var src = GetFreeSfxSource();
        src.pitch = Random.Range(e.pitchRange.x, e.pitchRange.y);
        src.PlayOneShot(clip, e.volume);
    }

    public void StopAllSfx()
    {
        if (sfxSources == null) return;
        for(int i = 0; i < sfxSources.Length; i++)
            if(sfxSources[i] != null) sfxSources[i].Stop();
    }
    private AudioSource GetFreeSfxSource()
    {
        for (int i = 0; i < sfxSources.Length; i++)
            if (!sfxSources[i].isPlaying) return sfxSources[i];
        return sfxSources[0];
    }
    
    public void SetBgmVolume(float v01) => ApplyVolume(BgmParam, BgmVolumeKey, v01);
    public void SetSfxVolume(float v01) => ApplyVolume(SfxParam, SfxVolumeKey, v01);
    public float GetBgmVolume() => PlayerPrefs.GetFloat(BgmVolumeKey, 0.8f);
    public float GetSfxVolume() => PlayerPrefs.GetFloat(SfxVolumeKey, 0.8f);

    private void ApplyVolume(string param, string key, float v01)
    {
        v01 = Mathf.Clamp01(v01);
        PlayerPrefs.SetFloat(key, v01);
        if (mixer != null)
            mixer.SetFloat(param, v01 <= 0.0001f ? -80f : Mathf.Log10(v01) * 20f);
    }

}
