# DNSSEC (authoritative)

SiNS supports **authoritative DNSSEC** for zones you configure in the database: **ECDSAP256SHA256 (algorithm 13)**, **NSEC** denial of existence, **RRSIG** on answers when the client sets **EDNS0 DO** (DNSSEC OK). **Recursive resolution is unchanged** (no AD-bit validation of upstream answers).

Full implementation lives under `sins/Services/Dnssec/` and in `DnsServer` integration.

## What is signed

- **Zone model**: Each **DNSSEC zone** has an **apex** (e.g. `example.net`). Names **equal to** or **under** that apex (`www.example.net`) are treated as inside the zone for **NXDOMAIN** / **NODATA** authoritative behaviour when DNSSEC is enabled.
- **Positive answers**: `A`, `AAAA`, `TXT`, `CNAME`, `NS`, `MX` (and Ingress synthetic **A** when configured), plus **RRSIG** when **DO** is set.
- **DNSKEY** at the apex: served from generated/imported keys; **RRSIG(DNSKEY)** signed with the **KSK**; data RRsets signed with the **ZSK**.
- **Without DO**: DNSKEY may still be returned **without** RRSIG/OPT (smaller answers); other types use unsigned authoritative responses as before.

## Admin API

All routes require authentication; mutating routes require **Admin** (except where noted).

| Method | Path | Description |
|--------|------|----------------|
| GET | `/api/dnssec/zones` | List zones (no PEM in list) |
| GET | `/api/dnssec/zones/{id}` | Zone metadata |
| POST | `/api/dnssec/zones` | Create zone + generate P-256 KSK/ZSK (`{ "apex": "example.net", "generateKeys": true }`) |
| PUT | `/api/dnssec/zones/{id}` | `{ "enabled": true \| false }` |
| DELETE | `/api/dnssec/zones/{id}` | Remove zone |
| GET | `/api/dnssec/zones/{id}/ds` | **Admin** — DS digest for parent registrar (`dsRecordLine`, `digestHex`, key tag) |
| GET | `/api/dnssec/zones/{id}/dnskeys` | **Admin** — SPKI PEM for KSK and ZSK public keys |

Publish the **DS** record at the **parent** zone (registrar or parent nameserver) so validating resolvers can anchor trust.

## Keys and security

- Keys are stored as **PKCS#8 PEM** in PostgreSQL. **Protect database access** in production; prefer external secret storage if you harden the deployment.
- After changing DNS records or zones, SiNS bumps internal invalidation so NSEC-related data is rebuilt on the next signed query.

## Verification

### Authoritative (your apex)

```bash
dig +norecurse @<dns-host> <apex> DNSKEY +dnssec
```

Expect **`aa`** (authoritative) in flags when the name is served authoritatively by SiNS.

### Forwarded names (sanity)

Compare SiNS to a public resolver — **sorted** `DNSKEY +short` output should match for the same zone:

```bash
dig @<sins> +tcp example.com DNSKEY +short | sort
dig @1.1.1.1 +tcp example.com DNSKEY +short | sort
```

### `delv` caveat

`delv @<sins> example.com DNSKEY` often reports **broken trust chain** when SiNS acts as a **stub/recursive** forwarder, because `delv` expects a full validation path from the root. That is **not** proof the DNSKEY RRset is wrong; use DS chain + validating parent or tools meant for authoritative zones.

## Rancher Desktop and NodePort

When SiNS runs on **Rancher Desktop** with the manifests in `deploy/rancher-desktop/k8s/`, the `sins` Service exposes DNS on **NodePort** (see `06-sins-service-nodeport.yaml`; default **30053** TCP/UDP in-repo — **always confirm** with `kubectl get svc sins -n sins-rd`).

- **macOS host → NodePort UDP** to `127.0.0.1` is often **unreliable**; prefer **`dig +tcp ... -p <nodePort>`** or query from **inside the cluster** (same Pod network as the Service ClusterIP on port **53**).
- After code changes, **rebuild the image and roll the Deployment** before claiming DNS behaviour is verified (see [Rancher Desktop README](../deploy/rancher-desktop/README.md)).

## Further reading

- [Rancher Desktop deployment](../deploy/rancher-desktop/README.md)
- [Installation — Kubernetes](installation.md#kubernetes-deployment)
- [API reference — DNSSEC endpoints](api-reference.md#dnssec-zone-endpoints)
