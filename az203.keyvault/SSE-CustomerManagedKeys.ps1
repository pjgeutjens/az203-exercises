Connect-AzAccount

$resourceGroup = "myResourceGroup"
$storageAccountName = "myStorageAccount"
$keyVaultName = "myKeyVault"
$keyName = "myVaultKey"
$location = "West-Europe"

# assign a Managed Identity to the storage account
$storageAccount = Set-AzStorageAccount -ResourceGroupName $resourceGroup -Name $storageAccountName -AssignIdentity

# create a new KeyVault
$keyVault = New-AzKeyVault -Name $keyVaultName `
    -ResourceGroupName $resourceGroup `
    -Location $location `
    -EnableSoftDelete `
    -EnablePurgeProtection

# configure KeyVault Access Policy
Set-AzKeyVaultAccessPolicy `
    -VaultName $keyVault.VaultName `
    -ObjectId $storageAccount.Identity.PrincipalId `
    -PermissionsToKeys wrapkey,unwrapkey,get,recover

# create a new key
$key = Add-AzKeyVaultKey -VaultName $keyVault.VaultName -Name $keyName -Destination 'Software'

# configure Storage Account encryption using the key
Set-AzStorageAccount -ResourceGroupName $storageAccount.ResourceGroupName `
    -AccountName $storageAccount.StorageAccountName `
    -KeyvaultEncryption `
    -KeyName $key.Name `
    -KeyVersion $key.Version `
    -KeyVaultUri $keyVault.VaultUri

# update the storage key by getting a new key and setting the Storage Account key
$key = Get-AzKeyVaultKey -VaultName $keyVaultName -KeyName $keyName

Set-AzStorageAccount -ResourceGroupName $storageAccount.ResourceGroupName `
    -AccountName $storageAccount.StorageAccountName `
    -KeyvaultEncryption `
    -KeyName $key.Name `
    -KeyVersion $key.Version `
    -KeyVaultUri $keyVault.VaultUri
