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
vault kv put secret/myapp/config \
  "ApiKey=super-secret-api-key" \
  "DbPassword=P@ssw0rd-from-vault" \
  "Environment=docker-dev"

echo "Done. Current value:"
vault kv get secret/myapp/config
