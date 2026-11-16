# Code Samples

Selected scripts from **Admission Desk**, a solo retro pixel-art desk-inspection game about reviewing university applications. The full Unity project is not published because it contains third-party art and audio assets.

> **AI assistance:** Some code was written with AI assistance. I was responsible for the design, integration and debugging.

## What to read first

| Feature | File | Highlights |
|---|---|---|
| Application data | [`Review/ResumeData.cs`](Review/ResumeData.cs) | A `ScriptableObject` per applicant: personal details, education, experience, skills, relationships, hidden internal notes, and per-record Linkedout results. A tolerant GPA parser handles values typed as text. |
| Review rules | [`Review/ReviewRuleSet.cs`](Review/ReviewRuleSet.cs) | Rules live in an asset (rank limit, minimum GPA) and one method both decides and explains a result, so the decision and the feedback shown to the player can never disagree. From Day 2, an unconfirmed Linkedout record blocks acceptance. |
| Data-driven days | [`Review/DayDefinition.cs`](Review/DayDefinition.cs) | One asset per workday defines the applicant queues, rule set, mails, supervisor dialogue and flags. Day 2 and Day 3 run on the same code path as Day 1. |
| Cross-day state | [`Review/DayProgress.cs`](Review/DayProgress.cs) | Small static state that survives the scene reload between days (referral accepted, record checked, confessed), with an explicit reset for a new game. |
| Day flow state machine | [`Review/FirstDayFlowController.cs`](Review/FirstDayFlowController.cs) | Drives a workday: intro, mail gating, tutorial gates, morning and afternoon queues, decisions, submission and the day-end summary. |
| Document picking and dragging | [`Input/ResumeRaycastController.cs`](Input/ResumeRaycastController.cs), [`Input/ResumeDraggable.cs`](Input/ResumeDraggable.cs) | New Input System pointer (mouse, touch or pen). A 2D ray picks the overlapping paper with the highest sorting order, ignores clicks that land on UI, and keeps the grab offset so the paper never snaps to the cursor. |
| Runtime localization | [`Localization/LocalizationService.cs`](Localization/LocalizationService.cs) | English and Simplified Chinese catalogs loaded from `Resources`, a saved language choice, a language-changed event, safe formatting with a fallback, and a reset hook for fast play-mode entry. |

## Design notes

**Content as data.** Applicants, rules and whole workdays are assets, not code. Adding a day means creating a `DayDefinition` and a few `ResumeData` assets, with no new controller. This kept three days of content cheap to build and easy to rebalance.

**One source for decision and explanation.** `ShouldAccept` and `Explain` check the same conditions in the same order, so the "why was this wrong" feedback always matches the actual rule.

**Cross-day state on purpose.** The three-day demo is a single playthrough, so a small static class carries the story flags across scene loads, with one reset on new game. For a longer game I would move this into a saved game-state object.

**Known limitation.** `FirstDayFlowController` has grown into a large class that handles flow, tutorial gating, mail and chat. If I continued the project I would split it into a day-flow state machine and separate presenters.

## Dependencies

These scripts reference classes that are not included, so they will not compile on their own: `DayOneContent`, `DayOneView`, `MailCatalog`, `ComputerUIController`, `DocumentPresentationController`, `WorkInput` and others. Treat them as reading material.

Unity 6000.3.19f1 · URP 2D · Input System
