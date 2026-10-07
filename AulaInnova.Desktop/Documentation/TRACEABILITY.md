# Desktop Frontend Traceability

## Integration update — 22 September 2026

- RF-01 now uses the real `/api/Auth/login` Identity/JWT flow.
- RF-02 and RF-03 administrative catalogs persist through `/api/admin/*`; classroom, group, subject, school-day and enrollment maintenance is also connected.
- RF-04 and RF-06 are no longer desktop destinations. NFC assignment and station setup move to the future mobile application with integrated NFC.
- RF-08 through RF-26 now read persisted operational data through the administrator-only `/api/workspace` facade. Assignment creation is connected to `/api/admin/asignaciones`; other typed operational writes remain pending and their screens are intentionally read-only.
- RF-21 reports and the dashboard are calculated from the API database. RF-27 remains empty because the API has no approved notice contract yet.

The original matrix below documents the prototype baseline and should be read together with this integration update.

Status legend:

- **Frontend complete**: presentation and interface behavior are implemented without requiring a domain mutation.
- **Frontend complete with simulation**: the full interface and session-level operation are implemented against in-memory services.
- **Pending backend integration**: the interface exists, but persistence, authorization or source data must come from the API/service.
- **Outside desktop scope**: belongs to a different approved platform or physical device.
- **Blocked by missing information**: requires an institutional or technical decision that is still pending.

| Requirement | Desktop roles | Flow and screens | Components and interaction | Status |
|---|---|---|---|---|
| RF-01 Access | All desktop roles | Login, shell, logout | validation, loading, invalid credentials, session switch | Frontend complete with simulation; backend authentication pending |
| RF-02 Accounts/permissions | Administrator | Users and permissions | search, status filter, create/edit, logical disable, detail | Frontend complete with simulation; API enforcement pending |
| RF-03 People | Administrator | Students; Staff | CRUD-like session operations, enrollment/area context, history-safe copy | Frontend complete with simulation |
| RF-04 NFC credentials | Administrator | NFC cards | associate, edit, disable and inspect history context | Frontend complete with simulation; hardware/API pending |
| RF-05 Fixed/mobile NFC identification | Station and Android staff app | No capture screen in desktop | desktop only consumes resulting records | Outside desktop scope; integration pending |
| RF-06 Station management | Administrator | NFC stations | configuration/version, mode, communication and technical status | Frontend complete with simulation; API/telemetry pending |
| RF-07 Physical operation selection | Physical station | No desktop station UI | exactly two modes documented, not reimplemented | Outside desktop scope |
| RF-08 Schedules/care | Administrator | Schedules and care; Coverage | assignment form, overlap rejection, coverage continuation | Frontend complete with simulation |
| RF-09 Responsibilities | Teacher, Auxiliary | My responsibilities | daily schedule, current/next class, care and free period | Frontend complete with simulation |
| RF-10 Institutional access | Administrator, Auxiliary | School access | query, create where authorized, explicit operation/status | Frontend complete with simulation; capture/API pending |
| RF-11 Staff presence | Administrator, Auxiliary | Staff presence | readings, compatible responsibility and review state | Frontend complete with simulation; capture/API pending |
| RF-12 Classroom coverage | Administrator, Auxiliary | Classroom coverage | pending/assigned/present/human verification distinctions | Frontend complete with simulation |
| RF-13 Attendance/tardiness | All desktop roles within scope | Attendance | query, filters, one-per-day duplicate rejection, contingency | Frontend complete with simulation; station rules/API pending |
| RF-14 Temporary exits | All desktop roles within scope | Exits and returns | states accepted/repeated/review/rejected and declared reason | Frontend complete with simulation; station capture outside desktop |
| RF-15 Returns | All desktop roles within scope | Exits and returns | paired/open episode context and inconsistent-return review | Frontend complete with simulation; station capture outside desktop |
| RF-16 Incidents/observations | Administrator, Teacher, Auxiliary | Incidents and observations | validated form, duplicate becomes authored observation, no automatic sanction | Frontend complete with simulation; approved catalog/API pending |
| RF-17 Manual contingencies | Administrator, Auxiliary | Manual records and contingencies | event/reason/times/origin/author context | Frontend complete with simulation; device queue outside desktop |
| RF-18 History/corrections | Administrator | History and audit | detail, edit, justification copy, logical annulment, before/after audit | Frontend complete with simulation; API permission pending |
| RF-19 Student portal | Student | No desktop screen | separate Spring Boot portal | Outside desktop scope |
| RF-20 Institutional query | Administrator, Auxiliary | All records; Institutional query | live search, status filter, sorting, paging, scoped role menu | Frontend complete with simulation; API scope enforcement pending |
| RF-21 Reports | Administrator, Auxiliary | Reports | report type, period, scope, generated-at, error/retry | Frontend complete with simulation; server calculation pending |
| RF-22 Statistics | Administrator | Statistics | totals, frequency/duration summaries and denominator copy | Frontend complete with simulation; server calculation pending |
| RF-23 Class context | Administrator, Teacher | Class context | create/edit context for class and compatible interval | Frontend complete with simulation; schedule/API validation pending |
| RF-24 Visual indicators | Authorized administrator/teacher | Visual analysis | approved variables, validity, coverage, unit and method | Frontend complete with simulation; visual service pending |
| RF-25 Aggregated visual history | Authorized administrator/teacher | Visual analysis/history | valid/invalid intervals, temporal series, no individual identity | Frontend complete with simulation; persisted aggregate API pending |
| RF-26 Teacher visual feedback | Teacher | Visual analysis | safe summary, context and explicit interpretation limits | Frontend complete with simulation; approved units/interval blocked |
| RF-27 Internal notices | Authorized staff | Internal notices | cause, relation, state, last update and follow-up | Frontend complete with simulation; recipients/closure blocked |

## Cross-role continuations

| Initiating action | Next role or service | Coherent continuation implemented |
|---|---|---|
| Administrator creates a care/substitution assignment | Auxiliary or teacher checks responsibilities; administrator/auxiliary checks coverage | Shared in-memory schedule and a new `Confirmación pendiente` coverage record |
| Teacher or auxiliary records an incident already present | Administrator/authorized staff reviews history | Existing incident receives a timestamped observation; count remains unchanged; audit entry is added |
| Auxiliary records a contingency | Administrator audits/corrects | New domain record and audit entry are available during the session |
| Administrator disables an account/card/station | Later operator and auditor | Logical disabled state and an audit entry are retained |
| Station/API reports missing communication | Administrator follows station status; authorized staff sees internal notice | Separate technical state with copy that forbids inferring absence |
| Teacher declares class context | Visual-results service and teacher summary | Context is displayed alongside results without changing measurements |

## Non-functional coverage

- RNF-01 and RNF-02: loading, retry, feedback, clear labels and non-blocking simulated operations.
- RNF-03: stable IDs and explicit status/context in view models.
- RNF-04 and RNF-05: only UI state is simulated; secure authentication and authorization remain backend work.
- RNF-06: fictitious and minimized data; no family functions.
- RNF-07 and RNF-08: no media storage and no prohibited visual inferences.
- RNF-09: no automatic discipline or digital permission certification.
- RNF-10: audit copy preserves author/time and before/after data in the simulation.
- RNF-11: Views -> ViewModels -> contracts -> mock adapter is implemented.
- RNF-12, RNF-14 and RNF-15: hardware/performance/queue verification is outside desktop frontend.
- RNF-13: UI exposes method, unit, coverage and non-evaluable intervals; scientific validation is pending.
- RNF-16: retention and recovery are backend/infrastructure responsibilities.
