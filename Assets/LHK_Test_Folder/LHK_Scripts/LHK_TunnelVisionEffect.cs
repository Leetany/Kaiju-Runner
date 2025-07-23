using System.Collections;
using UnityEngine;

public class LHK_TunnelVisionEffect : MonoBehaviour
{
    [SerializeField] CanvasGroup cg;           // 검은 마스크 투명도
    [SerializeField] Material circleMat;       // Radial Cutoff (0=FullBlack 1=NoMask)
    [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    float dur;
    public void Play(float d) { dur = d; StartCoroutine(Routine()); }
    IEnumerator Routine()
    {
        float t = 0;
        while (t < dur)
        {
            float p = t / dur;
            float cut = Mathf.Lerp(0.2f, 1f, ease.Evaluate(p)); // 중앙 구멍이 점점 커짐
            if (circleMat) circleMat.SetFloat("_Cutoff", cut);
            if (cg) cg.alpha = 1 - cut; // 구멍 클수록 알파 줄어듦
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}

