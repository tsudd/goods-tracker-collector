# goods-tracker-collector
Data collector for goods tracker web service

## Database
Data collector uses PostgreSQL combined with Entity Framework (EF). All entities and their configurations are stored in a
separate project: `GoodsTracker.DataCollector.DB`.

To create a migration according to the current schema use:
```bash
dotnet ef migrations add <migration-name>
```

To update postgres instance with fresh EF schema use:
```bash
dotnet ef database update --connection 'Server=127.0.0.1;Port=5432;Database=trackerDB;UID=sa;PWD=sa'
```

