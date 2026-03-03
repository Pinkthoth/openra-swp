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

using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Swp.Traits
{
	[Desc("Transform infiltrator into a different actor type.")]
	sealed class InfiltrateToTransformInfiltratorInfo : TraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		public readonly string IntoActor = null;

		public readonly int ForceHealthPercentage = 0;

		public readonly int UpgradePrice = 0;

		public readonly bool SkipMakeAnims = true;

		[Desc("Experience to grant to the infiltrating player.")]
		public readonly int PlayerExperience = 0;

		[Desc("The `TargetTypes` from `Targetable` that are allowed to enter.")]
		public readonly BitSet<TargetableType> Types = default;

		public override object Create(ActorInitializer init) { return new InfiltrateToTransformInfiltrator(init, this); }
	}

	sealed class InfiltrateToTransformInfiltrator : INotifyInfiltrated
	{
		readonly InfiltrateToTransformInfiltratorInfo info;
		readonly string faction;

		public InfiltrateToTransformInfiltrator(ActorInitializer init, InfiltrateToTransformInfiltratorInfo info)
		{
			this.info = info;
			faction = init.GetValue<FactionInit, string>(init.Self.Owner.Faction.InternalName);
		}

		void INotifyInfiltrated.Infiltrated(Actor self, Actor infiltrator, BitSet<TargetableType> types)
		{
			var ownerResources = infiltrator.Owner.PlayerActor.Trait<PlayerResources>();
			if (!info.Types.Overlaps(types))
				return;

			var transform = new Transform(info.IntoActor)
			{
				ForceHealthPercentage = info.ForceHealthPercentage,
				Faction = faction,
				SkipMakeAnims = info.SkipMakeAnims
			};

			var facing = self.TraitOrDefault<IFacing>();
			if (facing != null)
				transform.Facing = facing.Facing;

			infiltrator.Owner.PlayerActor.TraitOrDefault<PlayerExperience>()?.GiveExperience(info.PlayerExperience);
			
			if (ownerResources.Cash > info.UpgradePrice){
				infiltrator.QueueActivity(false, transform);
				ownerResources.TakeCash(info.UpgradePrice);
				if ( info.UpgradePrice > 0 ){
					self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.Owner.Color, FloatingText.FormatCashTick(-info.UpgradePrice), 30)));
				}
			}
		}
	}
}