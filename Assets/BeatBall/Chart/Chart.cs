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
		public float Bpm { get; private set; }
		public Beat Beat { get; private set; }
		public Difficulty Difficulty { get; private set; }
		public Level Level { get; private set; }
		public float Offset { get; private set; }
		public List<NoteBase> Notes { get; private set; }
		public AudioClip Song { get; set; }
		public string SongFile { get; private set; }
		public List<EventBase> Events { get; private set; }


		public static readonly Version SupportedVersion = new Version(1, 0);

		readonly CommandDictionary commands = new CommandDictionary();

		readonly Dribble[] dribbleLayers = new Dribble[10];
		readonly Volley[] volleyLayers = new Volley[10];

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

			var measures = new List<MeasureTemp>();

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
						ProcessNotes(l, i, ref chunk, measures);
						break;

					case null:
					case "":
						ProcessGlobal(l, i, ref chunk);
						break;

					default:
						throw new ChartErrorException($"不正なチャンク {chunk} です．", i);
				}
			}

			// ノーツを逐次解析する

			int measure = 0;

			List<NoteTemp> noteTemps = new List<NoteTemp>();

			while (measures.Count > 0)
			{
				var measureTemp = measures.Where(n => n.Measure == measure).ToList();

				for (i = 0; i < 192; i++)
				{
					noteTemps.Clear();
					// 現在tickまでのノーツを取得
					foreach (var tmp in measureTemp)
					{
						while (tmp.Notes.Count > 0 && tmp.Notes.Peek().Tick <= i)
						{
							noteTemps.Add(tmp.Notes.Dequeue());
						}
					}

					foreach (var tmp in noteTemps)
					{
						switch (tmp.Note)
						{
							case '1':
								Notes.Add(new Kick(measure, tmp.Tick, tmp.Lane));
								break;
							case '2':
								Notes.Add(new Knock(measure, tmp.Tick, tmp.Lane));
								break;
							case '3':
								{
									Dribble d;
									Notes.Add(d = new Dribble(measure, tmp.Tick, tmp.Lane));
									dribbleLayers[tmp.Layer] = d;
								}
								break;
							case '4':
								{
									Dribble d;
									if (dribbleLayers[tmp.Layer] == null)
										throw new ChartErrorException($"Dribble が非対応です {tmp.Layer}", tmp.LineNumber);
									Notes.Add(d = new Dribble(measure, tmp.Tick, tmp.Lane));
									d.Previous = dribbleLayers[tmp.Layer];
									dribbleLayers[tmp.Layer].Next = d;
									dribbleLayers[tmp.Layer] = d;
								}
								break;
							case '5':
								{
									Dribble d;
									if (dribbleLayers[tmp.Layer] == null)
										throw new ChartErrorException($"Dribble が非対応です {tmp.Layer}", tmp.LineNumber);
									Notes.Add(d = new Dribble(measure, tmp.Tick, tmp.Lane));
									d.Previous = dribbleLayers[tmp.Layer];
									dribbleLayers[tmp.Layer].Next = d;
									dribbleLayers[tmp.Layer] = null;
								}
								break;
							case '6':
								{
									Volley v;
									Notes.Add(v = new Volley(measure, tmp.Tick, tmp.Lane));
									volleyLayers[tmp.Layer] = v;
								}
								break;
							case '7':
								{
									Volley v;
									if (volleyLayers[tmp.Layer] == null)
										throw new ChartErrorException($"Volley が非対応です {tmp.Layer}", tmp.LineNumber);
									Notes.Add(v = new Volley(measure, tmp.Tick, tmp.Lane));
									v.Previous = volleyLayers[tmp.Layer];
									volleyLayers[tmp.Layer].Next = v;
									volleyLayers[tmp.Layer] = v;
								}
								break;
							case '8':
								{
									Volley v;
									if (volleyLayers[tmp.Layer] == null)
										throw new ChartErrorException($"Volley が非対応です {tmp.Layer}", tmp.LineNumber);
									Notes.Add(v = new Volley(measure, tmp.Tick, tmp.Lane));
									v.Previous = volleyLayers[tmp.Layer];
									volleyLayers[tmp.Layer].Next = v;
									volleyLayers[tmp.Layer] = null;
								}
								break;
							case '9':
								Notes.Add(new Puck(measure, tmp.Tick, tmp.Lane));
								break;
							case 'a':
							case 'b':
								Notes.Add(new RotateNote(measure, tmp.Tick, tmp.Lane, tmp.Note == 'a' ? Direction.Left : Direction.Right, tmp.Layer));
								break;
							case 'c':
							case 'd':
								Notes.Add(new VibrateNote(measure, tmp.Tick, tmp.Lane, tmp.Note == 'c' ? Orientation.Horizontal : Orientation.Vertical, tmp.Layer));
								break;
						}
					}

				}

				foreach (var tmp in measureTemp)
					measures.Remove(tmp);
				measure++;
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

		struct MeasureTemp
		{
			public Queue<NoteTemp> Notes;
			public int Lane;
			public int Measure;
		}

		struct NoteTemp
		{
			public char Note;
			public int Layer;
			public int Tick;
			public int LineNumber;
			public int Lane;
		}


		static readonly Regex chartRegexp = new Regex(@"^(\d{3})([a-dA-Z]): (.+)$");
		void ProcessNotes(string statement, int lineNumber, ref string chunkName, List<MeasureTemp> notes)
		{
			var match = chartRegexp.Match(statement);
			if (match.Success)
			{
				//ノーツ定義
				var measure = int.Parse(match.Groups[1].Value);
				var lane = match.Groups[2].Value.ToLower()[0] - 'a';
				var data = match.Groups[3].Value.ToLower().Replace("  ", "00");
				if (data.Length % 2 != 0)
					throw new ChartErrorException("データ列が不正です．", lineNumber);

				var queue = new Queue<NoteTemp>();

				var tick = 0;
				for (int i = 0; i < data.Length; i += 2)
				{
					var note = new NoteTemp();
					note.LineNumber = lineNumber;
					note.Note = data[i];
					note.Layer = data[i + 1] - '0';
					if (note.Layer < 0 || 9 < note.Layer)
						throw new ChartErrorException("レイヤー番号が不正です．", lineNumber);

					note.Tick = tick;
					note.Lane = lane;
					queue.Enqueue(note);
					tick += (int)(192 / (data.Length / 2f));
				}

				notes.Add(new MeasureTemp
				{
					Lane = lane,
					Measure = measure,
					Notes = queue,
				});
			}
			else if ((match = cmdRegexp.Match(statement)).Success)
			{
				//コマンド
				//Debug.Log($"{lineNumber}: コマンド Header:{match.Groups[1]} Value:{match.Groups[2]}");
				var cmd = match.Groups[1].Value.ToLower();
				var meas = 0;
				if (cmd.StartsWith("bpm"))
				{
					// BPM
					cmd = cmd.Remove(0, 3);
					if (!int.TryParse(cmd, out meas))
						throw new ChartErrorException("ノーツ定義文が不正です", lineNumber);
					Events.Add(new TempoEvent(meas, ParseBpm(match.Groups[2].Value, lineNumber)));
				}
				else if (cmd.StartsWith("beat"))
				{
					// 拍子
					cmd = cmd.Remove(0, 4);
					if (!int.TryParse(cmd, out meas))
						throw new ChartErrorException("ノーツ定義文が不正です", lineNumber);
					Events.Add(new BeatEvent(meas, ParseBeat(match.Groups[2].Value, lineNumber)));
				}
				else if (cmd.StartsWith("speed"))
				{
					// ハイスピード
					cmd = cmd.Remove(0, 5);
					if (!int.TryParse(cmd, out meas))
						throw new ChartErrorException("ノーツ定義文が不正です", lineNumber);
					float speed;
					int tick;

					ParseSpeed(match.Groups[2].Value, lineNumber, out speed, out tick);
					Events.Add(new SpeedEvent(meas, speed, tick));
				}
				else
				{
					Debug.LogWarning($"サポートされないコマンドです．無視されます．{lineNumber}");
				}
			}
			else
				throw new ChartErrorException("ノーツ定義でない不正な行です．", lineNumber);

		}

		Chart()
		{
			Notes = new List<NoteBase>();
			Events = new List<EventBase>();

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
			Add("bpm", (v, l) => Bpm = ParseBpm(v, l));
			Add("beat", (v, l) => Beat = ParseBeat(v, l));
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
				Offset = offset;
			});
		}

		public float ParseBpm(string v, int l)
		{
			float bpm;
			if (!float.TryParse(v, out bpm))
				throw new ChartErrorException("数値でない値を BPM に設定しようとしました．", l);
			return bpm;
		}

		public void ParseSpeed(string v, int l, out float speed, out int tick)
		{
			var arg = v.Split(',');

			if (arg.Length == 2 && float.TryParse(arg[0], out speed) && int.TryParse(arg[1], out tick))
				return;
			else if (arg.Length == 1 && float.TryParse(arg[0], out speed))
			{
				tick = 0;
				return;
			}
			
			throw new ChartErrorException("不正なハイスピード設定です．", l);
		}

		public Beat ParseBeat(string v, int l)
		{
			var beats = v.Split('/');
			int rhythm, note;
			if (beats.Length == 2 && int.TryParse(beats[0], out rhythm) && int.TryParse(beats[1], out note))
				return new Beat(rhythm, note);
			
			    throw new ChartErrorException("不正な拍子設定です．", l);
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