az aks create --resource-group myResourceGroup --name myAKSCluster --node-count 1 --enable-addons monitoring --generate-ssh-keys

az aks install-cli

az aks get-credentials --resource-group myResourceGroup --name myAKSCluster

kubectl get nodes

kubectl apply -f manifest.yml

kubectl get service azure-vote-front --watch

kubectl scale --replicas=5 deployment/azure-vote-front

# creating an azure container registry and pushing an image

az acr create --resource-group myResourceGroup --name <acrName> --sku Basic

az acr login --name <acrName>

az acr list --resource-group myResourceGroup --query "[].{acrLoginServer:loginServer}" --output table

docker tag azure-vote-front <acrLoginServer>/azure-vote-front:v1

docker push <acrLoginServer>/azure-vote-front:v1

az acr repository list --name <acrName> --output table