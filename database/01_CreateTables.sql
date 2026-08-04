/*
  SANTICAZA Capacitaciones — creación de tablas
  Servidor: LARA-NB\SQLEXPRESS02
  Base:     CursoVentas

  Ejecutar en SSMS o sqlcmd contra la base CursoVentas.
  (La base debe existir previamente.)
*/

USE [CursoVentas];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ========== Users ========== */
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id           NVARCHAR(64)  NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Name         NVARCHAR(200) NOT NULL,
        Email        NVARCHAR(256) NOT NULL,
        PasswordHash NVARCHAR(500) NOT NULL,
        Role         NVARCHAR(50)  NOT NULL CONSTRAINT DF_Users_Role DEFAULT (N'student'),
        CreatedAt    DATETIME2(3)  NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME())
    );

    CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users (Email);
END
GO

/* ========== Courses ========== */
IF OBJECT_ID(N'dbo.Courses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Courses
    (
        Id                  NVARCHAR(64)    NOT NULL CONSTRAINT PK_Courses PRIMARY KEY,
        Slug                NVARCHAR(160)   NOT NULL,
        Title               NVARCHAR(300)   NOT NULL,
        Subtitle            NVARCHAR(500)   NULL,
        Description         NVARCHAR(MAX)   NULL,
        Category            NVARCHAR(100)   NOT NULL CONSTRAINT DF_Courses_Category DEFAULT (N'General'),
        Level               NVARCHAR(50)    NOT NULL CONSTRAINT DF_Courses_Level DEFAULT (N'Inicial'),
        Price               DECIMAL(18, 2)  NOT NULL CONSTRAINT DF_Courses_Price DEFAULT (0),
        Currency            NVARCHAR(10)    NOT NULL CONSTRAINT DF_Courses_Currency DEFAULT (N'ARS'),
        DurationHours       INT             NOT NULL CONSTRAINT DF_Courses_DurationHours DEFAULT (1),
        IncludesCertificate BIT             NOT NULL CONSTRAINT DF_Courses_IncludesCertificate DEFAULT (1),
        CertificateName     NVARCHAR(300)   NULL,
        ThumbnailGradient   NVARCHAR(500)   NULL,
        Instructor          NVARCHAR(200)   NULL,
        Published           BIT             NOT NULL CONSTRAINT DF_Courses_Published DEFAULT (1),
        UpdatedAt           DATETIME2(3)    NULL
    );

    CREATE UNIQUE INDEX UX_Courses_Slug ON dbo.Courses (Slug);
END
GO

/* ========== CourseLearningOutcomes ========== */
IF OBJECT_ID(N'dbo.CourseLearningOutcomes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CourseLearningOutcomes
    (
        Id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CourseLearningOutcomes PRIMARY KEY,
        CourseId  NVARCHAR(64)      NOT NULL,
        SortOrder INT               NOT NULL CONSTRAINT DF_CLO_SortOrder DEFAULT (0),
        Text      NVARCHAR(500)     NOT NULL,
        CONSTRAINT FK_CLO_Courses FOREIGN KEY (CourseId)
            REFERENCES dbo.Courses (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_CLO_CourseId ON dbo.CourseLearningOutcomes (CourseId);
END
GO

/* ========== CourseModules ========== */
IF OBJECT_ID(N'dbo.CourseModules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CourseModules
    (
        Id        NVARCHAR(64)  NOT NULL CONSTRAINT PK_CourseModules PRIMARY KEY,
        CourseId  NVARCHAR(64)  NOT NULL,
        Title     NVARCHAR(300) NOT NULL,
        SortOrder INT           NOT NULL CONSTRAINT DF_Modules_SortOrder DEFAULT (0),
        CONSTRAINT FK_Modules_Courses FOREIGN KEY (CourseId)
            REFERENCES dbo.Courses (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_Modules_CourseId ON dbo.CourseModules (CourseId);
END
GO

/* ========== Lessons ========== */
IF OBJECT_ID(N'dbo.Lessons', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Lessons
    (
        Id              NVARCHAR(64)  NOT NULL CONSTRAINT PK_Lessons PRIMARY KEY,
        ModuleId        NVARCHAR(64)  NOT NULL,
        Title           NVARCHAR(300) NOT NULL,
        DurationMinutes INT           NOT NULL CONSTRAINT DF_Lessons_Duration DEFAULT (0),
        SourceUrl       NVARCHAR(500) NOT NULL,
        [Order]         INT           NOT NULL CONSTRAINT DF_Lessons_Order DEFAULT (0),
        CONSTRAINT FK_Lessons_Modules FOREIGN KEY (ModuleId)
            REFERENCES dbo.CourseModules (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_Lessons_ModuleId ON dbo.Lessons (ModuleId);
END
GO

/* ========== Orders ========== */
IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders
    (
        Id            NVARCHAR(64)   NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
        UserId        NVARCHAR(64)   NOT NULL,
        CourseId      NVARCHAR(64)   NOT NULL,
        Amount        DECIMAL(18, 2) NOT NULL,
        Currency      NVARCHAR(10)   NOT NULL CONSTRAINT DF_Orders_Currency DEFAULT (N'ARS'),
        Status        NVARCHAR(50)   NOT NULL CONSTRAINT DF_Orders_Status DEFAULT (N'pending'),
        CreatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_Orders_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        PreferenceId  NVARCHAR(100)  NULL,
        PaymentId     NVARCHAR(100)  NULL,
        PaymentMethod NVARCHAR(100)  NULL,
        StatusDetail  NVARCHAR(300)  NULL,
        PayerEmail    NVARCHAR(256)  NULL,
        Simulated     BIT            NOT NULL CONSTRAINT DF_Orders_Simulated DEFAULT (0),
        CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId)
            REFERENCES dbo.Users (Id),
        CONSTRAINT FK_Orders_Courses FOREIGN KEY (CourseId)
            REFERENCES dbo.Courses (Id)
    );

    CREATE INDEX IX_Orders_UserId ON dbo.Orders (UserId);
    CREATE INDEX IX_Orders_CourseId ON dbo.Orders (CourseId);
    CREATE INDEX IX_Orders_Status ON dbo.Orders (Status);
END
GO

/* ========== Enrollments ========== */
IF OBJECT_ID(N'dbo.Enrollments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Enrollments
    (
        Id                   NVARCHAR(64)  NOT NULL CONSTRAINT PK_Enrollments PRIMARY KEY,
        UserId               NVARCHAR(64)  NOT NULL,
        CourseId             NVARCHAR(64)  NOT NULL,
        PurchasedAt          DATETIME2(3)  NOT NULL CONSTRAINT DF_Enrollments_PurchasedAt DEFAULT (SYSUTCDATETIME()),
        OrderId              NVARCHAR(64)  NULL,
        CertificateCode      NVARCHAR(100) NULL,
        CertificateIssuedAt  DATETIME2(3)  NULL,
        CONSTRAINT FK_Enrollments_Users FOREIGN KEY (UserId)
            REFERENCES dbo.Users (Id),
        CONSTRAINT FK_Enrollments_Courses FOREIGN KEY (CourseId)
            REFERENCES dbo.Courses (Id),
        CONSTRAINT FK_Enrollments_Orders FOREIGN KEY (OrderId)
            REFERENCES dbo.Orders (Id)
    );

    CREATE UNIQUE INDEX UX_Enrollments_User_Course ON dbo.Enrollments (UserId, CourseId);
END
GO

/* ========== EnrollmentProgress ========== */
IF OBJECT_ID(N'dbo.EnrollmentProgress', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EnrollmentProgress
    (
        EnrollmentId NVARCHAR(64) NOT NULL,
        LessonId     NVARCHAR(64) NOT NULL,
        Completed    BIT          NOT NULL CONSTRAINT DF_EP_Completed DEFAULT (1),
        CompletedAt  DATETIME2(3) NOT NULL CONSTRAINT DF_EP_CompletedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_EnrollmentProgress PRIMARY KEY (EnrollmentId, LessonId),
        CONSTRAINT FK_EP_Enrollments FOREIGN KEY (EnrollmentId)
            REFERENCES dbo.Enrollments (Id) ON DELETE CASCADE
    );
END
GO

PRINT N'Tablas de CursoVentas creadas / verificadas correctamente.';
GO
