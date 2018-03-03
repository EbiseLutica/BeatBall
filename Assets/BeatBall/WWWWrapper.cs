using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Xeltica.BeatBall
{
	public static class WWWWrapper
	{
		public static WWW OpenLocalFile(string file) => file.StartsWith("/") ? new WWW("file://" + EscapeFile(file)) : new WWW("file:///" + EscapeFile(file));

		static string EscapeFile(string file) => EscapeData(file.Replace(Path.DirectorySeparatorChar, '/'));
		static string EscapeData(string url) => Uri.EscapeDataString(url).Replace(Uri.EscapeDataString("/"), "/");
	}
}
