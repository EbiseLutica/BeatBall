using UnityEngine;

namespace Xeltica.BeatBall
{
	public abstract class BaseBehaviour : MonoBehaviour
	{
		public I18nProvider I18n => I18nProvider.Instance;
	}

}