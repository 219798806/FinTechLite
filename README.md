# FinTechLiteAPI

Quick Start
Prerequisites:
{
.NET 8 SDK 
SQL Server 2022 or SQL Server Express 2022 
SQL Server Management Studio (SSMS)
}

Step 1: Clone the repository

git clone https://github.com/219798806/FinTechLite.git
cd FinTechLite

Step 2: Create configuration file

cp appsettings.EXAMPLE.json appsettings.json

Step 3: Update connection string in appsettings.json

{
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=FinTechLite;Integrated Security=true;TrustServerCertificate=true;"
     }
}

Step 4: Create database in SSMS

CREATE DATABASE FinTechLite;

Step 5: Apply migrations

dotnet restore
dotnet ef database update

Step 6: Load sample test data
Run this in SSMS (connected to FinTechLite database)

USE FinTechLite;
   GO

   -- Password for all test users: "Password123"
   DECLARE @PasswordHash VARCHAR(255) = '$2a$11$rQz3Y9Z8KX9Y5Z8KX9Y5ZeL0O9Y5Z8KX9Y5Z8KX9Y5Z8KX9Y5Z8KX';

   -- Alice (R1,000)
   DECLARE @AliceUserId UNIQUEIDENTIFIER = NEWID();
   INSERT INTO Users VALUES (@AliceUserId, 'alice', 'alice@gmail.com', @PasswordHash, GETUTCDATE());
   INSERT INTO Accounts VALUES (NEWID(), @AliceUserId, 1000.00, 0, GETUTCDATE());

   -- Bob (R500)
   DECLARE @BobUserId UNIQUEIDENTIFIER = NEWID();
   INSERT INTO Users VALUES (@BobUserId, 'bob', 'bob@gmail.com', @PasswordHash, GETUTCDATE());
   INSERT INTO Accounts VALUES (NEWID(), @BobUserId, 500.00, 0, GETUTCDATE());

   -- Verify data
   SELECT u.Username, a.Balance, 'Password123' AS Password
   FROM Users u INNER JOIN Accounts a ON u.UserId = a.UserId;

Step 7: Start the API
dotnet run
