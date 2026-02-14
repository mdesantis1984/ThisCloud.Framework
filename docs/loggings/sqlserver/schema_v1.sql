/*******************************************************************************
 * ThisCloud.Framework.Loggings — SQL Server Schema v1.0
 *******************************************************************************
 * 
 * SCOPE:
 *   - Persistence design for logging configuration and audit trail
 *   - Settings storage with versioning and concurrency control
 *   - Audit/history tracking for all configuration changes
 *   - Prepared schema for event logging (v1.2 future implementation)
 *
 * TARGET:
 *   - SQL Server 2019+ (CompatibilityLevel 150+)
 *   - Azure SQL Database (compatible)
 *
 * VERSION:
 *   - Schema Version: 1.0
 *   - Framework Version: ThisCloud.Framework.Loggings v1.1
 *   - Date: 2026-02-15
 *
 * USAGE:
 *   - This schema is DOCUMENTATION ONLY for v1.1
 *   - Host applications own migrations (EF Core, Fluent Migrator, etc.)
 *   - Framework ships this as reference design
 *   - v1.2 will include persistence implementation
 *
 * SECURITY:
 *   - NO secrets or PII in schema
 *   - Redaction handled at application layer
 *   - Audit metadata excludes sensitive values
 *   - Use encrypted connections and TDE in production
 *
 * NAMING CONVENTIONS:
 *   - Schema: dbo (standard)
 *   - Prefix: tc_loggings_* (ThisCloud Loggings namespace)
 *   - Keys: PK_*, FK_*, IX_*
 *   - Timestamps: UTC only
 *
 * DEPENDENCIES:
 *   - None (self-contained schema)
 *
 * NOTES:
 *   - RowVersion for optimistic concurrency
 *   - Indexed for read-heavy workloads
 *   - JSON column for flexible settings structure
 *   - History table for full audit trail
 *
 ******************************************************************************/

-- =============================================================================
-- SECTION 1: Settings Table
-- =============================================================================
-- Purpose: Store current logging configuration (singleton pattern expected)
-- Concurrency: RowVersion for optimistic locking
-- Features: JSON settings, enabled flag, environment tracking

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tc_loggings_settings]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tc_loggings_settings] (
        -- Primary Key
        [Id]                    INT IDENTITY(1,1) NOT NULL,
        
        -- Business Data
        [IsEnabled]             BIT NOT NULL DEFAULT 1,
        [MinimumLevel]          NVARCHAR(20) NOT NULL DEFAULT 'Information',
        [SettingsJson]          NVARCHAR(MAX) NOT NULL,
        
        -- Environment Tracking
        [Environment]           NVARCHAR(50) NULL,
        [AllowedEnvironments]   NVARCHAR(500) NULL,
        
        -- Audit Metadata (creation)
        [CreatedAtUtc]          DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedBy]             NVARCHAR(256) NULL,
        
        -- Audit Metadata (last modification)
        [ModifiedAtUtc]         DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [ModifiedBy]            NVARCHAR(256) NULL,
        
        -- Concurrency Control
        [RowVersion]            ROWVERSION NOT NULL,
        
        -- Constraints
        CONSTRAINT [PK_tc_loggings_settings] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [CK_tc_loggings_settings_MinimumLevel] CHECK (
            [MinimumLevel] IN ('Verbose', 'Debug', 'Information', 'Warning', 'Error', 'Critical')
        )
    );

    -- Index for enabled flag queries
    CREATE NONCLUSTERED INDEX [IX_tc_loggings_settings_IsEnabled]
        ON [dbo].[tc_loggings_settings] ([IsEnabled] ASC);

    -- Index for environment-based queries
    CREATE NONCLUSTERED INDEX [IX_tc_loggings_settings_Environment]
        ON [dbo].[tc_loggings_settings] ([Environment] ASC)
        WHERE [Environment] IS NOT NULL;

    PRINT 'Table [dbo].[tc_loggings_settings] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [dbo].[tc_loggings_settings] already exists.';
END
GO

-- =============================================================================
-- SECTION 2: Settings History Table
-- =============================================================================
-- Purpose: Audit trail for all configuration changes
-- Features: Full snapshot on each change, who/when/what tracking
-- Retention: Application-defined (recommend 90-365 days)

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tc_loggings_settings_history]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tc_loggings_settings_history] (
        -- Primary Key
        [HistoryId]             BIGINT IDENTITY(1,1) NOT NULL,
        
        -- Foreign Key to Settings
        [SettingsId]            INT NOT NULL,
        
        -- Change Tracking
        [ChangeType]            NVARCHAR(20) NOT NULL,
        [ChangedAtUtc]          DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [ChangedBy]             NVARCHAR(256) NULL,
        [ChangeReason]          NVARCHAR(500) NULL,
        
        -- Snapshot of Settings (before change)
        [IsEnabled_Old]         BIT NULL,
        [MinimumLevel_Old]      NVARCHAR(20) NULL,
        [SettingsJson_Old]      NVARCHAR(MAX) NULL,
        [Environment_Old]       NVARCHAR(50) NULL,
        
        -- Snapshot of Settings (after change)
        [IsEnabled_New]         BIT NULL,
        [MinimumLevel_New]      NVARCHAR(20) NULL,
        [SettingsJson_New]      NVARCHAR(MAX) NULL,
        [Environment_New]       NVARCHAR(50) NULL,
        
        -- Additional Context
        [CorrelationId]         UNIQUEIDENTIFIER NULL,
        [RequestId]             UNIQUEIDENTIFIER NULL,
        [ClientIpAddress]       NVARCHAR(50) NULL,
        [UserAgent]             NVARCHAR(500) NULL,
        
        -- Constraints
        CONSTRAINT [PK_tc_loggings_settings_history] PRIMARY KEY CLUSTERED ([HistoryId] DESC),
        CONSTRAINT [FK_tc_loggings_settings_history_settings] FOREIGN KEY ([SettingsId])
            REFERENCES [dbo].[tc_loggings_settings] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [CK_tc_loggings_settings_history_ChangeType] CHECK (
            [ChangeType] IN ('Created', 'Updated', 'Deleted', 'Enabled', 'Disabled', 'Reset')
        )
    );

    -- Index for settings FK queries
    CREATE NONCLUSTERED INDEX [IX_tc_loggings_settings_history_SettingsId]
        ON [dbo].[tc_loggings_settings_history] ([SettingsId] ASC);

    -- Index for temporal queries (by date range)
    CREATE NONCLUSTERED INDEX [IX_tc_loggings_settings_history_ChangedAtUtc]
        ON [dbo].[tc_loggings_settings_history] ([ChangedAtUtc] DESC);

    -- Index for correlation tracking
    CREATE NONCLUSTERED INDEX [IX_tc_loggings_settings_history_CorrelationId]
        ON [dbo].[tc_loggings_settings_history] ([CorrelationId] ASC)
        WHERE [CorrelationId] IS NOT NULL;

    -- Index for audit queries by user
    CREATE NONCLUSTERED INDEX [IX_tc_loggings_settings_history_ChangedBy]
        ON [dbo].[tc_loggings_settings_history] ([ChangedBy] ASC)
        WHERE [ChangedBy] IS NOT NULL;

    PRINT 'Table [dbo].[tc_loggings_settings_history] created successfully.';
END
ELSE
BEGIN
    PRINT 'Table [dbo].[tc_loggings_settings_history] already exists.';
END
GO

-- =============================================================================
-- SECTION 3: Events Table (v1.2 Future Implementation)
-- =============================================================================
-- Purpose: Store log events for querying and analytics
-- Status: PREPARED SCHEMA (not implemented in v1.1)
-- Features: High-volume insert optimization, partitioning-ready
-- Indexing Strategy: Covering indexes for common query patterns

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tc_loggings_events]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[tc_loggings_events] (
        -- Primary Key
        [EventId]               BIGINT IDENTITY(1,1) NOT NULL,
        
        -- Core Event Data
        [Timestamp]             DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [Level]                 NVARCHAR(20) NOT NULL,
        [MessageTemplate]       NVARCHAR(MAX) NOT NULL,
        [RenderedMessage]       NVARCHAR(MAX) NULL,
        [Exception]             NVARCHAR(MAX) NULL,
        
        -- Correlation
        [CorrelationId]         UNIQUEIDENTIFIER NULL,
        [RequestId]             UNIQUEIDENTIFIER NULL,
        [TraceId]               NVARCHAR(100) NULL,
        [SpanId]                NVARCHAR(50) NULL,
        
        -- Source Context
        [ApplicationName]       NVARCHAR(100) NULL,
        [Environment]           NVARCHAR(50) NULL,
        [MachineName]           NVARCHAR(100) NULL,
        [ProcessId]             INT NULL,
        [ThreadId]              INT NULL,
        
        -- Event Metadata (Serilog properties as JSON)
        [PropertiesJson]        NVARCHAR(MAX) NULL,
        
        -- Constraints
        CONSTRAINT [PK_tc_loggings_events] PRIMARY KEY NONCLUSTERED ([EventId] ASC),
        CONSTRAINT [CK_tc_loggings_events_Level] CHECK (
            [Level] IN ('Verbose', 'Debug', 'Information', 'Warning', 'Error', 'Critical')
        )
    );

    -- Clustered index on Timestamp for time-series optimization
    CREATE CLUSTERED INDEX [IX_tc_loggings_events_Timestamp]
        ON [dbo].[tc_loggings_events] ([Timestamp] DESC);

    -- Index for level-based queries (filtering errors, warnings, etc.)
    CREATE NONCLUSTERED INDEX [IX_tc_loggings_events_Level]
        ON [dbo].[tc_loggings_events] ([Level] ASC)
        INCLUDE ([Timestamp], [RenderedMessage]);

    -- Index for correlation tracking (trace distributed requests)
    CREATE NONCLUSTERED INDEX [IX_tc_loggings_events_CorrelationId]
        ON [dbo].[tc_loggings_events] ([CorrelationId] ASC)
        INCLUDE ([Timestamp], [Level], [RenderedMessage])
        WHERE [CorrelationId] IS NOT NULL;

    -- Index for application/environment filtering
    CREATE NONCLUSTERED INDEX [IX_tc_loggings_events_Application_Environment]
        ON [dbo].[tc_loggings_events] ([ApplicationName] ASC, [Environment] ASC)
        INCLUDE ([Timestamp], [Level]);

    -- Index for W3C TraceContext queries
    CREATE NONCLUSTERED INDEX [IX_tc_loggings_events_TraceId]
        ON [dbo].[tc_loggings_events] ([TraceId] ASC)
        WHERE [TraceId] IS NOT NULL;

    PRINT 'Table [dbo].[tc_loggings_events] created successfully (v1.2 prepared schema).';
END
ELSE
BEGIN
    PRINT 'Table [dbo].[tc_loggings_events] already exists.';
END
GO

-- =============================================================================
-- SECTION 4: Schema Validation
-- =============================================================================
-- Verify all expected objects exist

DECLARE @MissingObjects NVARCHAR(MAX) = '';

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tc_loggings_settings]'))
    SET @MissingObjects += 'tc_loggings_settings, ';

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tc_loggings_settings_history]'))
    SET @MissingObjects += 'tc_loggings_settings_history, ';

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[tc_loggings_events]'))
    SET @MissingObjects += 'tc_loggings_events, ';

IF LEN(@MissingObjects) > 0
BEGIN
    RAISERROR('Missing objects: %s', 16, 1, @MissingObjects);
END
ELSE
BEGIN
    PRINT '';
    PRINT '=============================================================================';
    PRINT 'ThisCloud.Framework.Loggings Schema v1.0 — Validation PASSED';
    PRINT '=============================================================================';
    PRINT 'All tables created successfully:';
    PRINT '  ✓ dbo.tc_loggings_settings';
    PRINT '  ✓ dbo.tc_loggings_settings_history';
    PRINT '  ✓ dbo.tc_loggings_events (v1.2 prepared)';
    PRINT '';
    PRINT 'Next Steps:';
    PRINT '  1. Apply this schema via your migration tool (EF Core, Fluent Migrator, etc.)';
    PRINT '  2. Configure TDE and encrypted connections in production';
    PRINT '  3. Implement application-level retention policies';
    PRINT '  4. Monitor index fragmentation on high-volume inserts';
    PRINT '=============================================================================';
END
GO

/*******************************************************************************
 * END OF SCHEMA v1.0
 ******************************************************************************/
