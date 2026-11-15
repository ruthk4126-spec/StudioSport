DELETE FROM "Feedbacks";
DELETE FROM "Enrollments";

UPDATE "ClassSessions" SET "EnrolledCount" = 0;

UPDATE "Subscriptions" SET "UsedClasses" = 0;