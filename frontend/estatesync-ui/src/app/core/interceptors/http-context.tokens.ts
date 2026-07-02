import { HttpContextToken } from '@angular/common/http';

/**
 * When set to `true` on a request's HttpContext, the error interceptor skips its
 * 401-refresh / redirect handling and rethrows the error untouched.
 *
 * Used by the app-bootstrap silent refresh so a missing/expired session cookie
 * does not trigger a navigation while the app is still initializing.
 */
export const SKIP_AUTH_HANDLING = new HttpContextToken<boolean>(() => false);
