using UnityEngine;
using UnityEngine.UI;

namespace Xeltica.BeatBall
{
	/// <summary>
	/// テキストを表示できるビューコントローラーのベースクラスです．
	/// </summary>
	public abstract class TextViewBaseController : ViewController
	{
		protected string Text { get; set; }

		[SerializeField]
		private Text textView;

		protected virtual void Update()
		{
			textView.text = Text;
		}
	}

}