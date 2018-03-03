namespace Xeltica.BeatBall
{
	/// <summary>
	/// ドリブルノーツ．
	/// </summary>
	public class Dribble : LongNoteBase
	{
		public override NoteType Type => NoteType.Dribble;
		protected Dribble(int measure, int tick) : base(measure, tick) { }
	}
}
