#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

CTX="${SINS_KUBE_CONTEXT:-rancher-desktop}"
IMAGE_TAG="${SINS_IMAGE_TAG:-sins-rancher:local}"
DOCKERFILE="deploy/rancher-desktop/Dockerfile"

echo "==> Using kubectl context: ${CTX}"
kubectl config use-context "${CTX}"

build_image() {
  if command -v nerdctl >/dev/null 2>&1; then
    echo "==> Building ${IMAGE_TAG} with nerdctl (k8s.io namespace — visible to Kubernetes)"
    nerdctl -n k8s.io build -f "${DOCKERFILE}" -t "${IMAGE_TAG}" .
    return
  fi
  if command -v docker >/dev/null 2>&1; then
    echo "==> Building ${IMAGE_TAG} with docker"
    docker build -f "${DOCKERFILE}" -t "${IMAGE_TAG}" .
    echo "WARN: nerdctl was not used for the build. If the sins pod is ImagePullBackOff, load the image into k8s:"
    echo "      docker save ${IMAGE_TAG} | nerdctl -n k8s.io load"
    return
  fi
  echo "ERROR: Neither nerdctl nor docker is available in PATH."
  exit 1
}

build_image

echo "==> Applying Kubernetes manifests (namespace sins-rd, Postgres, sins, NodePorts 30080/30053)"
kubectl apply -f deploy/rancher-desktop/k8s/

echo "==> Waiting for postgres Deployment..."
kubectl rollout status deployment/postgres -n sins-rd --timeout=180s

echo "==> Waiting for keycloak Deployment..."
kubectl rollout status deployment/keycloak -n sins-rd --timeout=300s

echo "==> Waiting for sins Deployment..."
kubectl rollout status deployment/sins -n sins-rd --timeout=300s

NODE_IP="$(kubectl get nodes -o jsonpath='{.items[0].status.addresses[?(@.type=="InternalIP")].address}' 2>/dev/null || true)"
if test -z "${NODE_IP}"; then
  NODE_IP="127.0.0.1"
fi

echo ""
echo "========== Access (Rancher Desktop / k3s) =========="
echo "Web UI / API:  http://127.0.0.1:30080/   (or http://${NODE_IP}:30080/)"
echo "Keycloak:      http://127.0.0.1:30081/   (or http://${NODE_IP}:30081/)"
echo "Swagger (Dev): http://127.0.0.1:30080/swagger"
echo "DNS (UDP):     dig @127.0.0.1 -p 30053 example.com +short"
echo "               (or dig @${NODE_IP} -p 30053 ...)"
echo "DNS (TCP):     dig @127.0.0.1 -p 30053 example.com +tcp +short"
echo ""
echo "Default login (first DB init): admin / admin123"
echo "Keycloak admin (for setup):    admin / admin123"
echo "Postgres (in-cluster only): host postgres port 5432 db dns_server user postgres"
echo "====================================================="
