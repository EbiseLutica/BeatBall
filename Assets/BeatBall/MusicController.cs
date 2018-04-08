using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using System;
using UnityEngine.SceneManagement;

namespace Xeltica.BeatBall
{
	[RequireComponent(typeof(AudioSource))]
	[RequireComponent(typeof(Music))]
	public class MusicController : Singleton<MusicController>
	{
		[SerializeField]
		string chartPath;

		Chart currentChart;
		AudioSource aud;
		Music mus;
		string rootPathOfChart;

		[SerializeField]
		GameObject kick;

		[SerializeField]
		GameObject dribble;

		[SerializeField]
		GameObject volley;

		[SerializeField]
		GameObject knock;

		[SerializeField]
		GameObject puck;


		[SerializeField]
		Transform laneA;

		[SerializeField]
		Transform laneB;

		[SerializeField]
		Transform laneC;

		[SerializeField]
		Transform laneD;

		[SerializeField]
		Material dribbleMaterial;


		[SerializeField]
		float hiSpeed = 10;

		[SerializeField]
		PlayBallRenderer playBallRenderer;

		Dictionary<NoteBase, Transform> notesDic;
		Dictionary<NoteBase, int> notesTicks;
		Dictionary<NoteBase, float> notesTimes;
		Dictionary<NoteBase, float> speededNotesTimes;

		Dictionary<SpeedEvent, float> speedsTimes;
		Dictionary<TempoEvent, int> temposTicks;
		List<TempoEvent> tempos;
		IEnumerable<BeatEvent> beats;
		IEnumerable<SpeedEvent> speeds;

		bool prevPlaying;

		const ulong mul = 100000000000;

		public double CurrentTime => currentTimeInteger / (double)mul;

		ulong currentTimeInteger;


		// Use this for initialization
		IEnumerator Start()
		{
			aud = GetComponent<AudioSource>();
			mus = GetComponent<Music>();
			notesDic = new Dictionary<NoteBase, Transform>();
			notesTicks = new Dictionary<NoteBase, int>();
			notesTimes = new Dictionary<NoteBase, float>();
			speededNotesTimes = new Dictionary<NoteBase, float>();
			tempos = new List<TempoEvent>();

			hiSpeed = StaticData.Hispeed;

			if (!string.IsNullOrEmpty(StaticData.ChartPath))
				chartPath = StaticData.ChartPath;

			if (kick == null)
			{
				Debug.LogError("Prefabを設定してください");
				yield break;
			}
			if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
			{
				rootPathOfChart = Path.Combine(Application.persistentDataPath, "charts");
			}
			else
			{
				rootPathOfChart = Path.Combine(Environment.CurrentDirectory, "charts");
			}
			var chart = Path.Combine(rootPathOfChart, chartPath);

			try
			{
				currentChart = Chart.Parse(File.ReadAllText(chart));
			}
			catch (ChartIncompatibleException ex)
			{
				Debug.LogError($"譜面の互換性がありません．{ex.LineNumber}行目");
				yield break;
			}
			catch (ChartErrorException ex)
			{
				Debug.LogError($"パースエラー {ex.Message} {ex.LineNumber}行目");
				yield break;
			}

			WWW www;
			yield return www = WWWWrapper.OpenLocalFile(Path.Combine(Path.GetDirectoryName(chart), currentChart.SongFile));
			currentChart.Song = www.GetAudioClip();

			var board = ScoreBoardController.Instance;
			board.Difficulty = currentChart.Difficulty;
			board.Name = currentChart.Title;
			board.Level = currentChart.Level;
			board.Artist = currentChart.Artist;

			// hack ここ雑すぎるのでちゃんとする
			board.NoteCount = currentChart.Notes.Count;

			Beat beat = currentChart.Beat;
			float tempo = currentChart.Bpm;
			mus.Sections.Clear();
			mus.Sections.Add(new Music.Section(0, 16 / beat.Note, 16 / beat.Note * beat.Rhythm, tempo));

			mus.Sections.AddRange(currentChart.Events.Select(e =>
			{
				if (e is TempoEvent)
				{
					tempo = (e as TempoEvent).Tempo;
					tempos.Add(e as TempoEvent);
				}
				else if (e is BeatEvent)
				{
					beat = (e as BeatEvent).Beat;
				}
				return new Music.Section(e.Measure, 16 / beat.Note, 16 / beat.Note * beat.Rhythm, tempo);
			}));

			// オフセットを補正し，ゲーム用に1小節余白を開ける
			var song = currentChart.Song;
			var buf = new float[song.samples * song.channels - TimeToSample(currentChart.Offset, song.frequency, song.channels)];

			song.GetData(buf, TimeToSample(currentChart.Offset, song.frequency, song.channels) / song.channels);

			var newSongLength = TimeToSample(GetTimeOfMeasure(currentChart.Beat, currentChart.Bpm), song.frequency, song.channels) / song.channels;

			song = AudioClip.Create(song.name, buf.Length / 2 + newSongLength, song.channels, song.frequency, false);
			song.SetData(buf, newSongLength);

			aud.clip = currentChart.Song = song;

			tempos = tempos.OrderBy(t => t.Measure).ToList();
			beats = currentChart.Events.OfType<BeatEvent>().OrderBy(b => b.Measure);
			speeds = currentChart.Events.OfType<SpeedEvent>().OrderBy(b => b.Measure * 1000 + b.Tick);
			temposTicks = tempos.Select(t => new KeyValuePair<TempoEvent, int>(t, GetTickOfMeasure(t.Measure))).ToDictionary(k => k.Key, v => v.Value);

			foreach (var note in currentChart.Notes)
			{
				notesTicks[note] = GetTickOfMeasure(note.Measure) + note.Tick;
				notesTimes[note] = TickToTime(notesTicks[note]);
				speededNotesTimes[note] = SpeededTickToTime(notesTicks[note]);
			}

			speedsTimes = speeds.Select(s => new KeyValuePair<SpeedEvent, float>(s, TickToTime(GetTickOfMeasure(s.Measure + 1) + s.Tick))).ToDictionary(k => k.Key, k => k.Value);

			// ノーツ生成
			StartCoroutine(InstantiateNoteObjects());

			yield return new WaitForSeconds(1);
			board.Ready = true;
			if (playBallRenderer != null)
			{
				playBallRenderer.Visible = true;
				yield return new WaitForSeconds(1);
				playBallRenderer.Message = PlayBallRenderer.StartMessage.PlayBall;
				yield return new WaitForSeconds(2);
				playBallRenderer.Visible = false;
				yield return new WaitForSeconds(0.25f);
			}
			// 少々待つ
			yield return new WaitForSeconds(0.25f);

			mus.PlayStart();
		}

		public static int TimeToSample(float time, int samplingRate = 44100, int ch = 2) => (int)(time * samplingRate + 0.5) * ch;
		public static float SampleToTime(int sample, int samplingRate = 44100, int ch = 2) => (float)sample / samplingRate / ch;

		public int GetTickOfMeasure(int measure)
		{
			var temp = 0;
			var beat = currentChart.Beat;
			var beatsList = beats.ToList();

			// 順番にソート
			beatsList.Sort((x, y) => x.Measure < y.Measure ? -1 : 1);
			for (int i = 1; i <= measure; i++)
			{
				if (beatsList.Count > 0 && beatsList[0].Measure <= i)
				{
					beat = beatsList[0].Beat;
					beatsList.RemoveAt(0);
				}
				temp += 16 / beat.Note * beat.Rhythm * 12;
			}
			return temp;
		}

		public Transform GetLane(int l) =>
			l == 0 ? laneA :
			l == 1 ? laneB :
			l == 2 ? laneC :
			l == 3 ? laneD : null;

		public GameObject GetNote(NoteType n)
		{
			switch (n)
			{
				case NoteType.Kick:
					return kick;
				case NoteType.Dribble:
					return dribble;
				case NoteType.Knock:
					return knock;
				case NoteType.Volley:
					return volley;
				case NoteType.Puck:
					return puck;
				default:
					return null;
			}
		}

		IEnumerator InstantiateNoteObjects()
		{
			yield return new WaitUntil(() => aud.isPlaying);

			foreach (var note in currentChart.Notes)
			{
				if (note.Type == NoteType.Rotate || note.Type == NoteType.Vibrate)
					continue;

				var tr = Instantiate(GetNote(note.Type), new Vector3(0, 0, 200), new Quaternion(), GetLane(note.Lane)).transform;

				// ドリブルの補助線
				if (note.Type == NoteType.Dribble)
				{
					var dri = note as Dribble;
					if (dri.Previous != null && notesDic.ContainsKey(dri.Previous))
					{
						//var line = tr.gameObject.AddComponent<LineRenderer>();
						//line.startWidth = line.endWidth = 0.6f;
						//line.material = dribbleMaterial;
						//line.endColor = dribbleStartColor;
						//line.startColor = dribbleEndColor;
						//line.useWorldSpace = false;
						//line.SetPositions(new Vector3[2]);

						var line = new GameObject();
						line.transform.SetParent(tr);
						line.transform.localPosition = Vector3.zero;
						var renderer = line.AddComponent<MeshRenderer>();
						renderer.material = dribbleMaterial;
						line.AddComponent<MeshFilter>();
						var meshMod = line.AddComponent<LongNoteMeshModifier>();
						meshMod.Width = tr.localScale.x;
					}
				}
				tr.localPosition = new Vector3(0, 0, 200);
				tr.localRotation = Quaternion.Euler(0, 0, 0);

				notesDic.Add(note, tr);
			}
		}

		// Update is called once per frame
		void FixedUpdate()
		{
			ProcessNotes();

			if (!Music.IsPlaying && prevPlaying)
			{
				// hack 成績発表実装時に移動させる
				SceneManager.LoadScene("Title");
			}

			if (Input.GetKeyDown(KeyCode.Escape))
			{
				// hack メニュー実装時は変更する
				SceneManager.LoadScene("Title");
			}

			prevPlaying = Music.IsPlaying;
		}

		float Distance(float v, float t) => v * t;

		float GetTimeOfMeasure(Beat beat, float bpm) => (60 * 4) / bpm * ((float)beat.Rhythm / beat.Note);

		float TickToTime(int tick) 
		{
			const int mul = 1000000;
			int m = (int)(60f / currentChart.Bpm / 4f / 12f * mul);
			int tempoPtr = 0;
			var result = 0;
			for (int i = 0; i < tick; i++)
			{
				if (tempoPtr < tempos.Count && temposTicks[tempos[tempoPtr]] <= i)
				{
					m = (int)(60f / tempos[tempoPtr].Tempo / 4f / 12f * mul);
					tempoPtr++;
				}
				result += (int)(m);
			}
			return (float)result / mul;
		}

		float SpeededTickToTime(int tick)
		{
			const int mul = 1000000;
			int m = (int)(60f / currentChart.Bpm / 4f / 12f * mul);
			int tempoPtr = 0;
			var result = 0;
			var multiplier = 1f;
			for (int i = 0; i < tick; i++)
			{
				if (tempoPtr < tempos.Count && temposTicks[tempos[tempoPtr]] <= i)
				{
					m = (int)(60f / tempos[tempoPtr].Tempo / 4f / 12f * mul);
					tempoPtr++;
				}
				multiplier = speeds.LastOrDefault(s => GetTickOfMeasure(s.Measure) + s.Tick <= i)?.Speed ?? 1;
				result += (int)(m * multiplier);
			}
			return (float)result / mul;
		}

		/// <summary>
		/// 判定音用のフラグ．
		/// </summary>
		NoteFlag noteFlag;

		ulong prevTime, deltaTime;

		void ProcessNotes()
		{
			if (!aud.isPlaying)
				return;
			var audioTime = Music.AudioTimeSec;
			if (Music.Just.Bar == 0 && Music.IsJustChangedBeat())
			{
				NotesFX.Instance.Metronome();
			}

			noteFlag = NoteFlag.None;

			Beat beat = currentChart.Beat;
			var speedMul = 1f;
			var time = GetTimeOfMeasure(beat, currentChart.Bpm);

			speedMul = speedsTimes.LastOrDefault(s => s.Value <= audioTime).Key?.Speed ?? speedMul;

			if (Music.IsPlaying)
				deltaTime = (ulong)(audioTime * mul) - prevTime;

			currentTimeInteger += (ulong)(deltaTime * speedMul);
			foreach (var note in notesDic.ToList())
			{
				if (note.Value == null)
					continue;

				var pos = note.Value.localPosition;
				note.Value.localPosition = new Vector3(pos.x, pos.y, Distance(hiSpeed, (time + speededNotesTimes[note.Key] - (float)(CurrentTime))));
				if (note.Key.Type == NoteType.Dribble)
				{
					//var l = note.Value.gameObject.GetComponent<LineRenderer>();
					//var prev = (note.Key as Dribble).Previous;
					//if (l != null && notesDic.ContainsKey(prev) && notesDic[prev] != null)
					//{
					//	var prevPos = notesDic[prev].position - note.Value.position;
					//	var scale = note.Value.transform.localScale;
					//	prevPos = new Vector3(prevPos.x / scale.x, prevPos.y / scale.y, prevPos.z / scale.z);
					//	l.SetPosition(1, prevPos);
					//}
					var l = note.Value.gameObject.GetComponentInChildren<LongNoteMeshModifier>();
					var prev = (note.Key as Dribble).Previous;

					if (l != null && notesDic.ContainsKey(prev) && notesDic[prev] != null)
					{
						var prevPos = notesDic[prev].position - note.Value.position;
						prevPos.z = -prevPos.z;
						//var scale = note.Value.transform.localScale;
						//prevPos = new Vector3(prevPos.x / scale.x, prevPos.y / scale.y, -prevPos.z / scale.z);
						l.EndPosition = prevPos;
					}
				}

				if (time + notesTimes[note.Key] - audioTime <= 0)
				{
					Judge(note.Key, note.Value);
				}
			}
			prevTime = (ulong)(audioTime * mul);
		}

		void Judge(NoteBase note, Transform tf)
		{
			//hack ちゃんと判定させる
			ScoreBoardController.Instance.Great++;

			switch (note.Type)
			{
				case NoteType.Kick:
					if (!noteFlag.HasFlag(NoteFlag.Kick))
						NotesFX.Instance.Kick();

					noteFlag |= NoteFlag.Kick;
					break;
				case NoteType.Dribble:
					var d = (Dribble)note;
				
					if (d.IsFirstNote)
						NotesFX.Instance.DribbleStart();
					if (d.IsLastNote)
						NotesFX.Instance.DribbleStop();	

					if (!noteFlag.HasFlag(NoteFlag.Dribble))
						NotesFX.Instance.Dribble();

					noteFlag |= NoteFlag.Dribble;
					break;
				case NoteType.Knock:
					if (!noteFlag.HasFlag(NoteFlag.Knock))
						NotesFX.Instance.Knock();

					noteFlag |= NoteFlag.Knock;
					break;
				case NoteType.Volley:
					var v = (Volley)note;

					if (v.IsFirstNote && !noteFlag.HasFlag(NoteFlag.Receive))
					{
						NotesFX.Instance.Receive();
						noteFlag |= NoteFlag.Receive;
					}
					else if (v.IsLastNote && !noteFlag.HasFlag(NoteFlag.Spike))
					{
						NotesFX.Instance.Spike();
						noteFlag |= NoteFlag.Spike;
					}
					else if (!noteFlag.HasFlag(NoteFlag.Toss))
					{
						NotesFX.Instance.Toss();
						noteFlag |= NoteFlag.Toss;
					}
					break;
				case NoteType.Puck:
					if (!noteFlag.HasFlag(NoteFlag.Puck))
						NotesFX.Instance.Puck();

					noteFlag |= NoteFlag.Puck;
					break;
				case NoteType.Rotate:
					//todo 回転
					break;
				case NoteType.Vibrate:
					//todo 振動
					break;
			}
			notesDic.Remove(note);
			StartCoroutine(DelayedDestroy(note, tf.gameObject));
		}

		IEnumerator DelayedDestroy(NoteBase n, GameObject go)
		{
			var rigid = go.AddComponent<Rigidbody>();
			if (n.Type == NoteType.Puck)
				rigid.useGravity = false;
			var re = go.GetComponentInChildren<LongNoteMeshModifier>();
			if (re != null) Destroy(re.gameObject);

			// ドリブルの場合は最後のものだけ飛ばす
			if (n.Type != NoteType.Dribble || (n as Dribble).IsLastNote)
			{
				rigid.AddForce(Vector3.forward * 1000);
				if (n.Type == NoteType.Knock)
					rigid.AddForce(Vector3.up * 50);
				else if (n.Type == NoteType.Kick)
					rigid.AddForce(Vector3.up * 200);
				else if (n.Type != NoteType.Puck)
					rigid.AddForce(Vector3.up * 500);
				rigid.AddForce((n.Lane < 2 ? Vector3.left : Vector3.right) * 250);
				yield return new WaitUntil(() => go.transform.position.z > 20 || go.transform.position.y < -50);
			}
			Destroy(go);
		}

		[Flags]
		enum NoteFlag
		{
			None = 0,
			Kick = 1,
			Dribble = 2,
			Knock = 4,
			Receive = 8,
			Spike = 16,
			Toss = 32,
			Puck = 64,
		}

	}

	public static class StaticData
	{
		public static string ChartPath { get; set; }
		public static float Hispeed { get; set; } = 20;
	}
}
