#==============================================================================#
# (c) 2022 coolOrange s.r.l.                                                   #
#                                                                              #
# THIS SCRIPT/CODE IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER    #
# EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES  #
# OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, OR NON-INFRINGEMENT.   #
#==============================================================================#

# To disable the ERP Item tab for files, move this script to the %ProgramData%/coolorange/powerEvents/Events/Disabled directory

if ($processName -notin @('Connectivity.VaultPro')) {
	return
}

Add-VaultTab -Name "$erpName Item" -EntityType File -Action {
	param($selectedFile)
	$script:itemNumber = $selectedFile._PartNumber
	if ($script:itemNumber) { $script:itemNumber = $script:itemNumber.ToUpper() } #TODO: Business Central converts Item Number to upper case

	Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, Autodesk.DataManagement.Client.Framework.Forms
	$xamlFile = [xml](Get-Content "$PSScriptRoot\$filePrefix.Vault-Tab-ErpItem.xaml")
	$tab_control = [Windows.Markup.XamlReader]::Load( (New-Object System.Xml.XmlNodeReader $xamlFile) )
	ApplyVaultTheme $tab_control

	#region Link ERP button
	$tab_control.FindName('ButtonLinkErpItem').Add_Click({
		if (LinkErpItemVaultFile $selectedFile) {
			[System.Windows.Forms.SendKeys]::SendWait('{F5}')
		}
	}.GetNewClosure())

	try {
		$file = $vault.DocumentService.GetLatestFileByMasterId($selectedFile.MasterId)
	}
	catch {
		$file = $null
	}
	$file = $vault.DocumentService.GetLatestFileByMasterId($selectedFile.MasterId)
	
	if ($null -eq $file -or $file.Locked) {
		$tab_control.FindName('ButtonLinkErpItem').IsEnabled = $false
		$tab_control.FindName('ButtonLinkErpItem').ToolTip = "The Vault item is locked and cannot be linked to an ERP item!"
	}
	#endregion

	#region Pre-Checks
	$tab_control.FindName('ButtonErpItem').Visibility = "Collapsed"
	
	if ($selectedFile._Extension -notin @('ipt', 'iam')) {
		$tab_control.FindName('StatusIcon').Source = 'pack://application:,,,/powerGate.UI;component/Resources/status_error.png'
		$tab_control.FindName('Title').Content = "Please select a part or an assembly!"
		return $tab_control
	}

	if ($null -eq $itemNumber -or $itemNumber -eq "") {
		$tab_control.FindName('StatusIcon').Source = 'pack://application:,,,/powerGate.UI;component/Resources/status_error.png'
		$tab_control.FindName('Title').Content = "The Part Number is empty! A Part Number is required to create an Item in $erpName."
		return $tab_control
	}

	if ($itemNumber.Contains("/") -or $itemNumber.Contains("%2F") -or $itemNumber.Contains("\") -or $itemNumber.Contains("%5C")) {
		$tab_control.FindName('StatusIcon').Source = 'pack://application:,,,/powerGate.UI;component/Resources/status_error.png'
		$tab_control.FindName('Title').Content = "The Part Number contains unsupported slashes or backslashes!"
		return $tab_control
	}

	#TODO: Business Central validate Item Number length
	if ($itemNumber.Length -gt 20) {
		$tab_control.FindName('StatusIcon').Source = 'pack://application:,,,/powerGate.UI;component/Resources/status_error.png'
		$tab_control.FindName('Title').Content = "The Part Number is $($itemNumber.Length) characters long! It must not exceed 20 characters to create an Item in $erpName."
		return $tab_control
	}

	$tab_control.FindName('ButtonErpItem').Visibility = "Visible"
	#endregion

	$erpItem = Get-ERPObject -EntitySet $itemEntitySet -Keys @{ Number = $itemNumber } 
	if ($? -eq $false) { return }

	$tab_control.FindName('ButtonErpItem').IsEnabled = $true

	if (-not $erpItem) {
		$tab_control.FindName('StatusIcon').Source = 'pack://application:,,,/powerGate.UI;component/Resources/status_new.png'
		$tab_control.FindName('Title').Content = "$erpName Item '$itemNumber' does not exist. Would you like to create it?"
		$tab_control.FindName('ButtonErpItem').Content = "Create new ERP Item..."
		
		$tab_control.FindName('ButtonErpItem').Add_Click({
			if (CreateNewErpItemVaultFile $selectedFile) {
				[System.Windows.Forms.SendKeys]::SendWait('{F5}')
			}
		}.GetNewClosure())
	}
	else {
		$tab_control.FindName('StatusIcon').Source = 'pack://application:,,,/powerGate.UI;component/Resources/status_identical.png'
		$tab_control.FindName('Title').Content = "$erpName Item '$itemNumber' - '$($erpItem.Title)'"

		if ($erpItem.Blocked) { 
			$tab_control.FindName('Title').Content = $tab_control.FindName('Title').Content + " (blocked)"
			$tab_control.FindName('Title').Foreground = "Red"
			$tab_control.FindName('StatusIcon').Source = 'pack://application:,,,/powerGate.UI;component/Resources/status_error.png'
		}

		$tab_control.FindName('Thumbnail').Source = GetImageFromByteArray $erpItem.Thumbnail
		$tab_control.FindName('UnitOfMeasureCombobox').ItemsSource = (GetPowerGateConfiguration 'UnitOfMeasures')
		$tab_control.FindName('ItemData').DataContext = $erpItem
		$tab_control.FindName('ButtonErpItem').Content = "Update ERP Item..."
		
		$tab_control.FindName('ButtonErpItem').Add_Click({
			if (UpdateErpItemVaultFile -erpItem $erpItem -vaultFile $selectedFile) {
				[System.Windows.Forms.SendKeys]::SendWait('{F5}')
			}
		}.GetNewClosure())
	}

	return $tab_control
}

function LinkErpItemVaultFile($vaultFile) {
	Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, Autodesk.DataManagement.Client.Framework.Forms
	$xamlFile = [xml](Get-Content "$PSScriptRoot\$filePrefix.ErpItemLink.xaml")
	$window = [Windows.Markup.XamlReader]::Load( (New-Object System.Xml.XmlNodeReader $xamlFile) )
	ApplyVaultTheme $window
	$window.Title = "powerGate - Link $erpName Item"
	$window.FindName('Title').Content = "Search and link $erpName Item"
	$window.FindName('SearchResults').Tag = "Search $erpName Item"

	$searchCriteria = New-Object PsObject -Property @{"SearchTerm" = "" }
	$window.FindName('SearchCriteria').DataContext = $searchCriteria

	$window.FindName('ButtonSearch').Add_Click({
		$searchCriteria = $window.FindName('SearchCriteria').DataContext
		if ($null -ne $searchCriteria.SearchTerm -and $searchCriteria.SearchTerm -ne "") {
			$window.Cursor = [System.Windows.Input.Cursors]::Wait
			# TODO: Change the search fields if needed
			$results = Get-ERPObjects -EntitySet $itemEntitySet -Filter "(substringof('$($searchCriteria.SearchTerm.ToUpper())',toupper(Number)) eq true) or (substringof('$($searchCriteria.SearchTerm.ToUpper())',toupper(Title)) eq true) or (substringof('$($searchCriteria.SearchTerm.ToUpper())',toupper(Description)) eq true) or (substringof('$($searchCriteria.SearchTerm.ToUpper())',toupper(Material)) eq true)"
			$window.FindName('SearchResults').ItemsSource = $results
			$window.FindName('SearchResults').Tag = "No $erpName Item found for '$($searchCriteria.SearchTerm)'"
			$window.FindName('SearchTerm').Cursor = $null
			$window.Cursor = $null
		}
	})
	
	$window.FindName('SearchTerm').Add_KeyDown({
		if ($_.Key -eq "Enter") { 
			$window.FindName('SearchTerm').Cursor = [System.Windows.Input.Cursors]::Wait
			$window.FindName('ButtonSearch').RaiseEvent((New-Object System.Windows.RoutedEventArgs([System.Windows.Controls.Button]::ClickEvent)))
		}
	})

	$window.FindName('ButtonLink').Add_Click({
		$selectedElement = $window.FindName('SearchResults').SelectedItems[0]
		$number = $selectedElement.Number
		$message = "To link the $erpName Item '$number' with the file '$($vaultFile._Name)', the 'Part Number' property of the Vault file will be changed from '$($vaultFile._PartNumber)' to '$number'.`n`nAre you sure you want to proceed?"
    	$title = "powerGate - Confirm operation"
    	$answer = [Autodesk.DataManagement.Client.Framework.Forms.Library]::ShowWarning($message, $title, [Autodesk.DataManagement.Client.Framework.Forms.Currency.ButtonConfiguration]::YesNo)
		if ($answer -eq "Yes") {
			$updatedVaultFile = Update-VaultFile -File $vaultFile._FullPath -Properties @{"Part Number" = $number} -ErrorAction Stop
			#TODO: Write back other properties from ERP if needed. The following line is just an example
			#$updatedVaultFile = Update-VaultFile -File $vaultFile._FullPath -Properties @{"Part Number" = $number; "Title" = $selectedElement.Number; "Description" = $selectedElement.Description} -ErrorAction Stop
			
			if (-not $updatedVaultFile) {
				$null = [Autodesk.DataManagement.Client.Framework.Forms.Library]::ShowError("The file could not be updated", "powerGate - Update error")
			}

			$window.DialogResult = $true
			$window.Close()
		}
	})

	$null = $window.FindName('SearchTerm').Focus()
	return $window.ShowDialog()
}

function CreateNewErpItemVaultFile($vaultFile) {
	Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, Autodesk.DataManagement.Client.Framework.Forms
	$xamlFile = [xml](Get-Content "$PSScriptRoot\$filePrefix.ErpItemCreate.xaml")
	$window = [Windows.Markup.XamlReader]::Load( (New-Object System.Xml.XmlNodeReader $xamlFile) )
	ApplyVaultTheme $window
	$window.Title = "powerGate - Create new $erpName Item"
	$window.FindName('Title').Content = "Create new $erpName Item with number '$itemNumber'"
	$window.FindName('UnitOfMeasureCombobox').ItemsSource = (GetPowerGateConfiguration 'UnitOfMeasures')

	$erpItem = GetErpItemFromVaultFile $vaultFile
	$window.FindName('ItemData').DataContext = $erpItem

	$window.FindName('ButtonCreate').Add_Click({
		$newItem = $window.FindName('ItemData').DataContext

		#TODO: Validate the ERP item before creating it

		Add-ErpObject -EntitySet $itemEntitySet -Properties $newItem
		if ($? -eq $false) { return }

		$window.DialogResult = $true
		$window.Close()
	})

	return $window.ShowDialog()
}

function UpdateErpItemVaultFile($erpItem, $vaultFile) {
	Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, Autodesk.DataManagement.Client.Framework.Forms
	$xamlFile = [xml](Get-Content "$PSScriptRoot\$filePrefix.ErpItemUpdate.xaml")
	$window = [Windows.Markup.XamlReader]::Load( (New-Object System.Xml.XmlNodeReader $xamlFile) )
	ApplyVaultTheme $window
	$window.Title = "powerGate - Update $erpName Item"
	$window.FindName('Title').Content = "Update $erpName Item '$itemNumber'"

	$newErpItem = GetErpItemFromVaultFile $vaultFile
	$dataGridRows = @()
	$properties = $newErpItem | Get-Member -MemberType Properties | Select-Object Name
	foreach ($property in $properties) {
		if ($property.Name -eq "_Keys" -or $property.Name -eq "_Properties") { continue }
		if (@("Price", "Stock", "MakeBuy", "Blocked", "Supplier") -contains $property.Name) { continue }

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

	return $window.ShowDialog()
}