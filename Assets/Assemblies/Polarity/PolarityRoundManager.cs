using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Resonance.Assemblies.MatchStat;

namespace Resonance.Assemblies.Polarity
{
    public class PolarityRoundManager
    {
        #region Custom data types
        public struct Team
        {
            public PolarityTeamRole currentRole;
            public HashSet<ulong> players;
        }

        public enum PolarityTeamRole
        {
            Taggers,
            Runners,
        }
        #endregion

        #region Configuration
        public struct PolarityRoundManagerConfig
        {
            public int timeBetweenRoleSwitchSeconds;
            public int teamEliminationsToWin;
            public float matchStartCountdownSeconds;

            public static PolarityRoundManagerConfig Default => new()
            {
                timeBetweenRoleSwitchSeconds = 90,
                teamEliminationsToWin = 10,
                matchStartCountdownSeconds = 5f,
            };
        }
        private int timeBetweenRoleSwitchSeconds = 90;
        private int teamEliminationsToWin = 10;
        private float matchStartCountdownSeconds = 5f;
        #endregion

        #region State
        private PolarityMatchState matchState = PolarityMatchState.Waiting;
        private DateTime? timeOfLastRoleSwitch = null;
        private Timer roleSwitchCheckTimer = null;
        private Team teamA;
        private Team teamB;
        private int teamAEliminations = 0;
        private int teamBEliminations = 0;
        #endregion

        #region Properties
        public PolarityMatchState MatchState => matchState;
        public bool IsMatchActive => matchState == PolarityMatchState.MatchActive;
        public bool IsMatchEnded => matchState == PolarityMatchState.MatchEnded;
        public int TeamEliminationsToWin => teamEliminationsToWin;
        public float MatchStartCountdownSeconds => matchStartCountdownSeconds;
        public Team TeamA => teamA;
        public Team TeamB => teamB;
        public int TeamAEliminations => teamAEliminations;
        public int TeamBEliminations => teamBEliminations;
        public DateTime? TimeOfLastRoleSwitch => timeOfLastRoleSwitch;
        public double SecondsUntilNextRoleSwitch
        {
            get
            {
                if (!IsMatchActive || timeOfLastRoleSwitch == null) return 0;
                var elapsed = (DateTime.Now - timeOfLastRoleSwitch.Value).TotalSeconds;
                var remaining = timeBetweenRoleSwitchSeconds - elapsed;
                return remaining > 0 ? remaining : 0;
            }
        }
        #endregion

        private MatchStatTracker matchStatTracker;


        #region Startup
        public PolarityRoundManager(MatchStatTracker tracker)
            : this(tracker, PolarityRoundManagerConfig.Default)
        {
        }

        public PolarityRoundManager(MatchStatTracker tracker, PolarityRoundManagerConfig config)
        {
            matchStatTracker = tracker;
            timeBetweenRoleSwitchSeconds = config.timeBetweenRoleSwitchSeconds;
            teamEliminationsToWin = config.teamEliminationsToWin;
            matchStartCountdownSeconds = config.matchStartCountdownSeconds;

            teamA = new Team
            {
                currentRole = PolarityTeamRole.Taggers,
                players = new HashSet<ulong>()
            };
            teamB = new Team
            {
                currentRole = PolarityTeamRole.Runners,
                players = new HashSet<ulong>()
            };

            SubscribeToEvents();
        }
        #endregion

        #region Event Subscriptions
        private void SubscribeToEvents()
        {
            if (matchStatTracker != null)
            {
                matchStatTracker.OnPlayerKill += OnPlayerKilled;
            }
        }

        #endregion

        #region Events
        public event Action<PolarityMatchState, PolarityMatchState> OnMatchStateChange;
        public event Action<float> OnMatchCountdownStart;
        public event Action OnMatchStart;
        public event Action<Team> OnMatchEnd;
        public event Action OnRoleSwitch;
        public event Action<double> OnRoleSwitchTimerElapsed;
        #endregion

        #region Player Registration
        public void RegisterPlayersForTeamA(HashSet<ulong> players)
        {
            teamA = new()
            {
                currentRole = teamA.currentRole,
                players = players,
            };
        }

        public void RegisterPlayersForTeamB(HashSet<ulong> players)
        {
            teamB = new()
            {
                currentRole = teamB.currentRole,
                players = players,
            };
        }
        #endregion

        #region Match Control
        public async Task StartMatchCountdown()
        {
            if (matchState == PolarityMatchState.MatchActive || matchState == PolarityMatchState.Countdown)
                return;

            var oldMatchState = matchState;
            matchState = PolarityMatchState.Countdown;

            OnMatchCountdownStart?.Invoke(matchStartCountdownSeconds);
            OnMatchStateChange?.Invoke(oldMatchState, matchState);

            await Task.Delay((int)(matchStartCountdownSeconds * 1000));
            StartMatchWithoutCountdown();
        }

        public void StartMatchWithoutCountdown()
        {
            if (IsMatchActive) return;

            var oldMatchState = matchState;
            matchState = PolarityMatchState.MatchActive;

            teamAEliminations = 0;
            teamBEliminations = 0;
            teamA.currentRole = PolarityTeamRole.Taggers;
            teamB.currentRole = PolarityTeamRole.Runners;
            timeOfLastRoleSwitch = DateTime.Now;

            matchStatTracker?.ResetAllStats();

            OnMatchStart?.Invoke();
            OnMatchStateChange?.Invoke(oldMatchState, matchState);

            SetRoleSwitchCheckTimer();
        }

        private void SetRoleSwitchCheckTimer()
        {
            roleSwitchCheckTimer?.Stop();
            roleSwitchCheckTimer?.Dispose();

            roleSwitchCheckTimer = new Timer(1000);
            roleSwitchCheckTimer.Elapsed += (_, _) =>
            {
                OnRoleSwitchTimerElapsed?.Invoke(SecondsUntilNextRoleSwitch);
                CheckForRoleSwitch();
            };
            roleSwitchCheckTimer.AutoReset = true;
            roleSwitchCheckTimer.Enabled = true;
        }

        public void SwitchRoles()
        {
            if (!IsMatchActive) return;

            (teamA.currentRole, teamB.currentRole) = (teamB.currentRole, teamA.currentRole);

            timeOfLastRoleSwitch = DateTime.Now;

            OnRoleSwitch?.Invoke();
        }

        public async Task EndMatch(Team winningTeam)
        {
            if (matchState != PolarityMatchState.MatchActive) return;

            matchState = PolarityMatchState.MatchEnded;
            OnMatchStateChange?.Invoke(PolarityMatchState.MatchActive, matchState);
            OnMatchEnd?.Invoke(winningTeam);

            roleSwitchCheckTimer?.Stop();
            roleSwitchCheckTimer?.Dispose();
        }

        private void CheckForRoleSwitch()
        {
            if (!IsMatchActive || timeOfLastRoleSwitch == null) return;

            var elapsed = (DateTime.Now - timeOfLastRoleSwitch.Value).TotalSeconds;
            if (elapsed >= timeBetweenRoleSwitchSeconds)
            {
                SwitchRoles();
            }
        }
        #endregion

        #region Kill Event Handling
        private void OnPlayerKilled(ulong attacker, ulong victim)
        {
            if (!IsMatchActive || IsMatchEnded) return;

            if (teamA.players.Contains(attacker))
            {
                teamAEliminations++;
                if (teamAEliminations >= teamEliminationsToWin)
                {
                    _ = EndMatch(teamA);
                }
            }
            else if (teamB.players.Contains(attacker))
            {
                teamBEliminations++;
                if (teamBEliminations >= teamEliminationsToWin)
                {
                    _ = EndMatch(teamB);
                }
            }
        }
        #endregion
    }
}
