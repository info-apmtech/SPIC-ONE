-- Farmer portal Task 7: removes the harvest forecasts of the LOCAL test farmers (smoke_task7.py and shoot_task7.py
-- save some). LOCAL spicone_dev only.
--     psql -h localhost -U postgres -d spicone_dev -v ON_ERROR_STOP=1 -f tools/farmer-portal-check/cleanup_task7_local.sql
DO $$
BEGIN
    IF current_database() <> 'spicone_dev' THEN
        RAISE EXCEPTION 'cleanup_task7_local.sql runs on the local spicone_dev database only (this is %)', current_database();
    END IF;
END $$;

DELETE FROM "YieldPredictions" y
USING "FarmerProfiles" p
WHERE p."UserId" = y."FarmerUserId"
  AND p."Phone" LIKE '6%';
