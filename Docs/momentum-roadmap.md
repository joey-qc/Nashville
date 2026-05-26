# Momentum Roadmap

## Vision

Momentum is evolving beyond a traditional habit tracker into a personal behavioral momentum system.

The long-term vision is to provide:
- fast, low-friction behavioral capture
- multidimensional life analysis
- momentum trend visibility
- balance awareness
- reflective analytics
- emotionally reinforcing UX

Momentum is intended to function as a lightweight personal operating system for intentional living.

---

# Core Product Philosophy

## Fast Capture + Reflective Analytics

Momentum intentionally separates:
- rapid behavioral logging
- deeper behavioral reflection

### Logging UX Principles
Logging should remain:
- fast
- low-friction
- emotionally lightweight
- mobile-friendly
- action-oriented

### Analytics UX Principles
Analytics may become:
- introspective
- nuanced
- insight-rich
- interpretive
- behaviorally meaningful

The app should avoid requiring excessive reflection during data entry.

---

# Current Architectural Direction

## UI Direction

Momentum is moving away from MudBlazor toward:
- custom Razor markup
- custom HTML/CSS
- shared design tokens
- inline SVG graphics/charts
- responsive mobile-first layouts

MudBlazor components may temporarily remain only on legacy pages not yet converted.

---

# Current Technical Stack

- Blazor WebAssembly
- ASP.NET Core API
- Entity Framework Core
- SQL Server
- JWT Authentication
- Azure Hosting
- GitHub Actions CI/CD

---

# Current UX Identity

The Momentum UI is intended to feel:
- calm
- focused
- reinforcing
- forward-moving
- immersive
- emotionally coherent

The design language emphasizes:
- momentum
- accumulation
- progress
- behavioral reinforcement
- clarity over clutter

---

# Known Architectural Evolution

## Activity Template vs Behavioral Event

Current model:
- Activities define default meaning
- Log entries inherit metadata from activities

Future direction:
- Activities become reusable templates
- Individual log entries become richer behavioral events

Future log entries may support:
- dimension overrides
- contextual meaning
- custom point adjustments
- richer metadata

This transition will impact:
- database schema
- EF Core entities
- DTOs
- APIs
- reporting
- filtering
- analytics
- charting
- UX flows

This should be treated as a major architectural milestone.

---

# Terminology Evolution

Current user-facing terminology:
- Category

Potential future internal terminology:
- Dimension
- Aspect
- Vector
- Domain

Rationale:
Momentum models multidimensional impact rather than mutually-exclusive categorization.

Transition strategy:
- internal architecture first
- user-facing terminology later

---

# Future Enhancements

## Logging UX

### Pinned Favorites
Allow users to pin commonly-used activities for rapid access.

### Time-of-Day Smart Picks
Suggest activities based on:
- time of day
- usage patterns
- recency
- behavioral history

### Recent Activities
Surface recently-used activities for fast repeat logging.

### Progressive Log Detail
Allow optional deeper behavioral annotation while preserving low-friction logging.

---

## Analytics & Reporting

### Richer Trend Analysis
Expand reporting around:
- momentum trends
- consistency
- dimension balance
- streaks
- momentum drift

### Balance Targets
Allow users to define desired balance ratios between dimensions.

Example:
- 40% Mental
- 25% Physical
- 15% Social
- 10% Spiritual
- 10% Housekeeping

### Behavioral Insights
Generate observations such as:
- neglected dimensions
- over-dominant dimensions
- behavioral drift
- momentum stagnation
- recovery patterns

---

## Personalization & Settings

Future settings may evolve into a personal operating model configuration system.

Potential settings domains:
- scoring philosophy
- dimension weighting
- preferred balance targets
- reminder windows
- notification tuning
- emotional tone preferences
- daily reset hour
- recovery behavior
- weekly/monthly calibration

---

## Notification System

Potential future features:
- push notifications
- reminder scheduling
- nudges
- recovery prompts
- momentum encouragement

Potential future delivery:
- PWA push notifications
- mobile notifications

---

## Recovery & Re-engagement UX

Future UX should eventually address:
- stagnation
- burnout
- avoidance spirals
- disengagement
- behavioral recovery

Momentum should eventually support:
- graceful recovery
- resets
- re-engagement
- non-punitive behavioral support

---

# Known Risks

## Overcomplication Risk

The current logging flow is intentionally lightweight.

Future enhancements must preserve:
- speed
- clarity
- low cognitive load
- low-friction capture

Advanced features should remain:
- optional
- progressively disclosed
- non-intrusive

---

## Timezone Complexity

The application currently uses:
- UTC storage
- local time conversion

Future risk areas:
- DST transitions
- timezone consistency
- user timezone preferences
- reporting boundaries

---

## Chart Responsiveness

Known issue:
- chart/layout responsiveness during resize/orientation changes

Potential causes:
- SVG resizing
- viewport recalculation
- render timing

---

# Future Documentation Possibilities

Potential future addition:
- Architecture Decision Records (ADR)

Example structure:

/Docs/ADR
    ADR-001-move-away-from-mudblazor.md
    ADR-002-log-entry-dimension-overrides.md
    ADR-003-svg-charting-approach.md

Not required yet, but likely valuable later.

---

# Product Positioning

Momentum is not intended to become:
- a generic productivity app
- an enterprise dashboard
- a traditional task manager

Momentum is intended to become:
- a behavioral momentum system
- a multidimensional life tracking platform
- a reflective personal operating system
- an emotionally reinforcing behavioral UX experience