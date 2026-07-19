import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Chart as ChartJS, BarController, LineController, CategoryScale, LinearScale, BarElement, LineElement, PointElement, Title, Tooltip, Legend, Filler } from 'chart.js'
import jsPDF from 'jspdf'
import 'jspdf-autotable'
import { apiFetch, clearSession, getSession, saveSession } from './api'
import { Icon } from './Icons'
import { toast, TOAST_EVENT } from './toast'

ChartJS.register(BarController, LineController, CategoryScale, LinearScale, BarElement, LineElement, PointElement, Title, Tooltip, Legend, Filler)

const emptyPatient = {
  fullName: '',
  phoneNumber: '',
  email: '',
  dateOfBirth: '',
  medicalHistory: '',
  insuranceInfo: '',
}

const emptyItem = () => ({ description: '', quantity: 1, unitPrice: '' })
const money = (value, currency = 'USD') => new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency,
}).format(value || 0)
const initials = (name) => (name || '?')
  .split(' ')
  .filter(Boolean)
  .slice(0, 2)
  .map((part) => part[0]?.toUpperCase())
  .join('')
const toInputDateTime = (value) => {
  const date = new Date(value)
  date.setMinutes(date.getMinutes() - date.getTimezoneOffset())
  return date.toISOString().slice(0, 16)
}
const dateKey = (value) => {
  const d = new Date(value)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}
const isSameDay = (a, b) => dateKey(a) === dateKey(b)
const startOfWeek = (value) => {
  const date = new Date(value)
  const day = date.getDay() || 7
  date.setHours(0, 0, 0, 0)
  date.setDate(date.getDate() - day + 1)
  return date
}

function Toaster() {
  const [toasts, setToasts] = useState([])

  useEffect(() => {
    const handler = (event) => {
      const item = event.detail
      setToasts((current) => [...current, item])
      setTimeout(() => {
        setToasts((current) => current.filter((t) => t.id !== item.id))
      }, 4000)
    }
    window.addEventListener(TOAST_EVENT, handler)
    return () => window.removeEventListener(TOAST_EVENT, handler)
  }, [])

  return (
    <div className="toaster">
      {toasts.map((item) => (
        <div className={`toast ${item.type}`} key={item.id} role="status">
          <Icon name={item.type === 'error' ? 'alert' : item.type === 'info' ? 'clock' : 'check'} size={18} />
          <span>{item.message}</span>
          <button className="toast-close" onClick={() => setToasts((current) => current.filter((t) => t.id !== item.id))} aria-label="Dismiss">
            <Icon name="close" size={15} />
          </button>
        </div>
      ))}
    </div>
  )
}

function Login({ onLogin }) {
  const [form, setForm] = useState({ username: 'admin', password: 'admin' })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function submit(event) {
    event.preventDefault()
    setLoading(true)
    setError('')
    try {
      const session = await apiFetch('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify(form),
      }, false)
      saveSession(session)
      toast.success(`Welcome back, ${session.username}`)
      onLogin(session)
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setLoading(false)
    }
  }

  const demo = [
    ['Admin', 'admin', 'admin'],
    ['Doctor', 'doctor', 'doctor'],
    ['Receptionist', 'receptionist', 'receptionist'],
  ]

  return (
    <main className="login-shell">
      <section className="login-hero">
        <div className="login-hero-inner">
          <div className="brand"><span className="brand-mark"><Icon name="stethoscope" size={22} /></span><div><strong>CareFlow</strong><small>Clinic Suite</small></div></div>
          <h2>Run your clinic with clarity and calm.</h2>
          <p>Appointments, patients, and billing in one modern workspace built for care teams.</p>
          <ul className="hero-points">
            <li><Icon name="calendar" size={18} /> Smart scheduling with double-booking protection</li>
            <li><Icon name="patients" size={18} /> Complete patient records and history</li>
            <li><Icon name="revenue" size={18} /> Itemized billing, VAT, and payments</li>
          </ul>
        </div>
      </section>
      <section className="login-panel">
        <div className="login-card">
          <p className="eyebrow">Clinic operations</p>
          <h1>Welcome back</h1>
          <p className="muted">Sign in to manage patients, appointments, and billing.</p>
          {error && <div className="alert error"><Icon name="alert" size={18} /><span>{error}</span></div>}
          <form onSubmit={submit} className="stack">
            <label>
              Username
              <div className="input-icon">
                <Icon name="user" size={18} />
                <input required value={form.username} onChange={(event) => setForm({ ...form, username: event.target.value })} autoComplete="username" />
              </div>
            </label>
            <label>
              Password
              <div className="input-icon">
                <Icon name="lock" size={18} />
                <input required type="password" value={form.password} onChange={(event) => setForm({ ...form, password: event.target.value })} autoComplete="current-password" />
              </div>
            </label>
            <button className="primary block" disabled={loading}>
              {loading ? 'Signing in…' : 'Sign in'}
            </button>
          </form>
          <div className="demo-grid">
            {demo.map(([role, username, password]) => (
              <button type="button" className="demo-chip" key={role} onClick={() => setForm({ username, password })}>
                <strong>{role}</strong>
                <span>{username} / {password}</span>
              </button>
            ))}
          </div>
        </div>
      </section>
    </main>
  )
}

function BarChart({ labels, data, color, label }) {
  const canvasRef = useRef(null)
  const chartRef = useRef(null)

  useEffect(() => {
    if (!canvasRef.current) return
    chartRef.current?.destroy()
    const ctx = canvasRef.current.getContext('2d')
    const chart = new ChartJS(ctx, {
      type: 'bar',
      data: { labels, datasets: [{ data, backgroundColor: color, borderRadius: 10, maxBarThickness: 36, hoverBackgroundColor: `${color}cc` }] },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: 'rgba(22, 35, 59, .92)',
            padding: 10,
            cornerRadius: 8,
            displayColors: false,
            callbacks: { label: (context) => `${context.parsed.y} ${label || ''}`.trim() },
          },
        },
        scales: {
          y: { beginAtZero: true, ticks: { precision: 0 }, grid: { color: 'rgba(148,163,184,.12)' } },
          x: { grid: { display: false } },
        },
      },
    })
    chartRef.current = chart
    return () => { chart.destroy(); chartRef.current = null }
  }, [labels, data, color, label])

  return <canvas ref={canvasRef} />
}

function LineChart({ labels, data, color, label }) {
  const canvasRef = useRef(null)
  const chartRef = useRef(null)

  useEffect(() => {
    if (!canvasRef.current) return
    chartRef.current?.destroy()
    const ctx = canvasRef.current.getContext('2d')
    const gradient = ctx.createLinearGradient(0, 0, 0, 280)
    gradient.addColorStop(0, `${color}44`)
    gradient.addColorStop(1, `${color}00`)
    const chart = new ChartJS(ctx, {
      type: 'line',
      data: {
        labels,
        datasets: [{ data, borderColor: color, backgroundColor: gradient, fill: true, tension: 0.4, pointRadius: 5, pointHoverRadius: 7, pointBackgroundColor: color, pointBorderColor: '#fff', pointBorderWidth: 2, borderWidth: 3 }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: 'rgba(22, 35, 59, .92)',
            padding: 10,
            cornerRadius: 8,
            displayColors: false,
            callbacks: { label: (context) => `${context.parsed.y} ${label || ''}`.trim() },
          },
        },
        scales: {
          y: { beginAtZero: true, grid: { color: 'rgba(148,163,184,.12)' } },
          x: { grid: { display: false } },
        },
      },
    })
    chartRef.current = chart
    return () => { chart.destroy(); chartRef.current = null }
  }, [labels, data, color, label])

  return <canvas ref={canvasRef} />
}

function Dashboard() {
  const [summary, setSummary] = useState(null)
  const [charts, setCharts] = useState(null)
  const [error, setError] = useState('')

  useEffect(() => {
    apiFetch('/api/dashboard').then(setSummary).catch((requestError) => setError(requestError.message))
    apiFetch('/api/dashboard/charts').then(setCharts).catch((requestError) => setError(requestError.message))
  }, [])

  if (error) return <div className="alert error"><Icon name="alert" size={18} /><span>{error}</span></div>
  if (!summary || !charts) return <Loading />

  const cards = [
    { label: 'Today’s appointments', value: summary.todayAppointments, icon: 'calendar', tone: 'blue' },
    { label: 'Total patients', value: summary.totalPatients, icon: 'patients', tone: 'teal' },
    { label: 'Revenue', value: money(summary.revenue), icon: 'revenue', tone: 'green' },
    { label: 'Unpaid invoices', value: summary.unpaidInvoices, icon: 'invoice', tone: 'amber' },
  ]
  return (
    <section className="dashboard">
      <div className="metrics">
        {cards.map((card) => (
          <article className={`metric-card tone-${card.tone}`} key={card.label}>
            <div className="metric-icon"><Icon name={card.icon} size={22} /></div>
            <div className="metric-body">
              <span>{card.label}</span>
              <strong>{card.value}</strong>
            </div>
          </article>
        ))}
      </div>
      <div className="charts">
        <article className="panel chart-card">
          <header className="panel-head"><h2>Appointments</h2><span className="chip">Past & next 7 days</span></header>
          <div className="chart"><BarChart labels={charts.appointments.labels} data={charts.appointments.data} color="#2f6fed" label="appointments" /></div>
        </article>
        <article className="panel chart-card">
          <header className="panel-head"><h2>Revenue</h2><span className="chip">Last 6 months</span></header>
          <div className="chart"><LineChart labels={charts.revenue.labels} data={charts.revenue.data} color="#0fb5a0" label="revenue" /></div>
        </article>
      </div>
    </section>
  )
}

function Patients({ canManage }) {
  const fileInputRef = useRef(null)
  const [patients, setPatients] = useState([])
  const [search, setSearch] = useState('')
  const [form, setForm] = useState(emptyPatient)
  const [editingId, setEditingId] = useState(null)
  const [showForm, setShowForm] = useState(false)
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [totalPages, setTotalPages] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [submitting, setSubmitting] = useState(false)
  const [uploadingPatientId, setUploadingPatientId] = useState(null)
  const [pendingUploadPatientId, setPendingUploadPatientId] = useState(null)

  const loadPatients = useCallback(async () => {
    setLoading(true)
    try {
      const result = await apiFetch(`/api/patients?page=${page}&pageSize=${pageSize}&search=${encodeURIComponent(search)}`)
      setPatients(result.items)
      setTotalPages(result.totalPages || 1)
      setTotalCount(result.totalCount || 0)
    } catch (requestError) {
      toast.error(requestError.message)
    } finally {
      setLoading(false)
    }
  }, [search, page, pageSize])

  useEffect(() => { loadPatients() }, [loadPatients])

  async function submit(event) {
    event.preventDefault()
    setSubmitting(true)
    try {
      await apiFetch(editingId ? `/api/patients/${editingId}` : '/api/patients', {
        method: editingId ? 'PUT' : 'POST',
        body: JSON.stringify(form),
      })
      toast.success(editingId ? 'Patient updated.' : 'Patient created.')
      setForm(emptyPatient)
      setEditingId(null)
      setShowForm(false)
      await loadPatients()
    } catch (requestError) {
      toast.error(requestError.message)
    } finally {
      setSubmitting(false)
    }
  }

  function edit(patient) {
    setEditingId(patient.id)
    setShowForm(true)
    setForm({
      fullName: patient.fullName,
      phoneNumber: patient.phoneNumber,
      email: patient.email,
      dateOfBirth: patient.dateOfBirth,
      medicalHistory: patient.medicalHistory,
      insuranceInfo: patient.insuranceInfo,
    })
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  function startCreate() {
    setEditingId(null)
    setForm(emptyPatient)
    setShowForm((current) => !current)
  }

  async function remove(patient) {
    if (!window.confirm(`Delete ${patient.fullName}?`)) return
    try {
      await apiFetch(`/api/patients/${patient.id}`, { method: 'DELETE' })
      toast.success('Patient deleted.')
      await loadPatients()
    } catch (requestError) {
      toast.error(requestError.message)
    }
  }

  async function uploadFile(patient, file) {
    if (!file) return
    setUploadingPatientId(patient.id)
    try {
      const body = new FormData()
      body.append('file', file)
      await apiFetch(`/api/patients/${patient.id}/documents`, { method: 'POST', body })
      toast.success(`Document uploaded for ${patient.fullName}.`)
      await loadPatients()
    } catch (requestError) {
      toast.error(requestError.message)
    } finally {
      setUploadingPatientId(null)
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  return (
    <section className="paged-section">
      {canManage && showForm && (
        <form className="panel form-grid" onSubmit={submit}>
          <header className="panel-head wide"><h2>{editingId ? 'Edit patient' : 'New patient'}</h2></header>
          <label>Full name<input required maxLength="200" value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} /></label>
          <label>Phone<input required maxLength="50" value={form.phoneNumber} onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })} /></label>
          <label>Email<input required type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /></label>
          <label>Date of birth<input required type="date" max={new Date().toISOString().split('T')[0]} value={form.dateOfBirth} onChange={(e) => setForm({ ...form, dateOfBirth: e.target.value })} /></label>
          <label className="wide">Medical history<textarea maxLength="10000" value={form.medicalHistory} onChange={(e) => setForm({ ...form, medicalHistory: e.target.value })} /></label>
          <label className="wide">Insurance information<textarea maxLength="500" value={form.insuranceInfo} onChange={(e) => setForm({ ...form, insuranceInfo: e.target.value })} /></label>
          <div className="actions wide">
            <button className="primary" disabled={submitting}>{submitting ? 'Saving…' : 'Save patient'}</button>
            <button type="button" className="secondary" onClick={() => { setEditingId(null); setForm(emptyPatient); setShowForm(false) }}>Cancel</button>
          </div>
        </form>
      )}
      <div className="toolbar">
        <div className="input-icon search">
          <Icon name="search" size={18} />
          <input placeholder="Search name, email, or phone" value={search} onChange={(event) => { setSearch(event.target.value); setPage(1) }} />
        </div>
        <div className="toolbar-right">
          <span className="chip">{totalCount} record(s)</span>
          {canManage && (
            <button className="primary" onClick={startCreate}><Icon name="plus" size={17} /> Add patient</button>
          )}
        </div>
      </div>
      {loading ? <Loading /> : patients.length === 0 ? (
        <EmptyState icon="patients" title="No patients found" subtitle="Try a different search, or add a new patient." />
      ) : (
        <div className="table-wrap">
          <table>
            <thead><tr><th>Patient</th><th>Contact</th><th>Medical history</th><th>Insurance</th><th>Docs</th>{canManage && <th className="right">Actions</th>}</tr></thead>
            <tbody>
              {patients.map((patient) => (
                <tr key={patient.id}>
                  <td>
                    <div className="cell-user">
                      <span className="avatar">{initials(patient.fullName)}</span>
                      <div><strong>{patient.fullName}</strong><small>{patient.dateOfBirth}</small></div>
                    </div>
                  </td>
                  <td><span className="cell-line"><Icon name="mail" size={14} />{patient.email}</span><span className="cell-line muted"><Icon name="phone" size={14} />{patient.phoneNumber}</span></td>
                  <td className="truncate">{patient.medicalHistory || '—'}</td>
                  <td className="truncate">{patient.insuranceInfo || '—'}</td>
                  <td><span className="chip soft">{patient.documentCount || 0}</span></td>
                  {canManage && (
                    <td className="right">
                      <div className="icon-actions">
                        <button className="icon-btn" title="Edit" onClick={() => edit(patient)}><Icon name="edit" size={17} /></button>
                        <button className="icon-btn" title="Upload document" disabled={uploadingPatientId === patient.id} onClick={() => { setPendingUploadPatientId(patient.id); fileInputRef.current?.click() }}><Icon name="upload" size={17} /></button>
                        <button className="icon-btn danger" title="Delete" onClick={() => remove(patient)}><Icon name="trash" size={17} /></button>
                      </div>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
          <input ref={fileInputRef} type="file" style={{ display: 'none' }} onChange={(e) => {
            const file = e.target.files?.[0]
            const patientId = pendingUploadPatientId
            setPendingUploadPatientId(null)
            const patient = patients.find((p) => p.id === patientId)
            if (file && patient) uploadFile(patient, file)
          }} />
        </div>
      )}
      <Pagination page={page} totalPages={totalPages} pageSize={pageSize} onPageChange={setPage} onPageSizeChange={(size) => { setPage(1); setPageSize(size) }} />
    </section>
  )
}

function Appointments({ canManage }) {
  const [weekStart, setWeekStart] = useState(startOfWeek(new Date()))
  const [appointments, setAppointments] = useState([])
  const [patients, setPatients] = useState([])
  const [doctors, setDoctors] = useState([])
  const [form, setForm] = useState({ patientId: '', doctorId: '', startTime: '', durationMinutes: 30, notes: '' })
  const [reschedule, setReschedule] = useState(null)
  const [showForm, setShowForm] = useState(false)
  const [loading, setLoading] = useState(true)

  const durationOptions = [15, 30, 45, 60, 90, 120]

  function computeEndTime(startTime, durationMinutes) {
    const end = new Date(startTime)
    end.setMinutes(end.getMinutes() + Number(durationMinutes))
    return end
  }

  const days = useMemo(() => Array.from({ length: 7 }, (_, index) => {
    const date = new Date(weekStart)
    date.setDate(date.getDate() + index)
    return date
  }), [weekStart])

  const load = useCallback(async () => {
    setLoading(true)
    const weekEnd = new Date(weekStart)
    weekEnd.setDate(weekEnd.getDate() + 7)
    try {
      const [appointmentResult, patientResult, doctorResult] = await Promise.all([
        apiFetch(`/api/appointments?startDate=${weekStart.toISOString()}&endDate=${weekEnd.toISOString()}&pageSize=100`),
        apiFetch('/api/patients?pageSize=100'),
        apiFetch('/api/doctors'),
      ])
      setAppointments(appointmentResult.items)
      setPatients(patientResult.items)
      setDoctors(doctorResult)
      setForm((current) => ({
        ...current,
        patientId: current.patientId || patientResult.items[0]?.id || '',
        doctorId: current.doctorId || doctorResult[0]?.id || '',
      }))
    } catch (requestError) {
      toast.error(requestError.message)
    } finally {
      setLoading(false)
    }
  }, [weekStart])

  useEffect(() => { load() }, [load])

  async function book(event) {
    event.preventDefault()
    try {
      await apiFetch('/api/appointments', {
        method: 'POST',
        body: JSON.stringify({
          ...form,
          startTime: new Date(form.startTime).toISOString(),
          endTime: computeEndTime(form.startTime, form.durationMinutes).toISOString(),
        }),
      })
      toast.success('Appointment booked.')
      setForm({ ...form, startTime: '', notes: '' })
      setShowForm(false)
      await load()
    } catch (requestError) {
      toast.error(requestError.message)
    }
  }

  async function cancel(appointment) {
    if (!window.confirm(`Cancel the appointment for ${appointment.patientName}?`)) return
    try {
      await apiFetch(`/api/appointments/${appointment.id}/cancel`, { method: 'POST' })
      toast.success('Appointment cancelled.')
      await load()
    } catch (requestError) {
      toast.error(requestError.message)
    }
  }

  async function saveReschedule(event) {
    event.preventDefault()
    try {
      await apiFetch(`/api/appointments/${reschedule.id}/reschedule`, {
        method: 'POST',
        body: JSON.stringify({
          startTime: new Date(reschedule.startTime).toISOString(),
          endTime: computeEndTime(reschedule.startTime, reschedule.durationMinutes).toISOString(),
        }),
      })
      setReschedule(null)
      toast.success('Appointment rescheduled.')
      await load()
    } catch (requestError) {
      toast.error(requestError.message)
    }
  }

  function moveWeek(offset) {
    const next = new Date(weekStart)
    next.setDate(next.getDate() + offset * 7)
    setWeekStart(next)
  }

  const today = new Date()

  return (
    <section>
      {canManage && showForm && (
        <form className="panel form-grid" onSubmit={book}>
          <header className="panel-head wide"><h2>Book appointment</h2></header>
          <label>Patient<select required value={form.patientId} onChange={(e) => setForm({ ...form, patientId: e.target.value })}>{patients.map((patient) => <option key={patient.id} value={patient.id}>{patient.fullName}</option>)}</select></label>
          <label>Doctor<select required value={form.doctorId} onChange={(e) => setForm({ ...form, doctorId: e.target.value })}>{doctors.map((doctor) => <option key={doctor.id} value={doctor.id}>{doctor.fullName} — {doctor.specialty}</option>)}</select></label>
          <label>Starts<input required type="datetime-local" value={form.startTime} onChange={(e) => setForm({ ...form, startTime: e.target.value })} /></label>
          <label>Duration<select value={form.durationMinutes} onChange={(e) => setForm({ ...form, durationMinutes: Number(e.target.value) })}>{durationOptions.map((min) => <option key={min} value={min}>{min === 60 ? '1 hour' : min < 60 ? `${min} min` : `${min / 60} hours`}</option>)}</select></label>
          <label className="wide">Notes<textarea maxLength="2000" value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} /></label>
          <div className="actions wide">
            <button className="primary">Book appointment</button>
            <button type="button" className="secondary" onClick={() => setShowForm(false)}>Cancel</button>
          </div>
        </form>
      )}
      {reschedule && (
        <form className="panel inline-form" onSubmit={saveReschedule}>
          <strong>Reschedule {reschedule.patientName}</strong>
          <input required type="datetime-local" value={reschedule.startTime} onChange={(e) => setReschedule({ ...reschedule, startTime: e.target.value })} />
          <select value={reschedule.durationMinutes} onChange={(e) => setReschedule({ ...reschedule, durationMinutes: Number(e.target.value) })}>{durationOptions.map((min) => <option key={min} value={min}>{min === 60 ? '1 hour' : min < 60 ? `${min} min` : `${min / 60} hours`}</option>)}</select>
          <button className="primary">Save</button>
          <button type="button" className="secondary" onClick={() => setReschedule(null)}>Cancel</button>
        </form>
      )}
      <div className="calendar-toolbar">
        <div className="week-nav">
          <button className="icon-btn" onClick={() => moveWeek(-1)} aria-label="Previous week"><Icon name="chevron-left" size={18} /></button>
          <strong>{days[0].toLocaleDateString(undefined, { month: 'short', day: 'numeric' })} – {days[6].toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}</strong>
          <button className="icon-btn" onClick={() => moveWeek(1)} aria-label="Next week"><Icon name="chevron-right" size={18} /></button>
        </div>
        <div className="toolbar-right">
          <button className="secondary" onClick={() => setWeekStart(startOfWeek(new Date()))}>Today</button>
          {canManage && <button className="primary" onClick={() => setShowForm((current) => !current)}><Icon name="plus" size={17} /> Book</button>}
        </div>
      </div>
      {loading ? <Loading /> : (
        <div className="calendar">
          {days.map((day) => {
            const dayAppointments = appointments
              .filter((appointment) => isSameDay(appointment.startTime, day))
              .sort((a, b) => new Date(a.startTime) - new Date(b.startTime))
            const isToday = isSameDay(day, today)
            return (
              <article className={`calendar-day${isToday ? ' today' : ''}`} key={day.toISOString()}>
                <header>
                  <span>{day.toLocaleDateString(undefined, { weekday: 'short' })}</span>
                  <strong>{day.getDate()}</strong>
                </header>
                <div className="calendar-body">
                  {dayAppointments.length === 0 && <p className="calendar-empty">No visits</p>}
                  {dayAppointments.map((appointment) => (
                    <div className={`appointment ${appointment.status.toLowerCase()}`} key={appointment.id}>
                      <div className="appt-top">
                        <strong>{new Date(appointment.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</strong>
                        <span className={`dot ${appointment.status.toLowerCase()}`} />
                      </div>
                      <span className="appt-name">{appointment.patientName}</span>
                      <small>{appointment.doctorName}</small>
                      {canManage && appointment.status !== 'Cancelled' && (
                        <div className="mini-actions">
                          <button className="link" onClick={() => setReschedule({ ...appointment, startTime: toInputDateTime(appointment.startTime), durationMinutes: 30 })}>Move</button>
                          <button className="link danger" onClick={() => cancel(appointment)}>Cancel</button>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </article>
            )
          })}
        </div>
      )}
    </section>
  )
}

function downloadInvoicePdf(invoice) {
  const doc = new jsPDF()
  doc.setFontSize(18)
  doc.text('CareFlow Clinic', 14, 20)
  doc.setFontSize(12)
  doc.text(`Invoice ${invoice.id.slice(0, 8)}`, 14, 30)
  doc.text(`Patient: ${invoice.patientName}`, 14, 38)
  doc.text(`Date: ${new Date(invoice.invoiceDate).toLocaleString()}`, 14, 46)

  const body = invoice.items.map((item) => [
    item.description,
    item.quantity,
    money(item.unitPrice),
    money(item.totalPrice),
  ])

  doc.autoTable({
    startY: 54,
    head: [['Description', 'Quantity', 'Unit Price', 'Total']],
    body,
  })

  const finalY = doc.lastAutoTable.finalY + 10
  doc.text(`Subtotal: ${money(invoice.totalAmount)}`, 14, finalY)
  doc.text(`VAT (${invoice.vatRate}%): ${money(invoice.vatAmount)}`, 14, finalY + 8)
  doc.text(`Discount (${invoice.discountRate}%): -${money(invoice.discountAmount)}`, 14, finalY + 16)
  doc.text(`Net Total: ${money(invoice.netAmount)}`, 14, finalY + 24)
  doc.text(`Paid: ${money(invoice.paidAmount)}`, 14, finalY + 32)
  doc.text(`Outstanding: ${money(invoice.outstandingAmount)}`, 14, finalY + 40)

  doc.save(`invoice-${invoice.id.slice(0, 8)}.pdf`)
}

function Billing() {
  const [patients, setPatients] = useState([])
  const [invoices, setInvoices] = useState([])
  const [form, setForm] = useState({ patientId: '', currency: 'USD', vatRate: 15, discountRate: 0, items: [emptyItem()] })
  const [payments, setPayments] = useState({})
  const [showForm, setShowForm] = useState(false)
  const [loading, setLoading] = useState(true)
  const [invoicePage, setInvoicePage] = useState(1)
  const [invoicePageSize, setInvoicePageSize] = useState(4)
  const [invoiceTotalPages, setInvoiceTotalPages] = useState(1)
  const [invoiceCount, setInvoiceCount] = useState(0)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const [patientResult, invoiceResult] = await Promise.all([
        apiFetch('/api/patients?pageSize=100'),
        apiFetch(`/api/invoices?page=${invoicePage}&pageSize=${invoicePageSize}`),
      ])
      setPatients(patientResult.items)
      setInvoices(invoiceResult.items)
      setInvoiceTotalPages(invoiceResult.totalPages || 1)
      setInvoiceCount(invoiceResult.totalCount || 0)
      setForm((current) => ({ ...current, patientId: current.patientId || patientResult.items[0]?.id || '' }))
    } catch (requestError) {
      toast.error(requestError.message)
    } finally {
      setLoading(false)
    }
  }, [invoicePage, invoicePageSize])

  useEffect(() => { load() }, [load])

  function updateItem(index, field, value) {
    setForm({
      ...form,
      items: form.items.map((item, itemIndex) => itemIndex === index ? { ...item, [field]: value } : item),
    })
  }

  const draftTotal = form.items.reduce((sum, item) => sum + (Number(item.quantity) || 0) * (Number(item.unitPrice) || 0), 0)

  async function createInvoice(event) {
    event.preventDefault()
    try {
      await apiFetch('/api/invoices', {
        method: 'POST',
        body: JSON.stringify({
          patientId: form.patientId,
          vatRate: Number(form.vatRate),
          discountRate: Number(form.discountRate),
          items: form.items.map((item) => ({
            description: item.description,
            quantity: Number(item.quantity),
            unitPrice: Number(item.unitPrice),
          })),
        }),
      })
      toast.success('Invoice created.')
      setForm({ ...form, items: [emptyItem()] })
      setShowForm(false)
      await load()
    } catch (requestError) {
      toast.error(requestError.message)
    }
  }

  async function pay(invoice) {
    const amount = Number(payments[invoice.id])
    if (!amount) return
    try {
      await apiFetch(`/api/invoices/${invoice.id}/payments`, {
        method: 'POST',
        body: JSON.stringify({ amount }),
      })
      toast.success('Payment recorded.')
      setPayments({ ...payments, [invoice.id]: '' })
      await load()
    } catch (requestError) {
      toast.error(requestError.message)
    }
  }

  return (
    <section className="paged-section">
      <div className="toolbar">
        <span className="chip">{invoiceCount} invoice(s)</span>
        <button className="primary" onClick={() => setShowForm((current) => !current)}><Icon name="plus" size={17} /> Create invoice</button>
      </div>
      {showForm && (
        <form className="panel invoice-form" onSubmit={createInvoice}>
          <header className="panel-head"><h2>Create invoice</h2><span className="chip soft">Subtotal {money(draftTotal, form.currency)}</span></header>
          <div className="form-grid">
            <label>Patient<select required value={form.patientId} onChange={(e) => setForm({ ...form, patientId: e.target.value })}>{patients.map((patient) => <option key={patient.id} value={patient.id}>{patient.fullName}</option>)}</select></label>
            <label>Currency<select value={form.currency} onChange={(e) => setForm({ ...form, currency: e.target.value })}>{['USD', 'EGP', 'EUR'].map((c) => <option key={c} value={c}>{c}</option>)}</select></label>
            <label>VAT %<input required min="0" max="100" step="0.01" type="number" value={form.vatRate} onChange={(e) => setForm({ ...form, vatRate: e.target.value })} /></label>
            <label>Discount %<input required min="0" max="100" step="0.01" type="number" value={form.discountRate} onChange={(e) => setForm({ ...form, discountRate: e.target.value })} /></label>
          </div>
          <div className="invoice-items">
            <div className="invoice-item head"><span>Description</span><span>Amount</span><span /></div>
            {form.items.map((item, index) => (
              <div className="invoice-item" key={index}>
                <input required maxLength="300" placeholder="Description" value={item.description} onChange={(e) => updateItem(index, 'description', e.target.value)} />
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                  <span>{form.currency}</span>
                  <input style={{ flex: '1' }} required min="1" step="1" type="number" aria-label="Amount" placeholder="0" value={item.unitPrice} onChange={(e) => updateItem(index, 'unitPrice', e.target.value)} />
                </div>
                <button type="button" className="icon-btn danger" title="Remove" disabled={form.items.length === 1} onClick={() => setForm({ ...form, items: form.items.filter((_, itemIndex) => itemIndex !== index) })}><Icon name="trash" size={16} /></button>
              </div>
            ))}
          </div>
          <div className="actions">
            <button type="button" className="secondary" onClick={() => setForm({ ...form, items: [...form.items, emptyItem()] })}><Icon name="plus" size={16} /> Add item</button>
            <button className="primary">Create invoice</button>
          </div>
        </form>
      )}
      {loading ? <Loading /> : invoices.length === 0 ? (
        <EmptyState icon="invoice" title="No invoices yet" subtitle="Create an invoice to start billing patients." />
      ) : (
        <div className="invoice-list">
          {invoices.map((invoice) => (
            <article className="invoice-card" key={invoice.id}>
              <header>
                <div className="cell-user">
                  <span className="avatar">{initials(invoice.patientName)}</span>
                  <div><span className="eyebrow">Invoice {invoice.id.slice(0, 8)}</span><h3>{invoice.patientName}</h3></div>
                </div>
                <span className={`badge ${invoice.status.toLowerCase()}`}>{invoice.status}</span>
              </header>
              <ul>{invoice.items.map((item) => <li key={item.id}><span>{item.quantity} × {item.description}</span><strong>{money(item.totalPrice)}</strong></li>)}</ul>
              <div className="invoice-totals">
                <span>Subtotal <strong>{money(invoice.totalAmount)}</strong></span>
                <span>VAT ({invoice.vatRate}%) <strong>{money(invoice.vatAmount)}</strong></span>
                <span>Discount ({invoice.discountRate}%) <strong>−{money(invoice.discountAmount)}</strong></span>
                <span className="net">Net total <strong>{money(invoice.netAmount)}</strong></span>
                <span>Paid <strong>{money(invoice.paidAmount)}</strong></span>
                <span>Outstanding <strong>{money(invoice.outstandingAmount)}</strong></span>
              </div>
              <div className="invoice-foot">
                <button type="button" className="secondary" onClick={() => downloadInvoicePdf(invoice)}><Icon name="download" size={16} /> PDF</button>
                {invoice.status !== 'Paid' && invoice.outstandingAmount > 0 && (
                  <div className="payment-row">
                    <input min="0.01" max={invoice.outstandingAmount} step="0.01" type="number" placeholder="Amount" value={payments[invoice.id] || ''} onChange={(e) => setPayments({ ...payments, [invoice.id]: e.target.value })} />
                    <button className="primary" onClick={() => pay(invoice)}>Pay</button>
                  </div>
                )}
              </div>
            </article>
          ))}
        </div>
      )}
      <Pagination page={invoicePage} totalPages={invoiceTotalPages} pageSize={invoicePageSize} onPageChange={setInvoicePage} onPageSizeChange={(size) => { setInvoicePage(1); setInvoicePageSize(size) }} />
    </section>
  )
}

function EmptyState({ icon, title, subtitle }) {
  return (
    <div className="empty-state">
      <div className="empty-icon"><Icon name={icon} size={26} /></div>
      <h3>{title}</h3>
      <p className="muted">{subtitle}</p>
    </div>
  )
}

function Loading() {
  return <div className="loading"><span /> Loading…</div>
}

function Pagination({ page, totalPages, pageSize, onPageChange, onPageSizeChange }) {
  if (totalPages <= 1) return null
  return (
    <div className="pagination" style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', justifyContent: 'center', marginTop: 'auto', paddingTop: '1rem' }}>
      <button type="button" className="secondary" disabled={page <= 1} onClick={() => onPageChange(page - 1)}><Icon name="chevron-left" size={16} /> Previous</button>
      <span>Page {page} of {totalPages}</span>
      <button type="button" className="secondary" disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>Next <Icon name="chevron-right" size={16} /></button>
      <select value={pageSize} onChange={(e) => onPageSizeChange(Number(e.target.value))}>{[4, 10, 25, 50, 100].map((size) => <option key={size} value={size}>{size} / page</option>)}</select>
    </div>
  )
}

function useTheme() {
  const [theme, setTheme] = useState(() => {
    try {
      return localStorage.getItem('clinicTheme') || 'light'
    } catch {
      return 'light'
    }
  })

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme)
    try {
      localStorage.setItem('clinicTheme', theme)
    } catch {
      // ignore
    }
  }, [theme])

  const toggle = () => setTheme((current) => (current === 'light' ? 'dark' : 'light'))
  return { theme, toggle }
}

const TAB_META = {
  dashboard: { label: 'Dashboard', icon: 'dashboard', subtitle: 'A live overview of today’s clinic activity.' },
  patients: { label: 'Patients', icon: 'patients', subtitle: 'Search records and maintain medical and insurance details.' },
  appointments: { label: 'Appointments', icon: 'calendar', subtitle: 'Book, cancel, and reschedule without double booking.' },
  billing: { label: 'Billing', icon: 'billing', subtitle: 'Build itemized invoices and track payment status.' },
}

export default function App() {
  const [session, setSession] = useState(getSession())
  const [tab, setTab] = useState('dashboard')
  const [navOpen, setNavOpen] = useState(false)
  const { theme, toggle: toggleTheme } = useTheme()
  const canManage = session?.role === 'Admin' || session?.role === 'Receptionist'

  useEffect(() => {
    const logout = () => setSession(null)
    window.addEventListener('clinic:logout', logout)
    return () => window.removeEventListener('clinic:logout', logout)
  }, [])

  async function logout() {
    try {
      if (session?.refreshToken) {
        await apiFetch('/api/auth/logout', {
          method: 'POST',
          body: JSON.stringify({ refreshToken: session.refreshToken }),
        }, false)
      }
    } finally {
      clearSession()
      setSession(null)
    }
  }

  if (!session) {
    return (
      <>
        <Login onLogin={setSession} />
        <Toaster />
      </>
    )
  }

  const tabs = ['dashboard', 'patients', 'appointments', ...(canManage ? ['billing'] : [])]
  const meta = TAB_META[tab]

  return (
    <div className={`app-shell${navOpen ? ' nav-open' : ''}`}>
      <aside>
        <div className="brand"><span className="brand-mark"><Icon name="stethoscope" size={20} /></span><div><strong>CareFlow</strong><small>Clinic system</small></div></div>
        <nav>
          {tabs.map((value) => (
            <button className={tab === value ? 'active' : ''} key={value} onClick={() => { setTab(value); setNavOpen(false) }}>
              <Icon name={TAB_META[value].icon} size={19} />
              {TAB_META[value].label}
            </button>
          ))}
        </nav>
        <div className="user-card">
          <span className="avatar lg">{initials(session.username)}</span>
          <div className="user-meta"><strong>{session.username}</strong><small>{session.role}</small></div>
          <button className="icon-btn" title="Sign out" onClick={logout}><Icon name="logout" size={18} /></button>
        </div>
      </aside>
      {navOpen && <div className="nav-backdrop" onClick={() => setNavOpen(false)} />}
      <main className="content">
        <header className="topbar">
          <div className="topbar-left">
            <button className="icon-btn nav-toggle" onClick={() => setNavOpen(true)} aria-label="Open navigation"><Icon name="menu" size={20} /></button>
            <div>
              <p className="eyebrow">Clinic management</p>
              <h1>{meta.label}</h1>
            </div>
          </div>
          <div className="topbar-right">
            <button className="icon-btn" title="Toggle theme" onClick={toggleTheme}><Icon name={theme === 'dark' ? 'sun' : 'moon'} size={18} /></button>
            <div className="topbar-user"><span className="avatar">{initials(session.username)}</span><div className="hide-sm"><strong>{session.username}</strong><small>{session.role}</small></div></div>
          </div>
        </header>
        <p className="page-subtitle muted">{meta.subtitle}</p>
        <div className="page-body">
          {tab === 'dashboard' && <Dashboard />}
          {tab === 'patients' && <Patients canManage={canManage} />}
          {tab === 'appointments' && <Appointments canManage={canManage} />}
          {tab === 'billing' && canManage && <Billing />}
        </div>
      </main>
      <Toaster />
    </div>
  )
}
