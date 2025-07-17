using System.Collections;
using System.Linq;
using UnityEngine;

public class LHK_FlashbangEffect : MonoBehaviour
{
    public static LHK_FlashbangEffect Instance { get; private set; }
    [SerializeField] CanvasGroup cg;

    void Awake() { Instance = this; cg.alpha = 0; }

    public void Play(float dur) => StartCoroutine(Fade(dur));

    IEnumerator Fade(float d)
    {
        cg.alpha = 1f;
        float t = 0f;
        while (t < d)
        {
            cg.alpha = Mathf.Lerp(1f, 0f, t / d);
            t += Time.deltaTime;
            yield return null;
        }
        cg.alpha = 0;
    }
}

//─────────────────────────────────────────────────────────────────  
//  PositionSwapUtility – 로컬 씬 내 플레이어 위치 교환 (Photon X)  
//─────────────────────────────────────────────────────────────────  
public static class PositionSwapUtility
{
    public static void SwapAllPlayers()
    {
        var players = Object.FindObjectsByType<LHK_PlayerController>(FindObjectsSortMode.None).OrderBy(_ => Random.value).ToList();
        if (players.Count < 2)
        {
            Debug.Log("[Swap] 플레이어가 2명 이상 있어야 위치 교환이 가능합니다.");
            return;
        }

        var poses = players.Select(p => new { p.transform.position, p.transform.rotation }).ToList();
        for (int i = 0; i < players.Count; i++)
        {
            int next = (i + 1) % players.Count; // 원형 시프트  
            players[i].transform.SetPositionAndRotation(poses[next].position, poses[next].rotation);
        }
        Debug.Log("⚡ Local position swap complete!");
    }
}
