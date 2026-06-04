# AUTH-001 — Session Persistence Improvements

**Status:** ✅ COMPLETE — AUTH-001 near-term plan implemented (2026-06-04)
**Last Updated:** 2026-06-04

---

## 1. Current State

Momentum uses a single short-lived JWT access token for all authentication. The complete current behavior:

- **Token lifetime:** 60 minutes, configured via `Jwt:ExpiryMinutes` in `Momentum.API/appsettings.json`.
- **Token creation:** `Momentum.Infrastructure/Services/AuthService.GenerateToken()`. Token payload includes `sub` (user ID), `email`, `displayName`, `jti`, `iss`, `aud`, and `exp`.
- **Storage:** Token is stored in **`localStorage`** under the key `"authToken"`. `localStorage` persists across browser tabs and sessions (tab close, browser restart) until explicitly removed.
- **Expiration detection:** `Momentum.Client/Auth/JwtAuthStateProvider.BuildPrincipal()` reads the token from `localStorage` and checks `jwt.ValidTo < DateTime.UtcNow` on each auth-state evaluation. An expired token returns `null` → `Anonymous` state. The stale token is **not removed from `localStorage`** at this point.
- **401 handling:** `Momentum.Client/Auth/AuthMessageHandler` intercepts any API response with status 401 and calls `authStateProvider.MarkUserAsLoggedOut()`, which removes the token from `localStorage` and broadcasts the logged-out auth state.
- **Refresh tokens:** Not implemented. There is no refresh-token entity, table, DTO, repository, service, or API endpoint.
- **Remember Me:** A "Keep me signed in" checkbox exists in the Login UI (`Login.razor`), bound to `_rememberMe`. It is **inert** — the field is never read and has no effect on token lifetime, storage, or any API behavior. `LoginRequest` has only `Email` and `Password`.

The net result: every user session expires after 60 minutes of issuance, with no mechanism for silent renewal.

---

## 2. Problem

The 60-minute access-only session model creates friction that conflicts with Momentum's intended usage patterns:

**Interrupts normal usage.** A user who logs an activity in the morning, then opens the app in the afternoon to log another entry or review their day, will find themselves logged out. 60 minutes is shorter than many realistic usage gaps in a wellness tracking app.

**Conflicts with planned Check-In flow.** The upcoming Check-In feature depends on a low-friction, always-available capture experience. The post-activity Check-In flow (`Save Activity Log → Check-In form opens`) should not be interrupted by a session expiry prompt. A user who is mid-flow and encounters a login wall will likely abandon the Check-In.

**Harmful to future PWA / mobile usage.** The deferred PWA/push notification roadmap assumes users can open the app from a home screen or notification and immediately start logging — not re-authenticate first. Frequent session expiry would undermine the value of push reminders entirely.

**"Keep me signed in" creates a misleading expectation.** The checkbox exists in the UI and is visually functional (it can be toggled), but it has no effect on session behavior. A user who checks it expecting a longer session will still be logged out after 60 minutes. This is a UI trust issue.

**Stale-token cleanup gap.** When a token expires, `BuildPrincipal` correctly returns `null` and the user is treated as anonymous — but the dead token remains in `localStorage` until the next 401 is received. This is a minor inconsistency but warrants cleanup.

---

## 3. Options Considered

### Option A — Increase JWT Lifetime Only

Increase `Jwt:ExpiryMinutes` from 60 to a longer value (e.g. 7 days = 10,080 minutes) via configuration. No code changes required.

**Pros:**
- Zero implementation cost — config change only.
- No schema changes, no new endpoints, no client code changes.
- Immediately eliminates the daily re-authentication friction.
- Azure App Service config can override without a deployment.

**Cons:**
- A stolen access token remains valid for the full extended lifetime with no revocation mechanism.
- Does not make "Keep me signed in" meaningfully different unless lifetime is conditional (which requires code changes).
- Does not address the long-term PWA/mobile session architecture needs.

---

### Option B — Implement Refresh Tokens

Issue a short-lived access token (e.g. 15 minutes) alongside a longer-lived refresh token (e.g. 30 days). Client silently exchanges the refresh token for a new access token before the access token expires.

**Pros:**
- Better balance of security and convenience — access token window is small, but session persists long-term.
- Enables real revocation: logging out invalidates the refresh token server-side.
- "Keep me signed in" becomes meaningful: short session (e.g. 1 day refresh) vs. long session (e.g. 30 days refresh).
- Correct long-term architecture for PWA/mobile/push workflows.

**Cons:**
- Requires a `RefreshToken` database table, EF migration, repository, service, rotation/revocation logic, a new `/api/auth/refresh` endpoint, and client-side retry-on-401 logic.
- Meaningfully more complex to implement correctly (token rotation, race conditions, revocation on logout).
- Overkill for the immediate pain point on a personal-use app.

---

### Option C — Hybrid Near-Term Approach

Increase access token lifetime moderately now (Option A) and document refresh tokens as a future improvement (Option B). Optionally wire up "Keep me signed in" when it can have real behavior.

**Pros:**
- Eliminates the immediate friction at minimal cost.
- Leaves the architecture clean — does not introduce a half-baked refresh mechanism.
- Preserves the upgrade path to full refresh tokens before PWA/mobile work begins.

**Cons:**
- Does not fully solve long-term session security.
- "Keep me signed in" remains inert unless specifically addressed.

---

## 4. Recommendation

**Adopt Option C — Hybrid Near-Term Approach.**

Momentum is a personal-use, low-scale application. The primary problem is daily re-authentication friction, not token theft risk from sophisticated adversaries. The recommended direction:

**Near term (AUTH-001):**
- Increase JWT lifetime from 60 minutes to **7 days** (10,080 minutes) via config.
- This eliminates the daily re-authentication problem with zero schema or code changes.
- Keep `localStorage` as token storage — the tradeoff is acceptable at this scale.
- Address the stale-token cleanup gap: if `BuildPrincipal` detects an expired token, remove it from `localStorage` proactively rather than waiting for a 401.
- Resolve the inert "Keep me signed in" checkbox — either remove it from the UI, or keep it but do not present it as functional until refresh tokens are implemented.

**Long term (pre-PWA/mobile):**
- Implement refresh tokens before serious PWA/push/mobile workflows are built.
- Full refresh-token design is outlined in §6.

---

## 5. Proposed Near-Term AUTH-001 Implementation

The near-term implementation requires minimal code change. All changes are confined to configuration and one client-side cleanup.

### 5.1 JWT Lifetime — Config Update

Update `Jwt:ExpiryMinutes` to `"10080"` (7 days).

In **development** (`Momentum.API/appsettings.json`):

```json
"Jwt": {
  "ExpiryMinutes": "10080"
}
```

In **production**, override via **Azure App Service → Configuration → Application settings** (`Jwt__ExpiryMinutes = 10080`). This keeps the secret and the configurable value out of source control and allows adjustment without a deployment.

No changes to `AuthService.GenerateToken()` are needed — it already reads `ExpiryMinutes` dynamically via `IConfiguration`.

### 5.2 Stale-Token Cleanup in `JwtAuthStateProvider`

Currently `BuildPrincipal` returns `null` for expired tokens but leaves the stale `authToken` in `localStorage`. The cleanup step should remove the stale entry:

- In `GetAuthenticationStateAsync`, after detecting an expired token via `jwt.ValidTo < DateTime.UtcNow`, call `localStorage.removeItem("authToken")` before returning `Anonymous`.
- This prevents the stale key from persisting indefinitely in storage for users who simply stop using the app and return after token expiry.

With a 7-day token, this path will be triggered far less frequently, but the cleanup is still correct behavior.

### 5.3 "Keep Me Signed In" Checkbox

The checkbox labeled "Keep me signed in" in `Login.razor` is currently inert. Two acceptable resolutions:

- **Option 1 — Remove it.** Remove the checkbox until refresh tokens are implemented and the checkbox has real behavior. Cleaner; avoids the misleading expectation entirely.
- **Option 2 — Keep it, fix the expectation.** Keep the checkbox but accept that with a 7-day token it is effectively always "kept signed in." Optionally remove the label or grey it out until refresh tokens are available.

**Do not leave the checkbox in a state where it appears functional but does nothing.** Either option is acceptable; the decision can be made at implementation time.

### 5.4 Files Affected

| File | Change |
|---|---|
| `Momentum.API/appsettings.json` | `Jwt:ExpiryMinutes` → `"10080"` |
| Azure App Service config | Override `Jwt__ExpiryMinutes = 10080` in production |
| `Momentum.Client/Auth/JwtAuthStateProvider.cs` | Remove stale `authToken` from localStorage when `ValidTo < UtcNow` |
| `Momentum.Client/Pages/Login.razor` | Remove or update the "Keep me signed in" checkbox |

No EF migrations, no DTO changes, no new API endpoints, no schema changes.

---

## 6. Future Refresh Token Design

Before PWA, push notification, or mobile-native work begins, refresh tokens should be implemented. Outline of the future design:

### Entity / Storage

A `RefreshToken` table (new EF migration required):

| Field | Type | Notes |
|---|---|---|
| `Id` | int (PK) | |
| `UserId` | FK → user | Scoped to user |
| `TokenHash` | string | Hashed token value — never store plaintext |
| `CreatedAt` | DateTime | Issuance timestamp |
| `ExpiresAt` | DateTime | Absolute expiry (e.g. 30 days) |
| `RevokedAt` | DateTime? | Null until revoked |
| `ReplacedByTokenHash` | string? | Populated on rotation — links revoked token to its successor |

### Access Token Lifetime

With refresh tokens in place, the access token lifetime drops back to short (e.g. 15 minutes). The refresh token carries the long-lived session.

### API Endpoint

`POST /api/auth/refresh` — accepts the refresh token, validates it, issues a new access token + rotated refresh token. Returns `400` / `401` if the refresh token is expired or revoked.

### Token Rotation

Every successful refresh replaces the old refresh token with a new one (rotation). The old token is marked `RevokedAt` and its `ReplacedByTokenHash` is recorded. If a previously-used (revoked) refresh token is presented, the entire token family is revoked (detects replay/theft).

### Logout

Logout calls `POST /api/auth/logout` (or similar), which revokes the current refresh token server-side. The access token still lives until its short expiry, but the session cannot be renewed.

### Client Retry Flow

`AuthMessageHandler` is extended: on 401, attempt one silent refresh via `/api/auth/refresh`. If the refresh succeeds, retry the original request. If the refresh also returns 401 (refresh token expired or revoked), log the user out.

### "Keep Me Signed In" with Refresh Tokens

With refresh tokens, the login request can include `RememberMe: bool`. If false, the refresh token expires in 1 day; if true, 30 days. This gives the "Keep me signed in" checkbox genuine meaning.

---

## 7. Security Notes

Honest assessment of the tradeoffs:

**Longer JWT lifetime (near-term):**
- A stolen access token remains valid for up to 7 days with no revocation mechanism.
- Risk is proportional to attack surface: Momentum is a personal app with no stored payment data or sensitive PII beyond user email and wellness scores.
- Acceptable tradeoff at current scale; should be revisited before any expansion of scope or user base.

**`localStorage` for token storage:**
- Tokens in `localStorage` are accessible to any JavaScript running on the page — making them vulnerable to XSS attacks.
- The alternative is `HttpOnly` cookies, which are not accessible to JavaScript and are immune to XSS token theft. However, cookies introduce CSRF complexity.
- Momentum already sanitizes rich-note HTML server-side (`Ganss.Xss.HtmlSanitizer`) and strips disallowed tags, which reduces the XSS attack surface. But XSS risk from other input paths should still be taken seriously.
- `localStorage` is the pragmatic choice for a Blazor WASM app at this scale; it should be revisited if the threat model changes.

**Refresh tokens (long-term):**
- Short access token + rotating refresh token is the correct long-term architecture.
- Token rotation with replay detection (revoke entire family on reuse of a revoked token) provides defense against token theft.
- Refresh tokens stored server-side (hashed) enable true revocation — something impossible with access-token-only architecture.

---

## 8. Relationship to Check-Ins

Session persistence should be improved **before** Check-In implementation begins.

The planned post-activity Check-In flow (`Save Activity Log → Check-In form opens → user saves or cancels`) is a fast, low-friction capture sequence that assumes a live authenticated session. If a user's session expires mid-flow:

- The activity log save would return 401.
- `AuthMessageHandler` would log the user out.
- The user would be redirected to login.
- The Check-In moment would be lost.

Extending session lifetime to 7 days makes this scenario extremely unlikely in normal usage. AUTH-001 should therefore be implemented before or alongside the Check-In feature — not after.

See `Docs/check-in-feature-design-spec.md` for the full Check-In design.

---

## 9. Status

AUTH-001 near-term plan is **complete** (2026-06-04). Changes implemented:

| File | Change |
|---|---|
| `Momentum.API/appsettings.json` | `Jwt:ExpiryMinutes` updated from `"60"` to `"10080"` (7 days) |
| `Momentum.Client/Auth/JwtAuthStateProvider.cs` | `GetAuthenticationStateAsync` now removes stale `authToken` from localStorage when `BuildPrincipal` detects expiry (`jwt.ValidTo < DateTime.UtcNow`) |
| `Momentum.Client/Pages/Login.razor` | Inert "Keep me signed in" checkbox removed from markup and `_rememberMe` field removed from `@code` |
| `Momentum.Client/wwwroot/css/auth-pages.css` | `.auth-meta-row` updated to `justify-content: flex-end` (single remaining element — Forgot password? link) |

No schema changes, no migrations, no new endpoints, no DTOs modified.

The long-term refresh-token plan (§6) remains unimplemented and should be addressed before PWA/mobile work begins.

---

*Session Persistence Design Specification — created 2026-06-04*
*Status: ✅ COMPLETE — near-term AUTH-001 implemented 2026-06-04; long-term refresh tokens deferred (see §6)*
