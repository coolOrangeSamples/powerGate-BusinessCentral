#==============================================================================#
# (c) 2022 coolOrange s.r.l.                                                   #
#                                                                              #
# THIS SCRIPT/CODE IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER    #
# EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES  #
# OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, OR NON-INFRINGEMENT.   #
#==============================================================================#

if ($processName -notin @('Connectivity.VaultPro', 'Inventor')) {
    return
}

#Remark: The functions are going to be replaced by the new powerGate Configuration Manager
#https://youtrack.coolorange.com/youtrack/issue/PG-1391/Configurable-ERP-Item-Creation-from-Vault-Files-Items-via-Field-Mappings

#region Vault Item mappings
function GetErpItemFromVaultItem($vaultItem) {
    $erpItem = New-ErpObject -EntityType 'Item'
    
    #TODO: Map the Vault Item properties to the ERP Item properties
    $erpItem.Number = $vaultItem._Number.ToUpper()
    $erpItem.Title = $vaultItem.'_Title(Item,CO)'
    $erpItem.Description = $vaultItem.'_Description(Item,CO)'
    $erpItem.UnitOfMeasure = (GetPowerGateConfiguration 'UnitOfMeasures')  | Where-Object { $_.Value -eq $vaultItem._Units } | Select-Object -ExpandProperty Key
    $erpItem.Material = $vaultItem._Material
    $erpItem.Weight = [double](($vaultItem.Mass -replace "[^\d*\.\,?\d*$/]", '') -replace ",", ".")
    if ($vaultItem._Thumbnail -and $vaultItem._Thumbnail.Image) {
        #$erpItem.Thumbnail = [System.Convert]::ToBase64String($vaultItem._Thumbnail.Image)
        $erpItem.Thumbnail = $vaultItem._Thumbnail.Image
    }
    $erpItem.ThinClientLink = $vaultItem.ThinClientHyperLink #GetVaultThinClientLink $vaultItem
    $erpItem.ThickClientLink = $vaultItem.ThickClientHyperLink #GetVaultThickClientLink $vaultItem
    
    return $erpItem
}

function GetErpBomHeaderFromVaultItem($vaultBom) {
    $erpBom = New-ERPObject -EntityType 'BomHeader'

    #TODO: Map the Vault Item properties to the ERP BomHeader properties
    $erpBom.Number = $vaultBom._Number.ToUpper()
    $erpBom.Children = @()
    
    return $erpBom
}

function GetErpBomRowFromVaultItem($vaultBom, $vaultBomRow) {
    $erpBomRow = New-ERPObject -EntityType 'BomRow'

    #TODO: Map the Vault Item properties to the ERP BomRow properties
    $erpBomRow.ParentNumber = $vaultBom._Number.ToUpper()
    $erpBomRow.Position = [int]$vaultBomRow.Bom_PositionNumber
    $erpBomRow.ChildNumber = $vaultBomRow._Number.ToUpper()
    $erpBomRow.Quantity = [double]$vaultBomRow.Bom_Quantity
    $erpBomRow.IsRawMaterial = ($vaultBomRow._ItemClass -eq "rawMaterial")

    return $erpBomRow
}
#endregion
