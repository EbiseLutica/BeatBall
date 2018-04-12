using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Xeltica.BeatBall
{

	public class TitleController : Singleton<TitleController>
	{
		[SerializeField]
		Text playButtonText;

		[SerializeField]
		Text copyRightText;

		[SerializeField]
		private Text hsText;

		[SerializeField]
		Text versionText;

		string[] charts;

		int chartPtr = 0;

		bool HasCharts => charts != null && charts.Length > 0;

		int hispeed = 0;

		string rootPathOfChart;

		void Start()
		{
			Application.targetFrameRate = 60;
			if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
			{
				rootPathOfChart = Path.Combine(Application.persistentDataPath, "charts");
			}
			else
			{
				rootPathOfChart = Path.Combine(Environment.CurrentDirectory, "charts");
			}
			hispeed = (int)(StaticData.Hispeed * 10);

			if (Directory.Exists(rootPathOfChart))
				charts = Directory.GetFiles(rootPathOfChart, "*.bbf", SearchOption.AllDirectories);
		}

		void Update()
		{
			if (HasCharts)
			{
				if (chartPtr < 0)
					chartPtr = charts.Length - 1;
				if (chartPtr > charts.Length - 1)
					chartPtr = 0;
				
				playButtonText.text = Path.GetFileNameWithoutExtension(charts[chartPtr]);
			}
			else
				playButtonText.text = I18nProvider.Instance.CurrentLang["message.nochart"];

			if (hispeed < 10)
				hispeed = 10;
			if (hispeed > 300)
				hispeed = 300;

			StaticData.Hispeed = hispeed * .1f;

			if (versionText != null)
				versionText.text = Constants.Version.ToString();
			
			if (copyRightText != null)
				copyRightText.text = Constants.Copyright;


			if (hsText != null)
				hsText.text = $"SPEED: {StaticData.Hispeed}";
		}

		public void Play()
		{
			if (!HasCharts)
				return;
			StaticData.ChartPath = charts[chartPtr].Substring(rootPathOfChart.Length + 1);
			SceneManager.LoadScene("Main");
		}

		public void Left()
		{
			chartPtr--;
		}

		public void Right()
		{
			chartPtr++;
		}

		public void HsDec() => hispeed -= 1;
		public void HsInc() => hispeed += 1;
	}
}
