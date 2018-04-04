using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayBallRenderer : MonoBehaviour
{
	RectTransform myRect;
	StartMessage prevMessage;
	Text text;
	Coroutine changeCoroutine;

	[SerializeField]
	RectTransform textRect;

	[SerializeField]
	float timeToAppear = 0.18f;

	[SerializeField]
	float timeToChange = 0.5f;

	[SerializeField]
	private bool visible;

	public bool Visible
	{
		get { return visible; }
		set { visible = value; }
	}

	[SerializeField]
	StartMessage message = StartMessage.AreYouReady;

	RectTransform canvasRect;

	public StartMessage Message
	{
		get { return message; }
		set { message = value; }
	}

	void Start()
	{
		text = textRect.gameObject.GetComponent<Text>();
		myRect = GetComponent<RectTransform>();
		text.text = CurrentText;
		canvasRect = GameObject.FindGameObjectWithTag("Canvas").GetComponent<RectTransform>();
	}

	// Update is called once per frame
	void Update()
	{
		if (textRect == null)
			return;

		if (text == null)
			text = textRect.gameObject.GetComponent<Text>();

		if (myRect == null)
			myRect = GetComponent<RectTransform>();

		if (Message != prevMessage)
		{
			if (changeCoroutine != null)
			{
				Debug.Log("Stop Coroutine");
				StopCoroutine(changeCoroutine);
			}

			changeCoroutine = StartCoroutine(ChangeMessage());
		}

		var size = Visible ? canvasRect.sizeDelta.x : 0;

		var t = (1 / timeToAppear) * Time.deltaTime;

		myRect.sizeDelta = new Vector2((int)Mathf.Lerp(myRect.sizeDelta.x, size, t), myRect.sizeDelta.y);
		textRect.localScale = Vector3.Lerp(textRect.localScale, Visible ? Vector3.one : Vector3.one * 2, t);

		prevMessage = Message;
	}

	IEnumerator ChangeMessage()
	{
		var t = 0f;
		var nowTime = Time.time;
		while (t <= timeToChange)
		{
			var angle = Mathf.Lerp(0, 360, t / timeToChange);

			if (angle >= 270)
				text.text = CurrentText;

			textRect.localScale = t < timeToChange * 0.75f ? Vector3.Lerp(Vector3.one, Vector3.zero, t) : Vector3.Lerp(Vector3.zero, Vector3.one, t - timeToChange * 0.75f);

			textRect.eulerAngles = new Vector3(angle, 0, 0);

			t = Time.time - nowTime;

			yield return new WaitForEndOfFrame();
		}

		textRect.eulerAngles = Vector3.zero;
		text.text = CurrentText;
	}

	string CurrentText => Message == StartMessage.AreYouReady ? "Are you <color=#7f0000>ready?</color>" : "Play <color=#7f0000>ball!</color>";

	public enum StartMessage
	{
		AreYouReady,
		PlayBall
	}
}
