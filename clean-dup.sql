DELETE FROM "ClassSessions" c
WHERE c."EnrolledCount" = 0
  AND EXISTS (
    SELECT 1 FROM "ClassSessions" d
    WHERE d."Name" = c."Name"
      AND d."StartTime" = c."StartTime"
      AND d."Id" < c."Id"
  )
  AND NOT EXISTS (
    SELECT 1 FROM "Enrollments" e WHERE e."ClassSessionId" = c."Id"
  );