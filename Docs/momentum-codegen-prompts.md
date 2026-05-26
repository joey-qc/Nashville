# Momentum Code Generation Prompts

This document stores:
- reusable AI coding prompts
- architectural guidance
- generation guardrails
- migration prompts
- successful prompting patterns

---

# Core Project Context Prompt

Use this context when generating Momentum code.

---

Momentum is a Blazor WebAssembly application using:
- ASP.NET Core API
- Entity Framework Core
- SQL Server
- JWT Authentication

Architecture layers:
- Momentum.API
- Momentum.Application
- Momentum.Client
- Momentum.Domain
- Momentum.Infrastructure
- Momentum.Shared
- Momentum.Tests

Momentum is moving away from MudBlazor toward:
- custom Razor markup
- custom HTML/CSS
- shared design tokens
- inline SVG graphics/charts
- responsive mobile-first layouts

Do NOT introduce new MudBlazor dependencies or components.

The application UX philosophy emphasizes:
- low-friction behavioral logging
- fast capture
- reflective analytics
- emotional clarity
- responsive mobile UX
- clean visual hierarchy

The UI should feel:
- calm
- focused
- immersive
- reinforcing
- momentum-oriented

Prefer:
- semantic HTML
- reusable CSS classes
- CSS variables/design tokens
- lightweight responsive layouts
- maintainable component structure

Avoid:
- over-engineered abstractions
- enterprise-heavy UI patterns
- excessive animation
- unnecessary complexity

---

# UI Conversion Prompt

Convert this page from MudBlazor to custom Razor/HTML/CSS.

Requirements:
- preserve existing functionality
- remove MudBlazor dependencies
- use semantic HTML
- use shared design tokens
- maintain responsive behavior
- preserve Momentum visual identity
- preserve accessibility
- prefer lightweight layouts
- avoid framework-heavy UI patterns

Return:
- updated Razor markup
- accompanying CSS
- notes about removed dependencies
- notes about required cleanup

---

# Reporting / Chart Prompt

Generate responsive SVG-based reporting components for Momentum.

Requirements:
- mobile responsive
- dark-theme compatible
- lightweight rendering
- avoid charting libraries unless necessary
- support dimension/category color tokens
- support resize handling
- support accessibility

Visual style:
- minimal
- clean
- restrained
- high readability
- emotionally reinforcing

---

# Domain Evolution Prompt

Momentum is evolving toward:
- activities as reusable templates
- log entries as richer behavioral events

Design code with future support for:
- dimension overrides
- richer log metadata
- optional contextual meaning
- reporting flexibility

Preserve backward compatibility where possible.

---

# Refactor Prompt

Refactor this code for:
- clarity
- maintainability
- separation of concerns
- consistency with Momentum architecture

Preserve:
- behavior
- API contracts
- UX behavior

Reduce:
- duplication
- complexity
- MudBlazor coupling

---

# CSS / Design Prompt

Generate CSS consistent with Momentum's visual language.

Theme characteristics:
- dark immersive UI
- Seahawks-inspired accent palette
- restrained neon-green highlights
- high contrast
- spacious layout
- emotionally reinforcing design
- mobile responsive

Avoid:
- overly bright colors
- excessive gradients
- dashboard clutter
- enterprise styling

---

# Future Prompt Categories

Potential future additions:
- migration prompts
- EF Core schema evolution prompts
- authentication/security prompts
- PWA prompts
- Azure deployment prompts
- performance optimization prompts
- testing prompts
- analytics prompts
- accessibility prompts