BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529151638_V2_DimensionModel'
)
BEGIN
    EXEC sp_rename N'[Categories]', N'Dimensions', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529151638_V2_DimensionModel'
)
BEGIN
    EXEC sp_rename N'[ActivityCategories]', N'ActivityDimensions', 'OBJECT';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529151638_V2_DimensionModel'
)
BEGIN
    EXEC sp_rename N'[ActivityDimensions].[CategoryId]', N'DimensionId', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529151638_V2_DimensionModel'
)
BEGIN
    ALTER TABLE [ActivityDimensions] DROP CONSTRAINT [FK_ActivityCategories_Categories_CategoryId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529151638_V2_DimensionModel'
)
BEGIN
    ALTER TABLE [ActivityDimensions] DROP CONSTRAINT [FK_ActivityCategories_Activities_ActivityId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529151638_V2_DimensionModel'
)
BEGIN
    ALTER TABLE [ActivityDimensions] DROP CONSTRAINT [PK_ActivityCategories];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529151638_V2_DimensionModel'
)
BEGIN
    DROP INDEX [IX_ActivityCategories_CategoryId] ON [ActivityDimensions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529151638_V2_DimensionModel'
)
BEGIN
    ALTER TABLE [ActivityDimensions] ADD CONSTRAINT [PK_ActivityDimensions] PRIMARY KEY ([ActivityId], [DimensionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529151638_V2_DimensionModel'
)
BEGIN
    CREATE INDEX [IX_ActivityDimensions_DimensionId] ON [ActivityDimensions] ([DimensionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529151638_V2_DimensionModel'
)
BEGIN
    ALTER TABLE [ActivityDimensions] ADD CONSTRAINT [FK_ActivityDimensions_Activities_ActivityId] FOREIGN KEY ([ActivityId]) REFERENCES [Activities] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529151638_V2_DimensionModel'
)
BEGIN
    ALTER TABLE [ActivityDimensions] ADD CONSTRAINT [FK_ActivityDimensions_Dimensions_DimensionId] FOREIGN KEY ([DimensionId]) REFERENCES [Dimensions] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529151638_V2_DimensionModel'
)
BEGIN
    CREATE TABLE [ActivityLogEntryDimensions] (
        [ActivityLogId] int NOT NULL,
        [DimensionId] int NOT NULL,
        CONSTRAINT [PK_ActivityLogEntryDimensions] PRIMARY KEY ([ActivityLogId], [DimensionId]),
        CONSTRAINT [FK_ActivityLogEntryDimensions_ActivityLogs_ActivityLogId] FOREIGN KEY ([ActivityLogId]) REFERENCES [ActivityLogs] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ActivityLogEntryDimensions_Dimensions_DimensionId] FOREIGN KEY ([DimensionId]) REFERENCES [Dimensions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529151638_V2_DimensionModel'
)
BEGIN
    CREATE INDEX [IX_ActivityLogEntryDimensions_DimensionId] ON [ActivityLogEntryDimensions] ([DimensionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529151638_V2_DimensionModel'
)
BEGIN

                    INSERT INTO ActivityLogEntryDimensions (ActivityLogId, DimensionId)
                    SELECT al.Id  AS ActivityLogId,
                           ad.DimensionId
                    FROM   ActivityLogs      al
                    JOIN   ActivityDimensions ad ON ad.ActivityId = al.ActivityId;
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260529151638_V2_DimensionModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260529151638_V2_DimensionModel', N'10.0.8');
END;

COMMIT;
GO

