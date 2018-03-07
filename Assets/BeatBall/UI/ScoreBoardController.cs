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

		[SerializeField]
		private string artist;
		public string Artist
		{
			get { return artist; }
			set { artist = value; }
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
		private Text artistText;
		[SerializeField]
		private Text difficulty;
		[SerializeField]
		private Text levelText;
		[SerializeField]
		private Text score;
		[SerializeField]
		private Text judge;
		[SerializeField]
		private float speed = 5;

		RectTransform rect;


		public bool Ready { get; set; }

		/// <summary>
		/// オブジェクト生成時に呼ばれます
		/// </summary>
		void Start()
		{
			Difficulty = Difficulty.Legend;
			rect = GetComponent<RectTransform>();
		}

		/// <summary>
		/// 1 フレームに 1 回ぐらい呼ばれます
		/// </summary>
		void Update()
		{
			UIUpdate();
			WindowUpdate();
		}

		void WindowUpdate()
		{
			//View 表示状態の制御
			var y = Ready ? 0 : rect.rect.height;
			rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, new Vector2(rect.anchoredPosition.x, y), speed * Time.deltaTime);
		}

		/// <summary>
		/// UI 更新
		/// </summary>
		void UIUpdate()
		{
			// タイトル
			titleText.text = name ?? "No Title";

			// 難易度表記
			difficulty.text = $"{Difficulty.Name}";
			difficulty.color = Difficulty.AccentColor;
			
			levelText.text = Level.ToString();
			
			// アーティスト
			artistText.text = Artist ?? "Unknown Artist";
			artistText.text = "　" + artistText.text;
			// 判定
			judge.text = $"<color=#bfbf00>{I18n["judge.great"]} {great}</color>\n<color=#ef8f00>{I18n["judge.good"]} {good}</color>\n<color=#40f77d>{I18n["judge.ok"]} {ok}</color>\n<color=#9f9f9f>{I18n["judge.bad"]} {bad}</color>";

			// スコア
			score.text = $"　{CalculateScore():N0}";
		}



		int CalculateScore()
		{
			float scoreOfGreat = (float)Constants.MaxScore / NoteCount;
			float scoreOfGood = scoreOfGreat / Constants.GreatRate;
			float scoreOfOk = scoreOfGood * Constants.OkRate;
			float scoreOfBad = 0;

			return (int)((great * scoreOfGreat) + (good * scoreOfGood) + (ok * scoreOfOk) + (bad * scoreOfBad) + 0.5f);
		}
	}

}
