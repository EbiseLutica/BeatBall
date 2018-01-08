namespace Xeltica.BeatBall
{
	public abstract class VersionViewController : TextViewBaseController
	{
		private void Start()
		{
			Text = string.Format(I18n["menu.pref.version"], Constants.BBVersion, Constants.Copyright);
		}
	}

}