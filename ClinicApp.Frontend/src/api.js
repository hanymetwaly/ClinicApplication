const sessionKey = 'clinicSession'

export function getSession() {
  try {
    const value = localStorage.getItem(sessionKey)
    return value ? JSON.parse(value) : null
  } catch {
    try {
      localStorage.removeItem(sessionKey)
    } catch {
      // ignore
    }
    return null
  }
}

export function saveSession(session) {
  localStorage.setItem(sessionKey, JSON.stringify(session))
}

export function clearSession() {
  localStorage.removeItem(sessionKey)
}

export async function apiFetch(path, options = {}, canRefresh = true) {
  const session = getSession()
  const headers = new Headers(options.headers)
  if (options.body && !(options.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }
  if (session?.accessToken) {
    headers.set('Authorization', `Bearer ${session.accessToken}`)
  }

  let response = await fetch(path, { ...options, headers })
  if (response.status === 401 && canRefresh && session?.refreshToken) {
    const refreshResponse = await fetch('/api/auth/refresh', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken: session.refreshToken }),
    })
    if (refreshResponse.ok) {
      const refreshed = await refreshResponse.json()
      saveSession(refreshed)
      headers.set('Authorization', `Bearer ${refreshed.accessToken}`)
      response = await fetch(path, { ...options, headers })
    } else {
      clearSession()
      window.dispatchEvent(new Event('clinic:logout'))
    }
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => ({}))
    const validationMessages = problem.errors
      ? Object.values(problem.errors).flat().join(' ')
      : ''
    throw new Error(problem.detail || validationMessages || problem.title || 'The request failed.')
  }

  if (response.status === 204) {
    return null
  }
  return response.json()
}
