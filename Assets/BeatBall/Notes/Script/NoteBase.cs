namespace Xeltica.BeatBall
{
	/// <summary>
	/// Beat Ball のノーツオブジェクトのベースクラスです．
	/// </summary>
	public abstract class NoteBase
	{
		public int Measure { get; set; }
		public int Tick { get; set; }

		public abstract NoteType Type { get; }
		public int Lane { get; set; }

		protected NoteBase(int measure, int tick)
		{
			Tick = tick;
		}
	}
}
