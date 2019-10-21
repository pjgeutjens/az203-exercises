az sql db create -g mygroup -s myserver -n mydb --service-objective S0

az storage blob generate-sas --account-name myAccountName -c myContainer -n myBacpac.bacpac \
    --permissions r --expiry 2018-01-01T00:00:00Z

az sql db import -s myserver -n mydatabase -g mygroup -p password -u login \
    --storage-key "?sr=b&sp=rw&se=2018-01-01T00%3A00%3A00Z&sig=mysignature&sv=2015-07-08" \
    --storage-key-type SharedAccessKey \
    --storage-uri https://myAccountName.blob.core.windows.net/myContainer/myBacpac.bacpac

az sql db import -s myserver -n mydatabase -g mygroup -p password -u login --storage-key MYKEY== \
    --storage-key-type StorageAccessKey \
    --storage-uri https://myAccountName.blob.core.windows.net/myContainer/myBacpac.bacpac