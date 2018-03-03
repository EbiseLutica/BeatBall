namespace Xeltica.BeatBall
{
	/// <summary>
	/// 回転ノーツです．
	/// </summary>
	public class RotateNote : NoteBase
	{
		public RotateNote(int measure, int tick, Direction dir, int speed) : base(measure, tick)
		{
			Direction = dir;
			SpeedId = speed;
		}

		public override NoteType Type => NoteType.Rotate;

		public Direction Direction { get; set; }
		public int SpeedId { get; set; }
	}

}
