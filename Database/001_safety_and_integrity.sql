/* ============================================================================
   PowerFit | 001_safety_and_integrity.sql
   RUN THIS BEFORE deploying the new code: the entities now map Food.Allergens and FitnessPlan.RowVersion.
   Safe to run more than once.
   (If you re-run Scaffold-DbContext afterwards, the generated Food.cs / FitnessPlan.cs / PowerFitDbContext.cs
    contain the same properties as the files in this package.)
   ============================================================================ */
SET XACT_ABORT ON;
BEGIN TRAN;

/* 1) One Active plan per user -------------------------------------------------
      First archive the older duplicates (keep the most recently updated Active plan of each user),
      otherwise the unique index cannot be created. */
;WITH ranked AS (
    SELECT PlanID,
           ROW_NUMBER() OVER (PARTITION BY UserID ORDER BY UpdatedAt DESC, PlanID DESC) AS rn
    FROM dbo.FitnessPlan
    WHERE Status = N'Active'
)
UPDATE p
SET    Status    = N'Archived',
       UpdatedAt = CASE WHEN SYSUTCDATETIME() < p.CreatedAt THEN p.CreatedAt ELSE SYSUTCDATETIME() END
FROM   dbo.FitnessPlan p
JOIN   ranked r ON r.PlanID = p.PlanID
WHERE  r.rn > 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_FitnessPlan_OneActivePerUser'
               AND object_id = OBJECT_ID(N'dbo.FitnessPlan'))
    CREATE UNIQUE INDEX UX_FitnessPlan_OneActivePerUser
        ON dbo.FitnessPlan (UserID)
        WHERE Status = N'Active';

/* 2) Optimistic concurrency: every admin edit touches the FitnessPlan row, so one rowversion there
      makes two simultaneous edits fail with 409 instead of "last writer wins". */
IF COL_LENGTH(N'dbo.FitnessPlan', N'RowVersion') IS NULL
    ALTER TABLE dbo.FitnessPlan ADD RowVersion rowversion NOT NULL;

/* 3) Food allergens that have no column of their own (gluten, dairy and nuts already have one).
      Comma separated, no spaces. Allowed: Egg, Soy, Fish, Shellfish, Sesame. */
IF COL_LENGTH(N'dbo.Food', N'Allergens') IS NULL
    ALTER TABLE dbo.Food ADD Allergens nvarchar(100) NULL;

COMMIT;
GO

/* 4) Optional: tag the foods you already have (examples - adjust to your own catalog).
UPDATE dbo.Food SET Allergens = N'Egg'       WHERE Name LIKE N'%egg%'    AND Allergens IS NULL;
UPDATE dbo.Food SET Allergens = N'Fish'      WHERE Category = N'Fish'    AND Allergens IS NULL;
UPDATE dbo.Food SET Allergens = N'Shellfish' WHERE Name LIKE N'%shrimp%' AND Allergens IS NULL;
UPDATE dbo.Food SET Allergens = N'Sesame'    WHERE Name LIKE N'%tahini%' AND Allergens IS NULL;
UPDATE dbo.Food SET Allergens = N'Soy'       WHERE Name LIKE N'%soy%' OR Name LIKE N'%tofu%';
*/
