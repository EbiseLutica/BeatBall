namespace Xeltica.BeatBall
{
	/// <summary>
	/// バージョン表示のビューです．
	/// </summary>
	public class VersionViewController : TextViewBaseController
	{
		private void Start()
		{
			Text = string.Format(I18n["menu.pref.version.format"], Constants.Version, Constants.Copyright);
		}
	}

}