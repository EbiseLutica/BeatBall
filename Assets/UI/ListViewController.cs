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

				btn.GetComponentInChildren<Text>().text = item.Text;

				btn.GetComponent<Button>().onClick.AddListener(() => Debug.Log("まだ"));

			}
		}


		[System.Serializable]
		public class ListViewItem
		{
			public string ViewNameToNavigate;
			public LocalizableString Text;
			public bool Enabled = true;
		}
	}

}