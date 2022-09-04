// -----------------------------------------------------------------------
// <copyright file="RoundSummaryStartPatch.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs;
using HarmonyLib;
using InventorySystem.Disarming;
using MEC;
using RoundRestarting;
using UnityEngine;

namespace Mistaken.CustomRoundEndConditions
{
    [HarmonyPatch(typeof(RoundSummary), nameof(RoundSummary.Start))]
    internal class RoundSummaryStartPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call)
                {
                    if (instruction.operand is MethodBase methodBase && methodBase.Name != nameof(RoundSummary._ProcessServerSideCode))
                    {
                        yield return instruction;
                    }
                    else
                    {
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RoundSummaryStartPatch), nameof(Process)));
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.FirstMethod(typeof(MECExtensionMethods2), (m) =>
                        {
                            Type[] generics = m.GetGenericArguments();
                            ParameterInfo[] paramseters = m.GetParameters();
                            return m.Name == "CancelWith"
                            && generics.Length == 1
                            && paramseters.Length == 2
                            && paramseters[0].ParameterType == typeof(IEnumerator<float>)
                            && paramseters[1].ParameterType == generics[0];
                        }).MakeGenericMethod(typeof(RoundSummary)));
                    }
                }
                else
                {
                    yield return instruction;
                }
            }
        }

        private static IEnumerator<float> Process(RoundSummary roundSummary)
        {
            float time = Time.unscaledTime;
            while (!(roundSummary is null))
            {
                yield return Timing.WaitForSeconds(2.5f);

                while (RoundSummary.RoundLock || !RoundSummary.RoundInProgress() || Time.unscaledTime - time < 15f || (roundSummary._keepRoundOnOne && PlayerManager.players.Count < 2))
                    yield return Timing.WaitForOneFrame;

                RoundSummary.SumInfo_ClassList newList = default;
                foreach (KeyValuePair<GameObject, ReferenceHub> kvp in ReferenceHub.GetAllHubs())
                {
                    if (kvp.Value is null)
                        continue;

                    CharacterClassManager component = kvp.Value.characterClassManager;
                    bool disarmed = PluginHandler.Instance.Config.ForceOppositeTeamForCuffedPlayers ? kvp.Value.inventory.IsDisarmed() : false;
                    if (component.Classes.CheckBounds(component.CurClass))
                    {
                        switch (component.CurRole.team)
                        {
                            case Team.SCP:
                                if (component.CurClass == RoleType.Scp0492)
                                    newList.zombies++;
                                else
                                    newList.scps_except_zombies++;
                                continue;
                            case Team.MTF:
                                if (!disarmed)
                                    newList.mtf_and_guards++;
                                else
                                    newList.chaos_insurgents++;
                                continue;
                            case Team.CHI:
                                if (!disarmed)
                                    newList.chaos_insurgents++;
                                else
                                    newList.mtf_and_guards++;
                                continue;
                            case Team.RSC:
                                if (!disarmed)
                                    newList.scientists++;
                                else
                                    newList.chaos_insurgents++;
                                continue;
                            case Team.CDP:
                                if (!disarmed)
                                    newList.class_ds++;
                                else
                                    newList.mtf_and_guards++;
                                continue;
                            default:
                                continue;
                        }
                    }
                }

                yield return Timing.WaitForOneFrame;
                newList.warhead_kills = AlphaWarheadController.Host.detonated ? AlphaWarheadController.Host.warheadKills : -1;
                yield return Timing.WaitForOneFrame;
                newList.time = (int)Time.realtimeSinceStartup;
                yield return Timing.WaitForOneFrame;
                RoundSummary.roundTime = newList.time - roundSummary.classlistStart.time;
                int facilityForces = newList.mtf_and_guards + newList.scientists;
                int chaosInsurgencyAndClassD = newList.chaos_insurgents + newList.class_ds;
                int scps = newList.scps_except_zombies + newList.zombies;
                int nonSCP = facilityForces + chaosInsurgencyAndClassD;
                int nonMTF = chaosInsurgencyAndClassD + scps;
                int escapedClassD = newList.class_ds + RoundSummary.EscapedClassD;
                int escapedScientists = newList.scientists + RoundSummary.EscapedScientists;
                float escapedClassDPercentage = (roundSummary.classlistStart.class_ds == 0) ? 0f : ((float)escapedClassD / roundSummary.classlistStart.class_ds * 100);
                float escapedScientistsPercentage = (roundSummary.classlistStart.scientists == 0) ? 0f : ((float)escapedScientists / roundSummary.classlistStart.scientists * 100);

                RoundSummary.SurvivingSCPs = newList.scps_except_zombies;

                int num9 = newList.class_ds + facilityForces;
                num9 += PluginHandler.Instance.Config.ScpCiWin ? 0 : newList.chaos_insurgents;
                if (num9 <= 0)
                {
                    roundSummary.RoundEnded = true;
                }
                else
                {
                    int num8 = 0;
                    if (facilityForces > 0)
                        num8++;
                    if (chaosInsurgencyAndClassD > 0)
                        num8++;
                    if (scps > 0)
                        num8++;
                    if (num8 <= 1)
                        roundSummary.RoundEnded = true;
                }

                EndingRoundEventArgs endingRoundEventArgs = new EndingRoundEventArgs(LeadingTeam.Draw, newList, roundSummary.RoundEnded);

                string message;
                bool classDWin = PluginHandler.Instance.Config.ClassDEscape <= escapedClassDPercentage;
                bool scientistWin = PluginHandler.Instance.Config.ScientistsEscape <= escapedScientistsPercentage || (facilityForces != 0 && nonMTF == 0 && PluginHandler.Instance.Config.ScientistsEscapeOnlyMtfAlive <= escapedScientistsPercentage);
                bool scpWin = scps != 0 && nonSCP == 0;
                if (classDWin && !scientistWin)
                {
                    message = $"Class D won. {escapedClassDPercentage}% Escaped. {PluginHandler.Instance.Config.ClassDEscape}% Required.";
                    endingRoundEventArgs.LeadingTeam = LeadingTeam.ChaosInsurgency;
                    goto Label;
                }

                if (scientistWin && !scpWin && !classDWin)
                {
                    message = $"MTF won. {escapedScientistsPercentage}% Scientists Escaped. {PluginHandler.Instance.Config.ScientistsEscape}% Required.\n{facilityForces} MTF Alive\n{nonMTF} Others Alive.";
                    endingRoundEventArgs.LeadingTeam = LeadingTeam.FacilityForces;
                    goto Label;
                }

                if (scpWin && !classDWin && !scientistWin)
                {
                    message = $"SCP won. {scps} SCPs Left. {nonSCP} Humans Left.";
                    endingRoundEventArgs.LeadingTeam = LeadingTeam.Anomalies;
                    goto Label;
                }

                message = "No one won.";
                endingRoundEventArgs.LeadingTeam = LeadingTeam.Draw;

                Label:

                Exiled.Events.Handlers.Server.OnEndingRound(endingRoundEventArgs);

                roundSummary.RoundEnded = endingRoundEventArgs.IsRoundEnded && endingRoundEventArgs.IsAllowed;

                if (roundSummary.RoundEnded)
                {
                    Log.Debug(message, PluginHandler.Instance.Config.VerbouseOutput);
                    FriendlyFireConfig.PauseDetector = true;
                    string str = "Round finished! Anomalies: " + scps + " | Chaos: " + chaosInsurgencyAndClassD + " | Facility Forces: " + facilityForces + " | D escaped percentage: " + escapedClassDPercentage + " | S escaped percentage: : " + escapedScientistsPercentage;
                    GameCore.Console.AddLog(str, Color.gray, false);
                    ServerLogs.AddLog(ServerLogs.Modules.Logger, str, ServerLogs.ServerLogType.GameEvent);
                    yield return Timing.WaitForSeconds(1.5f);
                    int timeToRoundRestart = Mathf.Clamp(GameCore.ConfigFile.ServerConfig.GetInt("auto_round_restart_time", 10), 5, 1000);

                    if (!(roundSummary is null))
                    {
                        RoundEndedEventArgs roundEndedEventArgs = new RoundEndedEventArgs(endingRoundEventArgs.LeadingTeam, newList, timeToRoundRestart);

                        Exiled.Events.Handlers.Server.OnRoundEnded(roundEndedEventArgs);

                        roundSummary.RpcShowRoundSummary(roundSummary.classlistStart, roundEndedEventArgs.ClassList, (RoundSummary.LeadingTeam)roundEndedEventArgs.LeadingTeam, RoundSummary.EscapedClassD, RoundSummary.EscapedScientists, RoundSummary.KilledBySCPs, roundEndedEventArgs.TimeToRestart);
                    }

                    yield return Timing.WaitForSeconds(timeToRoundRestart - 1);
                    roundSummary.RpcDimScreen();
                    yield return Timing.WaitForSeconds(1f);
                    RoundRestart.InitiateRoundRestart();
                    yield break;
                }
            }
        }
    }
}
