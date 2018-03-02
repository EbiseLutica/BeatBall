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
	public class MusicController : Singleton<MusicController>
	{

		[SerializeField]
		string chartPath;

		Chart currentChart;
		AudioSource aud;

		string rootPathOfChart;

		// Use this for initialization
		IEnumerator Start()
		{
			aud = GetComponent<AudioSource>();

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

			aud.clip = currentChart.Song;
			aud.Play();
		}

		// Update is called once per frame
		void Update()
		{
		}
	}
}