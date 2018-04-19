using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Xeltica.BeatBall
{

	public class TouchProvider : Singleton<TouchProvider>
	{

		// Use this for initialization
		void Start()
		{
			keyConfig = KeyConfig.Default;
		}

		KeyConfig keyConfig;

		[SerializeField]
		GameObject[] lanes = new GameObject[4];


		/// <summary>
		/// クロスプラットフォームに対応した，レーンが押下されているか確認するメソッドです．
		/// </summary>
		/// <returns>レーンの押下状態を表示します．</returns>
		/// <param name="laneId">左から0 1 2 3の順番のレーンID.</param>
		public PressInfo PressCheck(int laneId)
		{
			var c = KeyCheck(laneId);
			return c != PressInfo.None ? c : TouchCheck(laneId);
		}

		/// <summary>
		/// This is internally used checking method for keyboards.
		/// </summary>
		PressInfo KeyCheck(int laneId)
		{
			if (Input.GetKeyDown(keyConfig.Taps[laneId]))
				return PressInfo.Tap;
			if (Input.GetKey(keyConfig.Flicks[laneId]))
				return PressInfo.Slide;
			if (Input.GetKey(keyConfig.Taps[laneId]))
				return PressInfo.Stay;
			
			return PressInfo.None;
		}

		/// <summary>
		/// This is internally used checking method for touch devices.
		/// </summary>
		/// <returns>The check.</returns>
		/// <param name="laneId">Lane identifier.</param>
		PressInfo TouchCheck(int laneId)
		{
			foreach (var touch in Input.touches)
			{
				var ray = Camera.main.ScreenPointToRay(touch.position);
				var hit = default(RaycastHit);

				if (Physics.Raycast(ray, out hit))
				{
					if (hit.collider.gameObject == lanes[laneId])
					{
						switch (touch.phase)
						{
							case TouchPhase.Began:
								return PressInfo.Tap;
							
							case TouchPhase.Stationary:
								return PressInfo.Stay;
							
							case TouchPhase.Moved:
								return PressInfo.Slide;
						}
					}
				}
			}
			return PressInfo.None;
		}
	}

	public struct KeyConfig
	{
		public string[] Taps;

		public string[] Flicks;

		public KeyConfig(string tap0, string tap1, string tap2, string tap3,
						string flick0, string flick1, string flick2, string flick3)
		{
			Taps = new string[4];
			Flicks = new string[4];
			Taps[0] = tap0;
			Taps[1] = tap1;
			Taps[2] = tap2;
			Taps[3] = tap3;
			Flicks[0] = flick0;
			Flicks[1] = flick1;
			Flicks[2] = flick2;
			Flicks[3] = flick3;
		}

		public static readonly KeyConfig Default = new KeyConfig("d", "f", "j", "k",
		                                                         "e", "r", "u", "i");
	}

	public enum PressInfo
	{
		/// <summary>
		/// 少しも触れていない．
		/// </summary>
		None,
		/// <summary>
		/// 呼ばれたタイミングでちょうどタップされた
		/// </summary>
		Tap,
		/// <summary>
		/// 呼ばれたときタップされていた(以前にタップされている)
		/// </summary>
		Stay,
		/// <summary>
		/// 呼ばれた時スライドされていた
		/// </summary>
		Slide
	}
}