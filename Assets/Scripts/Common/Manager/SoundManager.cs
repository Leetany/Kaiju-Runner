using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

//실행할 매서드
// 걸음 종류시
// SoundManager.Instance.PlayFootstep(SoundManager.FootstepType.Snow);

// 환경음 종류
// SoundManager.Instance.PlayAmbientSound(AmbientType.Nature);

// UI/시스템 사운드
// SoundManager.Instance.PlayItemPickup();


[System.Serializable]
public class SoundClipGroup
{
    public string groupName;
    public AudioClip[] clips;

    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }
}

[System.Serializable]
public class SceneBGMSetting
{
    [Tooltip("씬 이름 (대소문자 구분 안함)")]
    public string sceneName; [Tooltip("재생할 BGM AudioClip")]
    public AudioClip bgmClip;

    [Tooltip("페이드 인 사용 여부")]
    public bool useFadeIn = true;
}

public enum FootstepType
{
    Road, Back, Landing
}


public class SoundManager : SingletonBehaviour<SoundManager>
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource footstepSource;

    [Header("BGM Settings")]
    [SerializeField] private float bgmFadeTime = 2f;


    [Header("Scene BGM Settings")]
    [Tooltip("씬별 BGM 및 환경음 설정")]
    [SerializeField] private SceneBGMSetting[] sceneBGMSettings;
    private Dictionary<string, SceneBGMSetting> sceneBGMDict = new Dictionary<string, SceneBGMSetting>();

    [Header("Footstep Sounds")]
    [SerializeField] private SoundClipGroup roadFootsteps;
    [SerializeField] private SoundClipGroup backFootsteps;
    [SerializeField] private SoundClipGroup landingFootsteps;

    [Range(0f, 1f)] public float masterVolume = 0.5f;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.5f;
    [Range(0f, 1f)] public float ambientVolume = 0.5f;

    [Header("Mute Settings")]
    public bool isMasterMuted = false;
    public bool isBGMMuted = false;
    public bool isSFXMuted = false;
    public bool isAmbientMuted = false;

    // 현재 재생될 사운드
    private string currentBGM = "";
    private Coroutine bgmFadeCoroutine;

    protected override void Init()
    {
        base.Init();
        InitializeAudioSources();
        InitializeSceneBGMDictionary();

        // 씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoadedCallback;
    }

    protected override void Dispose()
    {
        // 씬 로드 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoadedCallback;
        base.Dispose();
    }

    // Unity 씬 로드 이벤트 콜백
    private void OnSceneLoadedCallback(Scene scene, LoadSceneMode mode)
    {
        OnSceneChanged(scene.name);
    }

    void Start()
    {
        ApplyVolumeSettings();
        LoadVolumeSettings();
    }

    void Update()
    {
        // 볼륨 설정 실시간 적용 (Inspector에서 조정 시)
        ApplyVolumeSettings();
    }

    private void InitializeAudioSources()
    {
        // AudioSource가 없으면 자동 생성
        if (bgmSource == null) bgmSource = CreateAudioSource("BGM", true);
        if (sfxSource == null) sfxSource = CreateAudioSource("SFX", false);
        if (footstepSource == null) footstepSource = CreateAudioSource("Footstep", false);
    }

    private AudioSource CreateAudioSource(string name, bool loop)
    {
        GameObject audioObj = new GameObject($"AudioSource_{name}");
        audioObj.transform.SetParent(transform);
        AudioSource source = audioObj.AddComponent<AudioSource>();
        source.loop = loop;
        source.playOnAwake = false;
        return source;
    }



    private void InitializeSceneBGMDictionary()
    {
        //씬 이름 대소문자 구분 없이 딕셔너리에 저장, 인스펙터에 있는 씬네임
        sceneBGMDict.Clear();
        if (sceneBGMSettings != null)
        {
            foreach (var setting in sceneBGMSettings)
            {
                if (!string.IsNullOrEmpty(setting.sceneName))
                {
                    sceneBGMDict[setting.sceneName.ToLower()] = setting;
                }
            }
        }
    }
    private void ApplyVolumeSettings()
    {
        // 볼륨 설정을 AudioSource에 적용 (뮤트 상태 고려)
        float effectiveMasterVolume = isMasterMuted ? 0f : masterVolume;
        float effectiveBGMVolume = (isBGMMuted || isMasterMuted) ? 0f : bgmVolume;
        float effectiveSFXVolume = (isSFXMuted || isMasterMuted) ? 0f : sfxVolume;
        float effectiveAmbientVolume = (isAmbientMuted || isMasterMuted) ? 0f : ambientVolume;

        if (bgmSource != null) bgmSource.volume = effectiveBGMVolume * effectiveMasterVolume;
        if (sfxSource != null) sfxSource.volume = effectiveSFXVolume * effectiveMasterVolume;
        if (footstepSource != null) footstepSource.volume = effectiveSFXVolume * effectiveMasterVolume;
    }

    #region Scene Management
    public void OnSceneChanged(string sceneName)
    {
        string sceneKey = sceneName.ToLower();

        if (sceneBGMDict.ContainsKey(sceneKey)) // 씬 이름을 소문자로 변환하여 딕셔너리에서 찾기
        {
            SceneBGMSetting setting = sceneBGMDict[sceneKey];

            // BGM 재생
            if (setting.bgmClip != null)
            {
                PlayBGM(setting.bgmClip, setting.useFadeIn);
            }
        }
        else
        {
            Debug.Log($"씬 '{sceneName}'에 대한 BGM 설정을 찾을 수 없습니다. Inspector에서 Scene BGM Settings를 확인해주세요.");
        }
    }
    #endregion

    #region BGM Management

    // 다른 클래스에서 사용할 때:
    // 페이드 인으로 BGM 재생
    // SoundManager.Instance.PlayBGM(newBGMClip);

    // 즉시 BGM 재생 (페이드 없음)
    // SoundManager.Instance.PlayBGM(newBGMClip, false);
    public void PlayBGM(AudioClip bgmClip, bool fadeIn = true)
    {
        // BGM 재생 메소드

        if (bgmClip == null) return;

        if (currentBGM == bgmClip.name) return; // 이미 재생 중

        currentBGM = bgmClip.name; // 현재 BGM 업데이트

        if (bgmFadeCoroutine != null)
        {
            // 이전 BGM 페이드 아웃 중지
            StopCoroutine(bgmFadeCoroutine);
        }

        if (fadeIn)
        {
            bgmFadeCoroutine = StartCoroutine(FadeBGM(bgmClip));
        }
        else
        {
            // 즉시 BGM 변경
            bgmSource.clip = bgmClip;
            bgmSource.Play();
        }
    }

    public void StopBGM(bool fadeOut = true)
    {
        if (fadeOut)
        {
            // BGM 페이드 아웃
            if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = StartCoroutine(FadeOutBGM());
        }
        else
        {
            // 즉시 BGM 중지
            bgmSource.Stop();
        }
        currentBGM = ""; // 현재 BGM 초기화
    }


    // BGM을 다른 BGM으로 교체할 때 사용하는 페이드 효과
    // 기존 BGM을 페이드 아웃 → 새 BGM으로 교체 → 새 BGM을 페이드 인

    private IEnumerator FadeBGM(AudioClip newClip)
    {
        float originalVolume = bgmSource.volume;

        // 1단계: 현재 재생 중인 BGM을 페이드 아웃
        if (bgmSource.isPlaying)
        {
            while (bgmSource.volume > 0)
            {
                bgmSource.volume -= originalVolume * Time.deltaTime / bgmFadeTime;
                yield return null;
            }
        }

        // 2단계: 새로운 BGM으로 교체하고 재생 시작
        bgmSource.clip = newClip;
        bgmSource.Play();

        // 3단계: 새로운 BGM을 페이드 인
        while (bgmSource.volume < originalVolume)
        {
            bgmSource.volume += originalVolume * Time.deltaTime / bgmFadeTime;
            yield return null;
        }

        // 페이드 완료 후 볼륨을 정확한 값으로 설정
        bgmSource.volume = originalVolume;
        bgmFadeCoroutine = null;
    }


    // BGM을 완전히 정지할 때 사용하는 페이드 아웃 효과
    // 현재 BGM을 페이드 아웃한 후 완전히 정지 (새 BGM으로 교체하지 않음)

    private IEnumerator FadeOutBGM()
    {
        float startVolume = bgmSource.volume;

        // 현재 BGM을 페이드 아웃
        while (bgmSource.volume > 0)
        {
            bgmSource.volume -= startVolume * Time.deltaTime / bgmFadeTime;
            yield return null;
        }

        // BGM 완전히 정지하고 볼륨 복원
        bgmSource.Stop();
        bgmSource.volume = startVolume;  // 다음에 BGM을 재생할 때를 위해 볼륨 복원
        bgmFadeCoroutine = null;
    }
    #endregion

    #region Footstep Sounds    


    // 다른 클래스에서 사용할 때:
    // 눈 위를 걷는 소리
    // SoundManager.Instance.PlayFootstep(FootstepType.Snow);

    public void PlayFootstep(FootstepType type)
    {
        SoundClipGroup clipGroup = null;

        switch (type)
        {

            case FootstepType.Road: clipGroup = roadFootsteps; break;
            case FootstepType.Back: clipGroup = backFootsteps; break;
            case FootstepType.Landing: clipGroup = landingFootsteps; break;

        }

        if (clipGroup != null)
        {
            AudioClip clip = clipGroup.GetRandomClip(); // 랜덤으로 발소리 클립 선택
            if (clip != null)
            {
                footstepSource.pitch = Random.Range(0.9f, 1.1f); // 약간의 피치 변화
                footstepSource.PlayOneShot(clip);
            }
        }
    }
    #endregion

    #region Volume Control
    // 볼륨 설정을 저장하고 불러오는 메소드
    public void SetMasterVolume(float volume)
    {
        // 마스터 볼륨 설정
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        SaveVolumeSettings();
    }

    public void SetBGMVolume(float volume)
    {
        // BGM 볼륨 설정
        bgmVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        SaveVolumeSettings();
    }

    public void SetSFXVolume(float volume)
    {
        // SFX 볼륨 설정
        sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        SaveVolumeSettings();
    }

    public void SetAmbientVolume(float volume)
    {
        // 환경음 볼륨 설정
        ambientVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        SaveVolumeSettings();
    }
    private void SaveVolumeSettings()
    {
        // 볼륨 설정을 PlayerPrefs에 저장
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("AmbientVolume", ambientVolume);

        // 뮤트 설정 저장 (bool을 int로 변환: false = 0, true = 1)
        PlayerPrefs.SetInt("MasterMuted", isMasterMuted ? 1 : 0);
        PlayerPrefs.SetInt("BGMMuted", isBGMMuted ? 1 : 0);
        PlayerPrefs.SetInt("SFXMuted", isSFXMuted ? 1 : 0);
        PlayerPrefs.SetInt("AmbientMuted", isAmbientMuted ? 1 : 0);

        PlayerPrefs.Save(); // 변경 사항 저장
    }
    private void LoadVolumeSettings()
    {
        // PlayerPrefs에서 볼륨 설정을 불러오기,씬이동시 사용하기 위해
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.5f);
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
        ambientVolume = PlayerPrefs.GetFloat("AmbientVolume", 0.5f);

        // 뮤트 설정 불러오기 (0 = false, 1 = true)
        isMasterMuted = PlayerPrefs.GetInt("MasterMuted", 0) == 1;
        isBGMMuted = PlayerPrefs.GetInt("BGMMuted", 0) == 1;
        isSFXMuted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;
        isAmbientMuted = PlayerPrefs.GetInt("AmbientMuted", 0) == 1; ApplyVolumeSettings();
    }

    public void SetMasterMute(bool muted)
    {
        isMasterMuted = muted;
        ApplyVolumeSettings();
        SaveVolumeSettings();
    }

    public void SetBGMMute(bool muted)
    {
        isBGMMuted = muted;
        ApplyVolumeSettings();
        SaveVolumeSettings();
    }

    public void SetSFXMute(bool muted)
    {
        isSFXMuted = muted;
        ApplyVolumeSettings();
        SaveVolumeSettings();
    }

    public void SetAmbientMute(bool muted)
    {
        isAmbientMuted = muted;
        ApplyVolumeSettings();
        SaveVolumeSettings();
    }

    public bool IsMasterMuted() => isMasterMuted;
    public bool IsBGMMuted() => isBGMMuted;
    public bool IsSFXMuted() => isSFXMuted;
    public bool IsAmbientMuted() => isAmbientMuted;

    #endregion
}
