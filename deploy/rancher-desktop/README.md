# SiNS on Rancher Desktop (k3s)

This directory contains a **local Kubernetes** stack used to run and test SiNS on **Rancher Desktop** (or any cluster where you use the same manifests).

## Prerequisites

- [Rancher Desktop](https://rancherdesktop.io/) (or k3s/k8s) with **kubectl** working
- **nerdctl** in `PATH` (Rancher Desktop ships it, e.g. `~/.rd/bin/nerdctl`) — builds load images into the **`k8s.io`** namespace so the cluster can run `image: sins-rancher:local` with `imagePullPolicy: Never`

## One-shot deploy

From the **repository root**:

```bash
bash deploy/rancher-desktop/deploy.sh
```

The script:

1. Uses context **`${SINS_KUBE_CONTEXT:-rancher-desktop}`**
2. Builds **`sins-rancher:local`** with `nerdctl -n k8s.io build -f deploy/rancher-desktop/Dockerfile`
3. Applies **`deploy/rancher-desktop/k8s/*.yaml`**
4. Waits for **postgres**, **keycloak**, and **sins** rollouts

If **Keycloak** is scaled to `0` replicas, `kubectl rollout status deployment/keycloak` may not finish; scale Keycloak up or apply only the manifests you need and **`kubectl rollout restart deployment/sins -n sins-rd`** after a local image build.

## Ports (verify on your cluster)

Manifests in `k8s/` define NodePorts for HTTP and DNS. **Values can change** — always read the live Service:

```bash
kubectl get svc sins -n sins-rd -o wide
```

The in-repo `06-sins-service-nodeport.yaml` maps **80 → 30080** and **53 → 30053** (TCP and UDP for DNS).

## DNS testing

### From inside the cluster (recommended)

Uses the Service **ClusterIP** on port **53** (no NodePort / host UDP issues):

```bash
SVC=$(kubectl get svc sins -n sins-rd -o jsonpath='{.spec.clusterIP}')
kubectl run -n sins-rd dns-test --rm -i --restart=Never --image=alpine:3.20 -- \
  sh -c "apk add -q bind-tools && dig @${SVC} +tcp +dnssec example.com DNSKEY"
```

### From the Mac host via NodePort

Prefer **TCP** first (`+tcp`); UDP to `127.0.0.1:NodePort` is often flaky on macOS:

```bash
dig @127.0.0.1 -p 30053 +tcp +dnssec example.com DNSKEY
```

Use the port shown by `kubectl get svc` if it differs from `30053`.

## After code changes

1. **Rebuild** the image: `nerdctl -n k8s.io build -f deploy/rancher-desktop/Dockerfile -t sins-rancher:local .` (from repo root), or run `deploy.sh` which does this.
2. **Restart** SiNS so pods pick up the new image: `kubectl rollout restart deployment/sins -n sins-rd` and `kubectl rollout status deployment/sins -n sins-rd`.

## DNSSEC

See **[DNSSEC documentation](../../docs/dnssec.md)** for API, DS publication, and verification notes.
