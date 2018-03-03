namespace Xeltica.BeatBall
{
	/// <summary>
	/// バレーノーツ．
	/// </summary>
	public class Volley : LongNoteBase
	{
		protected Volley(int tick) : base(tick) { }
		public override NoteType Type => NoteType.Volley;
	}
}
