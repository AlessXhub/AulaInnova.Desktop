# Aula Innova Desktop

## Purpose

Aula Innova Desktop is the native WPF workspace for authorized administrators, teachers and auxiliaries. It supports school operations and institutional follow-up while keeping facts, assignments and human verification clearly separated.

The current delivery is connected to the ASP.NET Core .NET 10 API. Authentication uses ASP.NET Identity and JWT; administrative catalogs, operational reads, schedules, reports, audit and aggregate visual data come from SQL Server through the API. The desktop never accesses SQL Server directly.

## Source hierarchy

1. `requerimientos-definitivos-aula-innova.md` in the thesis workspace: 27 functional and 16 non-functional requirements.
2. Current use-case specification in `uml-definitivo/especificacion.md`.
3. Current architecture and database documentation.
4. Figma file `Aula Innova` (`1EkiZI9Wzb4onz5uDI7zRR`).
5. Documented frontend decisions in this project.

Historical drafts do not override the current requirements.

## Desktop actors

- Administrator: account and people administration; classrooms, groups, subjects, school days and enrollments; schedules; operational supervision; reports, statistics and audit.
- Teacher: own responsibilities, authorized classroom records, incidents, class context and safe aggregated visual feedback.
- Auxiliary: responsibilities, coverage, presence consultation, contingencies, incidents and institutional follow-up within the authorized scope.

The student portal is a separate Spring Boot client. NFC assignment and station setup are excluded from this desktop client and will be implemented in the future Android application using the phone's integrated NFC capability.

## Non-negotiable domain rules represented by the UI

- A missing NFC reading is not displayed as a confirmed absence.
- A care or substitute assignment is not displayed as confirmed presence.
- School access is distinct from classroom attendance and temporary classroom movements.
- Attendance has one valid record per student and local school day.
- Repeating the same incident type for the same student and day adds a traceable observation instead of increasing the incident count.
- Corrections and deactivations are logical and preserve evidence.
- Vision screens contain only estimated visible people and observable movement, with method, units, coverage, validity and context.
- The interface never claims attention, concentration, emotion, behavior, learning or teacher quality.
- Images, video, faces, trajectories and visual links to NFC identities are not stored or represented.

## Authentication

Accounts and roles are persisted by ASP.NET Identity in the API database. The WPF client keeps the access token only in process memory and clears it on logout. The API remains the authorization boundary; role-based navigation in WPF is presentation logic only.

## Design direction

The visual system keeps the Figma identity: Manrope, deep navy, periwinkle violet, lime accents, pale lavender and cool neutral surfaces. The experience is desktop-first, calm and information-dense, with explicit states and minimal decorative motion.
