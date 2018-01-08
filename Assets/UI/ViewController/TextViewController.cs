using UnityEngine;

namespace Xeltica.BeatBall
{

	/// <summary>
	/// テキストを表示できるビューコントローラーです．
	/// </summary>
	public class TextViewController : TextViewBaseController
	{
		[SerializeField]
		private string text;

		public new LocalizableString Text
		{
			get { return text; }
			set { text = value; }
		}

		protected override void Update()
		{
			base.Text = Text;
			base.Update();
		}
	}

}