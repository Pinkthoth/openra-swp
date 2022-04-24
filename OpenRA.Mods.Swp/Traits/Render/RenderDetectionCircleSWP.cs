#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public enum DetectionCircleVisibilitySWP { Always, WhenSelected }

	public class RenderDetectionCircleSWPInfo : TraitInfo, Requires<DetectCloakedInfo>
	{
		[Desc("WAngle the Radar update line advances per tick.")]
		public readonly WAngle UpdateLineTick = new WAngle(-1);

		[Desc("Number of trailing Radar update lines.")]
		public readonly int TrailCount = 0;

		[Desc("Color of the circle and scanner update line.")]
		public readonly Color Color = Color.FromArgb(128, Color.LimeGreen);

		[Desc("Range circle line width.")]
		public readonly float Width = 1;

		[Desc("Border color of the circle and scanner update line.")]
		public readonly Color BorderColor = Color.FromArgb(96, Color.Black);

		[Desc("Range circle border width.")]
		public readonly float BorderWidth = 3;

		[Desc("When to show the detection circle. Valid values are `Always`, and `WhenSelected`")]
		public readonly DetectionCircleVisibilitySWP Visible = DetectionCircleVisibilitySWP.WhenSelected;

		public override object Create(ActorInitializer init) { return new RenderDetectionCircleSWP(init.Self, this); }
	}

	public class RenderDetectionCircleSWP : ITick, IRenderAnnotationsWhenSelected, IRenderAnnotations
	{
		readonly RenderDetectionCircleSWPInfo info;
		readonly DetectCloaked[] detectCloaked;
		WAngle lineAngle;

		public RenderDetectionCircleSWP(Actor self, RenderDetectionCircleSWPInfo info)
		{
			this.info = info;
			detectCloaked = self.TraitsImplementing<DetectCloaked>().ToArray();
		}

		IEnumerable<IRenderable> RenderCircle(Actor self, DetectionCircleVisibilitySWP visibility)
		{
			if (info.Visible != visibility || !self.Owner.IsAlliedWith(self.World.RenderPlayer))
				yield break;

			var range = detectCloaked
				.Select(a => a.Range)
				.Append(WDist.Zero).Max();

			if (range == WDist.Zero)
				yield break;

			yield return new DetectionCircleAnnotationRenderable(
				self.CenterPosition,
				range,
				0,
				info.TrailCount,
				info.UpdateLineTick,
				lineAngle,
				info.Color,
				info.Width,
				info.BorderColor,
				info.BorderWidth);
		}

		IEnumerable<IRenderable> IRenderAnnotationsWhenSelected.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			return RenderCircle(self, DetectionCircleVisibilitySWP.WhenSelected);
		}

		bool IRenderAnnotationsWhenSelected.SpatiallyPartitionable => false;

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			return RenderCircle(self, DetectionCircleVisibilitySWP.Always);
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;

		void ITick.Tick(Actor self)
		{
			lineAngle += info.UpdateLineTick;
		}
	}
}
