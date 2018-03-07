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
		Color dribbleStartColor;

		[SerializeField]
		Color dribbleEndColor;

		[SerializeField]
		Material dribbleMaterial;


		[SerializeField]
		float hiSpeed = 10;

		[SerializeField]
		PlayBallRenderer playBallRenderer;

		Dictionary<NoteBase, Transform> notesDic;
		Dictionary<NoteBase, int> notesTicks;
		List<TempoEvent> tempos;

		// Use this for initialization
		IEnumerator Start()
		{
			aud = GetComponent<AudioSource>();
			mus = GetComponent<Music>();
			notesDic = new Dictionary<NoteBase, Transform>();
			notesTicks = new Dictionary<NoteBase, int>();
			tempos = new List<TempoEvent>();

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
					// buffering
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


			// ノーツ生成
			StartCoroutine(InstantiateNoteObjects());

			var beats = currentChart.Events.Where(e => e is BeatEvent).OfType<BeatEvent>();

			foreach (var note in currentChart.Notes)
			{
				notesTicks[note] = GetTickOfMeasure(note.Measure, beats) + note.Tick;
			}

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

		public int GetTickOfMeasure(int measure, IEnumerable<BeatEvent> beats)
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

				var tr = Instantiate(GetNote(note.Type), Vector3.zero, new Quaternion(), GetLane(note.Lane)).transform;

				// ドリブルの補助線
				if (note.Type == NoteType.Dribble)
				{
					var dri = note as Dribble;
					if (dri.Previous != null && notesDic.ContainsKey(dri.Previous))
					{
						var line = tr.gameObject.AddComponent<LineRenderer>();
						line.startWidth = line.endWidth = 0.6f;
						line.material = dribbleMaterial;
						line.endColor = dribbleStartColor;
						line.startColor = dribbleEndColor;
						line.useWorldSpace = false;
						line.SetPositions(new Vector3[2]);
					}
				}
				tr.localPosition = Vector3.zero;
				tr.localRotation = Quaternion.Euler(0, 0, 0);

				notesDic.Add(note, tr);
			}
		}

		// Update is called once per frame
		void FixedUpdate()
		{
			ProcessNotes();

			if (Input.GetKeyDown(KeyCode.Escape))
			{
				SceneManager.LoadScene("Title");
			}
		}

		float Distance(float v, float t) => v * t;

		float GetTimeOfMeasure(Beat beat, float bpm) => (60 * 4) / bpm * ((float)beat.Rhythm / beat.Note);
	
		float TickToTime(int tick, float bpm) => 60f / bpm / 4f / 12f * tick;

		/// <summary>
		/// 判定音用のフラグ．
		/// </summary>
		NoteFlag noteFlag;

		void ProcessNotes()
		{
			if (!aud.isPlaying)
				return;

			if (Music.Just.Bar == 0 && Music.IsJustChangedBeat())
			{
				NotesFX.Instance.Metronome();
			}

			noteFlag = NoteFlag.None;

			Beat beat = currentChart.Beat;
			float bpm = currentChart.Bpm;
			var time = GetTimeOfMeasure(beat, bpm);
			foreach (var note in notesDic.ToList())
			{
				if (note.Value == null)
					continue;
				var tempo = tempos.FirstOrDefault(t => t.Measure <= note.Key.Measure)?.Tempo;
				if (tempo != null && !bpm.Equals(tempo))
				{
					bpm = tempo.Value;
				}
				var pos = note.Value.localPosition;
				note.Value.localPosition = new Vector3(pos.x, pos.y, Distance(hiSpeed * 2, time + TickToTime(notesTicks[note.Key], bpm) - Music.AudioTimeSec));

				if (note.Key.Type == NoteType.Dribble)
				{
					var l = note.Value.gameObject.GetComponent<LineRenderer>();
					var prev = (note.Key as Dribble).Previous;
					if (l != null && notesDic.ContainsKey(prev) && notesDic[prev] != null)
					{
						var prevPos = notesDic[prev].position - note.Value.position;
						var scale = note.Value.transform.localScale;
						prevPos = new Vector3(prevPos.x / scale.x, prevPos.y / scale.y, prevPos.z / scale.z);
						l.SetPosition(1, prevPos);
					}
				}

				if (time + TickToTime(notesTicks[note.Key], bpm) - Music.AudioTimeSec <= 0)
				{
					Judge(note.Key, note.Value);
				}
			}
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
			var re = go.GetComponent<LineRenderer>();
			if (re != null) Destroy(re);

			// ドリブルの場合は最後のものだけ飛ばす
			if (n.Type != NoteType.Dribble || (n as Dribble).IsLastNote)
			{
				rigid.AddForce(Vector3.forward * 1000);
				rigid.AddForce(Vector3.up * 500);
				rigid.AddForce((n.Lane < 2 ? Vector3.left : Vector3.right) * 250);
				yield return new WaitUntil(() => go.transform.position.y < -10);
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
	}
}