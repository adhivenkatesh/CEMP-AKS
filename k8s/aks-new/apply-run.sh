kubectl apply -f ./k8s/aks-new/00-namespace.yaml
kubectl create secret docker-registry acr-secret --docker-server=containerregistrycemp.azurecr.io --docker-username=... --docker-password=... -n cemp
kubectl apply -f ./k8s/aks-new/01-configmap.yaml
kubectl apply -f ./k8s/aks-new/02-secret.yaml
kubectl apply -f ./k8s/aks-new/03-deployment.yaml
kubectl apply -f ./k8s/aks-new/04-service.yaml
kubectl apply -f ./k8s/aks-new/05-ingress-aks.yaml
kubectl apply -f ./k8s/aks-new/07-sql-deployment.yaml
kubectl apply -f ./k8s/aks-new/08-sql-service.yaml
kubectl get pods -n cemp -w
kubectl logs -f deployment/mssql -n cemp

# 5. Test
kubectl get all -n cemp