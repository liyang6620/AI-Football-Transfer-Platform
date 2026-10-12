# Security Policy

## Supported version

Only the latest commit on main is actively maintained.

## Reporting a vulnerability

Do not open a public issue for a suspected vulnerability. Use **Security > Report a vulnerability** in this GitHub repository. If private vulnerability reporting is unavailable, email [yil195@aucklanduni.ac.nz](mailto:yil195@aucklanduni.ac.nz) with the repository name, affected component, reproduction steps, and potential impact.

Please do not include real credentials, personal data, or destructive proof-of-concept payloads. Reports will be acknowledged as soon as practical and assessed before any public disclosure.

## Deployment boundary

- Public clients access structured transfer data through the Render API only.
- Crawler inputs, full article text, AI processing, and maintenance routes require `X-Admin-Api-Key`.
- Supabase public tables have row-level security enabled with no anonymous or authenticated policies.
- Secrets belong in provider-managed environment variables and must never use a `VITE_` prefix.
