using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text;

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
		Dictionary<NoteBase, float> notesTimes;
		Dictionary<NoteBase, float> speededNotesTimes;

		Dictionary<SpeedEvent, float> speedsTimes;
		Dictionary<TempoEvent, int> temposTicks;
		Dictionary<NoteBase, LongNoteMeshModifier> longNoteMeshCaches;
		Dictionary<NoteBase, float> notesJudgeWaitingTimes;
		List<TempoEvent> tempos;
		IEnumerable<BeatEvent> beats;
		IEnumerable<SpeedEvent> speeds;

		Queue<float> metronomeTime;

		bool prevPlaying;

		const ulong mul = 100000000000;

		/// <summary>
		/// ミスと判定されるまでの遊び時間．
		/// </summary>
		const float judgeThreshold = 0.3f;

		public float CurrentTime { get; private set; }

		ulong currentTimeInteger;

		[SerializeField]
		Text loadingText;

		[SerializeField]
		GameObject loadingUI;

		LocalizableString loadingLog;

		string errorLog;

		int loadingProgress;

		bool isInitialized;

		bool isError;

		[SerializeField]
		Material laneNormal;
		[SerializeField]
		Material laneTouched;

		[SerializeField]
		Renderer[] lanes;

		// Use this for initialization
		IEnumerator Start()
		{
			int count = 0;


			loadingLog = "loading.components";
			aud = GetComponent<AudioSource>();
			notesDic = new Dictionary<NoteBase, Transform>();
			notesTimes = new Dictionary<NoteBase, float>();
			speededNotesTimes = new Dictionary<NoteBase, float>();
			tempos = new List<TempoEvent>();
			metronomeTime = new Queue<float>();
			longNoteMeshCaches = new Dictionary<NoteBase, LongNoteMeshModifier>();

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

			yield return null;
			loadingLog = "loading.chart";
			try
			{
				currentChart = Chart.Parse(File.ReadAllText(chart));
			}
			catch (ChartIncompatibleException ex)
			{
				HandleError($"譜面の互換性がありません．{ex.LineNumber}行目");
				yield break;
			}
			catch (ChartErrorException ex)
			{
				HandleError($"構文エラー: {ex.Message} {ex.LineNumber}行目");
				yield break;
			}
			catch (Exception other)
			{
				HandleError($"不明なエラー: {other.Message}\n");
			}

			yield return null;
			loadingLog = "loading.music";
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

			currentChart.Notes.Sort((a, b) => a.Measure * 1000 + a.Tick < b.Measure * 1000 + b.Tick ? -1 : 1);

			Beat beat = currentChart.Beat;
			float tempo = currentChart.Bpm;

			count = 0;

			foreach (var e in currentChart.Events)
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
				count++;
				loadingProgress = (int)(count / (double)currentChart.Events.Count * 100);
				yield return null;
			}

			// オフセットを補正し，ゲーム用に1小節余白を開ける

			yield return null;
			loadingLog = "loading.optimize";
			var song = currentChart.Song;
			var buf = new float[song.samples * song.channels - TimeToSample(currentChart.Offset, song.frequency, song.channels)];

			song.GetData(buf, TimeToSample(currentChart.Offset, song.frequency, song.channels) / song.channels);

			var newSongLength = TimeToSample(GetTimeOfMeasure(currentChart.Beat, currentChart.Bpm), song.frequency, song.channels) / song.channels;

			song = AudioClip.Create(song.name, buf.Length / 2 + newSongLength, song.channels, song.frequency, false);
			song.SetData(buf, newSongLength);

			aud.clip = currentChart.Song = song;

			yield return null;

			loadingLog = "loading.cache";
			tempos = tempos.OrderBy(t => t.Measure).ToList();
			beats = currentChart.Events.OfType<BeatEvent>().OrderBy(b => b.Measure);
			speeds = currentChart.Events.OfType<SpeedEvent>().OrderBy(b => b.Measure * 1000 + b.Tick);
			temposTicks = tempos.Select(t => new KeyValuePair<TempoEvent, int>(t, GetTickOfMeasure(t.Measure))).ToDictionary(k => k.Key, v => v.Value);
			beats = beats.OrderBy(b => b.Measure);

			float time = 0, stime = 0;
			int tick = 0, prevTick = 0;
			count = 0;
			foreach (var note in currentChart.Notes)
			{
				GetTimeFromTick(tick = GetTickOfMeasure(note.Measure) + note.Tick, out time, out stime, prevTick, time, stime);
				notesTimes[note] = time;
				speededNotesTimes[note] = stime;
				//Debug.Log($"{time} {stime}");
				yield return null;
				count++;
				prevTick = tick;
				loadingProgress = (int)(count / (double)currentChart.Notes.Count * 100);
			}

			time = stime = tick = prevTick = 0;
			speedsTimes = speeds.Select(s =>
			{
				GetTimeFromTick(tick = GetTickOfMeasure(s.Measure + 1) + s.Tick, out time, out stime, prevTick, time, stime);
				prevTick = tick;
				return new KeyValuePair<SpeedEvent, float>(s, time);
			}).ToDictionary(k => k.Key, k => k.Value);
			
			yield return null;

			var tickOfBeat = 192 / currentChart.Beat.Note;
			for (int i = 0; i <= currentChart.Beat.Rhythm; i++)
			{
				metronomeTime.Enqueue((60f / currentChart.Bpm / 4f / 12f * (tickOfBeat * i)));
			}

			isInitialized = true;

			Destroy(loadingUI);

			// ノーツ生成
			StartCoroutine(InstantiateNoteObjects());

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
			aud.Play();
		}

		public static int TimeToSample(float time, int samplingRate = 44100, int ch = 2) => (int)(time * samplingRate + 0.5) * ch;
		public static float SampleToTime(int sample, int samplingRate = 44100, int ch = 2) => (float)sample / samplingRate / ch;

		void HandleError(string errorMessage)
		{
			Debug.LogError(errorMessage);
			errorLog = new StringBuilder()
				.AppendLine("<b>エラーが発生しました！</b>")
				.AppendLine(errorMessage)
				.AppendLine("ESC キーで戻る")
				.ToString();
			isError = true;
		}

		public int GetTickOfMeasure(int measure)
		{
			var temp = 0;

			var beat = currentChart.Beat;

			// 拍子イベントがなければそのまま
			if (!beats.Any())
			{
				return 16 / beat.Note * beat.Rhythm * 12 * measure;
			}

			var prevMeas = 0;
			var prevBeat = currentChart.Beat;

			foreach (var b in beats)
			{
				temp += 16 / prevBeat.Note * prevBeat.Rhythm * 12 * (b.Measure - prevMeas);
				prevMeas = b.Measure;
				prevBeat = b.Beat;
			}

			var last = beats.Last().Beat;

			temp += 16 / last.Note * last.Rhythm * 12 * (measure - prevMeas);

			//for (int i = 1; i <= measure; i++)
			//{
			//	if (beatsList.Count > 0 && beatsList[0].Measure <= i)
			//	{
			//		beat = beatsList[i].Beat;
			//		beatsList.RemoveAt(i);
			//	}

			//	temp += 16 / beat.Note * beat.Rhythm * 12;
			//}

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
			var cnt = 0;
			var prevMeasure = 0;
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
				cnt++;
				if (prevMeasure != note.Measure || cnt > 15)
				{
					yield return new WaitForEndOfFrame();
					cnt %= 15;
				}
				prevMeasure = note.Measure;
			}
		}

		private void OnGUI()
		{
			GUI.Label(new Rect(8, 100, 3000, 3000), $"<size=20><color={(CurrentTime == aud.time ? "#ff0000" : "#ffffff")}>{CurrentTime:#.0} {aud.time:#.0}</color></size>");

		}

		// Update is called once per frame
		void FixedUpdate()
		{
			ProcessNotes();

			for (int i = 0; i < 4; i++)
			{
				lanes[i].material = TouchProvider.Instance.PressCheck(i) == PressInfo.None ? laneNormal : laneTouched;
			}

			if (loadingUI != null)
			{
				if (isError)
				{
					loadingText.text = errorLog;
					loadingUI.GetComponent<Image>().color = new Color(0.5f, 0, 0, 0.6f);
				}
				else
				{
					loadingText.text = $"{loadingLog}\n{loadingProgress}%";
				}
			}

			if (isInitialized || isError)
			{
				if (!aud.isPlaying && prevPlaying)
				{
					// hack 成績発表実装時に移動させる
					SceneManager.LoadScene("Title");
				}

				if (Input.GetKeyDown(KeyCode.Escape))
				{
					// hack メニュー実装時は変更する
					SceneManager.LoadScene("Title");
				}
			}

			prevPlaying = aud.isPlaying;
		}

		float Distance(float v, float t) => v * t;

		float GetTimeOfMeasure(Beat beat, float bpm) => (60 * 4) / bpm * ((float)beat.Rhythm / beat.Note);

		void GetTimeFromTick(int tick, out float time, out float speededTime, int prevTick = 0, float prevTime = 0, float prevSpeededTime = 0)
		{
			const int mul = 1000000;
			int m = (int)(60f / currentChart.Bpm / 4f / 12f * mul);
			var multiplier = 1f;
			time = prevTime * mul;
			speededTime = prevSpeededTime * mul;
			for (int i = prevTick; i < tick; i++)
			{
				m = (int)(60f / (tempos.LastOrDefault(t => temposTicks[t] < i)?.Tempo ?? currentChart.Bpm) / 4f / 12f * mul);
				multiplier = speeds.LastOrDefault(s => GetTickOfMeasure(s.Measure) + s.Tick <= i)?.Speed ?? 1;
				time += m;
				speededTime += (int)(m * multiplier);
			}
			time /= mul;
			speededTime /= mul;
		}

		float TickToTime(int tick)
		{
			float time, _;
			GetTimeFromTick(tick, out time, out _);
			return time;
		}

		/// <summary>
		/// 判定音用のフラグ．
		/// </summary>
		NoteFlag noteFlag;

		ulong prevTime, deltaTime;

		float CalculateCurrentTime(float time)
		{
			var speedsToUse = speedsTimes.TakeWhile(s => s.Value <= time).Select(s => s.Key).ToList();
			var retVal = 0f;
			if (speedsToUse.Count == 0)
			{
				// スピード設定が特になければ実際のtimeを返す
				return time;
			}

			var prevTime = 0f;
			var prevSpeed = 1f;
			foreach (var s in speedsToUse)
			{
				retVal += (speedsTimes[s] - prevTime) * prevSpeed;
				prevTime = speedsTimes[s];
				prevSpeed = s.Speed;
			}
			return retVal + (time - speedsTimes[speedsToUse.Last()]) * prevSpeed;
		}

		void ProcessNotes()
		{
			if (!aud.isPlaying)
				return;
			var audioTime = aud.time;

			noteFlag = NoteFlag.None;

			Beat beat = currentChart.Beat;
			var time = GetTimeOfMeasure(beat, currentChart.Bpm);

			CurrentTime = CalculateCurrentTime(audioTime);


			if (metronomeTime.Count > 0 && metronomeTime.Peek() <= audioTime)
			{
				metronomeTime.Dequeue();
				NotesFX.Instance.Metronome();
			}

			if (Music.IsPlaying)
				deltaTime = (ulong)(audioTime * mul) - prevTime;
			
			foreach (var note in notesDic.ToList())
			{
				var noteTime = time + notesTimes[note.Key] - audioTime;

				if (noteTime < -judgeThreshold && note.Value != null)
				{
					ScoreSubmit(JudgeState.Bad);
					Destroy(note.Value.gameObject);
					continue;
				}

				if (note.Value == null)
				{
					if (note.Key.Type == NoteType.Dribble && longNoteMeshCaches.ContainsKey(note.Key))
					{
						longNoteMeshCaches.Remove(note.Key);
					}
					continue;
				}
				var pos = note.Value.localPosition;
				note.Value.localPosition = new Vector3(pos.x, pos.y, Distance(hiSpeed, (time + speededNotesTimes[note.Key] - CurrentTime)));

				if (note.Key.Type == NoteType.Dribble)
				{
					var l = longNoteMeshCaches.ContainsKey(note.Key) ? longNoteMeshCaches[note.Key]
						  : longNoteMeshCaches[note.Key] = note.Value.gameObject.GetComponentInChildren<LongNoteMeshModifier>();
					var prev = (note.Key as Dribble).Previous;

					if (l != null && notesDic.ContainsKey(prev) && notesDic[prev] != null)
					{
						var prevPos = notesDic[prev].position - note.Value.position;
						prevPos.z = -prevPos.z;
						l.EndPosition = prevPos;
					}
				}

				if (-judgeThreshold < noteTime && noteTime < judgeThreshold)
				{
					Judge(note.Key, note.Value, noteTime);
				}
			}
			prevTime = (ulong)(audioTime * mul);
		}

		JudgeState Calculate(float judgePhase)
		{
			var normalizedPhase = Mathf.Abs(judgePhase);

			return normalizedPhase < judgeThreshold * 0.25f ? JudgeState.Great
				 : normalizedPhase < judgeThreshold * 0.5f ? JudgeState.Good
										  : JudgeState.Ok;
		}

		void ScoreSubmit(JudgeState state)
		{
			var score = ScoreBoardController.Instance;
			switch (state)
			{
				case JudgeState.Great:
					score.Great++;
					break;
				case JudgeState.Good:
					score.Good++;
					break;
				case JudgeState.Ok:
					score.Ok++;
					break;
				case JudgeState.Bad:
					score.Bad++;
					break;
			}
		}

		void Judge(NoteBase note, Transform tf, float judgePhase)
		{
			var touch = TouchProvider.Instance;
			var info = touch.PressCheck(note.Lane);
			var processed = false;
			switch (note.Type)
			{
				case NoteType.Kick:
					if (info == PressInfo.Tap)
					{
						ScoreSubmit(Calculate(judgePhase));

						processed = true;

						// SFX
						if (!noteFlag.HasFlag(NoteFlag.Kick))
							NotesFX.Instance.Kick();
						
						noteFlag |= NoteFlag.Kick;
					}

					break;
				case NoteType.Dribble:
					var d = (Dribble)note;

					if (d.IsFirstNote)
					{
						if (info == PressInfo.Tap)
						{
							ScoreSubmit(Calculate(judgePhase));
							NotesFX.Instance.DribbleStart();
						}
						processed = true;
					}

					if (info != PressInfo.None && judgePhase <= 0)
					{
						ScoreSubmit(JudgeState.Great);
						processed = true;
						if (d.IsLastNote)
							NotesFX.Instance.DribbleStop();
						if (!noteFlag.HasFlag(NoteFlag.Dribble))
							NotesFX.Instance.Dribble();

						noteFlag |= NoteFlag.Dribble;
					}

					break;
				case NoteType.Knock:
					if (info == PressInfo.Slide)
					{
						processed = true;
						ScoreSubmit(JudgeState.Great);
						if (!noteFlag.HasFlag(NoteFlag.Knock))
							NotesFX.Instance.Knock();

						noteFlag |= NoteFlag.Knock;
					}
					else if (info == PressInfo.Tap && judgePhase < -judgeThreshold * 0.5f )
					{
						ScoreSubmit(JudgeState.Ok);
						if (!noteFlag.HasFlag(NoteFlag.Knock))
							NotesFX.Instance.Knock();

						noteFlag |= NoteFlag.Knock;
					}

					break;
				case NoteType.Volley:
					var v = (Volley)note;

					if (info == PressInfo.Tap)
					{
						processed = true;
						ScoreSubmit(Calculate(judgePhase));
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
					}
					break;
				case NoteType.Puck:
					if (info != PressInfo.None && judgePhase <= 0)
					{
						ScoreSubmit(JudgeState.Great);
						processed = true;
						if (!noteFlag.HasFlag(NoteFlag.Puck))
							NotesFX.Instance.Puck();

						noteFlag |= NoteFlag.Puck;
					}
					break;
				case NoteType.Rotate:
					//todo 回転
					break;
				case NoteType.Vibrate:
					//todo 振動
					break;
			}
			if (processed)
			{
				StartCoroutine(DelayedDestroy(note, notesDic[note].gameObject));
				notesDic.Remove(note);
			}
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

		enum JudgeState
		{
			Great, Good, Ok, Bad
		}

	}

	public static class StaticData
	{
		public static string ChartPath { get; set; }
		public static float Hispeed { get; set; } = 20;
		public static bool IsAutoPlay { get; set; }
		public static float Correction { get; set; } = 0;
	}
}
