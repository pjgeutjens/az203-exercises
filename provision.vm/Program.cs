using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using static System.Console;

namespace az203.provision.vm
{
    class Program
    {
        static void Main(string[] args)
        {
            var settingsFile = "C:\\Users\\Beheerder\\source\\repos\\az203\\provision.vm\\azureauth.properties";
            WriteLine($"Creating Credentials from {settingsFile}...");
            var credentials =
                SdkContext.AzureCredentialsFactory.FromFile(settingsFile);
            WriteLine(credentials.ToString());
            var azure = Azure.Configure().WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithDefaultSubscription();

            var groupName = "az203.iaas.code";
            var vmName = "az203ws12r2code";
            var location = Region.EuropeWest;

            WriteLine("Creating Resource Group..");
            var resourceGroup = azure.ResourceGroups
                .Define(groupName)
                .WithRegion(location)
                .Create();

            WriteLine("Creating Availablity Set..");
            var availabilitySet = azure.AvailabilitySets
                .Define("az203.as")
                .WithRegion(location)
                .WithExistingResourceGroup(resourceGroup)
                .WithSku(AvailabilitySetSkuTypes.Aligned).Create();

            WriteLine("Creating Public IP..");
            var publicIpAddress = azure.PublicIPAddresses
                .Define($"{vmName}.pip.1")
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithDynamicIP().Create();

            WriteLine("Creating VNet..");
            var vnet = azure.Networks.Define("az203.vnet")
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithAddressSpace("10.0.0.0/16")
                .WithSubnet("az203.sn.1", "10.0.0.0/24")
                .Create();

            WriteLine("Creating NIC..");
            var nic = azure.NetworkInterfaces.Define($"{vmName}.nic.1")
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithExistingPrimaryNetwork(vnet)
                .WithSubnet("az203.sn.1")
                .WithPrimaryPrivateIPAddressDynamic()
                .WithExistingPrimaryPublicIPAddress(publicIpAddress)
                .Create();

            WriteLine("Creating Virtual Machine..");
            azure.VirtualMachines.Define(vmName)
                .WithRegion(location)
                .WithExistingResourceGroup(groupName)
                .WithExistingPrimaryNetworkInterface(nic)
                .WithLatestWindowsImage("MicrosoftWindowsServer", "WindowsServer", "2012-R2-Datacenter")
                .WithAdminUsername("demoadmin")
                .WithAdminPassword("Azure12345678!@#")
                .WithComputerName(vmName)
                .WithExistingAvailabilitySet(availabilitySet)
                .WithSize(VirtualMachineSizeTypes.StandardB1s)
                .Create();

            // if you want a managed disk..

            //            var managedDisk = azure.Disks.Define("myosdisk")
            //                .WithRegion(location)
            //                .WithExistingResourceGroup(groupName)
            //                .WithWindowsFromVhd("https://mystorage.blob.core.windows.net/vhds/myosdisk.vhd")
            //                .WithSizeInGB(128)
            //                .WithSku(DiskSkuTypes.PremiumLRS)
            //                .Create();
            //
            //            azure.VirtualMachines.Define(vmName)
            //                .WithRegion(location)
            //                .WithExistingResourceGroup(groupName)
            //                .WithExistingPrimaryNetworkInterface(nic)
            //                .WithSpecializedOSDisk(managedDisk, OperatingSystemTypes.Windows)
            //                .WithExistingAvailabilitySet(availabilitySet)
            //                .WithSize(VirtualMachineSizeTypes.StandardDS1)
            //                .Create();


            var vm = azure.VirtualMachines.GetByResourceGroup(groupName, vmName);

            WriteLine("Getting information about the virtual machine...");
            WriteLine("hardwareProfile");
            WriteLine("   vmSize: " + vm.Size);
            WriteLine("storageProfile");
            WriteLine("  imageReference");
            WriteLine("    publisher: " + vm.StorageProfile.ImageReference.Publisher);
            WriteLine("    offer: " + vm.StorageProfile.ImageReference.Offer);
            WriteLine("    sku: " + vm.StorageProfile.ImageReference.Sku);
            WriteLine("    version: " + vm.StorageProfile.ImageReference.Version);
            WriteLine("  osDisk");
            WriteLine("    osType: " + vm.StorageProfile.OsDisk.OsType);
            WriteLine("    name: " + vm.StorageProfile.OsDisk.Name);
            WriteLine("    createOption: " + vm.StorageProfile.OsDisk.CreateOption);
            WriteLine("    caching: " + vm.StorageProfile.OsDisk.Caching);
            WriteLine("osProfile");
            WriteLine("  computerName: " + vm.OSProfile.ComputerName);
            WriteLine("  adminUsername: " + vm.OSProfile.AdminUsername);
            WriteLine("  provisionVMAgent: " + vm.OSProfile.WindowsConfiguration.ProvisionVMAgent.Value);
            WriteLine("  enableAutomaticUpdates: " + vm.OSProfile.WindowsConfiguration.EnableAutomaticUpdates.Value);
            WriteLine("networkProfile");
            foreach (string nicId in vm.NetworkInterfaceIds)
            {
                WriteLine("  networkInterface id: " + nicId);
            }
            WriteLine("vmAgent");
            WriteLine("  vmAgentVersion" + vm.InstanceView.VmAgent.VmAgentVersion);
            WriteLine("    statuses");
            foreach (InstanceViewStatus stat in vm.InstanceView.VmAgent.Statuses)
            {
                WriteLine("    code: " + stat.Code);
                WriteLine("    level: " + stat.Level);
                WriteLine("    displayStatus: " + stat.DisplayStatus);
                WriteLine("    message: " + stat.Message);
                WriteLine("    time: " + stat.Time);
            }
            WriteLine("disks");
            foreach (DiskInstanceView disk in vm.InstanceView.Disks)
            {
                WriteLine("  name: " + disk.Name);
                WriteLine("  statuses");
                foreach (InstanceViewStatus stat in disk.Statuses)
                {
                    WriteLine("    code: " + stat.Code);
                    WriteLine("    level: " + stat.Level);
                    WriteLine("    displayStatus: " + stat.DisplayStatus);
                    WriteLine("    time: " + stat.Time);
                }
            }
            WriteLine("VM general status");
            WriteLine("  provisioningStatus: " + vm.ProvisioningState);
            WriteLine("  id: " + vm.Id);
            WriteLine("  name: " + vm.Name);
            WriteLine("  type: " + vm.Type);
            WriteLine("  location: " + vm.Region);
            WriteLine("VM instance status");
            foreach (InstanceViewStatus stat in vm.InstanceView.Statuses)
            {
                WriteLine("  code: " + stat.Code);
                WriteLine("  level: " + stat.Level);
                WriteLine("  displayStatus: " + stat.DisplayStatus);
            }
            WriteLine("Press enter to continue...");
            ReadLine();
        }
    }
}

           