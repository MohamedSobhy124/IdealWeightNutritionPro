import { HttpErrorResponse } from '@angular/common/http';

/** Extract a user-facing message from FastEndpoints / ASP.NET error payloads. */
export function apiErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  const body = error.error;
  if (!body || typeof body !== 'object') {
    return fallback;
  }

  const errors = (body as { errors?: unknown }).errors;
  if (Array.isArray(errors)) {
    const first = errors.find((entry): entry is string => typeof entry === 'string');
    if (first) return first;
  }

  if (errors && typeof errors === 'object') {
    for (const value of Object.values(errors as Record<string, unknown>)) {
      if (Array.isArray(value)) {
        const first = value.find((entry): entry is string => typeof entry === 'string');
        if (first) return first;
      }
    }
  }

  const message = (body as { message?: unknown }).message;
  if (typeof message === 'string' && message !== 'One or more errors occurred!') {
    return message;
  }

  return fallback;
}
