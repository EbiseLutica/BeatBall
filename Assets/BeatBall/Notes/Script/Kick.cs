namespace Xeltica.BeatBall
{
	/// <summary>
	/// キックノーツ．
	/// </summary>
	public class Kick : NoteBase
	{
		public override NoteType Type => NoteType.Kick;
		public Kick(int measure, int tick) : base(measure, tick) { }
	}
}
