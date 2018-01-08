using UnityEngine;

namespace Xeltica.BeatBall
{
	/// <summary>
	/// テキストアセットを表示するビューコントローラーです
	/// </summary>
	public class TextAssetViewController : TextViewController
	{
		[SerializeField]
		private TextAsset textAsset;

		protected override void Update()
		{
			if (textAsset != null)
				Text = textAsset.text;
			base.Update();
		}

	}

}