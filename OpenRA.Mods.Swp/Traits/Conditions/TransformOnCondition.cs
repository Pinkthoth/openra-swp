﻿﻿#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Swp.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Swp.Traits
{
	public class TransformOnConditionInfo : ConditionalTraitInfo
	{
		[ActorReference]
		public readonly string IntoActor = null;
		public readonly int ForceHealthPercentage = 0;
		public readonly bool SkipMakeAnims = true;

		public override object Create(ActorInitializer init) { return new TransformOnCondition(init, this); }
	}

	public class TransformOnCondition : ConditionalTrait<TransformOnConditionInfo>
	{
		readonly TransformOnConditionInfo info;
		readonly string faction;

		public TransformOnCondition(ActorInitializer init, TransformOnConditionInfo info)
			: base(info)
		{
			this.info = info;
			faction = init.GetValue<FactionInit, string>(info, init.Self.Owner.Faction.InternalName);
		}

		protected override void TraitEnabled(Actor self)
		{
			var transform = new InstantTransform(self, info.IntoActor) { ForceHealthPercentage = info.ForceHealthPercentage, Faction = faction };
			transform.SkipMakeAnims = info.SkipMakeAnims;
			self.CancelActivity();
			self.QueueActivity(transform);
		}
	}
}