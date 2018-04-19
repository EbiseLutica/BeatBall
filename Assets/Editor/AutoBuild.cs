using UnityEditor;
using UnityEngine;
using System.IO;
using System;
using System.IO.Compression;

namespace Xeltica.BeatBall
{
	public static class BuildClass
	{
		[MenuItem("/Edit/Start X-platform Build")]
		public static void Build()
		{
			// ビルド対象シーンリスト
			string[] sceneList = {
				"./Assets/Title.unity",
				"./Assets/Main.unity",
			};

			PlatformInfo[] platforms =
			{
				new PlatformInfo("Windows", WindowsPath, BuildTarget.StandaloneWindows),
				new PlatformInfo("macOS", MacPath, BuildTarget.StandaloneOSX),
				new PlatformInfo("Linux", LinuxPath, BuildTarget.StandaloneLinux)
			};

			// 実行
			foreach (var p in platforms)
			{
				Console.WriteLine("Start building for " + p.Name);
				string errorMessage = BuildPipeline.BuildPlayer(
					sceneList,
					p.Path,
					p.Target,
					BuildOptions.None
				);
				// 結果出力
				if (!string.IsNullOrEmpty(errorMessage))
					Debug.LogError("[Error!] " + errorMessage);
				else
					Debug.Log("[Success!]");

				//var zipName = Path.GetDirectoryName(p.Path) + ".zip";

			}



		}

		static string RootPath => Path.Combine(Environment.CurrentDirectory, "Build", Constants.Version.ToString());

		static string WindowsPath => Path.Combine(RootPath, $"windows-{Constants.Version.ToShortString()}", $"beatball-{Constants.Version.ToShortString()}.exe");
		static string MacPath => Path.Combine(RootPath, $"macos-{Constants.Version.ToShortString()}", $"beatball-{Constants.Version.ToShortString()}");
		static string LinuxPath => Path.Combine(RootPath, $"linux-{Constants.Version.ToShortString()}", $"beatball-{Constants.Version.ToShortString()}");


		struct PlatformInfo
		{
			public string Name { get; private set; }
			public string Path { get; private set; }
			public BuildTarget Target { get; private set; }
			public PlatformInfo(string name, string path, BuildTarget target)
			{
				Name = name;
				Path = path;
				Target = target;
			}
		}

	}

}
