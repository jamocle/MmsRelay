# C#/.NET 8 Code Pro — Extended Guide

This document backs the Instructions with detailed scaffolds, policies, and ready-to-paste snippets.

## 0. Contents
- [Answer Format Guardrails](#answer-format-guardrails)
- [OpenAPI & API Design](#openapi--api-design)
- [Observability Defaults](#observability-defaults)
- [Security Checklist](#security-checklist)
- [Config & Flags](#config--flags)
- [Domain & Testing Extras](#domain--testing-extras)
- [TLS Opt-In Pattern (Dev-Only)](#tls-opt-in-pattern-dev-only)
- [EditorConfig](#editorconfig)
- [Directory.Build.props](#directorybuildprops)
- [Test Project Packages](#test-project-packages)
- [Dockerfile (API)](#dockerfile-api)
- [GitHub Actions CI](#github-actions-ci)
- [Benchmarking](#benchmarking)
- [Golden Mini Example (Structure Only)](#golden-mini-example-structure-only)

---

## Answer Format Guardrails
- Always show **file tree first**, then one code block **per file**.
- Include full `using` directives, `.csproj`, and required config.
- No placeholders; if optional, provide defaults and label them.
- End with **How to run/test** (CLI commands).
- For secrets/URLs: use dev-safe defaults and instruct to set **User Secrets**.

---

## OpenAPI & API Design
- Enable Swagger in dev/non-prod; gate/hide in prod.
- Errors: RFC 7807 ProblemDetails with machine-readable codes.
- Pagination: `page`, `pageSize`; return `totalCount` and `next`/`prev` links when useful.
- Versioning: prefix `/v1`; avoid breaking changes; mark deprecations with sunset notes.
- Include example requests/responses in Swagger.

---

## Observability Defaults
- OpenTelemetry Resource attributes: `service.name`, `service.version`, `deployment.environment`.
- Instrument ASP.NET Core, HttpClient, EF Core.
- Emit metrics: request duration, request count, failure rate, cache hit rate, DB duration.
- Sampling: 100% dev; parent-based ~10% non-prod; configurable in prod.
- Logging: Serilog console JSON; correlate trace IDs in logs.

---

## Security Checklist
- HTTPS redirect + HSTS (skip HSTS in dev).
- Strict input validation; never log secrets.
- Secure headers: X-Content-Type-Options, Referrer-Policy, `frame-ancestors`/CSP as appropriate.
- Secrets: **User Secrets** (dev), **Key Vault/KMS** (prod). No secrets in repo.
- TLS bypass: **forbidden by default**; only when explicitly requested, gated, and **fail-closed** in production (see pattern below).

---

## Config & Flags
- Layering: `appsettings.json` → `appsettings.{Environment}.json` → User Secrets → env vars.
- Use **typed Options** + validation; fail startup on invalid config.
- Gate risky features behind explicit **feature flags**.

---

## Domain & Testing Extras
- Keep domain logic pure; push I/O to edges.
- Tests: builders/object mothers for data; deterministic clocks/IDs via interfaces.
- Integration tests: `WebApplicationFactory<TEntryPoint>`; use **Testcontainers** for realistic DB/queues when accuracy matters.
- Targets: ≥80% Application, ~100% Domain coverage.

---

## TLS Opt-In Pattern (Dev-Only)
> Only use when user explicitly requests weaker TLS.

**Compile-time + config gates with prod fail-closed:**
```csharp
// Insecure mode: ONLY when explicitly requested.
// 1) Compile with: -define:ALLOW_INSECURE_TLS (or set in csproj/CI)
// 2) Require config flag; 3) Disallow in Production; 4) Loud warning log.

using System.Net.Http;
using System.Net.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public static class InsecureHttpClientFactory
{
    public static HttpClient Create(ILogger logger, IHostEnvironment env, bool enabledByConfig)
    {
#if !ALLOW_INSECURE_TLS
        throw new InvalidOperationException("Insecure TLS not compiled in. Define ALLOW_INSECURE_TLS to enable.");
#else
        if (!enabledByConfig)
            throw new InvalidOperationException("Insecure TLS not enabled by configuration.");
        if (env.IsProduction())
            throw new InvalidOperationException("Insecure TLS is forbidden in Production.");

        logger.LogWarning("**INSECURE TLS ENABLED** — certificate validation is bypassed. Dev/Lab ONLY.");

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = static (_, _, _, SslPolicyErrors _) => true
        };
        return new HttpClient(handler, disposeHandler: true);
#endif
    }
}
