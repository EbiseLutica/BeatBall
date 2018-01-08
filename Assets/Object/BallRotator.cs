using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Xeltica.BeatBall
{

	public class BallRotator : MonoBehaviour
	{

		[SerializeField]
		private float speed;

		private float angle;
		// Use this for initialization
		void Start()
		{

		}

		// Update is called once per frame
		void Update()
		{
			// 角度をほげほげする
			angle += speed * Time.deltaTime;
			angle %= 360;

			// 回転

			transform.Rotate(Vector3.left * speed * 10 * Time.deltaTime);

			// 移動
			transform.position += (Vector3.back * (Time.deltaTime * speed));
		}
	}
}