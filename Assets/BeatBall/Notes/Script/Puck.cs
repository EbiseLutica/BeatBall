namespace Xeltica.BeatBall
{
	/// <summary>
	/// パックノーツ．
	/// </summary>
	public class Puck : NoteBase
	{
		public override NoteType Type => NoteType.Puck;
		public Puck(int measure, int tick, int lane) : base(measure, tick, lane) { }
	}
}
