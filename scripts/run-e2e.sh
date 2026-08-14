#!/usr/bin/env bash
set -euo pipefail

gateway_url="${GATEWAY_URL:-http://localhost:8080}"
identity_url="${IDENTITY_URL:-http://localhost:8081}"
timeout_seconds="${E2E_TIMEOUT_SECONDS:-120}"

wait_for_http() {
  local url="$1"
  local deadline=$((SECONDS + timeout_seconds))
  until curl --silent --fail "$url" >/dev/null; do
    if (( SECONDS >= deadline )); then
      echo "Timed out waiting for $url" >&2
      return 1
    fi
    sleep 2
  done
}

token_for() {
  local username="$1"
  local password="$2"
  curl --silent --fail \
    --request POST \
    --header 'Content-Type: application/x-www-form-urlencoded' \
    --data-urlencode 'grant_type=password' \
    --data-urlencode 'client_id=restaurantflow-cli' \
    --data-urlencode "username=$username" \
    --data-urlencode "password=$password" \
    "$identity_url/realms/restaurantflow/protocol/openid-connect/token" | jq --raw-output '.access_token'
}

wait_for_order_status() {
  local order_id="$1"
  local expected_status="$2"
  local token="$3"
  local deadline=$((SECONDS + timeout_seconds))
  while (( SECONDS < deadline )); do
    local body
    body="$(curl --silent --fail --header "Authorization: Bearer $token" "$gateway_url/api/orders/$order_id")"
    if [[ "$(jq --raw-output '.status' <<<"$body")" == "$expected_status" ]]; then
      printf '%s' "$body"
      return 0
    fi
    sleep 2
  done
  echo "Order $order_id did not reach $expected_status" >&2
  return 1
}

wait_for_http "$gateway_url/health"
wait_for_http "$identity_url/realms/restaurantflow/.well-known/openid-configuration"

admin_token="$(token_for restaurant-admin admin)"
customer_token="$(token_for customer customer)"
suffix="$(date +%s)"

menu_item="$(curl --silent --fail \
  --request POST \
  --header "Authorization: Bearer $admin_token" \
  --header 'Content-Type: application/json' \
  --data "{\"name\":\"E2E Burger $suffix\",\"description\":\"Workflow test item\",\"category\":\"Main\",\"price\":27.50}" \
  "$gateway_url/api/menu/items")"
menu_item_id="$(jq --raw-output '.id' <<<"$menu_item")"

create_order() {
  local payment_reference="$1"
  curl --silent --fail \
    --request POST \
    --header "Authorization: Bearer $customer_token" \
    --header 'Content-Type: application/json' \
    --data "{\"customerId\":\"00000000-0000-0000-0000-000000000000\",\"customerEmail\":\"forged@example.com\",\"paymentReference\":\"$payment_reference\",\"items\":[{\"menuItemId\":\"$menu_item_id\",\"quantity\":2}]}" \
    "$gateway_url/api/orders"
}

approved_order="$(create_order "approved-$suffix")"
approved_id="$(jq --raw-output '.id' <<<"$approved_order")"
approved_result="$(wait_for_order_status "$approved_id" 'accepted-by-kitchen' "$customer_token")"
jq --exit-status '.total == 55' <<<"$approved_result" >/dev/null
approved_workflow="$(curl --silent --fail --header "Authorization: Bearer $customer_token" "$gateway_url/api/orders/$approved_id/workflow")"
[[ "$(jq --raw-output '.currentState' <<<"$approved_workflow")" == "InPreparation" ]]
[[ "$(jq --raw-output '.paymentId' <<<"$approved_workflow")" != "null" ]]

declined_order="$(create_order "decline-$suffix")"
declined_id="$(jq --raw-output '.id' <<<"$declined_order")"
wait_for_order_status "$declined_id" 'cancelled' "$customer_token" >/dev/null
declined_workflow="$(curl --silent --fail --header "Authorization: Bearer $customer_token" "$gateway_url/api/orders/$declined_id/workflow")"
[[ "$(jq --raw-output '.currentState' <<<"$declined_workflow")" == "Final" ]]
[[ "$(jq --raw-output '.failureReason' <<<"$declined_workflow")" == "Payment was declined by the simulated provider." ]]

anonymous_status="$(curl --silent --output /dev/null --write-out '%{http_code}' "$gateway_url/api/kitchen/tickets")"
[[ "$anonymous_status" == "401" ]]

echo "End-to-end workflow passed: approved=$approved_id declined=$declined_id"
