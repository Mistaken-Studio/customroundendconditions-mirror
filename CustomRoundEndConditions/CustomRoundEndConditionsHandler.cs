// -----------------------------------------------------------------------
// <copyright file="CustomRoundEndConditionsHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
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
        }

        public override void OnDisable()
        {
            Exiled.Events.Handlers.Server.EndingRound -= this.Server_EndingRound;
        }

        private unsafe void Server_EndingRound(EndingRoundEventArgs ev)
        {
            if (!ev.IsRoundEnded)
            {
                return;
            }

            int ciAlive = Player.List.Where(x => x.IsCHI).Count();
            int scpAlive = Player.List.Where(x => x.IsScp).Count();
            int mtfAlive = Player.List.Where(x => x.IsNTF).Count();
            int notMTFAlive = Player.List.Where(x => !x.IsNTF).Count();
            int notSCPAlive = Player.List.Where(x => !x.IsScp).Count();
            unsafe
            {
                if (!PluginHandler.Instance.Config.ScpCiWin && ciAlive != 0 && scpAlive != 0)
                {
                    ev.IsAllowed = false;
                }
                else if (PluginHandler.Instance.Config.ClassDEscape <= (RoundSummary.EscapedClassD / RoundSummary.singleton.CountRole(RoleType.ClassD) * 100))
                {
                    this.Log.Debug($"Class D won. {RoundSummary.EscapedClassD / RoundSummary.singleton.CountRole(RoleType.ClassD) * 100}% Escaped. {PluginHandler.Instance.Config.ClassDEscape}% Required.", PluginHandler.Instance.Config.VerbouseOutput);
                    ev.LeadingTeam = LeadingTeam.ChaosInsurgency;
                }
                else if (PluginHandler.Instance.Config.ScientistsEscape <= (RoundSummary.EscapedScientists / RoundSummary.singleton.CountRole(RoleType.Scientist) * 100) || (PluginHandler.Instance.Config.ScientistsEscapeOnlyMTFAlive >= (RoundSummary.EscapedScientists / RoundSummary.singleton.CountRole(RoleType.Scientist)) && mtfAlive != 0 && notMTFAlive == 0))
                {
                    this.Log.Debug($"MTF won. {RoundSummary.EscapedScientists / RoundSummary.singleton.CountRole(RoleType.Scientist) * 100}% Scientists Escaped. {PluginHandler.Instance.Config.ScientistsEscape}% Required.\n{mtfAlive} MTF Alive\n{notMTFAlive} Others Alive.", PluginHandler.Instance.Config.VerbouseOutput);
                    ev.LeadingTeam = LeadingTeam.FacilityForces;
                }
                else if (PluginHandler.Instance.Config.ScientistsEscape <= (RoundSummary.EscapedScientists / RoundSummary.singleton.CountRole(RoleType.Scientist) * 100) || (PluginHandler.Instance.Config.ScientistsEscapeOnlyMTFAlive >= (RoundSummary.EscapedScientists / RoundSummary.singleton.CountRole(RoleType.Scientist)) && mtfAlive != 0 && notMTFAlive == 0))
                {
                    this.Log.Debug($"MTF won. {RoundSummary.EscapedScientists / RoundSummary.singleton.CountRole(RoleType.Scientist) * 100}% Scientists Escaped. {PluginHandler.Instance.Config.ScientistsEscape}% Required.\n{mtfAlive} MTF Alive\n{notMTFAlive} Others Alive.", PluginHandler.Instance.Config.VerbouseOutput);
                    ev.LeadingTeam = LeadingTeam.FacilityForces;
                }
                else if (scpAlive != 0 && notSCPAlive == 0)
                {
                    this.Log.Debug($"SCP won. {scpAlive} SCPs Left. {notSCPAlive} Humans Left.", PluginHandler.Instance.Config.VerbouseOutput);
                    ev.LeadingTeam = LeadingTeam.FacilityForces;
                }
                else
                {
                    this.Log.Debug($"No one won.\n{RoundSummary.EscapedClassD / RoundSummary.singleton.CountRole(RoleType.ClassD) * 100}% Class D Escaped. {PluginHandler.Instance.Config.ClassDEscape}% Required.\n{RoundSummary.EscapedScientists / RoundSummary.singleton.CountRole(RoleType.Scientist) * 100}% Scientists Escaped. {PluginHandler.Instance.Config.ScientistsEscape}% Required.\n{mtfAlive} MTF Alive\n{notMTFAlive} Others Alive.", PluginHandler.Instance.Config.VerbouseOutput);
                    ev.LeadingTeam = LeadingTeam.Draw;
                }
            }

            ev.IsRoundEnded = true;
            ev.IsAllowed = true;
        }
    }
}
