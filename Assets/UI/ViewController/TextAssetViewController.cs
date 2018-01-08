using UnityEngine;

namespace Xeltica.BeatBall
{
	/// <summary>
	/// テキストアセットを表示するビューコントローラーです
	/// </summary>
	public class TextAssetViewController : TextViewBaseController
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