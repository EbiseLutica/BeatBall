namespace Xeltica.BeatBall
{
	/// <summary>
	/// 全てのロングノーツのベースクラスです．
	/// </summary>
	public abstract class LongNoteBase : NoteBase
	{
		public Dribble Next { get; set; }
		public Dribble Previous { get; set; }

		public bool IsFirstNote => Previous == null;
		public bool IsLastNote => Next == null;

		protected LongNoteBase(int measure, int tick) : base(measure, tick) { }
	}
}
