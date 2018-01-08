using UnityEngine;
using UnityEngine.UI;


namespace Xeltica.BeatBall
{
	public class ListViewController : ViewController
	{
		[SerializeField]
		private GameObject button;

		[SerializeField]
		private RectTransform listView;

		[SerializeField]
		private ListViewItem[] items;

		private void Start()
		{
			foreach (var item in items)
			{
				// ボタンをつくる
				var btn = (GameObject)Instantiate(button);
				btn.transform.SetParent(listView, false);

				// テキスト設定
				btn.GetComponentInChildren<Text>().text = item.Text;

				// イベント設定
				btn.GetComponent<Button>().onClick.AddListener(() => ViewWindowController.Instance.Navigate(item.ViewNameToNavigate));

			}
		}


		[System.Serializable]
		public class ListViewItem
		{
			public string ViewNameToNavigate;
			[SerializeField]
			private string text;
			public LocalizableString Text => text;
			public bool Enabled = true;
		}
	}

}