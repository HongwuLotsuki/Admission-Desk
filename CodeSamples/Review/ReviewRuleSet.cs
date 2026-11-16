using UnityEngine;

namespace Project22.ResumeSystem
{
    [CreateAssetMenu(menuName = "Project 22/Review Rules", fileName = "Day1ReviewRules")]
    public sealed class ReviewRuleSet : ScriptableObject
    {
        [Min(1)] [SerializeField] private int maximumSchoolRank = 100;
        [Range(0f, 4f)] [SerializeField] private float minimumGpa = 3f;

        public int MaximumSchoolRank => maximumSchoolRank;
        public float MinimumGpa => minimumGpa;

        public bool ShouldAccept(ResumeData resume, int schoolRank)
        {
            return resume != null && schoolRank > 0 && schoolRank <= maximumSchoolRank &&
                   resume.TryGetGpa(out float gpa) && gpa >= minimumGpa && !HasDisputedRecord(resume);
        }

        /// <summary>From Day 2 on, a Linkedout record that does not match the paper blocks acceptance.</summary>
        public static bool HasDisputedRecord(ResumeData resume)
        {
            var records = resume != null ? resume.Linkedout : null;
            if (records == null) return false;
            foreach (var record in records)
                if (record != null && record.Status != LinkedoutStatus.Confirmed) return true;
            return false;
        }

        public string Explain(ResumeData resume, int schoolRank)
        {
            var content = DayOneContent.Current;
            if (resume == null) return content.Text("rule.missing");
            if (schoolRank <= 0) return content.Text("rule.schoolUnknown");
            if (schoolRank > maximumSchoolRank) return string.Format(content.Text("rule.rank"), schoolRank, maximumSchoolRank);
            if (!resume.TryGetGpa(out float gpa)) return content.Text("rule.gpaUnknown");
            if (gpa < minimumGpa) return string.Format(content.Text("rule.gpa"), gpa, minimumGpa);
            if (HasDisputedRecord(resume)) return content.Text("rule.linkedout");
            return content.Text("rule.pass");
        }
    }
}
