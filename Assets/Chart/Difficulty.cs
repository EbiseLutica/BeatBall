using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Xeltica.BeatBall
{
	public class Difficulty
	{
		Difficulty(Color accentColor, LocalizableString name) : this()
		{
			AccentColor = accentColor;
			Name = name;
		}

		Difficulty()
		{
			
		}

		public Color AccentColor { get; }
		public LocalizableString Name { get; }

		public static readonly Difficulty Beginner = new Difficulty(new Color(128 / 255f, 1, 98 / 255f), "difficulty.beginner");
		public static readonly Difficulty  Amateur = new Difficulty(new Color(1, 128 / 255f, 98 / 255f), "difficulty.amateur");
		public static readonly Difficulty      Pro = new Difficulty(new Color(1, 64 / 255f, 128 / 255f), "difficulty.pro");
		public static readonly Difficulty   Legend = new Difficulty(new Color(128 / 255f, 64 / 255f, 1), "difficulty.legend");
	}
}