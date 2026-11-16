using System.Collections.Generic;
using System.Linq;
using Project22.Computer;
using Project22.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Project22.ResumeSystem
{
    public sealed class FirstDayFlowController : MonoBehaviour
    {
        [SerializeField] private ResumeSessionController session;
        [SerializeField] private ResumeSpawner spawner;
        [SerializeField] private ReviewRuleSet rules;
        [SerializeField] private ResumeData[] morningResumes = new ResumeData[5];
        [SerializeField] private ResumeData[] afternoonResumes = new ResumeData[3];
        [SerializeField] private ComputerUIController computer;
        [SerializeField] private DayOneView view;
        [SerializeField] private DayOneTutorialController tutorial;
        [SerializeField] private DocumentPresentationController documentPresentation;
        private DayOneContent content;
        private MailCatalog mailCatalog;
        private DayDefinition day;
        private readonly List<ReviewResult> results = new();
        private readonly HashSet<string> submitted = new();
        private ComputerScreenPresenter presenter;
        private WorkPhase phase;
        private bool paused, initialized;
        private bool? decision;
        private int introIndex, index;
        private float held, fade;
        public WorkPhase Phase => phase;
        public IReadOnlyList<ReviewResult> Results => results;
        private string T(string key) => content.Text(key);
        private ResumeData Current => session.CurrentResume;

        // Day 1 keeps its scene-serialized lists; Day 2 and Day 3 come from a
        // DayDefinition asset so the whole day is data.
        private ResumeData[] Morning => day != null && day.morningResumes.Length > 0 ? day.morningResumes : morningResumes;
        private ResumeData[] Afternoon => day != null && day.afternoonResumes.Length > 0 ? day.afternoonResumes : afternoonResumes;
        private ReviewRuleSet ActiveRules => day != null && day.rules != null ? day.rules : rules;
        private int DayTotal => Morning.Length + Afternoon.Length;
        private bool TutorialActive => day == null || day.tutorial;
        private bool GateMails => day == null || day.gateMails;
        private string WelcomeMailId => day != null ? day.welcomeMailId : "welcome";
        private string HandbookMailId => day != null ? day.handbookMailId : "rules";
        private string OpeningChat => day != null ? day.openingChatId : "boss.welcome";
        private string HandbookChat => day != null ? day.handbookChatId : "boss.first";
        private string MiddayChat => day != null ? day.middayChatId : "midday";
        private string AfternoonChat => day != null ? day.afternoonChatId : "boss.afternoon";
        private string DayEndGoodChat => day != null ? day.dayEndGoodId : "boss.end.good";
        private string DayEndReviewChat => day != null ? day.dayEndReviewId : "boss.end.review";
        private string SummaryId => day != null ? day.dayEndSummaryId : "dayend.summary";

        /// <summary>Applicants Linkedout can answer for: this day's queue plus any older files.</summary>
        private ResumeData[] LinkedoutCandidates()
        {
            var list = new List<ResumeData>();
            list.AddRange(Morning);
            list.AddRange(Afternoon);
            if (day != null && day.linkedoutExtra != null) list.AddRange(day.linkedoutExtra);
            return list.ToArray();
        }

        private void Awake()
        {
            WorkInput.Blocked = true;
            session ??= FindFirstObjectByType<ResumeSessionController>();
            spawner ??= FindFirstObjectByType<ResumeSpawner>();
            computer ??= FindFirstObjectByType<ComputerUIController>();
            tutorial ??= GetComponent<DayOneTutorialController>();
            content = DayOneContent.Current;
            mailCatalog = MailCatalog.Current;
            day = DayDefinition.Current;            presenter = FindFirstObjectByType<ComputerScreenPresenter>();
            documentPresentation ??= GetComponent<DocumentPresentationController>();
            documentPresentation ??= gameObject.AddComponent<DocumentPresentationController>();
            if(presenter != null) presenter.ViewChanged += Refresh;
            documentPresentation.Changed += Refresh;
            if(presenter != null) presenter.ViewChanged += OnComputerViewChanged;
            if(computer.Chat != null) computer.Chat.Changed += RefreshChatBadge;
            view ??= FindFirstObjectByType<DayOneView>();
            // Linkedout only exists from Day 2 onward; the teaching day hides the shortcut.
            bool wantsLinkedout = day != null && day.linkedout;
            if (computer != null && computer.Desktop != null && computer.Desktop.LinkedoutIcon != null)
                computer.Desktop.LinkedoutIcon.gameObject.SetActive(wantsLinkedout);
            if (wantsLinkedout && computer != null) computer.SetLinkedoutCandidates(LinkedoutCandidates());
            DayProgress.ReviewApplicationId = day != null ? day.reviewApplicationId : null;
            if (content == null || view == null) { Debug.LogError("Day One UI prefab instance is missing from SampleScene."); enabled = false; return; }
            if (mailCatalog == null) { Debug.LogError("Mail catalog is missing from Resources/DayOne/Mails.asset."); enabled = false; return; }
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DayOneMailbox.Reset();
            DayOneEvents.ApplicationRequested += OpenApplication;
            spawner.ActiveResumeViewChanged += OnResume;
            tutorial.Configure(Morning[0].ApplicationId, view.mailButton, view.nextPage, computer.Desktop.SchoolRankIcon, view.gpaButton, view.accept, view.reject, view.submit, view.continueButton);
            tutorial.Milestone += Message;
            tutorial.Changed += OnTutorialChanged;
            session.ClearCurrent();
            view.introNext.onClick.AddListener(AdvanceIntro);
            view.mailButton.onClick.AddListener(() => { if (!WorkInput.Blocked && !presenter.Transitioning) { computer.CloseChat(); computer.OpenMail(); } });
            view.previousPage.onClick.AddListener(() => { if (!documentPresentation.IsTransitioning) spawner.ActiveResumeView?.TryMoveToPreviousPage(); Refresh(); });
            view.nextPage.onClick.AddListener(() => { if (!documentPresentation.IsTransitioning) spawner.ActiveResumeView?.TryAdvancePage(); Refresh(); });
            view.chatButton.onClick.AddListener(OpenChat);
            view.chatClose.onClick.AddListener(() => { if (!paused) view.chat.SetActive(false); });
            view.gpaButton.onClick.AddListener(() => { if (!WorkInput.Blocked && !WorkInput.DesktopBlocked && Current != null && documentPresentation.IsReading && !documentPresentation.IsTransitioning) { tutorial.MarkGpaChecked(Current.ApplicationId); Refresh(); } });
            view.accept.onClick.AddListener(() => Select(true));
            view.reject.onClick.AddListener(() => Select(false));
            view.submit.onClick.AddListener(Submit);
            view.continueButton.onClick.AddListener(StartAfternoon);
            view.resumeButton.onClick.AddListener(() => Pause(false));
            view.pauseTitle.onClick.AddListener(ReturnToTitle);
            view.endTitle.onClick.AddListener(ReturnToTitle);
            phase = WorkPhase.Intro;
            view.hud.SetActive(false); view.chat.SetActive(false); view.pause.SetActive(false); view.dayEnd.SetActive(false);
            PadSummary();
            var theme = computer != null ? computer.Theme : null;
            if (view.chat != null) PixelScrollbarStyle.Apply(view.chat.transform, theme);
            if (view.dayEnd != null) PixelScrollbarStyle.Apply(view.dayEnd.transform, theme);
            // Day 2 and Day 3 skip the three intro screens. They start on the next frame
            // because other components (ResumeDemoBootstrap) still reconfigure the
            // spawner during their own Awake and would destroy a view created here.
            if (day == null || day.showIntro) ShowIntro();
            else StartCoroutine(StartNextFrame());
        }
        private void OnDestroy()
        {
            if(presenter != null) presenter.ViewChanged -= Refresh;
            if(documentPresentation != null) documentPresentation.Changed -= Refresh;
            if(presenter != null) presenter.ViewChanged -= OnComputerViewChanged;
            if(computer != null && computer.Chat != null) computer.Chat.Changed -= RefreshChatBadge;
            if(tutorial!=null){tutorial.Milestone-=Message;tutorial.Changed-=OnTutorialChanged;}
            DayOneEvents.ApplicationRequested -= OpenApplication;
            if (spawner != null) spawner.ActiveResumeViewChanged -= OnResume;
            Time.timeScale = 1; WorkInput.Blocked = false;
        }
        private void OnApplicationFocus(bool focus) { if (!focus) { held = 0; if (view != null) view.skipFill.fillAmount = 0; } }
        private void Update()
        {
            if (view == null) return;
            if (phase == WorkPhase.Intro)
            {
                var keyboard = Keyboard.current;
                TickIntro(Application.isFocused, keyboard != null && keyboard.escapeKey.isPressed,
                    keyboard != null && keyboard.spaceKey.wasPressedThisFrame && !WorkInput.Typing, Time.unscaledDeltaTime);
            }
            else if (phase != WorkPhase.DayEnd && Keyboard.current != null && !WorkInput.Typing && Keyboard.current.escapeKey.wasPressedThisFrame) Pause(!paused);
            if (phase != WorkPhase.Intro && phase != WorkPhase.DayEnd && !WorkInput.Typing && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                documentPresentation?.PutDown();
        }
        private void TickIntro(bool focused, bool escapeHeld, bool advance, float unscaledDelta)
        {
            if (phase != WorkPhase.Intro) return;
            fade += unscaledDelta; view.introFade.alpha = Mathf.Clamp01(fade / .35f);
            held = focused && escapeHeld ? held + unscaledDelta : 0;
            view.skipFill.fillAmount = Mathf.Clamp01(held / 1.5f);
            if (!focused) return;
            if (held >= 1.5f) BeginWork();
            else if (advance) AdvanceIntro();
        }
        private void ShowIntro() { view.introText.text = T("intro." + introIndex); held = fade = 0; view.skipFill.fillAmount = 0; view.introFade.alpha = 0; }
        private void AdvanceIntro() { if (phase != WorkPhase.Intro) return; if (++introIndex >= 3) BeginWork(); else ShowIntro(); }
        private void BeginWork()
        {
            if (initialized) return;
            initialized = true; phase = WorkPhase.Arrival; WorkInput.Blocked = false;
            view.intro.SetActive(false); view.hud.SetActive(true);
            if (GateMails)
            {
                // Day 1: the handbook is delivered only after the welcome letter is read.
                DeliverMail(WelcomeMailId);
                Message(OpeningChat);
            }
            else
            {
                if (day != null) foreach (var mail in day.startMails) DeliverMail(mail);
                Message(OpeningChat);
                EnterMorning();
            }
            OpenChat(); Refresh();
        }
        private System.Collections.IEnumerator StartNextFrame()        {
            yield return null;
            BeginWork();
        }
        /// <summary>The day-end text used to start flush against the panel edge.</summary>
        private void PadSummary()
        {
            if (view == null || view.summary == null) return;
            foreach (var group in view.summary.GetComponentsInParent<UnityEngine.UI.LayoutGroup>(true))
            {
                var padding = group.padding;
                if (padding.left < 24) padding.left = 24;
                if (padding.right < 24) padding.right = 24;
                group.padding = padding;
                return;
            }
            var rect = view.summary.rectTransform;
            rect.offsetMin = new Vector2(24f, rect.offsetMin.y);
            rect.offsetMax = new Vector2(-24f, rect.offsetMax.y);
        }
        private void EnterMorning()
        {
            // Day 2/3 call this from Awake, before the spawner may have subscribed to
            // the session, so bind it explicitly or the first file never spawns.
            if (spawner != null && session != null) spawner.Configure(session);
            phase = WorkPhase.Morning; index = 0;
            session.SetResumes(Morning);
            session.ShowResume(0);
        }
        private void OnResume(ResumeView resume)
        {
            documentPresentation?.SetView(resume);
            if (resume != null && resume.NextResumeButton != null) resume.NextResumeButton.gameObject.SetActive(false);
            if (resume != null) { resume.PreviousPageButton?.gameObject.SetActive(false); resume.NextPageButton?.gameObject.SetActive(false); }
            Refresh();
        }
        private void OnTutorialChanged()
        {
            if (!GateMails) { Refresh(); return; }
            if (phase == WorkPhase.Arrival && tutorial.HasRead(WelcomeMailId))
            {
                DeliverMail(HandbookMailId);
                if (tutorial.HasRead(HandbookMailId))
                {
                    EnterMorning();
                    Message(HandbookChat);
                }
            }
            Refresh();
        }
        private bool First => TutorialActive && phase == WorkPhase.Morning && index == 0;
        private bool CanStamp => Current != null && spawner.ActiveResumeView != null && documentPresentation != null && documentPresentation.IsReading && !documentPresentation.IsTransitioning && (!First || tutorial.CanReviewFirst(Current.ApplicationId, Current.SchoolId, spawner.ActiveResumeView.PageCount));
        private void Select(bool value)
        {
            if (WorkInput.Blocked || WorkInput.DesktopBlocked || !CanStamp || (phase != WorkPhase.Morning && phase != WorkPhase.Afternoon)) return;
            decision = value; spawner.ActiveResumeView.SetStamp(value); if(First)tutorial.MarkStampSelected(Current.ApplicationId); Refresh();
            RetroAudioController.PlayStamp();
        }
        private void Submit()
        {
            if (WorkInput.Blocked || WorkInput.DesktopBlocked || !CanStamp || !decision.HasValue || Current == null ||
                (phase != WorkPhase.Morning && phase != WorkPhase.Afternoon) || submitted.Contains(Current.ApplicationId)) return;
            var data = Current; bool expected = ActiveRules.ShouldAccept(data, data.SchoolRank);
            if (First && decision.Value != expected) { Message("boss.correctFirst"); OpenChat(); return; }
            var result = new ReviewResult(data.ApplicationId, data.CandidateName, decision.Value, decision == expected, ActiveRules.Explain(data, data.SchoolRank));
            if (day != null && !string.IsNullOrEmpty(day.referralApplicationId) &&
                data.ApplicationId == day.referralApplicationId && decision.Value)
                DayProgress.ReferralAccepted = true;
            submitted.Add(data.ApplicationId); results.Add(result); decision = null;
            DayOneEvents.Submit(result); DayOneMailbox.Complete(data.ApplicationId);
            if (TutorialActive && phase == WorkPhase.Morning && index == 1) Message(result.correct ? "boss.alyssa.correct" : "boss.alyssa.wrong");
            index++; session.ClearCurrent();
            if (phase == WorkPhase.Morning)
            {
                if (index < Morning.Length) { session.ShowResume(index); if (TutorialActive && index == 1) Message("boss.alyssa"); }
                else { phase = WorkPhase.Midday; Message(MiddayChat); OpenChat(); }
            }
            else if (index < Afternoon.Length) DayOneMailbox.SetAvailable(Afternoon[index].ApplicationId);
            else EndDay();
            Refresh();
        }
        private void StartAfternoon()
        {
            if (paused || WorkInput.DesktopBlocked || phase != WorkPhase.Midday) return;
            phase = WorkPhase.Afternoon; index = 0; session.SetResumes(Afternoon);
            if (day != null) foreach (var mail in day.afternoonMails) DeliverMail(mail);
            DeliverApplicationMails();
            DayOneMailbox.SetAvailable(Afternoon[0].ApplicationId);
            Message(AfternoonChat); computer.CloseChat(); Refresh();
        }
        private void OpenApplication(string id)
        {
            if (WorkInput.Blocked || phase != WorkPhase.Afternoon || Current != null || id != Afternoon[index].ApplicationId || submitted.Contains(id)) return;
            // The first online file explains why it is suddenly a sheet of paper.
            if (index == 0) Message("boss.print");
            session.ShowResume(index); Refresh();
        }
        private void OnComputerViewChanged() => documentPresentation?.SetComputerCoversDesk(presenter != null && (presenter.Expanded || presenter.Transitioning));
        private void Message(string id) => computer.Chat.AddLocalized(id, T(id));
        private void DeliverMail(string id)
        {
            var entry = mailCatalog != null ? mailCatalog.Find(id) : null;
            if (entry == null) { Debug.LogError("Mail entry '" + id + "' is missing from Resources/DayOne/Mails.asset."); return; }
            DayOneMailbox.Add(DayOneMail.FromEntry(entry));
        }
        private void DeliverApplicationMails()
        {
            var template = mailCatalog != null ? mailCatalog.Template() : null;
            if (template == null) { Debug.LogError("The mail catalog has no entry marked as an application template."); return; }
            foreach (var application in Afternoon)
                DayOneMailbox.Add(DayOneMail.FromEntry(template, application));
        }
        private void RefreshChatBadge()
        {
            if(view != null) view.chatBadge.text = T("ui.chat") + (computer.Chat.Unread > 0 ? " (" + computer.Chat.Unread + ")" : "");
        }
        private void OpenChat() { if(!WorkInput.Blocked && !presenter.Transitioning) computer.OpenChat(); }
        private void Pause(bool value)
        {
            EventSystem.current?.SetSelectedGameObject(null);
            paused = value; WorkInput.Blocked = value; Time.timeScale = value ? 0 : 1;
            view.pause.SetActive(value);
        }
        private void ReturnToTitle() { Time.timeScale = 1; WorkInput.Blocked = false; SceneManager.LoadScene("MainMenu"); }
        private void EndDay()
        {
            phase = WorkPhase.DayEnd; WorkInput.Blocked = true; DayOneMailbox.SetAvailable(null);
            computer.CloseChat(); view.chat.SetActive(false); view.hud.SetActive(false); view.dayEnd.SetActive(true);
            int correct = results.Count(r => r.correct), accepted = results.Count(r => r.accepted);
            view.summary.text = string.Format(T(SummaryId), results.Count, accepted, results.Count - accepted, correct)
                + "\n\n" + T(correct == results.Count ? DayEndGoodChat : DayEndReviewChat);
            if (day != null && !string.IsNullOrEmpty(day.referralApplicationId) &&
                results.Any(r => r.applicationId == day.referralApplicationId))
                view.summary.text += "\n\n" + T(DayProgress.ReferralAccepted ? "d2.referral.accepted" : "d2.referral.refused");
            view.summary.text += "\n\n" + string.Join("\n\n", results.Where(r => !r.correct).Select(r => r.applicationId + " - " + LocalizationService.Text("resume." + r.applicationId + ".name", r.candidateName) + "\n" + r.reason));
            if (IsAuditDay) { ShowAudit(); return; }
            ShowNextDayButton();
        }

        private bool IsAuditDay => day != null && DayDefinition.For(day.dayNumber + 1) == null;

        private readonly List<UnityEngine.UI.Button> auditButtons = new();

        /// <summary>Day 3: the auditor asks the question and the player picks an ending.</summary>
        private int auditBaseIndex;
        private RectTransform auditOverlay;

        private void ShowAudit()
        {
            // The audit opens differently depending on whether the Day 2 review was actually checked.
            if (day != null && !string.IsNullOrEmpty(day.reviewApplicationId))
                view.summary.text = T(DayProgress.ReviewChecked ? "audit.priya.checked" : "audit.priya.unchecked") + "\n\n";
            view.summary.text += T("audit.open");
            // The day-end panel is a layout group and clips extra children, so the choices
            // live on their own transparent overlay above everything instead.
            if (view.endTitle != null) view.endTitle.gameObject.SetActive(false);
            var host = new GameObject("AuditChoices", typeof(RectTransform));
            auditOverlay = (RectTransform)host.transform;
            auditOverlay.SetParent(view.dayEnd.transform.root, false);
            auditOverlay.anchorMin = Vector2.zero;
            auditOverlay.anchorMax = Vector2.one;
            auditOverlay.offsetMin = auditOverlay.offsetMax = Vector2.zero;
            auditOverlay.SetAsLastSibling();
            CreateAuditButton(0, "audit.confess", () => ResolveEnding(true, false));
            CreateAuditButton(1, "audit.hide", () => ResolveEnding(false, false));
            CreateAuditButton(2, "audit.leave", () => ResolveEnding(false, true));
        }

        private void CreateAuditButton(int index, string labelId, UnityEngine.Events.UnityAction action)
        {
            var template = view.endTitle;
            var button = Instantiate(template, auditOverlay);
            button.name = "AuditChoice" + index;
            // The template is hidden before cloning, so the copy has to be switched back on.
            button.gameObject.SetActive(true);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(360f, 48f);
            rect.anchoredPosition = new Vector2(0f, -140f - index * 58f);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
            var label = button.GetComponentInChildren<UnityEngine.UI.Text>();
            if (label != null) label.text = T(labelId);
            auditButtons.Add(button);
        }

        private void ResolveEnding(bool confess, bool leave)
        {
            foreach (var button in auditButtons) if (button != null) button.gameObject.SetActive(false);
            if (auditOverlay != null) Destroy(auditOverlay.gameObject);
            if (view.endTitle != null) view.endTitle.gameObject.SetActive(true);
            if (leave) { ShowEnding("audit.leave.reply", "ending.run"); return; }
            if (confess) { DayProgress.Confessed = true; ShowEnding("audit.confess.reply", "ending.clean"); return; }
            ShowEnding("audit.hide.reply", DayProgress.ReferralAccepted ? "ending.fired" : "ending.clean");
        }

        private void ShowEnding(string replyId, string endingId)
        {
            view.summary.text = T(replyId) + "\n\n" + T(endingId + ".title") + "\n\n" + T(endingId + ".body");
        }

        private UnityEngine.UI.Button nextDayButton;
        /// <summary>Day 2 and later offer a next-day button; the last day hands over to the ending screen.</summary>
        private void ShowNextDayButton()
        {
            if (day == null || view.endTitle == null) return;
            if (DayDefinition.For(day.dayNumber + 1) == null) return;
            if (nextDayButton == null)
            {
                var template = view.endTitle;
                nextDayButton = Instantiate(template, template.transform.parent);
                nextDayButton.name = "NextDayButton";
                var source = (RectTransform)template.transform;
                var copy = (RectTransform)nextDayButton.transform;
                const float horizontalGap = .03f;
                float buttonWidth = source.anchorMax.x - source.anchorMin.x;
                var horizontalOffset = new Vector2((buttonWidth + horizontalGap) * .5f, 0f);
                source.anchorMin -= horizontalOffset;
                source.anchorMax -= horizontalOffset;
                copy.anchorMin += horizontalOffset;
                copy.anchorMax += horizontalOffset;
                nextDayButton.onClick.RemoveAllListeners();
                nextDayButton.onClick.AddListener(AdvanceDay);
                var label = nextDayButton.GetComponentInChildren<UnityEngine.UI.Text>();
                if (label != null) label.text = T("ui.nextDay");
            }
            nextDayButton.gameObject.SetActive(true);
        }

        private void AdvanceDay()
        {
            Time.timeScale = 1; WorkInput.Blocked = false;
            DayProgress.Advance();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        private void Refresh()
        {
            if (view == null || content == null) return;
            string target = "objective.review";
            if (phase == WorkPhase.Arrival) target = "objective.mail";
            else if (phase == WorkPhase.Midday) target = "objective.break";
            else if (phase == WorkPhase.Afternoon && Current == null) target = "objective.application";
            else if (reviewingDocumentRequired && !documentPresentation.IsReading) target = "objective.pickup";
            else if (First && Current != null && spawner.ActiveResumeView != null)
            {
                if (!tutorial.HasViewedPage(Current.ApplicationId) && spawner.ActiveResumeView.PageCount > 1) target = "objective.page";
                else if (!tutorial.HasQueriedSchool(Current.SchoolId)) target = "objective.school";
                else if (!tutorial.HasCheckedGpa(Current.ApplicationId)) target = "objective.gpa";
                else target = decision.HasValue ? "objective.submit" : "objective.stamp";
            }
            view.objective.text = T(target);
            if (documentPresentation.IsReading && reviewingDocumentRequired)
                view.objective.text += "  ·  " + T("ui.putDownHint");
            view.status.text = string.Format(T("ui.progress"), results.Count, DayTotal);
            view.gpaButton.gameObject.SetActive(First && Current != null && !tutorial.HasCheckedGpa(Current.ApplicationId));
            bool reviewing = (phase == WorkPhase.Morning || phase == WorkPhase.Afternoon) && Current != null && documentPresentation.IsReading;
            view.accept.gameObject.SetActive(reviewing); view.reject.gameObject.SetActive(reviewing); view.submit.gameObject.SetActive(reviewing);
            view.accept.interactable = view.reject.interactable = CanStamp && !WorkInput.DesktopBlocked;
            view.submit.interactable = CanStamp && decision.HasValue && !WorkInput.DesktopBlocked;
            view.previousPage.interactable = reviewing && !WorkInput.DesktopBlocked && !documentPresentation.IsTransitioning && spawner.ActiveResumeView.CurrentPage > 1;
            view.nextPage.interactable = reviewing && !WorkInput.DesktopBlocked && !documentPresentation.IsTransitioning && spawner.ActiveResumeView.CurrentPage < spawner.ActiveResumeView.PageCount;
            view.continueButton.gameObject.SetActive(phase == WorkPhase.Midday);
            if (phase == WorkPhase.Midday) view.continueButton.GetComponentInChildren<UnityEngine.UI.Text>().text = T("ui.afternoon");
            if (phase == WorkPhase.Midday) view.continueButton.GetComponentInChildren<UnityEngine.UI.Text>().text = T("ui.afternoon");
            view.gpaButton.interactable = !WorkInput.DesktopBlocked && documentPresentation.IsReading && !documentPresentation.IsTransitioning;
            tutorial.RefreshHighlight(phase,index,Current!=null?Current.ApplicationId:null,Current!=null?Current.SchoolId:null,spawner.ActiveResumeView!=null?spawner.ActiveResumeView.PageCount:0,decision.HasValue,TutorialActive);
        }

        private bool reviewingDocumentRequired =>
            (phase == WorkPhase.Morning || phase == WorkPhase.Afternoon) && Current != null && spawner.ActiveResumeView != null;
    }
}
