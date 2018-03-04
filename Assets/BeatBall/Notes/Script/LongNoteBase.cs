namespace Xeltica.BeatBall
{
	/// <summary>
	/// 全てのロングノーツのベースクラスです．
	/// </summary>
	public abstract class LongNoteBase<T> : NoteBase where T : LongNoteBase<T>
	{
		public T Next { get; set; }
		public T Previous { get; set; }

		public bool IsFirstNote => Previous == null;
		public bool IsLastNote => Next == null;

		protected LongNoteBase(int measure, int tick, int lane) : base(measure, tick, lane) { }
	}
}
