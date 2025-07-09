using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LHK_UIWindFX : MonoBehaviour
{
    public RawImage windImage;

    public void PlayWindEffect()
    {
        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        float duration = 0.2f;
        float elapsed = 0f;

        Color color = windImage.color;

        // Fade In
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 0.8f, elapsed / duration);
            windImage.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        // Fade Out
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0.8f, 0f, elapsed / duration);
            windImage.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }
    }
}
