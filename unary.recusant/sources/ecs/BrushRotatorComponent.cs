using System;
using System.Collections.Generic;
using Godot;

namespace Unary.Core
{
	[Tool]
	[GlobalClass]
	public partial class BrushRotatorComponent : Component
	{
		[Export]
		public BrushEntity brush;

		public override void _Ready()
		{
			brush = Entity.GetComponent<BrushEntityComponent>().GetBrushEntity();
		}

		private double _timer = 0.0f;

		public override void _PhysicsProcess(double delta)
		{
			if (brush == null)
			{
				return;
			}

			brush.Rotate(Vector3.Up, (float)delta * 1.0f);

			var Position = brush.Position;

			_timer += delta;
			Position.Y = (float)(Mathf.Sin(_timer) + 1.0);
			brush.Position = Position;
		}
	}
}
