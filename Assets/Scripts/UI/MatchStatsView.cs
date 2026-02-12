using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Resonance.Match;
using Resonance.Assemblies.Arena;

public class MatchStatsView : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI leaderboardText;

    private void Awake()
    {
        var vm = MatchStatsViewModel.Instance;

        if (vm == null)
        {
            Debug.LogError("MatchStatsViewModel not found in scene");
            return;
        }
        
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

        for (int i = 0; i < rankings.Count; i++)
        {
            var ranking = rankings[i];
            leaderboardText.text +=
                $"#{i + 1} {ranking.player}" +
                $"K:{ranking.stats.kills} D:{ranking.stats.deaths}\n";
        }
    }
}
