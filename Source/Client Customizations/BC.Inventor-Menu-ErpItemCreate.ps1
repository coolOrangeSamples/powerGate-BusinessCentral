#==============================================================================#
# (c) 2022 coolOrange s.r.l.                                                   #
#                                                                              #
# THIS SCRIPT/CODE IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER    #
# EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES  #
# OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, OR NON-INFRINGEMENT.   #
#==============================================================================#

# To disable the Inventor Menu 'Create/Update ERP Item...', move this script to the %ProgramData%/coolorange/powerEvents/Events/Disabled directory

if ($processName -notin @('Inventor')) {
    return
}

Add-InventorMenuItem -Name "Create/Update`n$erpName Item..." -Action {
    $document = $inventor.ActiveDocument
    $itemNumber = $document.PropertySets.Item('Design Tracking Properties')['Part Number'].Value
    if ($itemNumber) { $itemNumber = $itemNumber.ToUpper() } #TODO: Business Central converts Item Number to upper case

    #region Pre-Checks
    if ($document.DocumentType -notin @([Inventor.DocumentTypeEnum]::kAssemblyDocumentObject, [Inventor.DocumentTypeEnum]::kPartDocumentObject)) {
        $message = "This function is available only on parts or assemblies!"
        $title = "powerGate - Document type not supported"
        $null = [Autodesk.DataManagement.Client.Framework.Forms.Library]::ShowMessage($message, $title, [Autodesk.DataManagement.Client.Framework.Forms.Currency.ButtonConfiguration]::OK)
        return
    }
    
    #TODO: In case the ERP system generates the number, we must allow to fetch the number from the ERP system before saving the file
    if ($document.FileSaveCounter -eq 0) {
        $message = "The current document is not saved! Please save it before creating an Item in $erpName."
        $title = "powerGate - Save Inventor Document"
        $null = [Autodesk.DataManagement.Client.Framework.Forms.Library]::ShowMessage($message, $title, [Autodesk.DataManagement.Client.Framework.Forms.Currency.ButtonConfiguration]::OK)
        return
    }

    if ($itemNumber.Contains("/") -or $itemNumber.Contains("%2F") -or $itemNumber.Contains("\") -or $itemNumber.Contains("%5C")) {
        $message = "The iProperty 'Part Number' contains unsupported slashes or backslashes!"
        $title = "powerGate - Part Number not valid"
        $null = [Autodesk.DataManagement.Client.Framework.Forms.Library]::ShowError($message, $title)
        return
	}
    
	if ($itemNumber.Length -gt 20) { #TODO: Business Central validate Item Number length
        $message = "The iProperty 'Part Number' is $($itemNumber.Length) characters long! A Part Number must not exceed 20 characters to create an Item in $erpName."
        $title = "powerGate - Part Number not valid"
        $null = [Autodesk.DataManagement.Client.Framework.Forms.Library]::ShowError($message, $title)
        return
	}
    #endregion

    $erpItem = Get-ERPObject -EntitySet $itemEntitySet -Keys @{ Number = $itemNumber }
    if (-not $erpItem) {
        # Create new ERP Item
        Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, Autodesk.DataManagement.Client.Framework.Forms
        $xamlFile = [xml](Get-Content "$PSScriptRoot\$filePrefix.ErpItemCreate.xaml")
        $window = [Windows.Markup.XamlReader]::Load( (New-Object System.Xml.XmlNodeReader $xamlFile) )
        ApplyVaultTheme $window
        $window.Title = "powerGate - Create new $erpName Item"
        $window.FindName('Title').Content = "Create new $erpName Item with number '$itemNumber'"
        $window.FindName('UnitOfMeasureCombobox').ItemsSource = (GetPowerGateConfiguration 'UnitOfMeasures')
        
        $window.FindName('ButtonCreate').Add_Click({
            $erpItem = $window.FindName('ItemData').DataContext
            
            #TODO: Validate the ERP item before creating it
            
            Add-ErpObject -EntitySet $itemEntitySet -Properties $erpItem
            if ($? -eq $false) { return }
            $window.Close()
        })

        $erpItem = GetErpItemFromInventorDocument $document
        $window.FindName('ItemData').DataContext = $erpItem

        $null = $window.ShowDialog()
    }
    else {
        # Update ERP Item
        Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, Autodesk.DataManagement.Client.Framework.Forms
		$xamlFile = [xml](Get-Content "$PSScriptRoot\$filePrefix.ErpItemUpdate.xaml")
		$window = [Windows.Markup.XamlReader]::Load( (New-Object System.Xml.XmlNodeReader $xamlFile) )
        ApplyVaultTheme $window
        $window.Title = "powerGate - Update $erpName Item"
        $window.FindName('Title').Content = "Update $erpName Item '$itemNumber'"

        $newErpItem = GetErpItemFromInventorDocument $document
		$dataGridRows = @()
		$properties = $newErpItem | Get-Member -MemberType Properties | Select-Object Name
		foreach ($property in $properties) {
			if ($property.Name -eq "_Keys" -or $property.Name -eq "_Properties") { continue }
			if (@("Price", "Stock", "MakeBuy", "Supplier", "Weight", "Thumbnail", "ThinClientLink", "ThickClientLink") -contains $property.Name) { continue }

			$currentValue = $erpItem.$($property.Name)
			$newValue = $newErpItem.$($property.Name)
			if ($property.Name -eq "Thumbnail") { $currentValue = $newValue = "[Thumbnail]" }

			#TODO: Validate the ERP item field and disable the transfer button in case of errors
			
			$dataGridRows += New-Object PSObject -Property @{
				PropertyName = $property.Name
				CurrentValue = $currentValue
				NewValue     = $newValue
				State        = if ([string]$erpItem.$($property.Name) -eq [string]$newErpItem.$($property.Name)) { "=" } else { "!" }
				StateIcon    = if ([string]$erpItem.$($property.Name) -eq [string]$newErpItem.$($property.Name)) { 'pack://application:,,,/powerGate.UI;component/Resources/status_identical.png' } else { 'pack://application:,,,/powerGate.UI;component/Resources/status_different.png' }
			}
		}
		$itemsSource = [System.Collections.ObjectModel.ObservableCollection[PsObject]]::new()
		foreach ($dataGridRow in $dataGridRows) { $itemsSource.Add($dataGridRow) }
		$window.FindName('DataGrid').ItemsSource = $itemsSource

		$window.FindName('ButtonUpdate').Add_Click({
			Update-ErpObject -EntitySet $itemEntitySet -Keys $newErpItem._Keys -Properties $newErpItem._Properties
			if ($? -eq $false) { return }
			$window.DialogResult = $true
			$window.Close()
		})

        $null = $window.ShowDialog()
    }
}