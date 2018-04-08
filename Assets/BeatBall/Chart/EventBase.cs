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
		public TempoEvent(int measure, float tempo, int tick) : base(measure)
		{
			Tempo = tempo;
			Tick = tick;
		}

		public float Tempo { get; set; }
		public int Tick { get; set; }
	}

	public class SpeedEvent : EventBase
	{
		public SpeedEvent(int measure, float speed, int tick) : base(measure)
		{
			Speed = speed;
			Tick = tick;
		}
		public float Speed { get; set; }
		public int Tick { get; set; }
	}

}