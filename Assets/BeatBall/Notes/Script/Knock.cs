namespace Xeltica.BeatBall
{
	/// <summary>
	/// ノックノーツ．
	/// </summary>
	public class Knock : NoteBase
	{
		public override NoteType Type => NoteType.Puck;
		public Knock(int measure, int tick) : base(measure, tick) { }
	}
}
