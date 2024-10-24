using System;
using System.Collections.Generic;
using System.Linq;

namespace Lers.Data
{
	/// <summary>
	/// Содержит методы для осреднения значений профиля мощности.
	/// </summary>
	static class PowerAveraging
	{
		private static readonly IEnumerable<DataParameter> ParamsToAverage = DataParameterDescriptor.ElectricPowerParams.Select(x => x.DataParameter);

		public static ElectricPowerRecord[] Average(IList<ElectricPowerRecord> data, ElectricPowerAveraging averagingInterval, short? electricPowerAveragingInterval)
		{
			if (data.Count == 0)
				return new ElectricPowerRecord[0];

			var list = new List<ElectricPowerRecord>();

			// Временные переменные для формирования следующей результирующей записи.
			var temp = new Dictionary<DataParameter, DataProperties>();

			foreach (var param in ParamsToAverage)
			{
				temp[param] = null;
			}

			int sourceRecordIndex = 0;
			int currentInterval = 0;
			ElectricPowerRecord record = data[0];

			// Получаем требуемый интервал в минутах.

			short requiredInterval = GetRequiredInterval(averagingInterval, electricPowerAveragingInterval);

			// Округляем дату и время до часов или до суток.

			DateTime dateTime = RoundDateTime(record.DateTime, averagingInterval);

			while (true)
			{
				// Определяем разрыв. Если следующая запись находится в другом часе, то
				// завершаем формирование текущей записи профиля и создаем новую.

				if ((record.DateTime - dateTime).TotalMinutes >= requiredInterval)
				{
					SaveNewRecord(list, temp, dateTime, currentInterval, requiredInterval);

					currentInterval = 0;
					dateTime = RoundDateTime(record.DateTime, averagingInterval);
				}

				// Величина в минутах, которая будет добавлена к текущему интервалу.
				// Если мы не выходим за требуемый интервал, то добавляем полное значение из записи, иначе добавляем разницу,
				// чтобы длина текущего интервала получилась равная длине требуемого интервала.

				int additive;

				if (currentInterval + record.Interval <= requiredInterval)
					additive = record.Interval;
				else
					additive = requiredInterval - currentInterval;

				currentInterval += additive;
				record.Interval -= (short)additive;

				// Вычисляем значения на этой итерации.

				foreach (var param in ParamsToAverage)
				{
					var paramValue = record.GetParam(param);

					if (paramValue != null)
					{
						if (temp[param] == null)
						{
							temp[param] = new DataProperties(0);
						}

						temp[param].Value = temp[param].Value + paramValue.Value * additive;

						// Если значение параметра недостоверно, отмечаем всю усреднённую запись как недостоверную.

						if (paramValue.IsBad)
						{
							temp[param].IsBad = true;
						}
					}
				}

				// Если получили требуемый интервал, то формируем запись, затем переходим
				// к формированию новой.

				// Не формируем интервал, если данные не полные.
				// http://bt.lers.ru/browse/LERSU-3893

				if (currentInterval == requiredInterval/* || i == data.Length - 1*/)
				{
					SaveNewRecord(list, temp, dateTime, currentInterval, requiredInterval);

					currentInterval = 0;
					dateTime = dateTime.AddMinutes(requiredInterval);
				}

				// Если использовали всю запись - переходим к следующей.
				// Если это была последняя запись, то выходим из цикла.

				if (record.Interval == 0)
				{
					if (sourceRecordIndex == data.Count - 1)
					{
						break;
					}

					record = data[++sourceRecordIndex];
				}
			}

			return list.ToArray();
		}

		private static void SaveNewRecord(IList<ElectricPowerRecord> list, Dictionary<DataParameter, DataProperties> temp, DateTime dateTime,
			int currentInterval,
			short requiredInterval)
		{
			var newRecord = new ElectricPowerRecord
			{
				DateTime = dateTime,
				Interval = (short)currentInterval
			};

			foreach (var param in ParamsToAverage)
			{
				var tempValue = temp[param];
				if (tempValue != null)
				{
					tempValue.Value = tempValue.Value / requiredInterval;

					newRecord.SetParam(param, tempValue);
				}

				temp[param] = null;
			}

			list.Add(newRecord);
		}

		private static short GetRequiredInterval(ElectricPowerAveraging averagingInterval, short? electricPowerAveragingInterval)
		{
			short requiredInterval;

			switch (averagingInterval)
			{
				case ElectricPowerAveraging.HalfHourly:
					requiredInterval = 30;
					break;

				case ElectricPowerAveraging.Hourly:
					requiredInterval = 60;
					break;

				case ElectricPowerAveraging.Daily:
					requiredInterval = 60 * 24;
					break;

				default:
					if (!electricPowerAveragingInterval.HasValue || electricPowerAveragingInterval.Value == 0)
					{
						throw new ArgumentOutOfRangeException(nameof(electricPowerAveragingInterval), electricPowerAveragingInterval, "Интервал осреднения задан неверно.");
					}

					requiredInterval = electricPowerAveragingInterval.Value;
					break;
			}

			return requiredInterval;
		}

		private static DateTime RoundDateTime(DateTime dateTime, ElectricPowerAveraging averagingInterval)
		{
			if (averagingInterval == ElectricPowerAveraging.HalfHourly)
			{
				// Все что больше 30 минут относится ко второму получасу, что меньше - к первому.

				if (dateTime.Minute >= 30)
					return dateTime.Date.AddHours(dateTime.Hour).AddMinutes(30);
				else
					return dateTime.Date.AddHours(dateTime.Hour);
			}
			else if (averagingInterval == ElectricPowerAveraging.Hourly)
			{
				return dateTime.Date.AddHours(dateTime.Hour);
			}
			else
			{
				return dateTime.Date;
			}
		}
	}
}
