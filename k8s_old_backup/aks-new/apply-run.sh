#!/bin/bash
echo "=== CEMP Deployment ==="

# ---- OPTION 1: Manual way (Your original Day 00-08 - WORKS OFFLINE) ----
# kubectl apply -f ./k8s/aks-new/00-namespace.yaml --validate=false
# kubectl apply -f ./k8s/aks-new/01-configmap.yaml --validate=false
# ... (8 files)

# ---- OPTION 2: Kustomize way (NEW - Multi-env) ----
# For local test only - just generates YAML, doesn't need AKS
echo "Testing Kustomize DEV..."
kubectl kustomize ./k8s/overlays/dev | head -30

echo "Testing Kustomize TEST..."
kubectl kustomize ./k8s/overlays/test | head -30

echo "Testing Kustomize PROD..."
kubectl kustomize ./k8s/overlays/prod | head -30

# ---- OPTION 3: Actual deploy to AKS (when connected) ----
# kubectl apply -k ./k8s/overlays/dev --validate=false
# kubectl apply -k ./k8s/overlays/test --validate=false
# kubectl apply -k ./k8s/overlays/prod --validate=false

# kubectl get all -n cemp-dev
# kubectl get all -n cemp-test
# kubectl get all -n cemp-prod