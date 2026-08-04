# Brand Style Service

`BrandStyleService` allows customers to override the CI/brand colors defined in
`site.overrides.css` via environment variables. This is primarily meant for
containerized deployments (e.g. Kubernetes), where the shipped CSS files inside
the image/container cannot easily be edited by the customer, but environment
variables can be injected via ConfigMaps/Secrets.

## Supported environment variables

| Environment variable            | CSS custom property            | Notes                                                                 |
|----------------------------------|---------------------------------|------------------------------------------------------------------------|
| `CSS_WEBGIS_BRAND_PRIMARY`       | `--webgis-brand-primary`       | Main brand color, e.g. `#a00`                                          |
| `CSS_WEBGIS_BRAND_PRIMARY_LIGHT` | `--webgis-brand-primary-light` | Optional. If not set, the existing `color-mix()`-based default in `Site.css`/`site.css` is used. |

The list of supported variables is intentionally fixed/explicit (see
`BrandStyleVariable`), not a generic prefix mapping, so it can be extended in a
controlled way when new brand variables are introduced.

## How it works

- `BrandStyleService` is registered as a singleton (`AddBrandStyleService()`)
  and injected into views via `_ViewImports.cshtml` (`@inject BrandStyleService StyleService`).
- Layout views render `StyleService.GetBrandVariablesFromEnvironment()` inside an
  inline `<style>:root { ... }</style>` block, placed **after** the
  `site.overrides.css` `<link>` so that env-var values win over the static file via
  the CSS cascade.
- Because environment variables don't change during the lifetime of the
  process, the generated CSS is computed once (lazily) and cached in memory.
  **A restart is required** to pick up changed environment variables.

## Example (Kubernetes)

```yaml
env:
  - name: CSS_WEBGIS_BRAND_PRIMARY
    value: "#a00"
  - name: CSS_WEBGIS_BRAND_PRIMARY_LIGHT
    value: "#faa"
```
