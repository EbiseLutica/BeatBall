namespace Xeltica.BeatBall
{
	public abstract class EventBase
	{
		public int Measure { get; set; }
	}

	public class BeatEvent : EventBase
	{
		public Beat Beat { get; set; }
	}

	public class TempoEvent : EventBase
	{
		public float Tempo { get; set; }
	}
}