#!/bin/sh
set -eu

SRC="/app/data/wcag-analyzer.db"
DEST_DIR="/backups"
KEEP="${BACKUP_KEEP_COUNT:-14}"

if [ ! -f "$SRC" ]; then
  echo "backup-db: source database not found at $SRC, skipping" >&2
  exit 0
fi

mkdir -p "$DEST_DIR"

TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
DEST="$DEST_DIR/wcag-analyzer-$TIMESTAMP.db"

# Uses SQLite's online backup API (safe to run against a database that is
# being written to concurrently) rather than copying the file directly,
# which could capture it mid-write and produce a corrupt backup.
sqlite3 "$SRC" ".backup '$DEST'"
gzip "$DEST"

echo "backup-db: wrote $DEST.gz"

# Rotate: keep only the $KEEP most recent backups.
cd "$DEST_DIR"
# shellcheck disable=SC2012
ls -1t wcag-analyzer-*.db.gz 2>/dev/null | tail -n "+$((KEEP + 1))" | xargs -r rm --
