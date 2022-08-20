// -----------------------------------------------------------------------
// <copyright file="PluginHandler.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Reflection;
using Exiled.API.Enums;
using Exiled.API.Features;

namespace Mistaken.CustomRoundEndConditions
{
    /// <inheritdoc/>
    public class PluginHandler : Plugin<Config>
    {
        /// <inheritdoc/>
        public override string Author => "Mistaken Devs";

        /// <inheritdoc/>
        public override string Name => "CustomRoundEndConditions";

        /// <inheritdoc/>
        public override string Prefix => "MCustomRoundEndConditions";

        /// <inheritdoc/>
        public override PluginPriority Priority => PluginPriority.Medium;

        /// <inheritdoc/>
        public override Version RequiredExiledVersion => new Version(4, 2, 2);

        /// <inheritdoc/>
        public override void OnEnabled()
        {
            Instance = this;

            this.Harmony = new HarmonyLib.Harmony("com.mistaken.customroundendconditions");
            this.Harmony.PatchAll();

            Exiled.Events.Events.DisabledPatchesHashSet.Add(typeof(RoundSummary).GetMethod(nameof(RoundSummary.Start), BindingFlags.Instance | BindingFlags.NonPublic));
            Exiled.Events.Events.Instance.ReloadDisabledPatches();

            base.OnEnabled();
        }

        /// <inheritdoc/>
        public override void OnDisabled()
        {
            this.Harmony.UnpatchAll();

            base.OnDisabled();
        }

        internal static PluginHandler Instance { get; private set; }

        internal HarmonyLib.Harmony Harmony { get; private set; }
    }
}
