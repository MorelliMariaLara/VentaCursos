/*
  SANTICAZA — preguntas por lección + intentos de quiz
  Servidor: LARA-NB\SQLEXPRESS02
  Base:     CursoVentas

  También agrega VideoWatched a EnrollmentProgress.
  Ejecutar en SSMS si la app no pudo crear las tablas sola.
*/

USE [CursoVentas];
GO

IF COL_LENGTH(N'dbo.EnrollmentProgress', N'VideoWatched') IS NULL
BEGIN
    ALTER TABLE dbo.EnrollmentProgress ADD VideoWatched BIT NOT NULL
        CONSTRAINT DF_EP_VideoWatched DEFAULT (0);
END
GO

IF COL_LENGTH(N'dbo.EnrollmentProgress', N'QuizPassed') IS NULL
BEGIN
    ALTER TABLE dbo.EnrollmentProgress ADD QuizPassed BIT NOT NULL
        CONSTRAINT DF_EP_QuizPassed DEFAULT (0);
END
GO

IF OBJECT_ID(N'dbo.LessonQuestions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LessonQuestions
    (
        Id        NVARCHAR(64)   NOT NULL CONSTRAINT PK_LessonQuestions PRIMARY KEY,
        LessonId  NVARCHAR(64)   NOT NULL,
        Prompt    NVARCHAR(1000) NOT NULL,
        SortOrder INT            NOT NULL CONSTRAINT DF_LQ_Sort DEFAULT (0),
        CONSTRAINT FK_LQ_Lessons FOREIGN KEY (LessonId)
            REFERENCES dbo.Lessons (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_LessonQuestions_LessonId ON dbo.LessonQuestions (LessonId);
END
GO

IF OBJECT_ID(N'dbo.LessonAnswers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LessonAnswers
    (
        Id         NVARCHAR(64)  NOT NULL CONSTRAINT PK_LessonAnswers PRIMARY KEY,
        QuestionId NVARCHAR(64)  NOT NULL,
        Text       NVARCHAR(500) NOT NULL,
        IsCorrect  BIT           NOT NULL CONSTRAINT DF_LA_Correct DEFAULT (0),
        SortOrder  INT           NOT NULL CONSTRAINT DF_LA_Sort DEFAULT (0),
        CONSTRAINT FK_LA_Questions FOREIGN KEY (QuestionId)
            REFERENCES dbo.LessonQuestions (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_LessonAnswers_QuestionId ON dbo.LessonAnswers (QuestionId);
END
GO

IF OBJECT_ID(N'dbo.QuizAttempts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuizAttempts
    (
        Id           NVARCHAR(64) NOT NULL CONSTRAINT PK_QuizAttempts PRIMARY KEY,
        EnrollmentId NVARCHAR(64) NOT NULL,
        LessonId     NVARCHAR(64) NOT NULL,
        Score        INT          NOT NULL,
        Total        INT          NOT NULL,
        PercentScore DECIMAL(5,2) NOT NULL CONSTRAINT DF_QA_Percent DEFAULT (0),
        Passed       BIT          NOT NULL CONSTRAINT DF_QA_Passed DEFAULT (0),
        AttemptedAt  DATETIME2(3) NOT NULL CONSTRAINT DF_QA_At DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_QA_Enrollments FOREIGN KEY (EnrollmentId)
            REFERENCES dbo.Enrollments (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_QuizAttempts_Enrollment_Lesson ON dbo.QuizAttempts (EnrollmentId, LessonId);
END
GO

IF OBJECT_ID(N'dbo.QuizAttemptAnswers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuizAttemptAnswers
    (
        AttemptId  NVARCHAR(64) NOT NULL,
        QuestionId NVARCHAR(64) NOT NULL,
        AnswerId   NVARCHAR(64) NOT NULL,
        IsCorrect  BIT          NOT NULL,
        CONSTRAINT PK_QuizAttemptAnswers PRIMARY KEY (AttemptId, QuestionId),
        CONSTRAINT FK_QAA_Attempts FOREIGN KEY (AttemptId)
            REFERENCES dbo.QuizAttempts (Id) ON DELETE CASCADE
    );
END
GO

PRINT N'Tablas de quiz listas.';
GO
