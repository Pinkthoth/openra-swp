﻿#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Swp.Traits
{
	[Desc("This must be attached to player in order for TeleportNetwork to work.")]
	public class TeleportNetworkManagerInfo : TraitInfo, IRulesetLoaded
	{
		[FieldLoader.Require]
		[Desc("Type of TeleportNetwork that pairs up, in order for it to work.")]
		public string Type;

		[Desc("If true, on entering the network a random exit is used.")]
		public bool RandomExit = false;

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var teleporters = rules.Actors.Values.Where(a => a.HasTraitInfo<TeleportNetworkInfo>());
			if (!teleporters.Any())
				throw new YamlException("TeleportNetworkManager without TeleportNetwork actors.");
			if (!teleporters.Any(a => a.TraitInfo<TeleportNetworkInfo>().Type == Type))
				throw new YamlException($"Can't find a TeleportNetwork with Type '{Type}'");
		}

		public override object Create(ActorInitializer init) { return new TeleportNetworkManager(this); }
	}

	public class TeleportNetworkManager
	{
		public readonly string Type;
		public int Count = 0;
		public Actor PrimaryActor = null;
		public readonly bool RandomExit;

		public TeleportNetworkManager(TeleportNetworkManagerInfo info)
		{
			Type = info.Type;
			RandomExit = info.RandomExit;
		}
	}
}