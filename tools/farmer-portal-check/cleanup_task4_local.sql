-- Undo seed_task4_local.sql (and the smoke test's fertilizer runs) on the LOCAL spicone_dev database only.
--   psql -h localhost -U postgres -d spicone_dev -v ON_ERROR_STOP=1 -f tools/farmer-portal-check/cleanup_task4_local.sql
DO $$
BEGIN
  IF current_database() <> 'spicone_dev' THEN
    RAISE EXCEPTION 'cleanup_task4_local.sql runs only on spicone_dev (this is %)', current_database();
  END IF;
END $$;

DELETE FROM "SampleLabResults" r USING "SampleItems" i
  WHERE r."SampleItemId" = i."Id" AND i."Code" LIKE 'DEMO-T4%';
DELETE FROM "LabReports" WHERE "Code" LIKE 'DEMO-T4%';
DELETE FROM "SampleItems" WHERE "Code" LIKE 'DEMO-T4%';
DELETE FROM "SampleBatches" WHERE "Code" LIKE 'DEMO-T4%';
DELETE FROM "SampleCollections" WHERE "Code" LIKE 'DEMO-T4%';
DELETE FROM "SasFarmers" WHERE "Name" IN ('Demo Task4 Murugan', 'Demo Task4 Kavitha') AND "CreatedBy" = 'seed_task4_local';
-- Runs of the local test farmers only (FarmerProfiles phones starting 6, the local smoke numbers).
DELETE FROM "FertilizerRecommendationRuns" r USING "FarmerProfiles" p
  WHERE r."FarmerUserId" = p."UserId" AND p."Phone" LIKE '6%';
