using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Runtime.Serialization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Security.Policy;

namespace Xeltica.BeatBall
{
	using CommandDictionary = Dictionary<string, Chart.CommandCallBack>;
	public class Chart
	{
		public string Title { get; private set; }
		public string Artist { get; private set; }
		public string Designer { get; private set; }
		public double Bpm { get; private set; }
		public Beat Beat { get; private set; }
		public Difficulty Difficulty { get; private set; }
		public Level Level { get; private set; }
		public float Offset { get; private set; }
		public List<NoteBase> Notes { get; private set; }
		public AudioClip Song { get; set; }
		public string SongFile { get; private set; }

		public static readonly Version SupportedVersion = new Version(1, 0);

		readonly CommandDictionary commands = new CommandDictionary();

		void Add(string key, CommandCallBack callBack)
		{
			commands.Add(key, callBack);
		}

		public static Chart Parse(string file)
		{
			var c = new Chart();
			c.Parse(file.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n'));
			return c;
		}

		private void Parse(string[] lines)
		{
			string chunk = null;
			int i = 0;
			foreach (var line in lines)
			{
				i++;
			
				// 余白を消す
				var l = line.TrimStart();
	
				// コメント部分を消す
				if (l.IndexOf("//") >= 0)
					l = l.Remove(l.IndexOf("//"));

				// 空行・コメント行は無視
				if (string.IsNullOrWhiteSpace(l))
					continue;

				if (l.TrimEnd().ToLower() == "end")
				{
					if (chunk == null)
						throw new ChartErrorException("チャンク外で end 文が実行されました．", i);
					chunk = null;
				}

				switch (chunk)
				{
					case "notes":
						ProcessNotes(l, i, ref chunk);
						break;

					case null:
					case "":
						ProcessGlobal(l, i, ref chunk);
						break;
					
					default:
						throw new ChartErrorException($"不正なチャンク {chunk} です．", i);
				}
			}
		}

		static readonly Regex cmdRegexp = new Regex(@"^#([\w\d_-]+) ?(.*)$");
		void ProcessGlobal(string statement, int lineNumber, ref string chunkName)
		{
			if (cmdRegexp.IsMatch(statement))
			{
				// コマンド
				var m = cmdRegexp.Match(statement);
				var key = m.Groups[1].Value.ToLower();
				var value = m.Groups[2].Value;

				if (commands.ContainsKey(key))
				{
					commands[key](value, lineNumber);
				}
				else
				{
					throw new ChartErrorException($"ヘッダー {key} は存在しません．", lineNumber);
				}
			}
			else if (statement.TrimEnd().All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
			{
				chunkName = statement.TrimEnd().ToLower();
			}
			else
			{
				throw new ChartErrorException("構文エラーです", lineNumber);
			}
		}

		static readonly Regex chartRegexp = new Regex(@"^(\d{3})([a-dA-Z]): ?(.+)$");
		void ProcessNotes(string statement, int lineNumber, ref string chunkName)
		{
			var match = chartRegexp.Match(statement);
			// todo まともな実装する
			if (match.Success)
			{
				//ノーツ定義
				var measure = int.Parse(match.Groups[1].Value);
				var lane = 'a' - match.Groups[2].Value.ToLower()[0];
				var data = match.Groups[3].Value.ToLower().Replace("  ", "00");
				if (data.Length % 2 != 0)
					throw new ChartErrorException("データ列が不正です．", lineNumber);
				
				Debug.Log($"{lineNumber}: 小節:{measure} レーン:{'A' + (char)lane} データ列:{data}");
			}
			else if ((match = cmdRegexp.Match(statement)).Success)
			{
				//コマンド
				Debug.Log($"{lineNumber}: コマンド Header:{match.Captures[1]} Value:{match.Captures[2]}");
			}
			else
				throw new ChartErrorException("ノーツ定義でない不正な行です．", lineNumber);

		}

		Chart() 
		{
			Add("version", (v, l) =>
			{
				var versions = v.Split('.');
				int major, minor;

				if (versions.Length != 2)
					throw new ChartErrorException("version ヘッダーの書式が不正です．", l);

				if (!int.TryParse(versions[0], out major))
				{
					throw new ChartErrorException("version ヘッダーの書式が不正です．", l);
				}

				if (!int.TryParse(versions[1], out minor))
				{
					throw new ChartErrorException("version ヘッダーの書式が不正です．", l);
				}

				var version = new Version(major, minor);

				// 互換性のないバージョンを検出したらエラー
				if (!SupportedVersion.IsCompatible(version))
					throw new ChartIncompatibleException();
			});
			Add("title", (v, l) => Title = v);
			Add("artist", (v, l) => Artist = v);
			Add("designer", (v, l) => Designer = v);
			Add("bpm", (v, l) =>
			{
				float bpm;
				if (!float.TryParse(v, out bpm))
					throw new ChartErrorException("数値でない値を BPM に設定しようとしました．", l);
				Bpm = bpm;
			});
			Add("beat", (v, l) =>
			{
				var beats = v.Split('/');
				if (beats.Length != 2)
					throw new ChartErrorException("不正な拍子設定です．", l);
				int rhythm, note;

				if (!int.TryParse(beats[0], out rhythm))
				{
					throw new ChartErrorException("不正な拍子設定です．", l);
				}

				if (!int.TryParse(beats[1], out note))
				{
					throw new ChartErrorException("不正な拍子設定です．", l);
				}

				Beat = new Beat(rhythm, note);
			});
			Add("difficulty", (v, l) =>
			{
				switch (v.ToLower())
				{
					default:
						throw new ChartErrorException("不正な難易度を設定しようとしました．", l);
					case "beginner":
						Difficulty = Difficulty.Beginner;
						break;
					case "amateur":
						Difficulty = Difficulty.Amateur;
						break;
					case "pro":
						Difficulty = Difficulty.Pro;
						break;
					case "legend":
						Difficulty = Difficulty.Legend;
						break;
				}
			});
			Add("level", (v, l) =>
			{
				var lv = new Level();
				if (v.Last() == '+')
				{
					v = v.Remove(v.Length - 1);
					lv.Plus = true;
				}
				int num;
				if (!int.TryParse(v, out num))
					throw new ChartErrorException("譜面レベルに数値以外が指定されました", l);
				lv.Numeric = num;
				Level = lv;
			});
			Add("songfile", (v, l) => SongFile = v);
			Add("offset", (v, l) =>
			{
				float offset;
				if (!float.TryParse(v, out offset))
					throw new ChartErrorException("offset 値に数値でない値が指定されました", l);
			});
		}

		public delegate void CommandCallBack(string value, int line);
	}

	[Serializable]
	public class ChartErrorException : Exception
	{
		public ChartErrorException() { }
		public ChartErrorException(string message, int line) : base(message) { LineNumber = line; }
		public ChartErrorException(string message, int line, Exception innerException) : base(message, innerException) { LineNumber = line; }
		protected ChartErrorException(SerializationInfo info, StreamingContext context) : base(info, context) { }
		public int LineNumber { get; protected set; }
	}

	[Serializable]
	public class ChartIncompatibleException : ChartErrorException { }

	public struct Beat
	{
		public int Note { get; set; }
		public int Rhythm { get; set; }

		public Beat(int note, int rhythm)
		{
			Note = note;
			Rhythm = rhythm;
		}
	}

	public struct Level
	{
		public int Numeric { get; set; }
		public bool Plus { get; set; }

		public Level(int lv, bool plus)
		{
			Numeric = lv;
			Plus = plus;
		}

		public override string ToString() => $"{Numeric}{(Plus ? "+" : "")}";
	}

	public struct Version
	{
		public int Major { get; set; }
		public int Minor { get; set; }

		public Version(int maj, int min)
		{
			Major = maj;
			Minor = min;
		}

		/// <summary>
		/// 指定したファイルのバージョンと互換性があるかどうか検証します．
		/// aaa
		/// </summary>
		/// <returns>互換性があれば<c>true</c>，違えば<c>false</c><．/returns>
		/// <param name="fileVer">検証する譜面ファイルのバージョン．</param>
		public bool IsCompatible(Version fileVer) => fileVer.Major == Major && fileVer.Minor <= Minor;
	}
}