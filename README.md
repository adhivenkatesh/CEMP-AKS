# CEMP - Cloud Employee Management Platform (AKS)

## Structure
- `src/` - .NET 8 API
- `k8s/aks-new/` - AKS Production Deployment (USE THIS)
- `helm/acs-old/` - Archived ACS Helm Chart (DO NOT DEPLOY)

## Deploy AKS NEW
```bash
kubectl apply -f k8s/aks-new/