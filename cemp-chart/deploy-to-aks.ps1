## notepad deploy-to-aks.ps1

param([Parameter(Mandatory=$true)][ValidateSet("dev","test","prod","local")][string]$TargetEnv)
$Namespace = "cemp-$TargetEnv"
Write-Host "=== Deploying to $TargetEnv ($Namespace) ===" -ForegroundColor Cyan
if ($TargetEnv -eq "local") {
  docker build -t cemp-api:dev --file./docker/Dockerfile.
  kind load docker-image cemp-api:dev --name cemp-local
  kubectl apply -k k8s/overlays/local
  kubectl rollout restart deployment cemp-api -n $Namespace
} else {
  $FullTag = "adhivenkatesh7/cemp-api:v2-$TargetEnv"
  if ($TargetEnv -eq "dev") { $FullTag = "adhivenkatesh7/cemp-api:v2" }
  docker build -t $FullTag --file./docker/Dockerfile.
  docker push $FullTag
  kubectl apply -k k8s/overlays/$TargetEnv
  kubectl rollout restart deployment cemp-api -n $Namespace
}
kubectl rollout status deployment cemp-api -n $Namespace --timeout=120s
kubectl get pods -n $Namespace
