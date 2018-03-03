using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Xeltica.BeatBall
{

	public class ScoreBoardController : Singleton<ScoreBoardController>
	{
		[SerializeField]
		private new string name;
		public string Name
		{
			get { return name; }
			set { name = value; }
		}

		public Difficulty Difficulty { get; set; }

		[SerializeField]
		private Level level;

		public Level Level
		{
			get { return level; }
			set { level = value; }
		}

		[SerializeField]
		private int noteCount;
		public int NoteCount
		{
			get { return noteCount; }
			set { noteCount = value; }
		}

		[Header("Judge")]
		[SerializeField]
		private int great;
		public int Great
		{
			get { return great; }
			set { great = value; }
		}

		[SerializeField]
		private int good;
		public int Good
		{
			get { return good; }
			set { good = value; }
		}

		[SerializeField]
		private int ok;
		public int Ok
		{
			get { return ok; }
			set { ok = value; }
		}

		[SerializeField]
		private int bad;
		public int Bad
		{
			get { return bad; }
			set { bad = value; }
		}

		[Header("UI Object")]

		[SerializeField]
		private Text titleText;
		[SerializeField]
		private Text difficulty;
		[SerializeField]
		private Text score;
		[SerializeField]
		private Text judge;

		/// <summary>
		/// オブジェクト生成時に呼ばれます
		/// </summary>
		void Start()
		{
			Difficulty = Difficulty.Legend;
		}

		/// <summary>
		/// 1 フレームに 1 回ぐらい呼ばれます
		/// </summary>
		void Update()
		{
			UIUpdate();
		}

		/// <summary>
		/// UI 更新
		/// </summary>
		void UIUpdate()
		{
			// タイトル
			titleText.text = name ?? "No Title";

			// 難易度表記
			difficulty.text = $"{Difficulty.Name}  Lv.{Level}";
			difficulty.color = Difficulty.AccentColor;

			// 判定
			judge.text = $"<color=#bfbf00>{I18n["judge.great"]} {great}</color>  <color=#9f9f9f>{I18n["judge.good"]} {good}</color>  <color=#ef8f00>{I18n["judge.ok"]} {ok}</color>  <color=#9f8f00>{I18n["judge.bad"]} {bad}</color>";

			// スコア
			score.text = CalculateScore().ToString("N0");
		}



		int CalculateScore()
		{
			float scoreOfGreat = (float)Constants.MaxScore / NoteCount;
			float scoreOfGood = scoreOfGreat / Constants.GreatRate;
			float scoreOfOk = scoreOfGood * Constants.OkRate;
			float scoreOfBad = 0;

			return (int)((great * scoreOfGreat) + (good * scoreOfGood) + (ok * scoreOfOk) + (bad * scoreOfBad));
		}
	}
}