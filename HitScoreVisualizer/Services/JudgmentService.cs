using System;
using System.Collections.Generic;
using System.Text;
using HitScoreVisualizer.Extensions;
using HitScoreVisualizer.Settings;
using TMPro;
using UnityEngine;

namespace HitScoreVisualizer.Services
{
	internal class JudgmentService
	{
		private readonly Configuration? _config;

		public JudgmentService(ConfigProvider configProvider)
		{
			_config = configProvider.GetCurrentConfig();
		}

		internal void Judge(IReadonlyCutScoreBuffer cutScoreBuffer, ref TextMeshPro text, ref Color color, int? assumedAfterCutScore)
		{
			if (_config == null)
			{
				return;
			}

			// enable rich text
			text.richText = true;
			// disable word wrap, make sure full text displays
			text.enableWordWrapping = false;
			text.overflowMode = TextOverflowModes.Overflow;

			switch (cutScoreBuffer.noteCutInfo.noteData.scoringType)
			{
				case NoteData.ScoringType.Normal:
				case NoteData.ScoringType.SliderHead:
				case NoteData.ScoringType.SliderTail:
					JudgeInternalNormal(cutScoreBuffer, ref text, ref color, assumedAfterCutScore);
					break;
				case NoteData.ScoringType.BurstSliderHead:
					JudgeInternalBurstSliderHead(cutScoreBuffer, ref text, ref color);
					break;
				case NoteData.ScoringType.BurstSliderElement:
					JudgeInternalBurstSliderElement(cutScoreBuffer, ref text, ref color);
					break;
				case NoteData.ScoringType.Ignore:
				case NoteData.ScoringType.NoScore:
					// NOP
					break;
				default:
					throw new NotSupportedException();
			}
		}

		private void JudgeInternalNormal(IReadonlyCutScoreBuffer cutScoreBuffer, ref TextMeshPro text, ref Color color, int? assumedAfterCutScore)
		{
			var before = cutScoreBuffer.beforeCutScore;
			var after = assumedAfterCutScore ?? cutScoreBuffer.afterCutScore;
			var accuracy = cutScoreBuffer.centerDistanceCutScore;
			var total = before + after + accuracy;
			var timeDependence = Mathf.Abs(cutScoreBuffer.noteCutInfo.cutNormal.z);

			// save in case we need to fade
			var index = _config!.Judgments!.FindIndex(j => j.Threshold <= total);
			var judgment = index >= 0 ? _config.Judgments[index] : Judgment.Default;

			if (judgment.Fade)
			{
				var fadeJudgment = _config.Judgments[index - 1];
				var baseColor = judgment.Color.ToColor();
				var fadeColor = fadeJudgment.Color.ToColor();
				var lerpDistance = Mathf.InverseLerp(judgment.Threshold, fadeJudgment.Threshold, total);
				color = Color.Lerp(baseColor, fadeColor, lerpDistance);
			}
			else
			{
				color = judgment.Color.ToColor();
			}

			text.text = _config.DisplayMode switch
			{
				"format" => DisplayModeFormatNormal(judgment.Text, cutScoreBuffer.noteScoreDefinition, total, before, after, accuracy, timeDependence, _config),
				"textOnly" => judgment.Text,
				"numeric" => total.ToString(),
				"scoreOnTop" => $"{total}\n{judgment.Text}\n",
				_ => $"{judgment.Text}\n{total}\n"
			};
		}

		private void JudgeInternalBurstSliderHead(IReadonlyCutScoreBuffer cutScoreBuffer, ref TextMeshPro text, ref Color color)
		{
			var before = cutScoreBuffer.beforeCutScore;
			var accuracy = cutScoreBuffer.centerDistanceCutScore;
			var total = before + accuracy;
			var timeDependence = Mathf.Abs(cutScoreBuffer.noteCutInfo.cutNormal.z);

			// save in case we need to fade
			var judgementsList = _config!.ChainHeadJudgements ?? _config!.Judgments;
			var index = judgementsList!.FindIndex(j => j.Threshold <= total);
			var judgment = index >= 0 ? judgementsList[index] : Judgment.Default;

			if (judgment.Fade)
			{
				var fadeJudgment = judgementsList[index - 1];
				var baseColor = judgment.Color.ToColor();
				var fadeColor = fadeJudgment.Color.ToColor();
				var lerpDistance = Mathf.InverseLerp(judgment.Threshold, fadeJudgment.Threshold, total);
				color = Color.Lerp(baseColor, fadeColor, lerpDistance);
			}
			else
			{
				color = judgment.Color.ToColor();
			}

			text.text = _config.DisplayMode switch
			{
				"format" => DisplayModeFormatBurstSliderHead(judgment.Text, cutScoreBuffer.noteScoreDefinition, total, before, accuracy, timeDependence, _config),
				"textOnly" => judgment.Text,
				"numeric" => total.ToString(),
				"scoreOnTop" => $"{total}\n{judgment.Text}\n",
				_ => $"{judgment.Text}\n{total}\n"
			};
		}

		private void JudgeInternalBurstSliderElement(IReadonlyCutScoreBuffer cutScoreBuffer, ref TextMeshPro text, ref Color color)
		{
			if (_config!.ChainLinkDisplay == null)
			{
				return;
			}

			var total = cutScoreBuffer.noteScoreDefinition.fixedCutScore; // maybe replace this with cutScoreBuffer.CutScore in case a future game update makes it not fully fixed.
			var timeDependence = Mathf.Abs(cutScoreBuffer.noteCutInfo.cutNormal.z);

			color = _config.ChainLinkDisplay.Color.ToColor();
			text.text = _config.DisplayMode switch
			{
				"format" => DisplayModeFormatBurstSliderElement(_config.ChainLinkDisplay.Text, cutScoreBuffer.noteScoreDefinition, total, timeDependence, _config),
				"textOnly" => _config.ChainLinkDisplay.Text,
				"numeric" => total.ToString(),
				"scoreOnTop" => $"{total}\n{_config.ChainLinkDisplay.Text}\n",
				_ => $"{_config.ChainLinkDisplay.Text}\n{total}\n"
			};
		}

		// TODO: Find a way to partially reuse this logic for
		// ReSharper disable once CognitiveComplexity
		private static string DisplayModeFormatNormal(string formatString, ScoreModel.NoteScoreDefinition noteScoreDefinition, int score, int before, int after, int accuracy, float timeDependence,
			Configuration instance)
		{
			var formattedBuilder = new StringBuilder();
			var nextPercentIndex = formatString.IndexOf('%');
			while (nextPercentIndex != -1)
			{
				formattedBuilder.Append(formatString.Substring(0, nextPercentIndex));
				if (formatString.Length == nextPercentIndex + 1)
				{
					formatString += " ";
				}

				var specifier = formatString[nextPercentIndex + 1];

				switch (specifier)
				{
					case 'b':
						formattedBuilder.Append(before);
						break;
					case 'c':
						formattedBuilder.Append(accuracy);
						break;
					case 'a':
						formattedBuilder.Append(after);
						break;
					case 't':
						formattedBuilder.Append(ConvertTimeDependencePrecision(timeDependence, instance.TimeDependenceDecimalOffset, instance.TimeDependenceDecimalPrecision));
						break;
					case 'B':
						formattedBuilder.Append(JudgeSegment(before, instance.BeforeCutAngleJudgments));
						break;
					case 'C':
						formattedBuilder.Append(JudgeSegment(accuracy, instance.AccuracyJudgments));
						break;
					case 'A':
						formattedBuilder.Append(JudgeSegment(after, instance.AfterCutAngleJudgments));
						break;
					case 'T':
						formattedBuilder.Append(JudgeTimeDependenceSegment(timeDependence, instance.TimeDependenceJudgments, instance));
						break;
					case 's':
						formattedBuilder.Append(score);
						break;
					case 'p':
						formattedBuilder.Append($"{(double) score / noteScoreDefinition.maxCutScore * 100:0}");
						break;
					case '%':
						formattedBuilder.Append("%");
						break;
					case 'n':
						formattedBuilder.Append("\n");
						break;
					default:
						formattedBuilder.Append("%" + specifier);
						break;
				}

				formatString = formatString.Remove(0, nextPercentIndex + 2);
				nextPercentIndex = formatString.IndexOf('%');
			}

			return formattedBuilder.Append(formatString).ToString();
		}

		private static string DisplayModeFormatBurstSliderHead(string formatString, ScoreModel.NoteScoreDefinition noteScoreDefinition, int score, int before, int accuracy, float timeDependence,
			Configuration instance)
		{
			var formattedBuilder = new StringBuilder();
			var nextPercentIndex = formatString.IndexOf('%');
			while (nextPercentIndex != -1)
			{
				formattedBuilder.Append(formatString.Substring(0, nextPercentIndex));
				if (formatString.Length == nextPercentIndex + 1)
				{
					formatString += " ";
				}

				var specifier = formatString[nextPercentIndex + 1];

				switch (specifier)
				{
					case 'b':
						formattedBuilder.Append(before);
						break;
					case 'c':
						formattedBuilder.Append(accuracy);
						break;
					case 't':
						formattedBuilder.Append(ConvertTimeDependencePrecision(timeDependence, instance.TimeDependenceDecimalOffset, instance.TimeDependenceDecimalPrecision));
						break;
					case 'B':
						formattedBuilder.Append(JudgeSegment(before, instance.BeforeCutAngleJudgments));
						break;
					case 'C':
						formattedBuilder.Append(JudgeSegment(accuracy, instance.AccuracyJudgments));
						break;
					case 'T':
						formattedBuilder.Append(JudgeTimeDependenceSegment(timeDependence, instance.TimeDependenceJudgments, instance));
						break;
					case 's':
						formattedBuilder.Append(score);
						break;
					case 'p':
						formattedBuilder.Append($"{(double) score / noteScoreDefinition.maxCutScore * 100:0}");
						break;
					case '%':
						formattedBuilder.Append("%");
						break;
					case 'n':
						formattedBuilder.Append("\n");
						break;
					default:
						formattedBuilder.Append("%" + specifier);
						break;
				}

				formatString = formatString.Remove(0, nextPercentIndex + 2);
				nextPercentIndex = formatString.IndexOf('%');
			}

			return formattedBuilder.Append(formatString).ToString();
		}

		private static string DisplayModeFormatBurstSliderElement(string formatString, ScoreModel.NoteScoreDefinition noteScoreDefinition, int score, float timeDependence,
			Configuration instance)
		{
			var formattedBuilder = new StringBuilder();
			var nextPercentIndex = formatString.IndexOf('%');
			while (nextPercentIndex != -1)
			{
				formattedBuilder.Append(formatString.Substring(0, nextPercentIndex));
				if (formatString.Length == nextPercentIndex + 1)
				{
					formatString += " ";
				}

				var specifier = formatString[nextPercentIndex + 1];

				switch (specifier)
				{
					case 't':
						formattedBuilder.Append(ConvertTimeDependencePrecision(timeDependence, instance.TimeDependenceDecimalOffset, instance.TimeDependenceDecimalPrecision));
						break;
					case 'T':
						formattedBuilder.Append(JudgeTimeDependenceSegment(timeDependence, instance.TimeDependenceJudgments, instance));
						break;
					case 's':
						formattedBuilder.Append(score);
						break;
					case 'p':
						formattedBuilder.Append($"{(double) score / noteScoreDefinition.maxCutScore * 100:0}");
						break;
					case '%':
						formattedBuilder.Append("%");
						break;
					case 'n':
						formattedBuilder.Append("\n");
						break;
					default:
						formattedBuilder.Append("%" + specifier);
						break;
				}

				formatString = formatString.Remove(0, nextPercentIndex + 2);
				nextPercentIndex = formatString.IndexOf('%');
			}

			return formattedBuilder.Append(formatString).ToString();
		}

		private static string JudgeSegment(int scoreForSegment, IList<JudgmentSegment>? judgments)
		{
			if (judgments == null)
			{
				return string.Empty;
			}

			foreach (var j in judgments)
			{
				if (scoreForSegment >= j.Threshold)
				{
					return j.Text ?? string.Empty;
				}
			}

			return string.Empty;
		}

		private static string JudgeTimeDependenceSegment(float scoreForSegment, IList<TimeDependenceJudgmentSegment>? judgments, Configuration instance)
		{
			if (judgments == null)
			{
				return string.Empty;
			}

			foreach (var j in judgments)
			{
				if (scoreForSegment >= j.Threshold)
				{
					return FormatTimeDependenceSegment(j, scoreForSegment, instance);
				}
			}

			return string.Empty;
		}

		private static string FormatTimeDependenceSegment(TimeDependenceJudgmentSegment? judgment, float timeDependence, Configuration instance)
		{
			if (judgment == null)
			{
				return string.Empty;
			}

			var formattedBuilder = new StringBuilder();
			var formatString = judgment.Text ?? string.Empty;
			var nextPercentIndex = formatString.IndexOf('%');
			while (nextPercentIndex != -1)
			{
				formattedBuilder.Append(formatString.Substring(0, nextPercentIndex));
				if (formatString.Length == nextPercentIndex + 1)
				{
					formatString += " ";
				}

				var specifier = formatString[nextPercentIndex + 1];

				switch (specifier)
				{
					case 't':
						formattedBuilder.Append(ConvertTimeDependencePrecision(timeDependence, instance.TimeDependenceDecimalOffset, instance.TimeDependenceDecimalPrecision));
						break;
					case '%':
						formattedBuilder.Append("%");
						break;
					case 'n':
						formattedBuilder.Append("\n");
						break;
					default:
						formattedBuilder.Append("%" + specifier);
						break;
				}

				formatString = formatString.Remove(0, nextPercentIndex + 2);
				nextPercentIndex = formatString.IndexOf('%');
			}

			return formattedBuilder.Append(formatString).ToString();
		}

		private static string ConvertTimeDependencePrecision(float timeDependence, int decimalOffset, int decimalPrecision)
		{
			var multiplier = Mathf.Pow(10, decimalOffset);
			return (timeDependence * multiplier).ToString($"n{decimalPrecision}");
		}
	}
}