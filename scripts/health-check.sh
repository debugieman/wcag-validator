#!/bin/sh
set -eu

HEALTH_URL="${App__BaseUrl}/api/health"
RESEND_URL="${RESEND_API_URL:-https://api.resend.com/emails}"
STATE_FILE="${HEALTH_STATE_FILE:-/state/health-check.state}"
FAIL_THRESHOLD="${HEALTH_FAIL_THRESHOLD:-3}"
ALERT_FROM="${Alert__From:-WCAG Analyzer Monitor <noreply@wcag-analyzer.com>}"

mkdir -p "$(dirname "$STATE_FILE")"
[ -f "$STATE_FILE" ] || echo "0 up" > "$STATE_FILE"
read -r FAIL_COUNT LAST_STATUS < "$STATE_FILE"

send_alert() {
  subject="$1"
  curl -fsS -X POST "$RESEND_URL" \
    -H "Authorization: Bearer ${Resend__ApiKey}" \
    -H "Content-Type: application/json" \
    -d "{\"from\":\"$ALERT_FROM\",\"to\":[\"${Alert__Email}\"],\"subject\":\"$subject\",\"text\":\"$subject ($HEALTH_URL)\"}" \
    > /dev/null
}

if curl -fsS --max-time 10 "$HEALTH_URL" > /dev/null 2>&1; then
  if [ "$LAST_STATUS" = "down" ]; then
    echo "health-check: $HEALTH_URL recovered, sending recovery alert"
    send_alert "RECOVERED: WCAG Analyzer is back up"
  fi
  echo "0 up" > "$STATE_FILE"
else
  FAIL_COUNT=$((FAIL_COUNT + 1))
  if [ "$FAIL_COUNT" -ge "$FAIL_THRESHOLD" ] && [ "$LAST_STATUS" != "down" ]; then
    echo "health-check: $HEALTH_URL failed $FAIL_COUNT consecutive checks, sending alert"
    send_alert "DOWN: WCAG Analyzer health check failing"
    echo "$FAIL_COUNT down" > "$STATE_FILE"
  else
    echo "health-check: $HEALTH_URL failed ($FAIL_COUNT/$FAIL_THRESHOLD)"
    echo "$FAIL_COUNT $LAST_STATUS" > "$STATE_FILE"
  fi
fi
