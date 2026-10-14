#!/usr/bin/env bash
# Database bootstrap: waits for SQL Server readiness and runs the initial SmartIot database SQL script.
set -euo pipefail

if [[ -x /opt/mssql-tools18/bin/sqlcmd ]]; then
  SQLCMD=/opt/mssql-tools18/bin/sqlcmd
elif [[ -x /opt/mssql-tools/bin/sqlcmd ]]; then
  SQLCMD=/opt/mssql-tools/bin/sqlcmd
else
  echo "sqlcmd was not found in the mssql-tools container." >&2
  exit 1
fi

until "$SQLCMD" -S sqlserver -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" -b >/dev/null 2>&1; do
  echo "Waiting for SQL Server to accept connections..."
  sleep 3
done

"$SQLCMD" -S sqlserver -U sa -P "$MSSQL_SA_PASSWORD" -C -b -i /scripts/001-create-database.sql
echo "SmartIot database bootstrap completed."
