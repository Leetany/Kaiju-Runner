using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public enum RunSFXType
{
    Footstep,
    Jump,
    Slide,
    Land,
    Skill,
    Hit,
    Item,
    GameOver,
    UIButton,
    // �߰��� ���� Ÿ�Ե�
    DragonRoar,
    DragonAttack,
    DragonDeath,
    MinionSpawn,
    MinionHit,
    MinionDeath,
    StageClear,
    StageStart
}

[System.Serializable]
public class RunSoundGroup
{
    public RunSFXType type;
    public AudioClip[] clips;

    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }
}

public class RunSoundManager : SingletonBehaviour<RunSoundManager>, IPunObservable
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("BGM")]
    [SerializeField] private AudioClip defaultBGM;

    [Header("SFX Groups")]
    [SerializeField] private List<RunSoundGroup> sfxGroups;

    private Dictionary<RunSFXType, RunSoundGroup> sfxDict;
    private PhotonView pv;

    void Awake()
    {
        pv = GetComponent<PhotonView>();
        sfxDict = new Dictionary<RunSFXType, RunSoundGroup>();
        foreach (var group in sfxGroups)
        {
            sfxDict[group.type] = group;
        }
    }

    public void PlayBGM()
    {
        if (defaultBGM != null && !bgmSource.isPlaying)
        {
            bgmSource.clip = defaultBGM;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    /// <summary>
    /// ���ÿ����� ��� (�߼Ҹ� ��)
    /// </summary>
    public void PlayLocalSFX(RunSFXType type)
    {
        PlayClip(type);
    }

    /// <summary>
    /// ��� �������� ��� (�巡�� ��ȿ ��)
    /// </summary>
    public void PlayNetworkSFX(RunSFXType type)
    {
        pv.RPC(nameof(RPC_PlaySFX), RpcTarget.All, (int)type);
    }

    [PunRPC]
    void RPC_PlaySFX(int type)
    {
        PlayClip((RunSFXType)type);
    }

    private void PlayClip(RunSFXType type)
    {
        if (sfxDict.TryGetValue(type, out var group))
        {
            var clip = group.GetRandomClip();
            if (clip != null)
            {
                sfxSource.pitch = Random.Range(0.95f, 1.05f);
                sfxSource.PlayOneShot(clip);
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // ����ȭ�� �����Ͱ� ���ٸ� �����
    }
}
