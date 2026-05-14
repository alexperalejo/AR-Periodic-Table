using UnityEngine;

namespace PeriodicAR.Quiz
{
    /// <summary>PlayerPrefs-backed quiz score tracking.</summary>
    public static class QuizScoreTracker
    {
        private const string KeyCorrect   = "quiz_correct_total";
        private const string KeyAttempted = "quiz_attempted_total";
        private const string KeyStreak    = "quiz_correct_streak";

        public static int Correct   => PlayerPrefs.GetInt(KeyCorrect,   0);
        public static int Attempted => PlayerPrefs.GetInt(KeyAttempted, 0);
        public static int Streak    => PlayerPrefs.GetInt(KeyStreak,    0);

        public static float Accuracy => Attempted > 0 ? (float)Correct / Attempted : 0f;

        public static void RecordCorrect()
        {
            PlayerPrefs.SetInt(KeyCorrect,   Correct   + 1);
            PlayerPrefs.SetInt(KeyAttempted, Attempted + 1);
            PlayerPrefs.SetInt(KeyStreak,    Streak    + 1);
            PlayerPrefs.Save();
        }

        public static void RecordWrong()
        {
            PlayerPrefs.SetInt(KeyAttempted, Attempted + 1);
            PlayerPrefs.SetInt(KeyStreak,    0);
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(KeyCorrect);
            PlayerPrefs.DeleteKey(KeyAttempted);
            PlayerPrefs.DeleteKey(KeyStreak);
            PlayerPrefs.Save();
        }
    }
}
