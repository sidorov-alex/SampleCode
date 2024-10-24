using System;
using System.Collections.Generic;
using System.Linq;

namespace SekuraTest
{
	/// <summary>
	/// Содержит метода парсинга текста.
	/// </summary>
	public class TextParser
	{
		private static string[] SplitSourceText(string text) =>
			text.Split(new char[] { ' ', }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		public static int CalcMinWordsDistance(string text, IList<string> wordList)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));
						
			if (wordList == null)
				throw new ArgumentNullException(nameof(wordList));

			if (text.Length == 0)
				return -1;

			if (wordList.Count == 0)
				return -1;

			// Разделяем исходный текст на слова.

			var sourceWords = SplitSourceText(text);

			return CalcMinWordsDistance(sourceWords, wordList);
		}
		
		/// <summary>
		/// Подсчитывает наикратчайшее расстояние в словах между перечисленными искомыми словами.
		/// </summary>
		/// <param name="sourceWords">Список исходных слов в тексте, в котором необходимо выполнить подсчет.</param>
		/// <param name="wordList">Список искомых слов.</param>
		/// <returns>Возвращает наикратчайшее расстояние в словах, либо -1, если искомые слова не встречаются ни разу.</returns>
		/// <remarks>Не учитываются регистр слов и порядок слов.</remarks>
		public static int CalcMinWordsDistance(IList<string> sourceWords, IList<string> wordList)
		{
			if (sourceWords == null)
				throw new ArgumentNullException(nameof(sourceWords));

			if (wordList == null)
				throw new ArgumentNullException(nameof(wordList));

			if (sourceWords.Count == 0)
				return -1;

			if (wordList.Count == 0)
				return -1;
						
			// Если в списке искомых слов только одно слово, то просто проверяем его наличие в тексте
			// и возвращаем 0, если оно есть.

			if (wordList.Count == 1)
				return sourceWords.Contains(wordList[0], StringComparer.CurrentCultureIgnoreCase) ? 0 : -1;

			// Удаляем повторяющиеся искомые слова.

			wordList = wordList.Distinct(StringComparer.CurrentCultureIgnoreCase).ToArray();

			// Идем по списку слов в тексте и ищем нужные слова, запоминая позицию каждого найденного.
			// Как только мы найдем полный набор слов, в любом порядке, считаем дистанцию между первым и последним.
			// Возвращаем минимальную дистанцию из всех возможных наборов.

			int minDistance = -1;
			var matches = new List<int>(wordList.Count);
			
			for (int i = 0; i < sourceWords.Count; )
			{
				// Если слово совпадает и его еще нет в списке, то добавляем его.

				if (wordList.Contains(sourceWords[i], StringComparer.CurrentCultureIgnoreCase))
				{
					if (!matches.Any(wIndex => String.Compare(sourceWords[wIndex], sourceWords[i], ignoreCase: true) == 0))
					{
						matches.Add(i);
					}
				}

				if (matches.Count == wordList.Count)
				{
					int distance = matches.Last() - matches.First();

					// Берем минимальную дистанцию.

					if (minDistance == -1 || distance < minDistance)
						minDistance = distance;

					// Начинаем со второго найденного слова, т. к. возможно мы найдем более короткую цепочку.

					i = matches[1];
					matches.Clear();
				}
				else
					i++;
			}

			return minDistance;
		}
	}
}
