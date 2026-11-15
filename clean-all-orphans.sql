-- 1. העברת הרשמות מהיתום לתאום עם תבנית באותו מועד
UPDATE "Enrollments" e
SET "ClassSessionId" = twin."Id"
FROM "ClassSessions" orphan
JOIN "ClassSessions" twin
  ON twin."Name" = orphan."Name"
 AND twin."StartTime" = orphan."StartTime"
 AND twin."ClassTemplateId" IS NOT NULL
WHERE e."ClassSessionId" = orphan."Id"
  AND orphan."ClassTemplateId" IS NULL
  AND orphan."StartTime" > NOW();

-- 2. מחיקת היתומים
DELETE FROM "ClassSessions"
WHERE "ClassTemplateId" IS NULL
  AND "StartTime" > NOW()
  AND NOT EXISTS (SELECT 1 FROM "Enrollments" e WHERE e."ClassSessionId" = "ClassSessions"."Id");

-- 3. איפוס כל המונים לפי הרשמות בפועל
UPDATE "ClassSessions" c
SET "EnrolledCount" = (SELECT COUNT(*) FROM "Enrollments" e
                       WHERE e."ClassSessionId" = c."Id"
                         AND e."Status" = 'Active');