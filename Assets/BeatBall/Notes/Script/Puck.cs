namespace Xeltica.BeatBall
{
	/// <summary>
	/// パックノーツ．
	/// </summary>
	public class Puck : NoteBase
	{
		public override NoteType Type => NoteType.Kick;
		protected Puck(int measure, int tick) : base(measure, tick) { }
	}
}
