using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Xeltica.BeatBall
{

	/// <summary>
	/// 国際化のための機能を提供します．
	/// </summary>
	public class I18nProvider : Singleton<I18nProvider>
	{
		[SerializeField]
		private string lang = "en";

		public string Language
		{
			get { return lang; }
			set { lang = value; }
		}

		public Dictionary<string, Dictionary<string, string>> Locales { get; private set; }

		public Dictionary<string, string> CurrentLang => Locales.ContainsKey(Language) ? Locales[Language] : null;

		protected override void Awake()
		{
			base.Awake();
			var localeAssets = Resources.LoadAll("lang", typeof(TextAsset)).Cast<TextAsset>();
			Locales = new Dictionary<string, Dictionary<string, string>>();
			foreach (var asset in localeAssets)
			{
				Locales[asset.name] = ParseLangFile(asset.text);
			}
		}
		/// <summary>
		/// 翻訳された文字列を取得します．
		/// </summary>
		/// <param name="key">Key.</param>
		public string this[string key] => CurrentLang != null ? (CurrentLang.ContainsKey(key) ? CurrentLang[key] : key) : key;

		/// <summary>
		/// 言語ファイルのパースを行います．
		/// </summary>
		private Dictionary<string, string> ParseLangFile(string text)
		{
			var dict = new Dictionary<string, string>();
			foreach (var kv in ToLFString(text).Split('\n'))
			{
				// 空行やおかしい行は飛ばす
				if (string.IsNullOrWhiteSpace(kv) || !kv.Contains(':'))
					continue;

				var split = kv.Split(':');

				// 構文がおかしいやつは飛ばす
				if (split.Length < 2)
					continue;

				// 辞書にぶちこむ(2つ目の:を考慮する)
				dict[split[0].Trim()] = string.Concat(split.Skip(1));
			}

			return dict;
		}
		/// <summary>
		/// 改行コードを統一した文字列に変換します．
		/// </summary>
		/// <returns>改行コードをLine Feedに統一した文字列．</returns>
		/// <param name="str">変換すべき文字列．</param>
		private string ToLFString(string str) => str.Replace("\r\n", "\n").Replace('\r', '\n');
	}

}