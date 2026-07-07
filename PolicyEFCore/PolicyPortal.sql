/*
    ATME Employee Policy Portal
    Complete SQL Server Database Creation Script
    Database Name: PolicyPortalDB
*/

USE [master]
GO

IF DB_ID(N'PolicyPortalDB') IS NOT NULL
BEGIN
    ALTER DATABASE [PolicyPortalDB]
        SET SINGLE_USER
        WITH ROLLBACK IMMEDIATE;

    DROP DATABASE [PolicyPortalDB];
END
GO

CREATE DATABASE [PolicyPortalDB]
GO

USE [PolicyPortalDB]
GO

/* =========================================================
   TABLES
   ========================================================= */

CREATE TABLE [Roles]
(
    [RoleId] TINYINT IDENTITY(1,1)
        CONSTRAINT [PK_Roles] PRIMARY KEY,

    [RoleName] VARCHAR(30)
        CONSTRAINT [UQ_Roles_RoleName] UNIQUE
        NOT NULL
)
GO

CREATE TABLE [Registrations]
(
    [UserId] INT IDENTITY(1,1)
        CONSTRAINT [PK_Registrations] PRIMARY KEY,

    [EmployeeId] VARCHAR(30)
        CONSTRAINT [UQ_Registrations_EmployeeId] UNIQUE
        NOT NULL,

    [Name] VARCHAR(150) NOT NULL,

    [Department] VARCHAR(200) NOT NULL,

    [Designation] VARCHAR(150) NOT NULL,

    [EmailId] VARCHAR(256)
        CONSTRAINT [UQ_Registrations_EmailId] UNIQUE
        NOT NULL,

    [ContactNo] VARCHAR(20) NOT NULL,

    [RegistrationCode] VARCHAR(50) NOT NULL,

    [PasswordHash] VARCHAR(500) NOT NULL,

    [RoleId] TINYINT
        CONSTRAINT [FK_Registrations_Roles]
        REFERENCES [Roles]([RoleId])
        NOT NULL,

    [CreatedAt] DATETIME2(0)
        CONSTRAINT [DF_Registrations_CreatedAt]
        DEFAULT SYSUTCDATETIME()
        NOT NULL,

    [UpdatedAt] DATETIME2(0) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1
)
GO

CREATE TABLE [Policies]
(
    [PolicyId] INT IDENTITY(1,1)
        CONSTRAINT [PK_Policies] PRIMARY KEY,

    [PolicyTitle] VARCHAR(500)
        CONSTRAINT [UQ_Policies_PolicyTitle] UNIQUE
        NOT NULL,

    [Category] VARCHAR(100) NOT NULL,

    [Version] VARCHAR(20) NOT NULL,

    [Overview] NVARCHAR(2000) NOT NULL,

    [PolicyDocumentLink] VARCHAR(1000) NOT NULL,

    [FaqFileId] VARCHAR(100) NULL,

    [QuizFileId] VARCHAR(100) NULL,

    [LastUploaded] DATETIME2(0)
        CONSTRAINT [DF_Policies_LastUploaded]
        DEFAULT SYSUTCDATETIME()
        NOT NULL,

    [LastUpdated] DATETIME2(0)
        CONSTRAINT [DF_Policies_LastUpdated]
        DEFAULT SYSUTCDATETIME()
        NOT NULL,

    [IsActive] BIT
        CONSTRAINT [DF_Policies_IsActive]
        DEFAULT 1
        NOT NULL
)
GO

CREATE TABLE [Status]
(
    [StatusId] BIGINT IDENTITY(1,1)
        CONSTRAINT [PK_Status] PRIMARY KEY,

    [EmployeeId] VARCHAR(30) NOT NULL,

    [Name] VARCHAR(150) NOT NULL,

    [Department] VARCHAR(200) NOT NULL,

    [Category] VARCHAR(100) NOT NULL,

    [PolicyTitle] VARCHAR(500) NOT NULL,

    [Acknowledged] VARCHAR(10)
        CONSTRAINT [DF_Status_Acknowledged]
        DEFAULT 'Yes'
        CONSTRAINT [CK_Status_Acknowledged]
        CHECK ([Acknowledged] IN ('Yes', 'No'))
        NOT NULL,

    [DateAcknowledged] DATETIME2(0)
        CONSTRAINT [DF_Status_DateAcknowledged]
        DEFAULT SYSUTCDATETIME()
        NOT NULL,

    CONSTRAINT [UQ_Status_Employee_Policy]
        UNIQUE ([EmployeeId], [PolicyTitle])
)
GO

CREATE TABLE [Quiz]
(
    [QuizAttemptId] BIGINT IDENTITY(1,1)
        CONSTRAINT [PK_Quiz] PRIMARY KEY,

    [EmployeeId] VARCHAR(30) NOT NULL,

    [QuizTitle] VARCHAR(500) NOT NULL,

    [Attempt1] DECIMAL(5,2) NULL,
    [Attempt2] DECIMAL(5,2) NULL,
    [Attempt3] DECIMAL(5,2) NULL,
    [Attempt4] DECIMAL(5,2) NULL,
    [Attempt5] DECIMAL(5,2) NULL,
    [Attempt6] DECIMAL(5,2) NULL,
    [Attempt7] DECIMAL(5,2) NULL,
    [Attempt8] DECIMAL(5,2) NULL,
    [Attempt9] DECIMAL(5,2) NULL,
    [Attempt10] DECIMAL(5,2) NULL,

    [BestScore] DECIMAL(5,2)
        CONSTRAINT [DF_Quiz_BestScore]
        DEFAULT 0.00
        NOT NULL,

    [DateAttempted] DATETIME2(0)
        CONSTRAINT [DF_Quiz_DateAttempted]
        DEFAULT SYSUTCDATETIME()
        NOT NULL,

    CONSTRAINT [UQ_Quiz_Employee_Title]
        UNIQUE ([EmployeeId], [QuizTitle])
)
GO

CREATE TABLE [LastLogin]
(
    [LoginSessionId] BIGINT IDENTITY(1,1)
        CONSTRAINT [PK_LastLogin] PRIMARY KEY,

    [EmployeeId] VARCHAR(30) NOT NULL,

    [LoginTime] DATETIME2(0)
        CONSTRAINT [DF_LastLogin_LoginTime]
        DEFAULT SYSUTCDATETIME()
        NOT NULL
)
GO

/* =========================================================
   INDEXES
   ========================================================= */

CREATE INDEX [IX_Registrations_Department]
    ON [Registrations]([Department])
GO

CREATE INDEX [IX_Registrations_Designation]
    ON [Registrations]([Designation])
GO

CREATE INDEX [IX_Policies_Category]
    ON [Policies]([Category])
GO

CREATE INDEX [IX_Status_EmployeeId_PolicyTitle]
    ON [Status]([EmployeeId], [PolicyTitle])
GO

CREATE INDEX [IX_Quiz_EmployeeId_QuizTitle]
    ON [Quiz]([EmployeeId], [QuizTitle])
GO

CREATE INDEX [IX_LastLogin_EmployeeId_LoginTime]
    ON [LastLogin]([EmployeeId], [LoginTime] DESC)
GO

/* =========================================================
   STORED PROCEDURES
   ========================================================= */

CREATE PROCEDURE [dbo].[usp_RegisterUser]
(
    @EmployeeId VARCHAR(30),
    @Name VARCHAR(150),
    @Department VARCHAR(200),
    @Designation VARCHAR(150),
    @EmailId VARCHAR(256),
    @ContactNo VARCHAR(20),
    @RegistrationCode VARCHAR(50),
    @PasswordHash VARCHAR(500),
    @UserId INT OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET @UserId = 0;

    SET @EmployeeId = UPPER(LTRIM(RTRIM(@EmployeeId)));
    SET @Name = LTRIM(RTRIM(@Name));
    SET @Department = LTRIM(RTRIM(@Department));
    SET @Designation = LTRIM(RTRIM(@Designation));
    SET @EmailId = LOWER(LTRIM(RTRIM(@EmailId)));

    IF EXISTS (SELECT 1 FROM [Registrations] WHERE [EmployeeId] = @EmployeeId)
        THROW 60001, 'An account with this Employee ID already exists!', 1;

    IF EXISTS (SELECT 1 FROM [Registrations] WHERE [EmailId] = @EmailId)
        THROW 60002, 'An account with this Email ID already exists!', 1;

    DECLARE @RoleId TINYINT;
    SELECT @RoleId = [RoleId] FROM [Roles] WHERE [RoleName] = 'Faculty';
    IF @RoleId IS NULL SET @RoleId = 2; -- Default fallback

    INSERT INTO [Registrations]
    (
        [EmployeeId], [Name], [Department], [Designation], 
        [EmailId], [ContactNo], [RegistrationCode], [PasswordHash], [RoleId]
    )
    VALUES
    (
        @EmployeeId, @Name, @Department, @Designation, 
        @EmailId, @ContactNo, @RegistrationCode, @PasswordHash, @RoleId
    );

    SET @UserId = SCOPE_IDENTITY();
END
GO

CREATE PROCEDURE [dbo].[usp_UpdateUserProfile]
(
    @EmployeeId VARCHAR(30),
    @Department VARCHAR(200),
    @Designation VARCHAR(150)
)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE [Registrations]
    SET 
        [Department] = @Department,
        [Designation] = @Designation,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [EmployeeId] = UPPER(LTRIM(RTRIM(@EmployeeId)));

    IF @@ROWCOUNT = 0
        THROW 60003, 'Employee ID not found.', 1;
END
GO

CREATE PROCEDURE [dbo].[usp_AcknowledgePolicy]
(
    @EmployeeId VARCHAR(30),
    @Name VARCHAR(150),
    @Department VARCHAR(200),
    @Category VARCHAR(100),
    @PolicyTitle VARCHAR(500)
)
AS
BEGIN
    SET NOCOUNT ON;

    SET @EmployeeId = UPPER(LTRIM(RTRIM(@EmployeeId)));
    SET @PolicyTitle = LTRIM(RTRIM(@PolicyTitle));

    IF EXISTS (SELECT 1 FROM [Status] WHERE [EmployeeId] = @EmployeeId AND [PolicyTitle] = @PolicyTitle AND [Acknowledged] = 'Yes')
    BEGIN
        RETURN; -- Already acknowledged
    END

    INSERT INTO [Status]
    (
        [EmployeeId], [Name], [Department], [Category], [PolicyTitle], [Acknowledged]
    )
    VALUES
    (
        @EmployeeId, @Name, @Department, @Category, @PolicyTitle, 'Yes'
    );
END
GO

CREATE PROCEDURE [dbo].[usp_SubmitQuizScore]
(
    @EmployeeId VARCHAR(30),
    @QuizTitle VARCHAR(500),
    @Score DECIMAL(5,2)
)
AS
BEGIN
    SET NOCOUNT ON;

    SET @EmployeeId = UPPER(LTRIM(RTRIM(@EmployeeId)));
    SET @QuizTitle = LTRIM(RTRIM(@QuizTitle));

    IF EXISTS (SELECT 1 FROM [Quiz] WHERE [EmployeeId] = @EmployeeId AND [QuizTitle] = @QuizTitle)
    BEGIN
        DECLARE @ColumnIndex INT = 0;
        
        -- Find first open slot from Attempt 1 to 10
        UPDATE [Quiz]
        SET
            [Attempt1] = CASE WHEN [Attempt1] IS NULL THEN @Score ELSE [Attempt1] END,
            [Attempt2] = CASE WHEN [Attempt1] IS NOT NULL AND [Attempt2] IS NULL THEN @Score ELSE [Attempt2] END,
            [Attempt3] = CASE WHEN [Attempt2] IS NOT NULL AND [Attempt3] IS NULL THEN @Score ELSE [Attempt3] END,
            [Attempt4] = CASE WHEN [Attempt3] IS NOT NULL AND [Attempt4] IS NULL THEN @Score ELSE [Attempt4] END,
            [Attempt5] = CASE WHEN [Attempt4] IS NOT NULL AND [Attempt5] IS NULL THEN @Score ELSE [Attempt5] END,
            [Attempt6] = CASE WHEN [Attempt5] IS NOT NULL AND [Attempt6] IS NULL THEN @Score ELSE [Attempt6] END,
            [Attempt7] = CASE WHEN [Attempt6] IS NOT NULL AND [Attempt7] IS NULL THEN @Score ELSE [Attempt7] END,
            [Attempt8] = CASE WHEN [Attempt7] IS NOT NULL AND [Attempt8] IS NULL THEN @Score ELSE [Attempt8] END,
            [Attempt9] = CASE WHEN [Attempt8] IS NOT NULL AND [Attempt9] IS NULL THEN @Score ELSE [Attempt9] END,
            [Attempt10] = CASE WHEN [Attempt9] IS NOT NULL AND [Attempt10] IS NULL THEN @Score ELSE [Attempt10] END,
            [BestScore] = CASE WHEN @Score > [BestScore] THEN @Score ELSE [BestScore] END,
            [DateAttempted] = SYSUTCDATETIME()
        WHERE [EmployeeId] = @EmployeeId AND [QuizTitle] = @QuizTitle;
    END
    ELSE
    BEGIN
        INSERT INTO [Quiz]
        (
            [EmployeeId], [QuizTitle], [Attempt1], [BestScore]
        )
        VALUES
        (
            @EmployeeId, @QuizTitle, @Score, @Score
        );
    END
END
GO

/* =========================================================
   SEED DATA
   ========================================================= */

INSERT INTO [Roles] ([RoleName])
VALUES
    ('Admin'),
    ('Faculty');
GO

INSERT INTO [Policies]
(
    [PolicyTitle], [Category], [Version], [Overview], 
    [PolicyDocumentLink], [FaqFileId], [QuizFileId]
)
VALUES
(
    'Leave Policy',
    'HR',
    '2.1',
    'This policy describes the types of leave available to ATME teaching faculty including casual leaves, medical leaves, and permissions.',
    'assets/policies/HR/PolicyDocuments/LeavePolicy.pdf',
    'assets/policies/HR/FAQ/LeavePolicyFAQ.html',
    'assets/policies/HR/Quiz/LeavePolicyQuiz.html'
);
GO

CREATE TABLE [Notifications] (
    [NotificationId] INT IDENTITY(1,1) PRIMARY KEY,
    [Message] NVARCHAR(MAX) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

INSERT INTO [Registrations] (
    [EmployeeId], [Name], [Department], [Designation], [EmailId], [ContactNo], [RegistrationCode], [PasswordHash], [RoleId]
) VALUES (
    'SUPPORT', 'Support Admin', 'Administration', 'Principal', 'policy.support@atme.edu.in', '0821-2593333', 'SUPPORT@ATME', 'policy.support', 1
);
GO