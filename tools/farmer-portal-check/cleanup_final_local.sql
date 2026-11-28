-- Farmer portal final fix wave: remove what smoke_final.py leaves behind on the LOCAL spicone_dev database.
--   psql -h localhost -U postgres -d spicone_dev -v ON_ERROR_STOP=1 -f tools/farmer-portal-check/cleanup_final_local.sql
-- Throw-away farmers use 600xxxxxxx numbers (user name f600xxxxxxx); a completed run deletes them through the API itself.
-- Never run this against any other database.

BEGIN;

CREATE TEMP TABLE smoke_final_users ON COMMIT DROP AS
    SELECT "Id" FROM "AppUsers" WHERE "UserName" LIKE 'f600%' AND "Role" = 12;

DELETE FROM "FarmerChatMessages" WHERE "FarmerUserId" IN (SELECT "Id" FROM smoke_final_users);
DELETE FROM "FarmerChatConversations" WHERE "FarmerUserId" IN (SELECT "Id" FROM smoke_final_users);
DELETE FROM "FarmerAiUsages" WHERE "FarmerUserId" IN (SELECT "Id" FROM smoke_final_users);
DELETE FROM "LeafDiagnoses" WHERE "FarmerUserId" IN (SELECT "Id" FROM smoke_final_users);
DELETE FROM "FertilizerRecommendationRuns" WHERE "FarmerUserId" IN (SELECT "Id" FROM smoke_final_users);
DELETE FROM "YieldPredictions" WHERE "FarmerUserId" IN (SELECT "Id" FROM smoke_final_users);
DELETE FROM "FarmerContactRequests" WHERE "Message" LIKE '[smoke-final]%';
DELETE FROM "RefreshTokens" WHERE "UserId" IN (SELECT "Id" FROM smoke_final_users);
DELETE FROM "FarmerProfiles" WHERE "UserId" IN (SELECT "Id" FROM smoke_final_users);
DELETE FROM "FarmerOtpChallenges" WHERE "Phone" LIKE '600%';
DELETE FROM "SasFarmers" WHERE "Name" = '[smoke-final] SAS';

-- Community rows the script wrote (its own throw-away farmers' posts, and the seeded qa.mdo posts).
DELETE FROM "CommunityPostAttachments" WHERE "PostId" IN (SELECT "Id" FROM "CommunityPosts" WHERE "Title" LIKE '[smoke-final]%');
DELETE FROM "CommunityPostReplies" WHERE "Body" LIKE '[smoke-final]%' OR "PostId" IN (SELECT "Id" FROM "CommunityPosts" WHERE "Title" LIKE '[smoke-final]%');
DELETE FROM "CommunityReactions" WHERE "UserId" IN (SELECT "Id" FROM smoke_final_users);
DELETE FROM "CommunityPosts" WHERE "Title" LIKE '[smoke-final]%';

DELETE FROM "AppUserRoles" WHERE "UserId" IN (SELECT "Id" FROM smoke_final_users);
DELETE FROM "AppUsers" WHERE "Id" IN (SELECT "Id" FROM smoke_final_users);

COMMIT;
