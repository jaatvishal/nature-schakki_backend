# Legacy SQL export

`Database_Data_Schema.LEGACY.sql` is a historical local SQL Server export.

It is **not** a deployment source:

- it creates a database under `master`;
- it contains machine-specific MDF/LDF paths;
- it represents an older schema;
- it is not safely repeatable;
- it is not compatible with Azure SQL database provisioning.

The authoritative database schema is the EF Core migration history under:

- `Infrastructure/Migrations/Store`
- `Infrastructure/Migrations/Identity`

Use `scripts/deploy-database.ps1` to apply both contexts to an existing Azure SQL Database.
