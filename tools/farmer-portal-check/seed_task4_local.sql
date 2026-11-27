-- Farmer portal Task 4: a clearly fake SPIC soil report for the LOCAL spicone_dev database ONLY, so
-- "Use my soil report" on /farmer/fertilizer can be smoke-tested and photographed.
-- Never run against any other database. Undo with cleanup_task4_local.sql.
--
--   psql -h localhost -U postgres -d spicone_dev -v ON_ERROR_STOP=1 -f tools/farmer-portal-check/seed_task4_local.sql
--
-- Adds one SAS farmer "Demo Task4 Murugan" linked to the local test farmer 6777777703 (UserId), one
-- completed soil collection / sample / batch / report (codes start "DEMO-T4") and its soil results:
-- Nitrogen 180 kg/ha (Low), Phosphorus 18 kg/ha (Low on the lab's 22-56), Potassium 95 kg/ha (Low on
-- 110-280), pH 8.1 (Alkaline), Zinc 0.5 ppm (Low). Units are the lab master's (kg/ha, ppm).
-- Fix round 1: a second farmer "Demo Task4 Kavitha" linked to the local test farmer 6614843176 with a report
-- DEMO-T4-RPT-2 that has N, P and pH but NO potassium row (the partial-report smoke check).
DO $$
DECLARE
  v_user text;
  v_user2 text;
  v_farmer int;
  v_collection int;
  v_item int;
  v_batch int;
BEGIN
  IF current_database() <> 'spicone_dev' THEN
    RAISE EXCEPTION 'seed_task4_local.sql runs only on spicone_dev (this is %)', current_database();
  END IF;
  IF EXISTS (SELECT 1 FROM "SampleCollections" WHERE "Code" LIKE 'DEMO-T4%') THEN
    RAISE EXCEPTION 'Task 4 demo soil report already exists; run cleanup_task4_local.sql first';
  END IF;
  SELECT "UserId" INTO v_user FROM "FarmerProfiles" WHERE "Phone" = '6777777703';
  IF v_user IS NULL THEN
    RAISE EXCEPTION 'local test farmer 6777777703 has no profile; sign in once and finish onboarding first';
  END IF;

  INSERT INTO "SasFarmers" ("Name", "Mobile", "Village", "DistrictId", "DistrictName", "StateId", "StateName", "UserId", "CreatedBy", "CreatedAt", "UpdatedAt", "IsDeleted")
  VALUES ('Demo Task4 Murugan', '6777777703', 'Kovilpatti', 1, 'Thoothukudi', 1, 'Tamil Nadu', v_user, 'seed_task4_local', now(), now(), false)
  RETURNING "Id" INTO v_farmer;

  INSERT INTO "SampleCollections" ("Code", "CollectionDate", "CollectedByUserId", "CollectedByName", "CollectedByRole", "PaymentType", "Status", "TotalAmount", "CreatedBy", "CreatedAt", "UpdatedAt", "IsDeleted", "CompletedAt")
  VALUES ('DEMO-T4-COL-1', now() - interval '20 days', 'seed', 'Demo MDO', 'MDO', 0, 9, 0, 'seed_task4_local', now() - interval '20 days', now(), false, now() - interval '5 days')
  RETURNING "Id" INTO v_collection;

  INSERT INTO "SampleItems" ("CollectionId", "Code", "FarmerId", "SampleType", "Crop1", "NoOfTests", "Amount", "CreatedAt", "IsDeleted", "AnalysisStatus")
  VALUES (v_collection, 'DEMO-T4-COL-1-1', v_farmer, 0, 'Cotton', 1, 0, now() - interval '20 days', false, 2)
  RETURNING "Id" INTO v_item;

  INSERT INTO "SampleBatches" ("Code", "BatchDate", "SampleCount", "Priority", "Status", "CreatedAt", "UpdatedAt", "IsDeleted", "CompletedAt")
  VALUES ('DEMO-T4-BAT-1', now() - interval '10 days', 1, 1, 4, now() - interval '10 days', now(), false, now() - interval '5 days')
  RETURNING "Id" INTO v_batch;

  INSERT INTO "LabReports" ("Code", "BatchId", "SampleItemId", "SampleType", "Status", "GeneratedAt", "GeneratedByName", "DownloadCount", "FinancialYearStart")
  VALUES ('DEMO-T4-RPT-1', v_batch, v_item, 0, 0, now() - interval '5 days', 'Demo analyst', 0, 2026);

  INSERT INTO "SampleLabResults" ("SampleItemId", "Parameter", "NormalRange", "EnteredValue", "Unit", "ResultLabel", "Status", "SortOrder", "LabParameterId", "EnteredByName", "EnteredAt")
  VALUES
    (v_item, 'pH', '6.5 - 7.5', '8.1', '-', 'Alkaline', 3, 1, 1, 'Demo analyst', now() - interval '6 days'),
    (v_item, 'Nitrogen', '280 - 560', '180', 'kg/ha', 'Low', 1, 4, 4, 'Demo analyst', now() - interval '6 days'),
    (v_item, 'Phosphorus', '22 - 56', '18', 'kg/ha', 'Low', 1, 5, 5, 'Demo analyst', now() - interval '6 days'),
    (v_item, 'Potassium', '110 - 280', '95', 'kg/ha', 'Low', 1, 6, 6, 'Demo analyst', now() - interval '6 days'),
    (v_item, 'Zinc', '0.6 - 1.2', '0.5', 'ppm', 'Low', 1, 7, 7, 'Demo analyst', now() - interval '6 days');

  -- the partial report (no potassium)
  SELECT "UserId" INTO v_user2 FROM "FarmerProfiles" WHERE "Phone" = '6614843176';
  IF v_user2 IS NULL THEN
    RAISE EXCEPTION 'local test farmer 6614843176 has no profile; sign in once and finish onboarding first';
  END IF;
  INSERT INTO "SasFarmers" ("Name", "Mobile", "Village", "DistrictId", "DistrictName", "StateId", "StateName", "UserId", "CreatedBy", "CreatedAt", "UpdatedAt", "IsDeleted")
  VALUES ('Demo Task4 Kavitha', '6614843176', 'Kovilpatti', 1, 'Thoothukudi', 1, 'Tamil Nadu', v_user2, 'seed_task4_local', now(), now(), false)
  RETURNING "Id" INTO v_farmer;
  INSERT INTO "SampleCollections" ("Code", "CollectionDate", "CollectedByUserId", "CollectedByName", "CollectedByRole", "PaymentType", "Status", "TotalAmount", "CreatedBy", "CreatedAt", "UpdatedAt", "IsDeleted", "CompletedAt")
  VALUES ('DEMO-T4-COL-2', now() - interval '15 days', 'seed', 'Demo MDO', 'MDO', 0, 9, 0, 'seed_task4_local', now() - interval '15 days', now(), false, now() - interval '3 days')
  RETURNING "Id" INTO v_collection;
  INSERT INTO "SampleItems" ("CollectionId", "Code", "FarmerId", "SampleType", "Crop1", "NoOfTests", "Amount", "CreatedAt", "IsDeleted", "AnalysisStatus")
  VALUES (v_collection, 'DEMO-T4-COL-2-1', v_farmer, 0, 'Paddy', 1, 0, now() - interval '15 days', false, 2)
  RETURNING "Id" INTO v_item;
  INSERT INTO "SampleBatches" ("Code", "BatchDate", "SampleCount", "Priority", "Status", "CreatedAt", "UpdatedAt", "IsDeleted", "CompletedAt")
  VALUES ('DEMO-T4-BAT-2', now() - interval '8 days', 1, 1, 4, now() - interval '8 days', now(), false, now() - interval '3 days')
  RETURNING "Id" INTO v_batch;
  INSERT INTO "LabReports" ("Code", "BatchId", "SampleItemId", "SampleType", "Status", "GeneratedAt", "GeneratedByName", "DownloadCount", "FinancialYearStart")
  VALUES ('DEMO-T4-RPT-2', v_batch, v_item, 0, 0, now() - interval '3 days', 'Demo analyst', 0, 2026);
  INSERT INTO "SampleLabResults" ("SampleItemId", "Parameter", "NormalRange", "EnteredValue", "Unit", "ResultLabel", "Status", "SortOrder", "LabParameterId", "EnteredByName", "EnteredAt")
  VALUES
    (v_item, 'pH', '6.5 - 7.5', '6.2', '-', 'Acidic', 1, 1, 1, 'Demo analyst', now() - interval '4 days'),
    (v_item, 'Nitrogen', '280 - 560', '310', 'kg/ha', 'Medium', 0, 4, 4, 'Demo analyst', now() - interval '4 days'),
    (v_item, 'Phosphorus', '22 - 56', '40', 'kg/ha', 'Medium', 0, 5, 5, 'Demo analyst', now() - interval '4 days');
END $$;
