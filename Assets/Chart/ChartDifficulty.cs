using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Xeltica.BeatBall
{
	public struct Difficulty
	{
		public Difficulty(Color accentColor, LocalizableString name) : this()
		{
			AccentColor = accentColor;
			Name = name;
		}

		public Color AccentColor { get; }
		public LocalizableString Name { get; }

	}
}