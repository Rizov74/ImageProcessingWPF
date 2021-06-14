using ImageProcessingWPF.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace ImageProcessingWPF.ViewModels
{
    public class StringComparatorViewModel : ObservableObject
    {
        public ICommand CompareCommand { get; }
        public string InputLeft { get => inputLeft; set => SetValue(ref inputLeft, value); }
        public string InputRight { get => inputRight; set => SetValue(ref inputRight, value); }
        public string ResultComment { get => resultComment; set => SetValue(ref resultComment, value); }

        private string inputLeft;
        private string inputRight;
        private string resultComment;

        public StringComparatorViewModel()
        {
            CompareCommand = new RelayCommand(p => CompareString());
        }

        private void CompareString()
        {
            ResultComment = string.Empty;
            if (string.IsNullOrEmpty(InputLeft) || string.IsNullOrEmpty(InputRight))
            {
                ResultComment = "Input empty";
                return;
            }

            var splitLeft = InputLeft.Split(';');
            var splitRight = InputRight.Split(';');

            if (splitLeft.Length != splitRight.Length)
            {
                ResultComment = "inputs should have same parts, splitted by ;";
                return;
            }

            List<double> scores = new List<double>();
            for (int i = 0; i < splitLeft.Length; i++)
            {
                var left = CleanAgentName(splitLeft[i]);
                var right = CleanAgentName(splitRight[i]);

                var score = 1d - Convert.ToDouble((LongestCommonSubsequence(left, right).Length) / Convert.ToDouble(Math.Min(left.Length, right.Length)));
                scores.Add(score);
            }

            for (int i = 0; i < scores.Count; i++)
            {
                ResultComment += $"part{i} : {scores[i]}\n";
            }
            ResultComment += $"Mean Score : {scores.Average()}";


        }

        private static string CleanAgentName(string name)
        {
            var tmp = name.Replace(",", " ").Replace("-", " ").Replace("&", " ").Replace("'", "");
            tmp = Regex.Replace(tmp, @"\bestate\b", " ", RegexOptions.IgnoreCase);
            tmp = Regex.Replace(tmp, @"\bagents*\b", " ", RegexOptions.IgnoreCase);
            tmp = Regex.Replace(tmp, @"\bagency\b", " ", RegexOptions.IgnoreCase);
            tmp = Regex.Replace(tmp, @"\band\b", " ", RegexOptions.IgnoreCase);
            tmp = Regex.Replace(tmp, @"\buk\b", " ", RegexOptions.IgnoreCase);
            tmp = Regex.Replace(tmp, @"\bengland\b", " ", RegexOptions.IgnoreCase);
            tmp = Regex.Replace(tmp, @"\bltd\b", " ", RegexOptions.IgnoreCase);
            tmp = Regex.Replace(tmp, @"\blpp\b", " ", RegexOptions.IgnoreCase);
            tmp = Regex.Replace(tmp, @"\bco\b", " ", RegexOptions.IgnoreCase);
            tmp = Regex.Replace(tmp, @"\blettings\b", " ", RegexOptions.IgnoreCase);
            tmp = Regex.Replace(tmp, @"[ ]{1,}", "", RegexOptions.None);
            return tmp.Trim();
        }

        private static string LongestCommonSubsequence(string source, string target)
        {
            int[,] C = LongestCommonSubsequenceLengthTable(source, target);

            return Backtrack(C, source, target, source.Length, target.Length);
        }

        private static int[,] LongestCommonSubsequenceLengthTable(string source, string target)
        {
            int[,] C = new int[source.Length + 1, target.Length + 1];

            for (int i = 0; i < source.Length + 1; i++) { C[i, 0] = 0; }
            for (int j = 0; j < target.Length + 1; j++) { C[0, j] = 0; }

            for (int i = 1; i < source.Length + 1; i++)
            {
                for (int j = 1; j < target.Length + 1; j++)
                {
                    if (source[i - 1].Equals(target[j - 1]))
                    {
                        C[i, j] = C[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        C[i, j] = Math.Max(C[i, j - 1], C[i - 1, j]);
                    }
                }
            }

            return C;
        }

        private static string Backtrack(int[,] C, string source, string target, int i, int j)
        {
            if (i == 0 || j == 0)
            {
                return "";
            }
            else if (source[i - 1].Equals(target[j - 1]))
            {
                return Backtrack(C, source, target, i - 1, j - 1) + source[i - 1];
            }
            else
            {
                if (C[i, j - 1] > C[i - 1, j])
                {
                    return Backtrack(C, source, target, i, j - 1);
                }
                else
                {
                    return Backtrack(C, source, target, i - 1, j);
                }
            }
        }
    }
}
