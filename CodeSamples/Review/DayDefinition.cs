using UnityEngine;

namespace Project22.ResumeSystem
{
    /// <summary>
    /// Everything that changes between workdays: which applications arrive, which
    /// rule set decides them, which mails are delivered and which supervisor lines
    /// are used. The flow controller reads one of these per day, so Day 2 and Day 3
    /// run on the same code path as Day 1.
    /// </summary>
    [CreateAssetMenu(fileName = "Day1", menuName = "Project 22/Day Definition", order = 0)]
    public sealed class DayDefinition : ScriptableObject
    {
        [Header("Identity")]
        public int dayNumber = 1;
        [Tooltip("Day 1 only: enables the step-by-step tutorial gates.")]
        public bool tutorial = true;
        [Tooltip("Day 1 only: plays the three intro screens before the office.")]
        public bool showIntro = true;
        [Tooltip("Deliver the handbook only after the welcome mail is read, like Day 1.")]
        public bool gateMails = true;
        [Tooltip("Show the Linkedout shortcut and hand it this day's applicants.")]
        public bool linkedout;

        [Header("Content")]
        public ReviewRuleSet rules;
        public ResumeData[] morningResumes = new ResumeData[0];
        public ResumeData[] afternoonResumes = new ResumeData[0];
        [Tooltip("Files from earlier days that Linkedout must still be able to look up.")]
        public ResumeData[] linkedoutExtra = new ResumeData[0];

        [Header("Mail")]
        [Tooltip("Delivered as soon as the workday starts when gateMails is off.")]
        public string[] startMails = new string[0];
        [Tooltip("Delivered when the afternoon begins, before the application mails.")]
        public string[] afternoonMails = new string[0];
        public string welcomeMailId = "welcome";
        public string handbookMailId = "rules";

        [Header("Dialogue")]
        public string openingChatId = "boss.welcome";
        public string handbookChatId = "boss.first";
        public string middayChatId = "midday";
        public string afternoonChatId = "boss.afternoon";
        public string dayEndGoodId = "boss.end.good";
        public string dayEndReviewId = "boss.end.review";
        public string dayEndSummaryId = "dayend.summary";

        [Header("Day 3")]
        [Tooltip("Closing text shown after the audit instead of a day-end summary.")]
        public string auditEndingId = "d3.end";

        /// <summary>The referral file the dean's office pushes; accepting it is a violation.</summary>
        public string referralApplicationId;
        /// <summary>The accepted file the player is asked to re-check in Linkedout.</summary>
        public string reviewApplicationId;

        public int Total => (morningResumes != null ? morningResumes.Length : 0) +
                            (afternoonResumes != null ? afternoonResumes.Length : 0);

        public static DayDefinition For(int day) => Resources.Load<DayDefinition>("Days/Day" + day);

        public static DayDefinition Current => For(DayProgress.CurrentDay);
    }
}
