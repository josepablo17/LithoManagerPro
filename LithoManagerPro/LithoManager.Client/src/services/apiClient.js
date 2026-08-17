const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? ''

export const unauthorizedSessionEventName =
  'lithomanager:unauthorized-session'

export class ApiClientError extends Error {
  constructor({ message, status, errorCode, details }) {
    super(message)
    this.name = 'ApiClientError'
    this.status = status
    this.errorCode = errorCode
    this.details = details
  }
}

export async function apiRequest(path, options = {}) {
  const {
    method = 'GET',
    body,
    accessToken,
    headers,
    signal,
  } = options

  const requestHeaders = new Headers(headers)
  requestHeaders.set('Accept', 'application/json')

  const isFormData =
    typeof FormData !== 'undefined'
    && body instanceof FormData

  if (body !== undefined && !isFormData) {
    requestHeaders.set('Content-Type', 'application/json')
  }

  if (accessToken) {
    requestHeaders.set('Authorization', `Bearer ${accessToken}`)
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    method,
    headers: requestHeaders,
    credentials: 'include',
    body: createRequestBody(body, isFormData),
    signal,
  })

  if (!response.ok) {
    if (
      response.status === 401
      && accessToken
      && typeof window !== 'undefined'
    ) {
      window.dispatchEvent(
        new CustomEvent(unauthorizedSessionEventName),
      )
    }

    throw await createApiError(response)
  }

  if (response.status === 204) {
    return null
  }

  return response.json()
}

export async function apiDownload(path, options = {}) {
  const {
    accessToken,
    headers,
    signal,
  } = options

  const requestHeaders = new Headers(headers)

  if (accessToken) {
    requestHeaders.set('Authorization', `Bearer ${accessToken}`)
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: 'GET',
    headers: requestHeaders,
    credentials: 'include',
    signal,
  })

  if (!response.ok) {
    if (
      response.status === 401
      && accessToken
      && typeof window !== 'undefined'
    ) {
      window.dispatchEvent(
        new CustomEvent(unauthorizedSessionEventName),
      )
    }

    throw await createApiError(response)
  }

  return {
    blob: await response.blob(),
    fileName: getFileNameFromDisposition(
      response.headers.get('content-disposition'),
    ),
  }
}

async function createApiError(response) {
  const details = await readProblemDetails(response)
  const errorCode = details?.errorCode ?? 'request_failed'
  const message = getUserSafeMessage(response.status, errorCode)

  return new ApiClientError({
    message,
    status: response.status,
    errorCode,
    details,
  })
}

async function readProblemDetails(response) {
  const contentType = response.headers.get('content-type') ?? ''

  if (!contentType.includes('application/problem+json')
      && !contentType.includes('application/json')) {
    return null
  }

  try {
    return await response.json()
  } catch {
    return null
  }
}

function getUserSafeMessage(status, errorCode) {
  if (status === 401) {
    return 'Tu sesión no es válida o ha expirado.'
  }

  if (status === 403) {
    return 'No tienes permisos para realizar esta acción.'
  }

  if (status === 404) {
    return 'No encontramos el recurso solicitado.'
  }

  if (status === 409) {
    return getConflictMessage(errorCode)
  }

  if (status >= 500) {
    return 'No pudimos completar la operación. Inténtalo de nuevo.'
  }

  return 'Revisa la información e inténtalo de nuevo.'
}

function getConflictMessage(errorCode) {
  const messages = {
    concurrency_conflict:
      'La información fue modificada. Actualiza los datos e inténtalo de nuevo.',
    duplicate_department_code:
      'Ya existe un departamento con el mismo código.',
    duplicate_department_name:
      'Ya existe un departamento con el mismo nombre.',
    duplicate_identification_number:
      'Ya existe un empleado con ese tipo y número de identificación.',
    user_already_assigned:
      'El usuario indicado ya está vinculado a otro empleado.',
    department_inactive:
      'El departamento indicado no está activo.',
    employee_inactive:
      'El empleado indicado no está activo.',
    leave_type_not_found:
      'El tipo de vacaciones indicado no está disponible.',
    leave_policy_not_found:
      'No hay una política de vacaciones activa.',
    leave_balance_not_found:
      'No hay un saldo de vacaciones registrado.',
    insufficient_leave_balance:
      'No hay suficientes días disponibles.',
    pending_leave_request_exists:
      'Ya existe una solicitud pendiente.',
    leave_request_date_overlap:
      'Ya existe una solicitud en ese rango de fechas.',
    leave_request_not_found:
      'No encontramos la solicitud indicada.',
    leave_request_already_resolved:
      'La solicitud ya fue resuelta.',
    duplicate_document_storage:
      'No pudimos registrar el archivo. Inténtalo de nuevo.',
    document_file_not_found:
      'No encontramos el archivo solicitado.',
  }

  return messages[errorCode]
    ?? 'Existe un conflicto con la información enviada.'
}

function createRequestBody(body, isFormData) {
  if (body === undefined) {
    return undefined
  }

  return isFormData ? body : JSON.stringify(body)
}

function getFileNameFromDisposition(disposition) {
  if (!disposition) {
    return null
  }

  const utf8Match = disposition.match(/filename\*=UTF-8''([^;]+)/i)

  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1])
  }

  const fileNameMatch = disposition.match(/filename="?([^";]+)"?/i)

  return fileNameMatch?.[1] ?? null
}
