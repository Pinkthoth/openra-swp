#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	sealed class InfiltrateToSpawnActorInfo : TraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		public readonly string Actor = null;

		[Desc("The `TargetTypes` from `Targetable` that are allowed to enter.")]
		public readonly BitSet<TargetableType> Types = default;

		[Desc("Experience to grant to the infiltrating player.")]
		public readonly int PlayerExperience = 0;

		[NotificationReference("Speech")]
		[Desc("Sound the victim will hear when technology gets stolen.")]
		public readonly string InfiltratedNotification = null;

		[FluentReference(optional: true)]
		[Desc("Text notification the victim will see when technology gets stolen.")]
		public readonly string InfiltratedTextNotification = null;

		[NotificationReference("Speech")]
		[Desc("Sound the perpetrator will hear after successful infiltration.")]
		public readonly string InfiltrationNotification = null;

		[FluentReference(optional: true)]
		[Desc("Text notification the perpetrator will see after successful infiltration.")]
		public readonly string InfiltrationTextNotification = null;

		[Desc("If true, the spawned actor is destroyed if the parent actor dies, is sold, or is captured.")]
		public readonly bool LinkedToParent = false;

		public override object Create(ActorInitializer init) { return new InfiltrateToSpawnActor(this); }
	}

	sealed class InfiltrateToSpawnActor : INotifyInfiltrated, INotifyRemovedFromWorld
	{
		readonly InfiltrateToSpawnActorInfo info;
		List<Actor> spawnedActors;

		public InfiltrateToSpawnActor(InfiltrateToSpawnActorInfo info)
		{
			this.info = info;
			spawnedActors = new List<Actor>();
		}

		void INotifyInfiltrated.Infiltrated(Actor self, Actor infiltrator, BitSet<TargetableType> types)
		{
			if (!info.Types.Overlaps(types))
				return;

			if (info.InfiltratedNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.InfiltratedNotification, self.Owner.Faction.InternalName);

			if (info.InfiltrationNotification != null)
				Game.Sound.PlayNotification(self.World.Map.Rules, infiltrator.Owner, "Speech", info.InfiltrationNotification, infiltrator.Owner.Faction.InternalName);

			TextNotificationsManager.AddTransientLine(self.Owner, info.InfiltratedTextNotification);
			TextNotificationsManager.AddTransientLine(infiltrator.Owner, info.InfiltrationTextNotification);

			infiltrator.Owner.PlayerActor.TraitOrDefault<PlayerExperience>()?.GiveExperience(info.PlayerExperience);
			
			infiltrator.World.AddFrameEndTask(w => spawnedActors.Add(w.CreateActor(info.Actor, new TypeDictionary
				{
					new LocationInit(self.World.Map.CellContaining(self.CenterPosition)),
					new OwnerInit(infiltrator.Owner),
				})));
		}
		
		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			if (!info.LinkedToParent)
				return;

			foreach (var a in spawnedActors)
			{
				if (!a.IsDead)
					a.Dispose();
			}
		}
	}
}