using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
namespace Xeltica.BeatBall
{
	static class Constants
	{

		/// <summary>
		/// 権利表記です．
		/// </summary>
		public static readonly string Copyright = "(C)2017-2018 Xeltica";

		/// <summary>
		/// BeatBall のバージョンです．
		/// </summary>
		public static readonly AppVersion Version = new AppVersion("pre180403");

		/// <summary>
		/// 理論値．
		/// </summary>
		public static readonly int MaxScore = 1500000;

		public static readonly float GreatRate = 1.5f;
		public static readonly float OkRate = 0.5f;

	}

	public class AppVersion
	{
		public DateTime PreviewDate { get; private set; }
		public VersionState State { get; private set; }
		public int Major { get; private set; }
		public int Minor { get; private set; }
		public int Revision { get; private set; }

		static readonly Regex rtmPattern = new Regex(@"^(\d+)\.(\d+)(?:\.(\d+))?$");
		static readonly Regex prvPattern = new Regex(@"^pre(\d{2})(\d{2})(\d{2})(?:.(\d+))?$");

		public AppVersion(string version, VersionState state = VersionState.Stable)
		{
			Match m;
			if ((m = rtmPattern.Match(version)).Success)
			{
				SetVersion(m);
				State = state;
			}
			else if ((m = prvPattern.Match(version)).Success)
			{
				PreviewDate = new DateTime(int.Parse("20" + m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value));
				if (m.Groups[4].Success)
					Revision = int.Parse(m.Groups[4].Value);
				State = VersionState.PreAlpha;
			}
			else
			{
				throw new ArgumentException("正しい文字列表現ではありません．", nameof(version));
			}
		}

		private void SetVersion(Match m)
		{
			Major = int.Parse(m.Groups[0].Value);
			Minor = int.Parse(m.Groups[1].Value);
			if (m.Groups[2].Success)
				Revision = int.Parse(m.Groups[2].Value);
		}

		private string VersionString => $"{Major}.{Minor}.{Revision}";

		public override string ToString()
		{
			switch (State)
			{
				case VersionState.PreAlpha:
					return $"pre-{PreviewDate.Year.ToString().Remove(0, 2)}{PreviewDate.Month:00}{PreviewDate.Day:00}.{Revision}";
				case VersionState.Alpha:
					return "alpha-" + VersionString;
				case VersionState.Beta:
					return "beta-" + VersionString;
				case VersionState.RC:
					return "rc-" + VersionString;
				case VersionState.Stable:
					return VersionString;
				default:
					throw new ArgumentOutOfRangeException(nameof(State), "異常なバージョンステートです．");
			}
		}
	}

	public enum VersionState
	{
		/// <summary>
		/// 開発中バージョン．ほとんど人前に見せられないレベル．
		/// </summary>
		PreAlpha, 
		/// <summary>
		/// 開発中バージョン．一部のユーザーに提供し始めるレベル．
		/// </summary>
		Alpha, 
		/// <summary>
		/// 開発中バージョン．ある程度のバグはあるがユーザーがプレイできるレベル．
		/// </summary>
		Beta, 
		/// <summary>
		/// リリース候補バージョン．バグチェックが終わり次第リリースできるバージョン．
		/// </summary>
		RC,
		/// <summary>
		/// 一般リリースバージョン．バグチェックにより致命的なバグがないとみなされたバージョン．
		/// </summary>
		Stable
	}
}