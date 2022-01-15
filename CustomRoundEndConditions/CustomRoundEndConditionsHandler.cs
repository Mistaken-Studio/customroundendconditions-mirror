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
        }

        public override void OnDisable()
        {
            Exiled.Events.Handlers.Server.EndingRound -= this.Server_EndingRound;
            Exiled.Events.Handlers.Server.RoundStarted -= this.Server_RoundStarted;
        }

        private int classD = 0;
        private int scientists = 0;

        private void Server_RoundStarted()
        {
            Timing.CallDelayed(3f, () =>
            {
                this.classD = RoundSummary.singleton.CountRole(RoleType.ClassD);
                this.scientists = RoundSummary.singleton.CountRole(RoleType.Scientist);
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
            if (!PluginHandler.Instance.Config.ScpCiWin && ciAlive != 0 && scpAlive != 0)
            {
                ev.IsAllowed = false;
                return;
            }
            else if (RoundSummary.EscapedClassD != 0 || this.classD != 0)
            {
                if (PluginHandler.Instance.Config.ClassDEscape <= (RoundSummary.EscapedClassD / this.classD * 100))
                {
                    this.Log.Debug($"Class D won. {RoundSummary.EscapedClassD / this.classD * 100}% Escaped. {PluginHandler.Instance.Config.ClassDEscape}% Required.", PluginHandler.Instance.Config.VerbouseOutput);
                    ev.LeadingTeam = LeadingTeam.ChaosInsurgency;
                }
            }
            else if (RoundSummary.EscapedScientists != 0 || this.scientists != 0)
            {
                if (PluginHandler.Instance.Config.ScientistsEscape <= (RoundSummary.EscapedScientists / this.scientists * 100) || (PluginHandler.Instance.Config.ScientistsEscapeOnlyMTFAlive >= (RoundSummary.EscapedScientists / this.scientists) && mtfAlive != 0 && nonMTFAlive == 0))
                {
                    this.Log.Debug($"MTF won. {RoundSummary.EscapedScientists / this.scientists * 100}% Scientists Escaped. {PluginHandler.Instance.Config.ScientistsEscape}% Required.\n{mtfAlive} MTF Alive\n{nonMTFAlive} Others Alive.", PluginHandler.Instance.Config.VerbouseOutput);
                }
                else if (PluginHandler.Instance.Config.ScientistsEscape <= (RoundSummary.EscapedScientists / this.scientists * 100) || (PluginHandler.Instance.Config.ScientistsEscapeOnlyMTFAlive >= (RoundSummary.EscapedScientists / this.scientists) && mtfAlive != 0 && nonMTFAlive == 0))
                {
                    this.Log.Debug($"MTF won. {RoundSummary.EscapedScientists / this.scientists * 100}% Scientists Escaped. {PluginHandler.Instance.Config.ScientistsEscape}% Required.\n{mtfAlive} MTF Alive\n{nonMTFAlive} Others Alive.", PluginHandler.Instance.Config.VerbouseOutput);
                }

                ev.LeadingTeam = LeadingTeam.FacilityForces;
            }
            else if (scpAlive != 0 && nonSCPAlive == 0)
            {
                this.Log.Debug($"SCP won. {scpAlive} SCPs Left. {nonSCPAlive} Humans Left.", PluginHandler.Instance.Config.VerbouseOutput);
                ev.LeadingTeam = LeadingTeam.FacilityForces;
            }
            else
            {
                this.Log.Debug($"No one won.", PluginHandler.Instance.Config.VerbouseOutput);
                ev.LeadingTeam = LeadingTeam.Draw;
            }

            ev.IsAllowed = true;
        }
    }
}
