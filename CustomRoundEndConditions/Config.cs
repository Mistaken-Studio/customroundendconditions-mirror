﻿// -----------------------------------------------------------------------
// <copyright file="Config.cs" company="Mistaken">
// Copyright (c) Mistaken. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using Mistaken.Updater.Config;

namespace Mistaken.CustomRoundEndConditions
{
    /// <inheritdoc/>
    public class Config : IAutoUpdatableConfig
    {
        /// <inheritdoc/>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether debug should be displayed.
        /// </summary>
        [Description("If true then debug will be displayed")]
        public bool VerbouseOutput { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the round should end when SCPs and CI are alive.
        /// </summary>
        [Description("If true the round will end when SCPs and CI are alive.")]
        public bool ScpCiWin { get; set; } = true;

        /// <summary>
        /// Gets or sets the percentage of Class D to escape for CI to win.
        /// </summary>
        [Description("Class D Escape percentage for CI to win")]
        public float ClassDEscape { get; set; } = 40;

        /// <summary>
        /// Gets or sets the percentage of Scientists to escape for MTF to win.
        /// </summary>
        [Description("Scientists Escape percentage for MTF to win")]
        public float ScientistsEscape { get; set; } = 50;

        /// <summary>
        /// Gets or sets the percentage of Scientists to escape for MTF to win when only MTF is alive.
        /// </summary>
        [Description("Scientists Escape percentage for MTF to win when only MTF is alive.")]
        public float ScientistsEscapeOnlyMtfAlive { get; set; } = 30;

        /// <summary>
        /// Gets or sets a value indicating whether Cuffed Players should be counted as if they were in an opposite team.
        /// </summary>
        [Description("If true Cuffed Players will be counted as if they were in an opposite team.")]
        public bool ForceOppositeTeamForCuffedPlayers { get; set; } = false;

        /// <inheritdoc/>
        [Description("Auto Update Settings")]
        public System.Collections.Generic.Dictionary<string, string> AutoUpdateConfig { get; set; }
    }
}
