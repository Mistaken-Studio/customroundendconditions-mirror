// -----------------------------------------------------------------------
// <copyright file="CustomRoundEndConditionsHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using MEC;
using Mistaken.API.Diagnostics;

namespace Mistaken.CustomRoundEndConditions
{
    internal class CustomRoundEndConditionsHandler : Module
    {
        public CustomRoundEndConditionsHandler(PluginHandler plugin)
            : base(plugin)
        {
        }

        public override string Name => "CustomRoundEndConditionsHandler";

        public override void OnEnable()
        {
            Exiled.Events.Handlers.Server.EndingRound += this.Server_EndingRound;
            Exiled.Events.Handlers.Server.RoundStarted += this.Server_RoundStarted;
            Exiled.Events.Handlers.Player.Escaping += this.Player_Escaping;
        }

        public override void OnDisable()
        {
            Exiled.Events.Handlers.Server.EndingRound -= this.Server_EndingRound;
            Exiled.Events.Handlers.Server.RoundStarted -= this.Server_RoundStarted;
            Exiled.Events.Handlers.Player.Escaping -= this.Player_Escaping;
        }

        private int classD = 0;
        private int scientists = 0;
        private int escapedclassD = 0;
        private int escapedscientists = 0;

        private void Server_RoundStarted()
        {
            Timing.CallDelayed(3f, () =>
            {
                this.classD = RoundSummary.singleton.CountRole(RoleType.ClassD);
                this.scientists = RoundSummary.singleton.CountRole(RoleType.Scientist);
                this.escapedclassD = 0;
                this.escapedscientists = 0;
            });
        }

        private void Server_EndingRound(EndingRoundEventArgs ev)
        {
            if (!ev.IsRoundEnded)
            {
                return;
            }

            int ciAlive = Player.List.Where(x => x.IsCHI).Count();
            int scpAlive = Player.List.Where(x => x.IsScp).Count();
            int mtfAlive = Player.List.Where(x => x.IsNTF).Count();
            int nonMTFAlive = Player.List.Where(x => !x.IsNTF).Count();
            int nonSCPAlive = Player.List.Where(x => !x.IsScp).Count();

            var escapedClassD = this.escapedclassD + Player.List.Where(x => x.Role == RoleType.ClassD).Count();
            var escapedScientists = this.escapedscientists + Player.List.Where(x => x.Role == RoleType.Scientist).Count();

            if (!PluginHandler.Instance.Config.ScpCiWin && ciAlive != 0 && scpAlive != 0)
            {
                ev.IsAllowed = false;
                return;
            }

            if (escapedClassD != 0 && this.classD != 0)
            {
                if (PluginHandler.Instance.Config.ClassDEscape <= (escapedClassD / this.classD * 100))
                {
                    this.Log.Debug($"Class D won. {escapedClassD / this.classD * 100}% Escaped. {PluginHandler.Instance.Config.ClassDEscape}% Required.", PluginHandler.Instance.Config.VerbouseOutput);
                    ev.LeadingTeam = LeadingTeam.ChaosInsurgency;
                    return;
                }
            }

            if (escapedScientists != 0 && this.scientists != 0)
            {
                if (PluginHandler.Instance.Config.ScientistsEscape <= (escapedScientists / this.scientists * 100) || (PluginHandler.Instance.Config.ScientistsEscapeOnlyMtfAlive >= (escapedScientists / this.scientists * 100) && mtfAlive != 0 && nonMTFAlive == 0))
                {
                    this.Log.Debug($"MTF won. {escapedScientists / this.scientists * 100}% Scientists Escaped. {PluginHandler.Instance.Config.ScientistsEscape}% Required.\n{mtfAlive} MTF Alive\n{nonMTFAlive} Others Alive.", PluginHandler.Instance.Config.VerbouseOutput);
                    ev.LeadingTeam = LeadingTeam.FacilityForces;
                    return;
                }
            }

            if (scpAlive != 0 && nonSCPAlive == 0)
            {
                this.Log.Debug($"SCP won. {scpAlive} SCPs Left. {nonSCPAlive} Humans Left.", PluginHandler.Instance.Config.VerbouseOutput);
                ev.LeadingTeam = LeadingTeam.FacilityForces;
                return;
            }

            {
                this.Log.Debug($"No one won.", PluginHandler.Instance.Config.VerbouseOutput);
                ev.LeadingTeam = LeadingTeam.Draw;
            }

            ev.IsAllowed = true;
        }

        private void Player_Escaping(EscapingEventArgs ev)
        {
            if (ev.Player.Role == RoleType.ClassD)
                this.escapedclassD++;
            else if (ev.Player.Role == RoleType.Scientist)
                this.escapedscientists++;
        }
    }
}
