using System.Collections.Generic;
using UnityEngine;
using Resonance.Match;

public class MatchStatsViewModel : MonoBehaviour
{
    public static MatchStatsViewModel Instance { get; private set; }

    public ObservableValue<bool> IsVisible = new(false);
    public ObservableValue<List<PlayerRanking>> Rankings =
        new(new List<PlayerRanking>());

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async void Show()
    {
        IsVisible.Value = true;

        if (ArenaRoundManager.Instance == null)
            return;

        Rankings.Value = await ArenaRoundManager.Instance.GetLeaderboard();
    }

    public void Hide()
    {
        IsVisible.Value = false;
    }

    public void Toggle()
    {
        IsVisible.Value = !IsVisible.Value;
    }
}
