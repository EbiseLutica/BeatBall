namespace Xeltica.BeatBall
{
	/// <summary>
	/// 振動ノーツです．
	/// </summary>
	public class VibrateNote : NoteBase
	{
		public VibrateNote(int measure, int tick, Orientation orientation, int power) : base(measure, tick)
		{
			Orientation = orientation;
			Power = power;
		}

		public override NoteType Type => NoteType.Vibrate;

		public Orientation Orientation { get; set; }
		public int Power { get; set; }
	}

}
