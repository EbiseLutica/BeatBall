using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System.IO;
using System;

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
		int measure;
		float prevTime;
		float timer;
		BeatMapItem currentBeatMapItem;

		// Use this for initialization
		IEnumerator Start()
		{
			aud = GetComponent<AudioSource>();
			mus = GetComponent<Music>();

			rootPathOfChart = Path.Combine(Environment.CurrentDirectory, "charts");
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

			timer = currentBeatMapItem.BeatTime;

			Beat beat = currentChart.Beat;
			float tempo = currentChart.Bpm;
			mus.Sections.Clear();
			mus.Sections.Add(new Music.Section(0, 16 / beat.Note, 16 / beat.Note * beat.Rhythm, tempo));

			mus.Sections.AddRange(currentChart.Events.Select(e =>
			{
				if (e is TempoEvent)
					tempo = (e as TempoEvent).Tempo;
				else if (e is BeatEvent)
					beat = (e as BeatEvent).Beat;
				else
					Debug.LogError($"サポートされないイベント {e.GetType().Name}");
				return new Music.Section(e.Measure, 16 / beat.Note, 16 / beat.Note * beat.Rhythm, tempo);
			}));


			var song = currentChart.Song;
			var buf = new float[song.samples - TimeToSample(currentChart.Offset, song.frequency, song.channels)];
			song.GetData(buf, TimeToSample(currentChart.Offset, song.frequency, song.channels) / 2);

			song = AudioClip.Create(song.name, buf.Length, song.channels, song.frequency, false);
			song.SetData(buf, 0);
			aud.clip = currentChart.Song = song;

			yield return new WaitForSeconds(1);

			mus.PlayStart();
		}

		public static int TimeToSample(float time, int samplingRate = 44100, int ch = 2) => (int)(time * samplingRate + 0.5) * ch;
		public static float SampleToTime(int sample, int samplingRate = 44100, int ch = 2) => (float)sample / samplingRate / ch;


		// Update is called once per frame
		void Update()
		{
			ProcessNotes();
		}

		void ProcessNotes()
		{
			if (!aud.isPlaying)
				return;
			if (Music.IsJustChangedBeat())
				NotesFX.Instance.Kick();

		}

	}
}