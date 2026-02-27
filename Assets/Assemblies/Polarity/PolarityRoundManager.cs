using System;
using System.Collections.Generic;
using System.Threading;
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
        #endregion

        private MatchStatTracker matchStatTracker;

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
        }
    }
}
