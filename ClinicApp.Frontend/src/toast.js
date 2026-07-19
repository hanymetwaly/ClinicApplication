const EVENT = 'clinic:toast'

export function toast(message, type = 'success') {
  if (!message) return
  window.dispatchEvent(new CustomEvent(EVENT, { detail: { message, type, id: Date.now() + Math.random() } }))
}

toast.success = (message) => toast(message, 'success')
toast.error = (message) => toast(message, 'error')
toast.info = (message) => toast(message, 'info')

export const TOAST_EVENT = EVENT
