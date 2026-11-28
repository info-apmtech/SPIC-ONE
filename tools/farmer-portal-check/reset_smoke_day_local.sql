-- Farmer portal recheck fixes: resets the LOCAL test farmers' daily allowances so the Task 1-7 and final smoke scripts
-- can run again on the same day (the caps and the OTP per-phone hourly limit work as designed and would refuse them).
-- LOCAL spicone_dev only:
--     psql -h localhost -U postgres -d spicone_dev -v ON_ERROR_STOP=1 -f tools/farmer-portal-check/reset_smoke_day_local.sql
-- Then run cleanup_task6_local.sql for the leaf checks.
DO $$
BEGIN
    IF current_database() <> 'spicone_dev' THEN
        RAISE EXCEPTION 'reset_smoke_day_local.sql runs on the local spicone_dev database only (this is %)', current_database();
    END IF;
END $$;

-- Sign-in codes of the shared test numbers (FarmerOtpChallenges.CreatedAtUtc is UTC without time zone).
DELETE FROM "FarmerOtpChallenges"
WHERE "Phone" IN ('6777777703', '6163665844', '6999174165', '6404528677', '6846575346', '6271619756', '6614843176', '6224431458')
  AND "CreatedAtUtc" >= (now() at time zone 'utc') - interval '1 day';

-- Fertilizer plans and harvest forecasts of the shared test farmers (50 a day each).
DELETE FROM "FertilizerRecommendationRuns" r
USING "AppUsers" u
WHERE u."Id" = r."FarmerUserId"
  AND u."UserName" IN ('f6777777703', 'f6163665844', 'f6999174165');
DELETE FROM "YieldPredictions" y
USING "AppUsers" u
WHERE u."Id" = y."FarmerUserId"
  AND u."UserName" IN ('f6777777703', 'f6163665844', 'f6999174165');

-- Smoke call-backs (10 a day across officers).
DELETE FROM "FarmerContactRequests" WHERE "Message" LIKE '[smoke]%';

-- The Task 3 smoke discussion of the shared test farmers (5 discussions a day, deleted ones count).
DELETE FROM "CommunityPostReplies" WHERE "PostId" IN (SELECT "Id" FROM "CommunityPosts" WHERE "Title" LIKE '[smoke] %'
    AND "AuthorUserId" IN (SELECT "Id" FROM "AppUsers" WHERE "UserName" IN ('f6777777703', 'f6163665844', 'f6999174165')));
DELETE FROM "CommunityPosts" WHERE "Title" LIKE '[smoke] %'
    AND "AuthorUserId" IN (SELECT "Id" FROM "AppUsers" WHERE "UserName" IN ('f6777777703', 'f6163665844', 'f6999174165'));
