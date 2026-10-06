import { ApiError } from './ApiError';
import i18n from '../../app/i18n';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? '/api';

export async function getJson<T>(path: string, signal?: AbortSignal): Promise<T> {
  return requestJson<T>(path, { signal });
}

export async function postJson<TResponse, TRequest>(
  path: string,
  body: TRequest,
  signal?: AbortSignal
): Promise<TResponse> {
  return requestJson<TResponse>(path, {
    method: 'POST',
    headers: { Accept: 'application/json' },
    body: JSON.stringify(body),
    signal
  });
}

async function requestJson<T>(path: string, init: RequestInit): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      ...init.headers
    }
  });

  if (!response.ok) {
    throw await toApiError(response);
  }

  return response.json() as Promise<T>;
}

async function toApiError(response: Response): Promise<ApiError> {
  const fallbackMessage = i18n.t('errors.requestFailed');

  try {
    const problem = (await response.json()) as {
      detail?: string;
      errors?: Record<string, string[]>;
      title?: string;
    };
    const validationErrors = problem.errors ?? {};
    return new ApiError(
      problem.detail ?? problem.title ?? fallbackMessage,
      response.status,
      validationErrors
    );
  } catch {
    return new ApiError(fallbackMessage, response.status);
  }
}
