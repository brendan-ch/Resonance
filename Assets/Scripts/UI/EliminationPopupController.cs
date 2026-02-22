using UnityEngine;
using System.Collections;
using PurrNet;
using Resonance.Match;

public class EliminationPopupController : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.15f;
    [SerializeField] private float displayTime = 0.6f;

    private MatchStatNetworkAdapter matchStats;

    private void Awake()
    {
        popupRoot.SetActive(true); // keep active so CanvasGroup works
        canvasGroup.alpha = 0f;

        matchStats = MatchLogicNetworkAdapter.Instance?.MatchStats;
    }

    private void OnEnable()
    {
        if (matchStats != null)
            matchStats.OnPlayerKill += HandlePlayerKill;
    }

    private void OnDisable()
    {
        if (matchStats != null)
            matchStats.OnPlayerKill -= HandlePlayerKill;
    }

    private void HandlePlayerKill(PlayerID killer, PlayerID victim)
    {
        if (killer == NetworkManager.main.localPlayer)
        {
            ShowPopup();
        }
    }

    private void ShowPopup()
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        // Fade In
        yield return Fade(0f, 1f, fadeDuration);

        // Hold
        yield return new WaitForSeconds(displayTime);

        // Fade Out
        yield return Fade(1f, 0f, fadeDuration);
    }

    private IEnumerator Fade(float start, float end, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            canvasGroup.alpha = Mathf.Lerp(start, end, t);
            yield return null;
        }

        canvasGroup.alpha = end;
    }
}