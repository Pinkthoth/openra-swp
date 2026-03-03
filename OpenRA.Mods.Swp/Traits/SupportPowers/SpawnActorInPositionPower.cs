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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Swp.Traits
{
	[Desc("Spawns an actor that stays for a limited amount of time in the position of the actor that has this trait.")]
	public class SpawnActorInPositionPowerInfo : SupportPowerInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actor to spawn.")]
		public readonly string Actor = null;

		[Desc("Amount of time to keep the actor alive in ticks. Value < 0 means this actor will not remove itself.")]
		public readonly int LifeTime = 250;

		[Desc("Only allow this to be spawned on this terrain.")]
		public readonly string[] Terrain = null;

		public readonly bool AllowUnderShroud = true;

		public readonly string DeploySound = null;

		public readonly string EffectImage = null;

		[SequenceReference(nameof(EffectImage))]
		public readonly string EffectSequence = null;

		[PaletteReference(nameof(EffectPaletteIsPlayerPalette))]
		public readonly string EffectPalette = null;

		public readonly bool EffectPaletteIsPlayerPalette = false;

		public override object Create(ActorInitializer init) { return new SpawnActorInPositionPower(init.Self, this); }
	}

	public class SpawnActorInPositionPower : SupportPower
	{
		public SpawnActorInPositionPower(Actor self, SpawnActorInPositionPowerInfo info)
			: base(self, info) { }

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			self.World.IssueOrder(new Order(order, manager.Self, false));
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			var info = Info as SpawnActorInPositionPowerInfo;
			var position = self.CenterPosition;
			var cell = self.Location;

			base.Activate(self, order, manager);

			self.World.AddFrameEndTask(w =>
			{
				PlayLaunchSounds();
				Game.Sound.Play(SoundType.World, info.DeploySound, position);

				if (!string.IsNullOrEmpty(info.EffectSequence) && !string.IsNullOrEmpty(info.EffectPalette))
				{
					var palette = info.EffectPalette;
					if (info.EffectPaletteIsPlayerPalette)
						palette += self.Owner.InternalName;

					w.Add(new SpriteEffect(position, w, info.EffectImage, info.EffectSequence, palette));
				}

				var actor = w.CreateActor(info.Actor, new TypeDictionary
				{
					new LocationInit(cell),
					new OwnerInit(self.Owner),
				});

				if (info.LifeTime > -1)
				{
					actor.QueueActivity(new Wait(info.LifeTime));
					actor.QueueActivity(new RemoveSelf());
				}
			});
		}
	}
}
