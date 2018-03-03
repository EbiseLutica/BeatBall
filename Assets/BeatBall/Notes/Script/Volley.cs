namespace Xeltica.BeatBall
{
	/// <summary>
	/// バレーノーツ．
	/// </summary>
	public class Volley : LongNoteBase<Volley>
	{
		public Volley(int measure, int tick) : base(measure, tick) { }
		public override NoteType Type => NoteType.Volley;
	}
}
