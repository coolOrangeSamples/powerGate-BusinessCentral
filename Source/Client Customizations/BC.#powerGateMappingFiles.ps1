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

#region Inventor mappings
function GetErpItemFromInventorDocument($document) {
    $erpItem = New-ErpObject -EntityType 'Item'

    # TODO: Map the Inventor iProperties to the ERP Item properties
    $erpItem.Number = $document.PropertySets.Item('Design Tracking Properties')['Part Number'].Value
    $erpItem.Title = $document.PropertySets.Item('Inventor Summary Information')['Title'].Value
    $erpItem.Description = $document.PropertySets.Item('Design Tracking Properties')['Description'].Value
    $erpItem.UnitOfMeasure = (GetPowerGateConfiguration 'UnitOfMeasures') | Where-Object { $_.Value -eq 'Each' } | Select-Object -ExpandProperty Key
    $erpItem.Material = $document.PropertySets.Item('Design Tracking Properties')['Material'].Value
    
    return $erpItem
}
#endregion

#region Vault File mappings
function GetErpItemFromVaultFile($vaultFile) {
    $erpItem = New-ErpObject -EntityType 'Item'

    #TODO: Map the Vault File properties to the ERP Item properties
    $erpItem.Number = $vaultFile._PartNumber.ToUpper()
    $erpItem.Title = $vaultFile._Title
    $erpItem.Description = $vaultFile._Description
    $erpItem.UnitOfMeasure = (GetPowerGateConfiguration 'UnitOfMeasures')  | Where-Object { $_.Value -eq "Each" } | Select-Object -ExpandProperty Key
    $erpItem.Material = $vaultFile._Material
    $erpItem.Weight = [double](($vaultFile.Mass -replace "[^\d*\.\,?\d*$/]", '') -replace ",", ".")
    if ($vaultFile._Thumbnail -and $vaultFile._Thumbnail.Image) {
        #$erpItem.Thumbnail = [System.Convert]::ToBase64String($vaultFile._Thumbnail.Image) 
        $erpItem.Thumbnail = $vaultFile._Thumbnail.Image
    }
    $erpItem.ThinClientLink = $vaultFile.ThinClientHyperLink #GetVaultThinClientLink $vaultFile
    $erpItem.ThickClientLink = $vaultFile.ThickClientHyperLink #GetVaultThickClientLink $vaultFile

    return $erpItem
}

function GetErpBomHeaderFromVaultFile($vaultBom) {
    $erpBom = New-ERPObject -EntityType 'BomHeader'

    #TODO: Map the Vault File properties to the ERP BomHeader properties
    $erpBom.Number = $vaultBom._PartNumber.ToUpper()
    $erpBom.Children = @()

    return $erpBom
}

function GetErpBomRowFromVaultFile($vaultBom, $vaultBomRow) {
    $erpBomRow = New-ERPObject -EntityType 'BomRow'

    #TODO: Map the Vault File properties to the ERP BomRow properties
    $erpBomRow.ParentNumber = $vaultBom._PartNumber.ToUpper()
    $erpBomRow.Position = [int]$vaultBomRow.Bom_PositionNumber
    $erpBomRow.ChildNumber = $vaultBomRow._PartNumber.ToUpper()
    $erpBomRow.Quantity = [double]$vaultBomRow.Bom_Quantity
    $erpBomRow.IsRawMaterial = ($vaultBomRow._FileClass -eq "rawMaterial")

    return $erpBomRow
}
#endregion
