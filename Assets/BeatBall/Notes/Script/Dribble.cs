﻿namespace Xeltica.BeatBall
{
	/// <summary>
	/// ドリブルノーツ．
	/// </summary>
	public class Dribble : LongNoteBase<Dribble>
	{
		public override NoteType Type => NoteType.Dribble;
		public Dribble(int measure, int tick) : base(measure, tick) { }
	}
}
