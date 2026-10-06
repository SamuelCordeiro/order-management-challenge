export type ApiValidationErrors = Record<string, string[]>;

export class ApiError extends Error {
  public constructor(
    message: string,
    public readonly status: number,
    public readonly validationErrors: ApiValidationErrors = {}
  ) {
    super(message);
    this.name = 'ApiError';
  }
}
