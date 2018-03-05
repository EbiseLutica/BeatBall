using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Remoting.Messaging;

namespace Xeltica.BeatBall
{
	/// <summary>
	/// ノーツ効果音の管理と再生を行います．
	/// </summary>
	[RequireComponent(typeof(AudioSource))]
	public class NotesFX : Singleton<NotesFX>
	{
		AudioSource aud, aud2;

		[Header("Clips")]
		[SerializeField]
		AudioClip kick;
		[SerializeField]
		AudioClip knock;
		[SerializeField]
		AudioClip dribble;
		[SerializeField]
		AudioClip dribbleLoop;
		[SerializeField]
		AudioClip receive;
		[SerializeField]
		AudioClip toss;
		[SerializeField]
		AudioClip spike;
		[SerializeField]
		AudioClip puck;
		[SerializeField]
		AudioClip metronome;

		// Use this for initialization
		void Start()
		{
			aud = GetComponent<AudioSource>();
			aud2 = gameObject.AddComponent<AudioSource>();

			// ループ再生が必要なので登録
			aud.clip = dribbleLoop;
			aud.loop = true;
			aud.playOnAwake = false;
		}

		int dribbleCount = 0;

		public void DribbleStart()
		{
			if (dribbleCount == 0)
				aud.Play();
			
			dribbleCount++;
		}

		public void DribbleStop()
		{
			dribbleCount--;

			if (dribbleCount == 0)
				aud.Stop();
		}

		public void Kick() => aud2.PlayOneShot(kick);
		public void Knock() => aud2.PlayOneShot(knock);
		public void Receive() => aud2.PlayOneShot(receive);
		public void Toss() => aud2.PlayOneShot(toss);
		public void Spike() => aud2.PlayOneShot(spike);
		public void Dribble() => aud2.PlayOneShot(dribble);
		public void Puck() => aud2.PlayOneShot(puck);
		public void Metronome() => aud2.PlayOneShot(metronome);
	}

}