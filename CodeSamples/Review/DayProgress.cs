using UnityEngine;

namespace Project22.ResumeSystem
{
    /// <summary>
    /// Cross-day state for the three day demo. Static on purpose: the demo has a
    /// single playthrough and the values have to survive the scene reload between
    /// days. MainMenuController resets it when a new game starts.
    /// </summary>
    public static class DayProgress
    {
        public static int CurrentDay = 1;

        /// <summary>Day 2: the referral file was waved through. Day 3 audits for it.</summary>
        public static bool ReferralAccepted;

        /// <summary>Day 2: the player actually looked the case up in Linkedout.</summary>
        public static bool ReviewChecked;

        /// <summary>Day 3: the player passed the audited file upward instead of hiding it.</summary>
        public static bool Confessed;

        /// <summary>Set by the day flow so Linkedout can tell whether the review target was checked.</summary>
        public static string ReviewApplicationId;

        public static void NoteReviewLookup(string applicationId)
        {
            if (!string.IsNullOrEmpty(ReviewApplicationId) && ReviewApplicationId == applicationId) ReviewChecked = true;
        }

        public static void ResetForNewGame()
        {
            CurrentDay = 1;
            ReferralAccepted = false;
            ReviewChecked = false;
            Confessed = false;
        }

        public static void Advance()
        {
            CurrentDay++;
            ReviewChecked = false;
        }
    }
}
