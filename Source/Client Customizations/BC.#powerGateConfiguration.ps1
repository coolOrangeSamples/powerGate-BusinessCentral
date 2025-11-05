#==============================================================================#
# (c) 2022 coolOrange s.r.l.                                                   #
#                                                                              #
# THIS SCRIPT/CODE IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER    #
# EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES  #
# OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, OR NON-INFRINGEMENT.   #
#==============================================================================#

# To disable the ToolsMenu items for showing the configuration, move this script to the %ProgramData%/coolorange/powerEvents/Events/Disabled directory

if ($processName -notin @('Connectivity.VaultPro', 'Inventor')) {
    return
}

#Remark: The configuration functions are going to be replaced by the new powerGate Configuration Manager
#https://youtrack.coolorange.com/youtrack/issue/PG-1391/Configurable-ERP-Item-Creation-from-Vault-Files-Items-via-Field-Mappings

$powerGateErpTabconfiguration = "$PSScriptRoot\$filePrefix.#powerGateConfiguration.xml"

Add-VaultMenuItem -Location ToolsMenu -Name "powerGate - Open $erpName Configuration..." -Action {

    if(Test-Path $powerGateErpTabconfiguration) {
        Start-Process -FilePath explorer.exe -ArgumentList "/select, ""$powerGateErpTabconfiguration"""
    }
}

function GetPowerGateConfiguration($section) {
    Write-Host "Retrieving configuration for section: $section"

    if(-not (Test-Path $powerGateErpTabconfiguration)) {
        Write-Host "Configuration could not be retrieved from: '$powerGateErpTabconfiguration'"
        return
    }

    $configuration = [xml](Get-Content $powerGateErpTabconfiguration) 
    if ($null -eq $configuration -or $configuration.HasChildNodes -eq $false) {
        return
    }
    $configEntries = Select-Xml -xml $configuration -XPath "//$section"
    return @($configEntries.Node.ChildNodes | Sort-Object -Property value)
}