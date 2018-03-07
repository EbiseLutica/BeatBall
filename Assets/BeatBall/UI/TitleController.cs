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
		Text versionText;

		string[] charts;

		int chartPtr = 0;

		bool HasCharts => charts.Length > 0;

		string rootPathOfChart;

		void Start()
		{
			
			if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
			{
				rootPathOfChart = Path.Combine(Application.persistentDataPath, "charts");
			}
			else
			{
				rootPathOfChart = Path.Combine(Environment.CurrentDirectory, "charts");
			}

			charts = Directory.GetFiles(rootPathOfChart, "*.bbf", SearchOption.AllDirectories);
		}

		void Update()
		{
			if (HasCharts)
			{
				if (chartPtr < 0)
					chartPtr = charts.Length;
				if (chartPtr > charts.Length - 1)
					chartPtr = 0;
				
				playButtonText.text = Path.GetFileNameWithoutExtension(charts[chartPtr]);
			}
			else
				playButtonText.text = I18nProvider.Instance.CurrentLang["message.nochart"];

			if (versionText != null)
				versionText.text = Constants.Version.ToString();
			
			if (copyRightText != null)
				copyRightText.text = Constants.Copyright;
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
	}
}