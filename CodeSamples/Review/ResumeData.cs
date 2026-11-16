using System;
using UnityEngine;

namespace Project22.ResumeSystem
{
    public enum CandidateGender
    {
        Unspecified,
        Female,
        Male,
        NonBinary
    }

    public enum LinkedoutStatus
    {
        Confirmed,
        Mismatch,
        Missing
    }

    /// <summary>
    /// What Linkedout reports for one employment record. Day 1 resumes leave this
    /// empty; Day 2 adds it so the player can compare the paper against the record.
    /// </summary>
    [Serializable]
    public sealed class LinkedoutRecord
    {
        [SerializeField] private string period;
        [SerializeField] private string organization;
        [SerializeField] private string position;
        [SerializeField] private LinkedoutStatus status;
        [TextArea(2, 4)]
        [SerializeField] private string note;

        public string Period => period;
        public string Organization => organization;
        public string Position => position;
        public LinkedoutStatus Status => status;
        public string Note => note;
    }

    [Serializable]
    public sealed class ResumeExperience
    {
        [SerializeField] private string period;
        [SerializeField] private string organization;
        [SerializeField] private string position;
        [TextArea(2, 4)]
        [SerializeField] private string description;

        public string Period => period;
        public string Organization => organization;
        public string Position => position;
        public string Description => description;
    }

    [CreateAssetMenu(
        fileName = "Resume_NewCandidate",
        menuName = "Project 22/Resume Data",
        order = 0)]
    public sealed class ResumeData : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField] private string candidateName;
        [SerializeField] private string intendedProgram;
        [SerializeField] private CandidateGender gender;
        [Min(0)]
        [SerializeField] private int age;
        [SerializeField] private string applicationId;
        [SerializeField] private string email;
        [SerializeField] private string education;
        [SerializeField] private string phone;
        [SerializeField] private string location;
        [SerializeField] private string schoolName;
        [SerializeField] private string schoolId;
        [SerializeField, Min(1)] private int schoolRank = 100;
        [SerializeField] private string gpa;
        [SerializeField, Range(0f, 4f)] private float gpaValue;
        [SerializeField] private string relevantCoursework;
        [SerializeField] private Sprite portrait;

        [Header("Resume Content")]
        [SerializeField] private ResumeExperience[] experiences = Array.Empty<ResumeExperience>();
        [SerializeField] private string[] skills = Array.Empty<string>();
        [SerializeField] private string[] activities = Array.Empty<string>();
        [SerializeField] private string[] relationships = Array.Empty<string>();
        [SerializeField] private string[] specialTraits = Array.Empty<string>();
        [Header("Linkedout")]
        [SerializeField] private LinkedoutRecord[] linkedout = Array.Empty<LinkedoutRecord>();
        [TextArea(3, 8)]
        [SerializeField] private string personalStatement;

        [Header("Game Logic")]
        [TextArea(2, 5)]
        [SerializeField] private string internalNotes;
        [SerializeField] private bool shouldBeAccepted;

        public string CandidateName => candidateName;
        public CandidateGender Gender => gender;
        public string IntendedProgram => intendedProgram;
        public int Age => age;
        public string Education => education;
        public string ApplicationId => applicationId;
        public string Email => email;
        public string Phone => phone;
        public string Location => location;
        public string SchoolId => schoolId;
        public int MigrationSchoolRank => schoolRank;
        public string SchoolName => DayOneContent.Current?.FindSchool(schoolId)?.name ?? schoolName;
        public int SchoolRank => DayOneContent.Current?.FindSchool(schoolId)?.rank ?? 0;
        public string Gpa => gpa;
        public float GpaValue => gpaValue;
        public bool TryGetGpa(out float value)
        {
            value = gpaValue;
            if (value > 0f) return true;
            return float.TryParse(gpa, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out value);
        }
        public string RelevantCoursework => relevantCoursework;
        public Sprite Portrait => portrait;
        public ResumeExperience[] Experiences => experiences;
        public string[] Skills => skills;
        public string[] Activities => activities;
        public string[] Relationships => relationships;
        public string[] SpecialTraits => specialTraits;
        public LinkedoutRecord[] Linkedout => linkedout;
        public string PersonalStatement => personalStatement;
        public string InternalNotes => internalNotes;
        public bool ShouldBeAccepted => shouldBeAccepted;
    }
}
