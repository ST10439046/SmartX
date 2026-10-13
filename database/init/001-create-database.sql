/*
  Docker first-run bootstrap.
  The API uses EF Core EnsureCreated against the SmartIot model to create
  the tables and relationships when this database has no schema yet.
  Safe to rerun: an existing SmartIot database is preserved.
*/
IF DB_ID(N'SmartIot') IS NULL
BEGIN
    CREATE DATABASE [SmartIot];
END;
GO
