

k3d cluster create cemp-local --servers 1 --agents 1 --api-port 6550 --port "8080:80@loadbalancer" --port "32088:32088@loadbalancer"

docker build -t cemp-api:dev -f Employee.API/Dockerfile.
k3d image import cemp-api:dev -c cemp-local

kubectl apply -k./k8s/aks-new

kubectl get pods -n cemp-dev -w