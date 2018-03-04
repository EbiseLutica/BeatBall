namespace Xeltica.BeatBall
{
	/// <summary>
	/// バレーノーツ．
	/// </summary>
	public class Volley : LongNoteBase<Volley>
	{
		public Volley(int measure, int tick, int lane) : base(measure, tick, lane) { }
		public override NoteType Type => NoteType.Volley;
	}
}
