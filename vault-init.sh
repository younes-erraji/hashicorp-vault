#!/bin/sh
# Seeds the dev Vault with a demo secret once the server is reachable.
set -e

export VAULT_ADDR="http://vault:8200"
export VAULT_TOKEN="${VAULT_DEV_ROOT_TOKEN_ID:-root}"

echo "Waiting for Vault to become available at $VAULT_ADDR ..."
until vault status >/dev/null 2>&1; do
  sleep 1
done

echo "Vault is up. Writing demo secret to secret/myapp/config ..."
# The 'secret/' KV v2 engine is mounted by default in dev mode.
#
# IMPORTANT: the Vault CLI ('vault kv put') forcibly converts EVERY value to a string
# before writing (documented behavior). That is fine for plain text/scalar values, but it
# turns nested objects/arrays (Smtp, CorsOrigins) into literal JSON text — so they can
# never bind as a config section. To handle BOTH cases correctly we write through the
# HTTP API, which preserves the JSON types exactly as given: strings stay strings, numbers
# stay numbers, objects stay objects, arrays stay arrays.
#
# The KV v2 API wraps the payload in a {"data": {...}} envelope, posted to
# /v1/<mount>/data/<path>.

PAYLOAD='{
  "data": {
    "ApiKey": "super-secret-api-key",
    "DbPassword": "P@ssw0rd-from-vault",
    "Environment": "docker-dev",
    "ConnectionStrings": {
      "Default": "Server=db;Database=MyApp;User Id=sa;Password=P@ssw0rd-from-vault;TrustServerCertificate=True"
    },
    "Smtp": {
      "Host": "mail.example.com",
      "Port": 587,
      "UseSsl": true
    },
    "CorsOrigins": ["a.com", "b.com"]
  }
}'

VAULT_API="${VAULT_ADDR}/v1/secret/data/myapp/config"

# Use whichever HTTP client the image provides (curl or wget).
if command -v curl >/dev/null 2>&1; then
  echo "$PAYLOAD" | curl -sf \
    --header "X-Vault-Token: ${VAULT_TOKEN}" \
    --header "Content-Type: application/json" \
    --request POST \
    --data @- \
    "$VAULT_API" >/dev/null
else
  wget -q -O /dev/null \
    --header="X-Vault-Token: ${VAULT_TOKEN}" \
    --header="Content-Type: application/json" \
    --post-data="$PAYLOAD" \
    "$VAULT_API"
fi

echo "Done. Current value (Smtp/CorsOrigins are real nested types, not strings):"
vault kv get -format=json secret/myapp/config
