using UnityEngine;
using UnityEngine.UI;

namespace Xeltica.BeatBall
{
	/// <summary>
	/// テキストを表示できるビューコントローラーのベースクラスです．
	/// </summary>
	[RequireComponent(typeof(Text))]
	public abstract class TextViewBaseController : ViewController
	{
		protected string Text { get; set; }

		protected virtual void Update()
		{
			GetComponent<Text>().text = Text;
		}
	}

}