# StudioSport — ניהול שיעורי סטודיו כושר

מערכת Web API לניהול שיעורים בסטודיו כושר: משתמשת בעלת מנוי נרשמת לשיעורים,
ומנהלת מפעילה את השיעורים.

## Limited Resource & Concurrency

המשאב המוגבל הוא מספר המקומות הפנויים בכל שיעור (`ClassSession.Capacity`).
התחרות נוצרת כאשר שתי משתמשות לוחצות "הירשם" לאותו שיעור באותו רגע.

הישות `ClassSession` נושאת שדה `Version` המוגדר כ-concurrency token
(`IsConcurrencyToken()` ב-Fluent API). בזמן ה-UPDATE משמש ה-`Version`
בתנאי ה-WHERE:

    UPDATE "ClassSessions" SET "EnrolledCount" = @new, "Version" = @newVersion
    WHERE "Id" = @id AND "Version" = @oldVersion

אם השורה השתנתה בייניים — אף שורה לא מתעדכנת, ו-EF Core זורק
`DbUpdateConcurrencyException`. ה-middleware מתרגם אותה ל-409 Conflict עם
הודעת JSON אחידה (`status`, `message`, `correlationId`), ורושם Warning ב-NLog.

הבדיקה `StudioSport.Tests/Integration/EnrollmentConcurrencyTests` מוכיחה את
המקרה בשני DbContexts נפרדים: הראשון שומר בהצלחה, השני מקבל
`DbUpdateConcurrencyException`.

## Tech Stack

- ASP.NET Core Web API (.NET 10)
- EF Core Code First, PostgreSQL (Npgsql)
- JWT Authentication + Authorization
- AutoMapper, NLog, Swagger
- xUnit + Moq

## Architecture

    StudioSport.Core      // Entities, Enums, DTOs, Interfaces, Common
    StudioSport.Data      // DbContext, Repositories, Migrations, Seed
    StudioSport.Service   // Business logic
    StudioSport.API       // Controllers, Middleware, Program.cs

## Running Locally

דרישות: .NET 10 SDK, PostgreSQL.

    dotnet restore
    dotnet build
    dotnet user-secrets init --project StudioSport.API
    dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=StudioSportDb;Username=YOUR_USER;Password=YOUR_PASSWORD" --project StudioSport.API
    dotnet user-secrets set "Jwt:Key" "YOUR_SECRET_KEY_AT_LEAST_32_CHARS" --project StudioSport.API
    dotnet ef database update -p StudioSport.Data -s StudioSport.API
    dotnet run --project StudioSport.API

Swagger: `http://localhost:5000/swagger`

הסודות אינם נשמרים בקוד ובקונפיגורציה — רק ב-User Secrets.

## Demo Users

| Username | Password | Role |
|---|---|---|
| admin | Admin123! | Admin |
| ruthi | Member123! | Member |
| david | Member123! | Member |

## Main Endpoints

| Method | Route | Role |
|---|---|---|
| POST | /api/auth/register | Anonymous |
| POST | /api/auth/login | Anonymous |
| GET | /api/classes | Anonymous |
| GET | /api/classes/{id} | Anonymous |
| POST | /api/classes | Admin |
| PUT | /api/classes/{id} | Admin |
| DELETE | /api/classes/{id} | Admin |
| POST | /api/classes/{classId}/enrollments | Member |
| DELETE | /api/enrollments/{id} | Owner |
| GET | /api/enrollments/my | Member |
| POST | /api/enrollments/{id}/feedback | Owner |

## Testing the Race Condition

דרושים שני משתמשים שונים בעלי מנוי פעיל, ושיעור עם מקום פנוי אחד
(`availableSpots: 1`).

1. היכנסי עם ruthi בלשונית פרטית אחת ועם david בשנייה.
2. שלחי `POST /api/classes/{id}/enrollments` משתי הלשוניות בו-זמנית.

בקשה אחת תחזור `201 Created`, השנייה `409 Conflict` עם הודעת JSON הכוללת
`correlationId`, ובלוג NLog יירשם אירוע Warning.

## Tests

    dotnet test

- בדיקות יחידה על שכבת ה-Service ב-Moq (המקרה התקין והמקרה הנדחה של המשאב המוגבל).
- בדיקת אינטגרציה עם שני DbContexts נפרדים המוכיחה את ה-concurrency token.
  דורשת משתנה סביבה `STUDIO_TEST_DB` עם מחרוזת חיבור לבסיס נתונים זמין.

## Out of Scope

תשלום, רשימת המתנה, ניהול חדרים, פריסה לענן, Docker.