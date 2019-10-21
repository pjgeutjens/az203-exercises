az acr create --name <name> --resource-group <rg> --sku Standard --location <location>

# in a folder containing a Dockerfile 
az acr build --registry <registryname> --image <imagename> .

az acr task create --registry <container_registry_name> --name buildwebapp --image webimage --context https://github.com/MicrosoftDocs/mslearn-deploy-run-container-app-service.git --branch master --file Dockerfile --git-access-token <access_token>

