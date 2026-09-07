from pathlib import Path
import math
import textwrap

PAGE_W, PAGE_H = 595, 842
MARGIN = 48
NAVY = (0.055, 0.102, 0.180)
BLUE = (0.075, 0.376, 0.650)
TEAL = (0.000, 0.557, 0.580)
LIGHT = (0.945, 0.965, 0.980)
INK = (0.110, 0.145, 0.190)
MUTED = (0.370, 0.430, 0.490)
WHITE = (1, 1, 1)
AMBER = (0.929, 0.608, 0.153)
GREEN = (0.145, 0.620, 0.380)
RED = (0.780, 0.220, 0.220)


def esc(value):
    return value.replace('\\', '\\\\').replace('(', '\\(').replace(')', '\\)')


def color(rgb):
    return f'{rgb[0]:.3f} {rgb[1]:.3f} {rgb[2]:.3f}'


class Pdf:
    def __init__(self):
        self.pages = []
        self.ops = []
        self.page_no = 0
        self.y = 0

    def page(self, title=None, section=None):
        if self.ops:
            self._footer()
            self.pages.append('\n'.join(self.ops))
        self.page_no += 1
        self.ops = []
        self.y = PAGE_H - MARGIN
        self.rect(0, PAGE_H - 18, PAGE_W, 18, NAVY)
        if section:
            self.text(MARGIN, PAGE_H - 39, section.upper(), 8, 'B', TEAL)
        if title:
            self.text(MARGIN, PAGE_H - 67, title, 20, 'B', NAVY)
            self.line(MARGIN, PAGE_H - 79, PAGE_W - MARGIN, PAGE_H - 79, LIGHT, 1)
            self.y = PAGE_H - 100

    def finish(self, output):
        if self.ops:
            self._footer()
            self.pages.append('\n'.join(self.ops))
        objects = []
        objects.append('<< /Type /Catalog /Pages 2 0 R >>')
        kids = ' '.join(f'{6 + i * 2} 0 R' for i in range(len(self.pages)))
        objects.append(f'<< /Type /Pages /Kids [{kids}] /Count {len(self.pages)} >>')
        objects.append('<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>')
        objects.append('<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>')
        objects.append('<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>')
        for i, stream in enumerate(self.pages):
            content_id = 7 + i * 2
            objects.append(f'<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PAGE_W} {PAGE_H}] /Resources << /Font << /R 3 0 R /B 4 0 R /M 5 0 R >> >> /Contents {content_id} 0 R >>')
            data = stream.encode('latin-1', 'replace')
            objects.append(f'<< /Length {len(data)} >>\nstream\n{stream}\nendstream')
        raw = bytearray(b'%PDF-1.4\n%\xe2\xe3\xcf\xd3\n')
        offsets = [0]
        for index, obj in enumerate(objects, 1):
            offsets.append(len(raw))
            raw.extend(f'{index} 0 obj\n{obj}\nendobj\n'.encode('latin-1'))
        xref = len(raw)
        raw.extend(f'xref\n0 {len(objects)+1}\n0000000000 65535 f \n'.encode())
        for offset in offsets[1:]:
            raw.extend(f'{offset:010d} 00000 n \n'.encode())
        raw.extend(f'trailer\n<< /Size {len(objects)+1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n'.encode())
        Path(output).write_bytes(raw)

    def _footer(self):
        self.line(MARGIN, 31, PAGE_W - MARGIN, 31, LIGHT, .8)
        self.text(MARGIN, 18, 'CLINICAPP  |  TECHNICAL & BUSINESS OVERVIEW', 7, 'B', MUTED)
        self.text(PAGE_W - MARGIN - 35, 18, f'{self.page_no:02d}', 8, 'B', NAVY)

    def text(self, x, y, value, size=10, font='R', fill=INK):
        self.ops.append(f'BT /{font} {size} Tf {color(fill)} rg 1 0 0 1 {x:.1f} {y:.1f} Tm ({esc(value)}) Tj ET')

    def rect(self, x, y, w, h, fill, stroke=None, radius=0):
        self.ops.append(f'{color(fill)} rg {x:.1f} {y:.1f} {w:.1f} {h:.1f} re f')
        if stroke:
            self.ops.append(f'{color(stroke)} RG {x:.1f} {y:.1f} {w:.1f} {h:.1f} re S')

    def line(self, x1, y1, x2, y2, stroke=MUTED, width=.8):
        self.ops.append(f'{color(stroke)} RG {width} w {x1:.1f} {y1:.1f} m {x2:.1f} {y2:.1f} l S')

    def ensure(self, height, title=None, section=None):
        if self.y - height < 47:
            self.page(title, section)

    def heading(self, value, level=2):
        size = 15 if level == 2 else 11
        gap = 25 if level == 2 else 19
        self.ensure(gap + 8)
        self.text(MARGIN, self.y, value, size, 'B', NAVY if level == 2 else BLUE)
        self.y -= gap

    def paragraph(self, value, width=88, size=9.4, leading=13, fill=INK):
        lines = []
        for part in value.split('\n'):
            lines.extend(textwrap.wrap(part, width=width) or [''])
        self.ensure(len(lines) * leading + 8)
        for line in lines:
            self.text(MARGIN, self.y, line, size, 'R', fill)
            self.y -= leading
        self.y -= 5

    def bullets(self, values, width=80, accent=TEAL):
        for value in values:
            lines = textwrap.wrap(value, width=width)
            self.ensure(len(lines) * 13 + 5)
            self.rect(MARGIN, self.y + 3, 4, 4, accent)
            for i, line in enumerate(lines):
                self.text(MARGIN + 13, self.y, line, 9.2, 'R', INK)
                self.y -= 13
            self.y -= 3

    def callout(self, label, value, fill=LIGHT, accent=BLUE):
        lines = []
        for part in value.split('\n'):
            lines.extend(textwrap.wrap(part, width=76) or [''])
        height = 34 + len(lines) * 12
        self.ensure(height + 10)
        top = self.y
        self.rect(MARGIN, top - height, PAGE_W - 2 * MARGIN, height, fill)
        self.rect(MARGIN, top - height, 5, height, accent)
        self.text(MARGIN + 16, top - 20, label.upper(), 8, 'B', accent)
        yy = top - 36
        for line in lines:
            self.text(MARGIN + 16, yy, line, 9, 'R', INK)
            yy -= 12
        self.y -= height + 12

    def cards(self, cards):
        gap, cols = 10, 2
        width = (PAGE_W - 2 * MARGIN - gap) / cols
        rows = math.ceil(len(cards) / cols)
        height = 87
        self.ensure(rows * (height + gap))
        top = self.y
        for i, (title, body, accent) in enumerate(cards):
            row, col = divmod(i, cols)
            x = MARGIN + col * (width + gap)
            y = top - row * (height + gap) - height
            self.rect(x, y, width, height, LIGHT)
            self.rect(x, y + height - 5, width, 5, accent)
            self.text(x + 12, y + height - 24, title, 10, 'B', NAVY)
            yy = y + height - 40
            for line in textwrap.wrap(body, width=39)[:4]:
                self.text(x + 12, yy, line, 8.1, 'R', MUTED)
                yy -= 11
        self.y -= rows * (height + gap) + 7

    def table(self, headers, rows, widths, font_size=7.8, row_height=25):
        total = sum(widths)
        self.ensure(row_height * (len(rows) + 1) + 8)
        x, top = MARGIN, self.y
        self.rect(x, top - row_height, total, row_height, NAVY)
        cx = x
        for header, width in zip(headers, widths):
            self.text(cx + 6, top - 17, header, font_size, 'B', WHITE)
            cx += width
        yy = top - row_height
        for idx, row in enumerate(rows):
            self.rect(x, yy - row_height, total, row_height, WHITE if idx % 2 else LIGHT)
            cx = x
            for value, width in zip(row, widths):
                line = textwrap.shorten(str(value), width=max(8, int(width / (font_size * .55))), placeholder='...')
                self.text(cx + 6, yy - 16, line, font_size, 'R', INK)
                cx += width
            yy -= row_height
        self.y = yy - 12

    def flow(self, steps):
        self.ensure(80)
        gap = 8
        width = (PAGE_W - 2 * MARGIN - gap * (len(steps) - 1)) / len(steps)
        y = self.y - 55
        for i, (number, title) in enumerate(steps):
            x = MARGIN + i * (width + gap)
            self.rect(x, y, width, 48, LIGHT)
            self.rect(x, y + 32, 25, 16, TEAL)
            self.text(x + 8, y + 37, str(number), 7, 'B', WHITE)
            for j, line in enumerate(textwrap.wrap(title, width=16)[:2]):
                self.text(x + 8, y + 20 - j * 10, line, 7.5, 'B', NAVY)
            if i < len(steps) - 1:
                self.line(x + width, y + 24, x + width + gap, y + 24, BLUE, 1.4)
        self.y -= 72


pdf = Pdf()
pdf.page()
pdf.rect(0, 0, PAGE_W, PAGE_H, NAVY)
pdf.rect(0, 0, 17, PAGE_H, TEAL)
pdf.text(48, 706, 'CLINICAPP', 13, 'B', TEAL)
pdf.text(48, 628, 'Clinic Appointment', 32, 'B', WHITE)
pdf.text(48, 590, '& Billing System', 32, 'B', WHITE)
pdf.text(48, 548, 'TECHNICAL & BUSINESS OVERVIEW', 12, 'B', (0.62, 0.78, 0.88))
pdf.text(48, 503, 'A decision-ready view of product value, architecture,', 11, 'R', WHITE)
pdf.text(48, 486, 'security, data, operations, and delivery readiness.', 11, 'R', WHITE)
pdf.rect(48, 330, 499, 105, (0.075, 0.145, 0.245))
pdf.text(66, 405, 'AUDIENCE', 8, 'B', TEAL)
pdf.text(66, 381, 'Technical leadership  |  Business stakeholders  |  Delivery teams', 10, 'R', WHITE)
pdf.text(66, 352, 'Version 1.0  |  July 2026  |  Assessment / demonstration baseline', 9, 'R', (0.70, 0.78, 0.85))
pdf.text(48, 83, 'ASP.NET CORE 8  /  REACT 19  /  SQL SERVER  /  JWT', 8, 'B', (0.62, 0.78, 0.88))

pdf.page('Executive Summary', '01  Product Overview')
pdf.callout('Purpose', 'ClinicApp digitizes the core front-office clinic journey: secure staff access, patient records, appointment scheduling, itemized billing, payment capture, and operational visibility.', LIGHT, TEAL)
pdf.heading('Business outcomes')
pdf.cards([
    ('One operating view', 'Patients, appointments, invoices, payments, and dashboard indicators live in one workflow.', BLUE),
    ('Safer scheduling', 'Overlap checks protect doctors from double booking; cancellation and rescheduling are explicit.', TEAL),
    ('Revenue visibility', 'Itemized invoices, VAT, discounts, partial payments, and balances improve billing control.', AMBER),
    ('Accountability', 'Role controls, soft deletion, audit records, and structured logs strengthen traceability.', GREEN),
])
pdf.heading('Solution snapshot')
pdf.bullets([
    'Responsive React single-page application backed by an ASP.NET Core 8 REST API.',
    'Layered backend separates domain rules, use cases, infrastructure, and HTTP delivery.',
    'SQL Server persistence through Entity Framework Core migrations, indexes, constraints, and query filters.',
    'JWT access tokens, rotating refresh tokens, and role policies for Admin, Doctor, and Receptionist.',
    'Swagger/OpenAPI, automated tests, linting, build checks, and GitHub Actions support delivery quality.'
])
pdf.callout('Current positioning', 'A strong assessment and demonstration baseline. Production deployment requires replacing development credentials, configuring external services and storage, and establishing operational controls described later in this document.', (1, .97, .90), AMBER)

pdf.page('Business Capability Map', '02  Business View')
pdf.heading('Primary users')
pdf.table(['ROLE', 'BUSINESS RESPONSIBILITY', 'ACCESS PROFILE'], [
    ['Admin', 'Operational oversight and full clinic access', 'All current capabilities'],
    ['Receptionist', 'Patient intake, scheduling, and billing', 'Read + operational writes'],
    ['Doctor', 'Clinical workflow visibility', 'Read-only operational access'],
], [90, 245, 164], 8, 31)
pdf.heading('End-to-end clinic journey')
pdf.flow([(1, 'Authenticate'), (2, 'Register patient'), (3, 'Book visit'), (4, 'Deliver service'), (5, 'Invoice & pay')])
pdf.cards([
    ('Patient records', 'Search, retrieve, create, edit, soft-delete, medical history, insurance, and documents.', BLUE),
    ('Appointments', 'Seven-day calendar, filtering, booking, cancellation, rescheduling, and reminders.', TEAL),
    ('Billing', 'Multiple items, VAT, discount, partial/full payments, and outstanding balances.', AMBER),
    ('Dashboard', 'Today appointments, patient count, revenue, unpaid invoices, and trend charts.', GREEN),
])
pdf.heading('Business rules enforced')
pdf.bullets([
    'Patient email addresses are unique and normalized before persistence.',
    'Appointment end time must be later than start time; overlapping doctor schedules are rejected.',
    'Invoices require positive-value items; VAT and discount values are calculated and retained per invoice.',
    'Payments must be positive and cannot exceed the invoice outstanding balance.',
    'Revenue represents recorded payments rather than issued invoice totals.',
    'Operational timestamps are stored as UTC; the browser displays calendar times locally.'
])

pdf.page('Role-Based Access Control', '03  Security & Governance')
pdf.heading('Implemented access matrix')
pdf.table(['CAPABILITY', 'ADMIN', 'RECEPTIONIST', 'DOCTOR'], [
    ['Patients: view/search/documents', 'Allow', 'Allow', 'Allow'],
    ['Patients: create/update/delete/upload', 'Allow', 'Allow', 'Deny'],
    ['Doctors: list/lookup', 'Allow', 'Allow', 'Allow'],
    ['Appointments: view/filter', 'Allow', 'Allow', 'Allow'],
    ['Appointments: book/cancel/reschedule', 'Allow', 'Allow', 'Deny'],
    ['Invoices and payments', 'Allow', 'Allow', 'Deny'],
    ['Dashboard and charts', 'Allow', 'Allow', 'Allow'],
], [250, 83, 93, 73], 7.4, 29)
pdf.heading('Authorization model')
pdf.cards([
    ('ClinicStaff', 'Admin, Doctor, or Receptionist. Applied to shared read-oriented controllers.', BLUE),
    ('ReceptionistOrAdmin', 'Protects patient and appointment writes plus all billing operations.', TEAL),
    ('AdminOnly', 'Registered for future administration endpoints; no current endpoint consumes it.', AMBER),
    ('Anonymous', 'Limited to login, refresh, and logout endpoints in the authentication controller.', GREEN),
])
pdf.callout('Important scope boundary', 'The current Doctor account is a security role, but it is not linked to a specific Doctor roster record. Therefore the app provides role-level read access rather than restricting a doctor to only their own appointments or patients.', (1, .94, .94), RED)

pdf.page('Authentication & Security Controls', '03  Security & Governance')
pdf.heading('Token lifecycle')
pdf.flow([(1, 'Credentials'), (2, 'Verify hash'), (3, 'Issue JWT'), (4, 'Rotate refresh'), (5, 'Revoke logout')])
pdf.bullets([
    'Access tokens include user identifier, username, and role claims and are signed with HMAC-SHA256.',
    'Issuer, audience, signature, and lifetime are validated; configured clock skew is 30 seconds.',
    'Refresh tokens use 64 cryptographically random bytes, are persisted, expire after seven days, and are single-use after rotation.',
    'Inactive or soft-deleted users cannot authenticate or rotate refresh tokens.',
    'Passwords are stored as hashes; password and token fields are excluded from audit-change details.',
    'The API fails fast when the database connection or JWT signing key is missing, and enforces a minimum 32-byte key.'
])
pdf.heading('Additional controls')
pdf.cards([
    ('Input safety', 'Data annotations, service validation, SQL constraints, and typed DTOs create layered validation.', BLUE),
    ('Error contract', 'RFC 7807 Problem Details maps validation, missing resources, conflicts, and server errors.', TEAL),
    ('CORS', 'Allowed frontend origins are configurable; credentials and required HTTP methods are supported.', AMBER),
    ('Traceability', 'Structured logs, file logging, audit columns, and automatic audit records support diagnosis.', GREEN),
])
pdf.callout('Production security action', 'Development database credentials and JWT key are committed for clone-and-run convenience. Production must inject unique secrets through environment variables or a managed secret store and must never use the seeded demo accounts.', (1, .97, .90), AMBER)

pdf.page('Solution Architecture', '04  Technical Architecture')
pdf.heading('Layered design')
pdf.table(['LAYER / PROJECT', 'RESPONSIBILITY', 'KEY CONTENT'], [
    ['ClinicApp.Domain', 'Business model and contracts', 'Entities, enums, repository interfaces'],
    ['ClinicApp.Application', 'Use cases and application rules', 'DTOs, services, validation, auth'],
    ['ClinicApp.Infrastructure', 'Technical implementations', 'EF context, repositories, files, SMTP'],
    ['ClinicApp.Api', 'HTTP delivery and composition', 'Controllers, policies, middleware, Swagger'],
    ['ClinicApp.Frontend', 'Staff user experience', 'React UI, charts, calendar, PDF export'],
    ['ClinicApp.Api.Tests', 'Automated backend verification', 'xUnit + EF Core InMemory tests'],
], [115, 190, 194], 7.7, 31)
pdf.heading('Runtime request path')
pdf.flow([(1, 'React UI'), (2, 'REST controller'), (3, 'ClinicService'), (4, 'Repository / EF'), (5, 'SQL Server')])
pdf.bullets([
    'API responses use DTOs rather than exposing persistence entities.',
    'Dependency injection supplies scoped repositories, services, DbContext, hashing, token generation, file storage, and email delivery.',
    'The Application layer depends on abstractions; Infrastructure supplies concrete persistence and external service implementations.',
    'Controllers remain thin while ClinicService coordinates business rules and repository calls.',
    'Swagger publishes request/response schemas and supports bearer-token testing.'
])

pdf.page('Data Architecture', '05  Data & Integrity')
pdf.heading('Core entity relationships')
pdf.table(['ENTITY', 'RELATIONSHIPS / PURPOSE', 'INTEGRITY MECHANISMS'], [
    ['Role / User', 'User belongs to one role', 'Unique role and username; restricted role delete'],
    ['Patient', 'Owns documents; referenced by visits/invoices', 'Unique email; indexes; soft delete'],
    ['Doctor', 'Referenced by appointments', 'Unique email; active flag; soft delete'],
    ['Appointment', 'Links patient and doctor', 'Time check; composite schedule index'],
    ['Invoice / Item', 'Invoice owns itemized charges', 'Money precision; amount constraints'],
    ['Payment', 'Applied to one invoice', 'Positive amount constraint; indexed history'],
    ['RefreshToken', 'Owned by one user', 'Unique token; expiry/revocation index'],
    ['AuditLog', 'Records entity changes', 'Entity/time indexes; protected fields omitted'],
], [96, 235, 168], 7.1, 28)
pdf.heading('Persistence strategy')
pdf.bullets([
    'Entity Framework Core 8 targets SQL Server and applies migrations automatically during non-test startup.',
    'Global query filters hide soft-deleted records while preserving historical data.',
    'CreatedAt and UpdatedAt are maintained centrally; tracked entity changes produce AuditLog rows.',
    'Decimal money values use precision 18,2 and domain calculations round monetary values.',
    'SQL indexes support common patient, appointment, invoice, payment, refresh-token, and audit queries.',
    'An idempotent SQL schema script is included as an alternate database deliverable.'
])
pdf.callout('File boundary', 'Patient document metadata is stored in SQL Server; uploaded file content is stored on the local filesystem through IFileStorageService. Production should move content to durable shared/object storage.', (1, .97, .90), AMBER)

pdf.page('API & Integration Surface', '06  Interfaces')
pdf.heading('Primary REST endpoints')
pdf.table(['METHOD', 'ROUTE', 'PURPOSE / ACCESS'], [
    ['POST', '/api/auth/login', 'Authenticate and issue tokens / anonymous'],
    ['POST', '/api/auth/refresh | logout', 'Rotate or revoke refresh token / anonymous'],
    ['GET/POST', '/api/patients', 'Search or create / staff; write ops restricted'],
    ['GET/PUT/DELETE', '/api/patients/{id}', 'Retrieve or maintain patient'],
    ['GET', '/api/doctors | /lookup', 'Doctor list and lightweight lookup / staff'],
    ['GET/POST', '/api/appointments', 'Filter or book appointments'],
    ['POST', '/api/appointments/{id}/cancel', 'Cancel appointment / Receptionist or Admin'],
    ['POST', '/api/appointments/{id}/reschedule', 'Reschedule / Receptionist or Admin'],
    ['GET/POST', '/api/invoices', 'Search or create / Receptionist or Admin'],
    ['POST', '/api/invoices/{id}/payments', 'Record payment / Receptionist or Admin'],
    ['GET/POST', '/api/patients/{id}/documents', 'List or upload documents'],
    ['GET', '/api/dashboard | /charts', 'Operational metrics / clinic staff'],
], [55, 225, 219], 6.9, 25)
pdf.heading('API behavior')
pdf.bullets([
    'Pagination defaults and maximums are centrally configurable: default page size 10, maximum 100, and lookup limit 20.',
    'List APIs support relevant search, filtering, and sorting parameters.',
    'JSON serializes enums as readable strings.',
    'Expected failures return stable HTTP statuses: 400 validation, 401 unauthenticated, 403 forbidden, 404 missing, and 409 conflict.',
    'Swagger UI is available at /swagger in the current runtime configuration.'
])

pdf.page('Frontend Experience', '07  User Experience')
pdf.heading('Application experience')
pdf.cards([
    ('Secure session', 'Login, access-token use, refresh handling, logout, and role-aware navigation.', BLUE),
    ('Operational dashboard', 'At-a-glance KPIs and Chart.js visualizations for activity and revenue.', TEAL),
    ('Weekly calendar', 'Responsive seven-day appointment view with filters and workflow actions.', AMBER),
    ('Billing output', 'Invoice details and client-side PDF generation through jsPDF and AutoTable.', GREEN),
    ('Patient workspace', 'Search, pagination, profile maintenance, and patient document uploads.', BLUE),
    ('Accessible preferences', 'Responsive design and a dark-mode toggle for different environments.', TEAL),
])
pdf.heading('Frontend technology')
pdf.table(['AREA', 'IMPLEMENTATION'], [
    ['Framework', 'React 19.1 with React DOM'],
    ['Build tooling', 'Vite 6.4 with React plugin'],
    ['Charts', 'Chart.js 4.4'],
    ['PDF invoices', 'jsPDF 2.5 + jsPDF-AutoTable 3.8'],
    ['Code quality', 'ESLint 9 with React and hooks rules'],
    ['Local integration', 'Vite proxy forwards /api to localhost:5002'],
], [145, 354], 8, 29)
pdf.callout('Business usability', 'The UI brings clinical administration and billing into one staff-facing workflow, reducing tool switching while preserving role-specific write restrictions.', LIGHT, TEAL)

pdf.page('Reliability, Observability & Background Work', '08  Operations')
pdf.heading('Operational safeguards')
pdf.cards([
    ('Resilient database', 'SQL Server retry-on-failure is enabled for transient database connectivity errors.', BLUE),
    ('Central errors', 'One middleware converts application exceptions into consistent Problem Details responses.', TEAL),
    ('Logging', 'Console providers plus a custom file provider capture structured application events.', AMBER),
    ('Audit history', 'Automatic records capture entity, identifier, action, changed fields, and timestamp.', GREEN),
])
pdf.heading('Appointment reminders')
pdf.flow([(1, 'Hourly scan'), (2, 'Next 24 hours'), (3, 'SMTP delivery'), (4, 'Mark sent'), (5, 'Retry failures')])
pdf.bullets([
    'A hosted background service finds scheduled appointments within the next 24 hours that have not been reminded.',
    'Messages include patient name, doctor name, and UTC appointment time.',
    'SMTP is configuration-driven and safely skips delivery when no host is configured.',
    'Failures are logged per appointment; successful sends persist ReminderSentAt to prevent duplicates.'
])
pdf.heading('Configuration model')
pdf.bullets([
    'Environment variables override JSON settings using ASP.NET Core double-underscore notation.',
    'Configurable areas include database, JWT, pagination, CORS origins, storage path, logging, and SMTP.',
    'Local SQL infrastructure uses Docker Compose with a persistent named volume on port 1433.'
])

pdf.page('Quality Engineering & Delivery', '09  Engineering Quality')
pdf.heading('Automated verification')
pdf.callout('Backend test inventory', '64 xUnit Fact/Theory test cases across authentication, JWT behavior, patients, appointments, invoices, repositories, and clinic service behavior. EF Core InMemory supports isolated test execution.', LIGHT, BLUE)
pdf.table(['CI JOB', 'CHECKS'], [
    ['Backend (.NET 8)', 'Restore tools/packages; build; test; dotnet format verification'],
    ['Frontend (Node 20)', 'npm ci; ESLint; production Vite build'],
    ['Triggers', 'Every pull request and pushes to main'],
], [145, 354], 8, 34)
pdf.heading('Maintainability choices')
pdf.bullets([
    'Layered projects and dependency inversion isolate business logic from delivery and persistence concerns.',
    'Repository abstractions centralize reusable data access for patients, appointments, and invoices.',
    'DTO boundaries reduce accidental coupling between API contracts and database entities.',
    'Configuration options remove business-sensitive pagination limits from controller defaults.',
    'EF Core migrations and the generated SQL script make schema evolution explicit and reviewable.',
    'XML documentation on key services, options, factories, and endpoints improves discoverability.'
])
pdf.heading('Local demonstration flow')
pdf.flow([(1, 'Start database'), (2, 'Build & test'), (3, 'Run API'), (4, 'Run React'), (5, 'Open app')])
pdf.paragraph('Default local URLs: frontend http://localhost:5173, API http://localhost:5002, and Swagger http://localhost:5002/swagger.', fill=MUTED)

pdf.page('Production Readiness Assessment', '10  Risks & Roadmap')
pdf.heading('Readiness summary')
pdf.table(['AREA', 'CURRENT STATE', 'PRODUCTION ACTION'], [
    ['Architecture', 'Clear layered modular monolith', 'Retain; define ownership and ADRs'],
    ['Security', 'JWT, refresh rotation, RBAC', 'Managed secrets; hardening; key rotation'],
    ['Data', 'SQL constraints, migrations, audit', 'Backups, retention, encryption policy'],
    ['Files', 'Local filesystem storage', 'Move to durable object/shared storage'],
    ['Email', 'Direct SMTP background sender', 'Queue/outbox, templates, monitoring'],
    ['Observability', 'Structured console/file logs', 'Central logs, metrics, traces, alerts'],
    ['Deployment', 'Local Compose database only', 'Containerize app; health checks; release pipeline'],
    ['Identity', 'Seeded local users', 'User lifecycle, password policy, MFA/SSO'],
], [90, 170, 239], 6.8, 29)
pdf.heading('Recommended roadmap')
pdf.cards([
    ('P0 - Before release', 'Externalize secrets, remove demo users, restrict Swagger, TLS, backups, health checks.', RED),
    ('P1 - Operationalize', 'Central telemetry, object storage, email queue/outbox, retention, restore testing.', AMBER),
    ('P2 - Identity depth', 'Admin user management, password reset, lockout, MFA/SSO, key rotation.', BLUE),
    ('P3 - Clinical scope', 'Link User to Doctor; restrict doctor data; roster administration and richer notes.', TEAL),
])
pdf.callout('Decision point', 'For a controlled internal pilot, the modular monolith is appropriate and cost-effective. Scale by strengthening operations and security first; split services only when independent scaling, ownership, or availability requirements justify the added complexity.', LIGHT, GREEN)

pdf.page('Stakeholder Talking Points', '11  Presentation Guide')
pdf.heading('For business stakeholders')
pdf.bullets([
    'ClinicApp connects patient intake, scheduling, billing, payment tracking, and management visibility.',
    'Role-based access separates clinical read needs from front-office write and billing responsibilities.',
    'Core rules prevent scheduling conflicts and payment over-collection while preserving an audit trail.',
    'The dashboard turns operational records into immediate activity and revenue indicators.',
    'Bonus capabilities - reminders, documents, charts, dark mode, and PDF invoices - show an extensible product direction.'
])
pdf.heading('For technical leadership')
pdf.bullets([
    'ASP.NET Core 8 and React 19 are organized as a layered modular monolith with clear dependency boundaries.',
    'Security includes validated signed JWTs, role claims, policy authorization, refresh-token rotation, and revocation.',
    'Data integrity is defended at request, service, EF mapping, and SQL constraint levels.',
    'Delivery quality includes 64 test cases, backend formatting, frontend lint/build, and pull-request CI.',
    'The codebase clearly distinguishes demonstration defaults from the production controls that must be implemented.'
])
pdf.heading('Suggested five-minute demo')
pdf.flow([(1, 'Login by role'), (2, 'Show dashboard'), (3, 'Create patient'), (4, 'Book visit'), (5, 'Invoice & pay')])
pdf.callout('Closing message', 'The solution demonstrates a maintainable foundation with meaningful business workflows, defense-in-depth validation, role-aware access, and a practical path from technical assessment to production-grade clinic operations.', (0.92, .98, .95), GREEN)

pdf.page('Glossary & Reference', '12  Appendix')
pdf.heading('Key terms')
pdf.table(['TERM', 'MEANING IN THIS SOLUTION'], [
    ['JWT', 'Signed short-lived access token carrying identity and role claims'],
    ['Refresh token', 'Longer-lived random credential used once to obtain a new token pair'],
    ['RBAC', 'Role-based access control using Admin, Doctor, and Receptionist policies'],
    ['DTO', 'Purpose-built API request/response object separate from EF entities'],
    ['Soft delete', 'Logical deletion that retains data but hides it through query filters'],
    ['Problem Details', 'RFC 7807 standard JSON error response shape'],
    ['Migration', 'Versioned EF Core database schema change'],
    ['Modular monolith', 'One deployable backend organized into strongly separated internal layers'],
], [115, 384], 7.7, 29)
pdf.heading('Technology baseline')
pdf.paragraph('ASP.NET Core 8 | Entity Framework Core 8 | SQL Server-compatible local database | React 19 | Vite 6 | Chart.js | jsPDF | xUnit | Swagger/OpenAPI | Docker Compose | GitHub Actions', fill=MUTED)
pdf.heading('Source-of-truth references')
pdf.bullets([
    'README.md for setup, features, assumptions, and API summary.',
    'ClinicApp.Api/Program.cs for dependency injection, authentication, authorization, CORS, Swagger, and startup.',
    'ClinicApp.Application/Services for business use cases and token behavior.',
    'ClinicApp.Infrastructure/Data for entity mappings, integrity controls, auditing, and migrations.',
    'ClinicApp.Api.Tests and .github/workflows/ci.yml for automated quality coverage.'
])
pdf.callout('Document status', 'Prepared from the repository implementation as of July 2026. Validate production policies, infrastructure, and regulatory requirements with the organization before release.', LIGHT, BLUE)

pdf.page('Submission Package', '13  Delivery & Repository')
pdf.callout('Repository', 'Source code and project deliverables are available at: https://github.com/hanymetwaly/ClinicApplication', (0.92, .98, .95), GREEN)
pdf.heading('Required deliverables checklist')
pdf.table(['DELIVERABLE', 'LOCATION', 'STATUS'], [
    ['Source code', 'GitHub repository - complete solution', 'Included'],
    ['SQL creation script', 'sql/schema.sql', 'Included'],
    ['README', 'README.md at repository root', 'Included'],
    ['Setup instructions', 'README Local setup section + this appendix', 'Included'],
    ['Technologies used', 'README Technology section + this document', 'Included'],
    ['Architecture overview', 'README Architecture + pages 6-7 here', 'Included'],
    ['Assumptions made', 'README Validation and assumptions', 'Included'],
    ['API documentation', 'Swagger UI at /swagger while API is running', 'Included'],
], [155, 274, 70], 7.3, 31)
pdf.heading('Repository structure')
pdf.table(['PATH', 'PURPOSE'], [
    ['ClinicApp.Domain', 'Domain entities, enums, and repository contracts'],
    ['ClinicApp.Application', 'DTOs, interfaces, use cases, validation, and security services'],
    ['ClinicApp.Infrastructure', 'EF Core, SQL mappings, repositories, migrations, files, SMTP'],
    ['ClinicApp.Api', 'REST controllers, middleware, policies, Swagger, application startup'],
    ['ClinicApp.Frontend', 'React user interface, calendar, charts, and PDF invoice output'],
    ['ClinicApp.Api.Tests', 'xUnit automated backend tests'],
    ['sql/schema.sql', 'Idempotent SQL Server database creation script'],
    ['README.md', 'Primary setup, architecture, assumptions, and API guide'],
], [164, 335], 7.4, 29)
pdf.page('Prerequisites & Local Installation', '14  Run Locally')
pdf.heading('Required software')
pdf.table(['TOOL', 'REQUIRED VERSION / PURPOSE'], [
    ['Git', 'Clone the GitHub repository'],
    ['.NET SDK', '.NET 8 SDK for API, migrations, build, and tests'],
    ['Node.js', 'Node.js 20 or newer with npm for the React frontend'],
    ['Docker Desktop', 'Docker Compose and the local SQL Server-compatible container'],
    ['Web browser', 'Current Chrome, Edge, Firefox, or Safari'],
], [145, 354], 8, 32)
pdf.heading('1. Clone the repository')
pdf.callout('Terminal', 'git clone https://github.com/hanymetwaly/ClinicApplication.git\ncd ClinicApplication', (0.94, .96, .98), NAVY)
pdf.heading('2. Start the local database')
pdf.callout('Terminal', 'docker compose up -d sqlserver', (0.94, .96, .98), NAVY)
pdf.paragraph('The local database listens on localhost:1433 and uses a persistent Docker volume. The repository includes development defaults so reviewers can run the demonstration without creating configuration files.', fill=MUTED)
pdf.heading('3. Restore, build, and test the backend')
pdf.callout('Terminal', 'dotnet tool restore\ndotnet restore ClinicAssessment.sln\ndotnet build ClinicAssessment.sln --no-restore\ndotnet test ClinicAssessment.sln --no-build', (0.94, .96, .98), NAVY)
pdf.callout('If Docker is unavailable', 'Use an existing SQL Server instance and override ConnectionStrings__DefaultConnection before starting the API. The SQL creation deliverable is sql/schema.sql.', (1, .97, .90), AMBER)

pdf.page('Start & Verify the Application', '14  Run Locally')
pdf.heading('4. Run the ASP.NET Core API')
pdf.callout('Terminal 1 - repository root', 'dotnet run --project ClinicApp.Api/ClinicApp.Api.csproj', (0.94, .96, .98), NAVY)
pdf.bullets([
    'API base URL: http://localhost:5002',
    'Swagger UI: http://localhost:5002/swagger',
    'On startup, EF Core applies pending migrations and seeds demonstration roles, users, one doctor, and one patient.'
])
pdf.heading('5. Run the React frontend')
pdf.callout('Terminal 2', 'cd ClinicApp.Frontend\nnpm ci\nnpm run dev', (0.94, .96, .98), NAVY)
pdf.bullets([
    'Open http://localhost:5173 in a browser.',
    'The Vite development server proxies /api requests to the API on port 5002.'
])
pdf.heading('6. Demonstration accounts')
pdf.table(['ROLE', 'USERNAME', 'PASSWORD', 'EXPECTED ACCESS'], [
    ['Admin', 'admin', 'admin', 'Full current application access'],
    ['Receptionist', 'receptionist', 'receptionist', 'Patients, scheduling, billing'],
    ['Doctor', 'doctor', 'doctor', 'Read-only clinical/operational views'],
], [85, 105, 105, 204], 7.7, 33)
pdf.callout('Development only', 'These credentials, the local database password, and the development JWT key exist only to support clone-and-run assessment use. They must not be reused in production.', (1, .94, .94), RED)

pdf.page('Verification & Configuration', '14  Run Locally')
pdf.heading('Successful startup checklist')
pdf.bullets([
    'docker compose ps shows the sqlserver service running.',
    'The API terminal reports: Now listening on http://localhost:5002.',
    'Swagger loads and exposes authentication, patients, doctors, appointments, invoices, and dashboard APIs.',
    'The React application loads at http://localhost:5173 and accepts one of the demonstration accounts.',
    'After login, role-appropriate screens and actions are available.',
    'Backend tests pass, and npm run lint plus npm run build complete successfully.'
])
pdf.heading('Optional quality commands')
pdf.callout('Terminal', 'dotnet format ClinicAssessment.sln --verify-no-changes\ndotnet test ClinicAssessment.sln\ncd ClinicApp.Frontend\nnpm run lint\nnpm run build', (0.94, .96, .98), NAVY)
pdf.heading('Environment-variable overrides')
pdf.table(['VARIABLE', 'PURPOSE'], [
    ['ConnectionStrings__DefaultConnection', 'Production or alternate SQL Server connection'],
    ['Jwt__Key / Jwt__Issuer / Jwt__Audience', 'JWT signing and validation configuration'],
    ['Pagination__DefaultPageSize', 'Default API page size'],
    ['Pagination__MaxPageSize', 'Maximum accepted API page size'],
    ['Cors__AllowedOrigins__0', 'Allowed frontend origin'],
    ['Smtp__Host / Port / Username / Password', 'Appointment reminder email delivery'],
    ['Storage__UploadsPath', 'Patient document storage location'],
], [224, 275], 7.2, 29)
pdf.callout('Production reminder', 'Before production: replace all defaults with managed secrets, disable or protect public Swagger, enforce HTTPS, remove demo accounts, configure durable file storage, and establish backups, monitoring, and alerting.', (1, .97, .90), AMBER)

output = Path(__file__).with_name('ClinicApp_Technical_Business_Overview.pdf')
pdf.finish(output)
print(output)
