-- Database bootstrap SQL: creates the SmartIot database if it does not already exist.
/*
  Docker first-run bootstrap.
  The API uses EF Core EnsureCreated against the SmartIot model to create
  the tables and relationships when this database has no schema yet.
  Safe to rerun: an existing SmartIot database is preserved.
*/
IF DB_ID(N'SmartIot') IS NULL
    EXEC(N'CREATE DATABASE [SmartIot]');
GO
