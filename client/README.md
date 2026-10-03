# Angular client

Angular 21 customer/Admin SPA. Framework packages are pinned to 21.2.25 and CLI/build tooling to compatible patched 21.2.x releases.

## Development

Requirements: Node 22 (`.nvmrc`) and npm 10.

```bash
npm ci
npm start
```

Open `http://localhost:4200`. The Development API URL is `https://localhost:5001/api`.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Production build

To build the project run:

```bash
npm run build -- --configuration production
```

Output: `dist/client/browser`.

Normal production deployment uses `dotnet publish API/API.csproj -c Release`, which runs `npm ci`, builds Angular and includes it in ASP.NET Core `wwwroot`. Production uses `/api`, and ASP.NET Core provides SPA fallback for refreshed Angular routes.

## Running unit tests

To execute unit tests with the [Vitest](https://vitest.dev/) test runner, use the following command:

```bash
npm test -- --watch=false
```

The current suite contains 16 Vitest tests. No browser E2E runner is configured; Playwright/Cypress deployment coverage remains planned.

## Environments

- `src/environments/environment.ts` — local API
- `src/environments/environment.prod.ts` — same-origin `/api`
- `angular.json` performs the Production file replacement explicitly
