# create account
az batch account create \
    --name mybatchaccount \
    --storage-account mystorageaccount \
    --resource-group myResourceGroup \
    --location eastus2

# login
az batch account login \
    --name mybatchaccount \
    --resource-group myResourceGroup \
    --shared-key-auth

# create a pool of nodes 
az batch pool create \
    --id mypool --vm-size Standard_A1_v2 \
    --target-dedicated-nodes 2 \
    --image canonical:ubuntuserver:16.04-LTS \
    --node-agent-sku-id "batch.node.ubuntu 16.04"

# show pool info 
az batch pool show --pool-id mypool \
    --query "allocationState"

# create a batch job 
az batch job create \
    --id myjob \
    --pool-id mypool

# add tasks to the job
for i in {1..4}
do
   az batch task create \
    --task-id mytask$i \
    --job-id myjob \
    --command-line "/bin/bash -c 'printenv | grep AZ_BATCH; sleep 90s'"
done

# get task status 
az batch task show \
    --job-id myjob \
    --task-id mytask1


# list task output
az batch task file list \
    --job-id myjob \
    --task-id mytask1 \
    --output table

# download output files
az batch task file download \
    --job-id myjob \
    --task-id mytask1 \
    --file-path stdout.txt \
    --destination ./stdout.txt

# delete the node pool
az batch pool delete --pool-id mypool