# Legacy deployment files

`azure-pipelines.legacy.yml` is retained only as historical reference.

It targets .NET 9 and deploys Angular and the API separately, so it does not implement the supported one-App-Service architecture. It must not be enabled for production. Use the manual process in `docs/12-Azure-Single-App-Service.md`.
