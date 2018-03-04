namespace Xeltica.BeatBall
{
	public abstract class EventBase
	{
		public int Measure { get; set; }

		protected EventBase(int measure) { Measure = measure; }
	}

	public class BeatEvent : EventBase
	{
		public BeatEvent(int measure, Beat beat) : base(measure)
		{
			Beat = beat;
		}

		public Beat Beat { get; set; }
	}

	public class TempoEvent : EventBase
	{
		public TempoEvent(int measure, float tempo) : base(measure)
		{
			Tempo = tempo;
		}

		public float Tempo { get; set; }
	}
}