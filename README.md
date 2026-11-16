# Admission Desk

A retro pixel-art admissions simulation. You review university applications at a desk, under pressure to hit your targets.

**▶ Play:** [Download the Windows build from Releases](https://github.com/HongwuLotsuki/Admission-Desk/releases/tag/1.0.0)

## Concept

Papers arrive on your desk. Read each application, check the GPA, look up the school's rank, compare the paper with the Linkedout records on your computer, and decide to accept or reject. The handbook has rules, but your supervisor, the dean's office and your own conscience push in other directions across a three-day demo.

## My role

Solo project: design, programming, UI and builds.

## Code samples

Selected scripts are in [`CodeSamples/`](CodeSamples/), with notes on the design. The full project contains third-party assets, so it is not published.

| Feature | File |
|---|---|
| Applicant data asset | [ResumeData.cs](CodeSamples/Review/ResumeData.cs) |
| Review rules and explanations | [ReviewRuleSet.cs](CodeSamples/Review/ReviewRuleSet.cs) |
| Data-driven workdays | [DayDefinition.cs](CodeSamples/Review/DayDefinition.cs) |
| Document picking and dragging | [ResumeRaycastController.cs](CodeSamples/Input/ResumeRaycastController.cs) |
| Runtime localization | [LocalizationService.cs](CodeSamples/Localization/LocalizationService.cs) |

> Some code was written with AI assistance. I was responsible for the design, integration and debugging.

## Tech

Unity 6000.3.19f1 · URP 2D · Input System

## Portfolio

[HongwuLotsuki.github.io](https://hongwulotsuki.github.io/)
