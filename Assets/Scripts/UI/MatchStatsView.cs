using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Resonance.Match;

public class MatchStatsView : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI leaderboardText;

    private void Awake()
    {
        var vm = MatchStatsViewModel.Instance;

        vm.IsVisible.ChangeEvent += OnVisibilityChanged;
        vm.Rankings.ChangeEvent += OnRankingsChanged;
    }

    private void OnVisibilityChanged(bool visible)
    {
        root.SetActive(visible);
    }

    private void OnRankingsChanged(List<PlayerRanking> rankings)
    {
        leaderboardText.text = "";

        foreach (var ranking in rankings)
        {
            leaderboardText.text +=
                $"#{ranking.rank} {ranking.player}  " +
                $"K:{ranking.stats.kills} D:{ranking.stats.deaths}\n";
        }
    }
}