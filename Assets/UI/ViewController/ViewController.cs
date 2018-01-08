using UnityEngine;


namespace Xeltica.BeatBall
{
	[RequireComponent(typeof(RectTransform))]
	public class ViewController : BaseBehaviour
	{
		[SerializeField]
		private new string name;

		public string Name
		{
			get { return name; }
			set { name = value; }
		}

		[SerializeField]
		private string label;

		public LocalizableString Label
		{
			get { return label; }
			set { label = value; }
		}

	}

}