# QA Report

## API integration verification — 22 September 2026

- API .NET 10 build: passed with 0 warnings and 0 errors.
- Desktop WPF build: passed with 0 warnings and 0 errors.
- Real Identity/JWT contract smoke test: passed.
- Twelve live endpoints were exercised against SQL Server: login, dashboard, eight record/catalog queries, schedule, report and visual summary; all returned HTTP success.
- The combined smoke suite passed 7/7, including the real desktop transport, JWT role mapping, account query, dashboard, report and schedule contracts.
- The database currently contains the bootstrap administrator but no domain seed data, so empty catalog/schedule results are expected and were verified as successful empty states.
- NFC credential assignment and station administration were removed from desktop navigation as requested; their API code was preserved for the future mobile client.
- Native-window capture was unavailable in the current automation surface, so this integration pass does not claim a new visual inspection. The previous visual checks below remain historical evidence for the unchanged shell/layout.

The remainder of this report describes the earlier in-memory prototype verification and is retained as historical context.

## Automated checks

- Debug build: passed with 0 warnings and 0 errors.
- `dotnet format --verify-no-changes`: passed.
- Service smoke suite: 6/6 passed.
  - authentication and role mapping;
  - one attendance per student/day in the demonstration;
  - repeated incident appended as an observation;
  - schedule conflict rejection and valid assignment continuation;
  - logical deactivation with audit entry;
  - recoverable simulated service failure.

## Runtime route checks

The administrator session rendered and navigated through all 22 destinations without process failure: dashboard, accounts, students, staff, credentials, stations, classrooms, schedules, coverage, presence, attendance, access, exits/returns, incidents, contingencies, audit, reports, statistics, class context, visual analysis, notices and profile.

Role-menu checks passed:

- Teacher: 9 expected destinations present; account/staff/NFC/report/statistics administration absent.
- Auxiliary: 13 expected destinations present; account/staff/NFC/statistics/visual administration absent.

## Visual checks

The running WPF application was captured and inspected at:

- 1024 x 700: login, dashboard and record table;
- 1280 x 800: dashboard;
- 1440 x 900: login and administrator dashboard;
- 1920 x 1080: dashboard.

At 1024 px the dashboard cards reflow to two columns and the page uses vertical scrolling only. Record tables retain their own horizontal scrollbar for wide columns. At 1920 px the content stays bounded instead of stretching cards across the full window.

The 76 px compact navigation state was also rendered at 1024 px: labels collapse, icons remain selectable, tooltips preserve destination names and the rail does not overlap page content.

Review captures are stored under `.impeccable/review/` and are not application resources.

## Interaction coverage

- Login: role selection, required inputs, busy state, invalid-credential error and password-recovery explanation.
- Records: loading, retryable error, empty search, live search, filter, sort, pagination, create/edit validation, detail and logical deactivation.
- Schedules: loading/error states, create assignment validation, conflict feedback and coverage continuation.
- Reports/statistics: selectors, regeneration/calculation and transparent denominators.
- Visual analysis: valid summary and explicit non-evaluable state.
- Profile: editable contact fields and validation feedback.

## Limits of this verification

No real API, database, Identity server, NFC reader, Android client, camera or visual-analysis service was available. Therefore network security, API authorization, persistence, hardware compatibility, idempotent offline delivery, measurement validity and retention/recovery were not claimed as tested.
