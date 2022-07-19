#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Swp.Traits
{
	[Desc("Produces an actor without using the standard production queue.")]
	public class PeriodicProducerWithPriceInfo : PausableConditionalTraitInfo
	{
		[ActorReference]
		[FieldLoader.Require]
		[Desc("Actors to produce.")]
		public readonly string[] Actors = null;

		[FieldLoader.Require]
		[Desc("Production queue type to use")]
		public readonly string Type = null;

		[Desc("Notification played when production is activated.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string ReadyAudio = null;

		[Desc("Notification played when the exit is jammed.",
			"The filename of the audio is defined per faction in notifications.yaml.")]
		public readonly string BlockedAudio = null;

		[Desc("Duration between productions.")]
		public readonly int ChargeDuration = 1000;

		[Desc("Price to be paid for production.")]
		public readonly int Price = 1000;

		public readonly bool ResetTraitOnEnable = false;

		public readonly bool ShowSelectionBar = false;
		public readonly Color ChargeColor = Color.DarkOrange;

		public override object Create(ActorInitializer init) { return new PeriodicProducerWithPrice(this); }
	}

	public class PeriodicProducerWithPrice : PausableConditionalTrait<PeriodicProducerWithPriceInfo>, ISelectionBar, ITick, ISync
	{
		readonly PeriodicProducerWithPriceInfo info;

		[Sync]
		int ticks;

		public PeriodicProducerWithPrice(PeriodicProducerWithPriceInfo info)
			: base(info)
		{
			this.info = info;
			ticks = info.ChargeDuration;
		}

		void ITick.Tick(Actor self)
		{
			var ownerResources = self.Owner.PlayerActor.Trait<PlayerResources>();
			var priceToPay = info.Price;
			if (IsTraitPaused)
				return;

			if (!IsTraitDisabled && --ticks < 0 && (ownerResources.Cash >= priceToPay))
			{
				var sp = self.TraitsImplementing<Production>()
				.FirstOrDefault(p => !p.IsTraitDisabled && !p.IsTraitPaused && p.Info.Produces.Contains(info.Type));

				var activated = false;

				if (sp != null)
				{
					foreach (var name in info.Actors)
					{
						var inits = new TypeDictionary
						{
							new OwnerInit(self.Owner),
							new FactionInit(sp.Faction)
						};

						activated |= sp.Produce(self, self.World.Map.Rules.Actors[name.ToLowerInvariant()], info.Type, inits, 0);
					}
					ownerResources.TakeCash(priceToPay);
					self.World.AddFrameEndTask(w => w.Add(new FloatingText(self.CenterPosition, self.Owner.Color, FloatingText.FormatCashTick(-priceToPay), 30)));
				}

				if (activated)
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.ReadyAudio, self.Owner.Faction.InternalName);
				else
					Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", info.BlockedAudio, self.Owner.Faction.InternalName);

				ticks = info.ChargeDuration;
			}
		}

		protected override void TraitEnabled(Actor self)
		{
			if (info.ResetTraitOnEnable)
				ticks = info.ChargeDuration;
		}

		float ISelectionBar.GetValue()
		{
			if (!info.ShowSelectionBar || IsTraitDisabled)
				return 0f;

			if (ticks < 0)
				return (float)(info.ChargeDuration -1) /info.ChargeDuration;
			else
				return (float)(info.ChargeDuration - ticks) / info.ChargeDuration;
		}

		Color ISelectionBar.GetColor()
		{
			return info.ChargeColor;
		}

		bool ISelectionBar.DisplayWhenEmpty
		{
			get { return info.ShowSelectionBar && !IsTraitDisabled; }
		}
	}
}
