using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.UI;

namespace Xeltica.BeatBall
{
	/// <summary>
	/// View Window の制御機能を提供します．
	/// </summary>
	[RequireComponent(typeof(RectTransform))]
	public class ViewWindowController : Singleton<ViewWindowController>
	{

		[SerializeField]
		private ViewController[] views;

		[SerializeField]
		private bool visible = false;

		[SerializeField]
		private float speed = 5;

		[SerializeField]
		private string defaultView;

		[SerializeField]
		private Text backButton;

		[SerializeField]
		private Text viewName;

		[SerializeField]
		private Transform parent;


		public ViewController CurrentView { get; private set; }

		private RectTransform rect;

		Stack<ViewController> viewStack;

		// Use this for initialization
		void Start()
		{
			rect = GetComponent<RectTransform>();
			viewStack = new Stack<ViewController>();
			if (defaultView == null)
				throw new InvalidOperationException("View が存在しません．");
			Navigate(defaultView);
		}

		public void Show() => visible = true;
		public void Hide()
		{
			if (viewStack.Count == 0)
			{
				visible = false;
			}
			else
			{
				if (CurrentView != null)
					Destroy(CurrentView.gameObject);
				SetView(viewStack.Pop());
			}
		}

		public void Navigate(string id)
		{
			if (CurrentView != null)
			{
				viewStack.Push(views.FirstOrDefault(v => v.Name== CurrentView.Name));
				Destroy(CurrentView.gameObject);
			}
			var view = views.FirstOrDefault(v => v.Name == id);
			if (view == null)
				throw new ArgumentException("ID に対応する View が存在しません．");

			SetView(view);
		}

		void SetView(ViewController view)
		{
			CurrentView = Instantiate(view.gameObject, parent).GetComponent<ViewController>();
			viewName.text = CurrentView.Label;
			if (viewStack.Count > 0)
				viewName.text = $"<color=#afafaf><size=14>{viewStack.Peek().Label} > </size></color>" + viewName.text;
		}

		// Update is called once per frame
		void Update()
		{
			// ViewWindow のスケーリング
			rect.sizeDelta = GameObject.FindGameObjectWithTag("Canvas").GetComponent<RectTransform>().rect.size;

			// ViewWindow の表示状態の制御
			var x = visible ? 0 : rect.rect.width;
			rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, new Vector2(x, rect.anchoredPosition.y), speed * Time.deltaTime);

			// ActionBar Button の制御
			backButton.text = viewStack.Count > 0 ? "←" : "×";

			// ESC キー や Android の戻るキー対応
			if (Input.GetKeyDown(KeyCode.Escape))
				Hide();
		}
	}
}