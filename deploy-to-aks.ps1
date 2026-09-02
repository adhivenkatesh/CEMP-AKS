param(
  [Parameter(Mandatory=$true)]
  [ValidateSet("dev","test","prod","local")]
  [string]$TargetEnv
)

$Namespace = "cemp-$TargetEnv"
Write-Host "=== Deploying to $TargetEnv ($Namespace) ===" -ForegroundColor Cyan

if ($TargetEnv -eq "local") {
  Write-Host "Building..." -ForegroundColor Yellow
  docker build -t cemp-api:dev -f./docker/Dockerfile .
  kind load docker-image cemp-api:dev --name cemp-local
  kubectl apply -k k8s/overlays/local
  kubectl rollout restart deployment cemp-api -n $Namespace
}
else {
  $FullTag = "adhivenkatesh7/cemp-api:v2-$TargetEnv"
  if ($TargetEnv -eq "dev") { $FullTag = "adhivenkatesh7/cemp-api:v2" }
  Write-Host "Building & Pushing $FullTag..." -ForegroundColor Yellow
  docker build -t $FullTag -f./docker/Dockerfile.
  docker push $FullTag
  kubectl apply -k k8s/overlays/$TargetEnv
  kubectl rollout restart deployment cemp-api -n $Namespace
}

kubectl rollout status deployment cemp-api -n $Namespace --timeout=120s
kubectl get pods -n $Namespace
Write-Host "=== DONE $TargetEnv ===" -ForegroundColor Green
# run this --> powershell -ExecutionPolicy Bypass -File./deploy-to-aks.ps1 -TargetEnv local