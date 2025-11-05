#==============================================================================#
# (c) 2022 coolOrange s.r.l.                                                   #
#                                                                              #
# THIS SCRIPT/CODE IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER    #
# EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES  #
# OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, OR NON-INFRINGEMENT.   #
#==============================================================================#

# To disable the ERP BOM tab for files, move this script to the %ProgramData%/coolorange/powerEvents/Events/Disabled directory

if ($processName -notin @('Connectivity.VaultPro')) {
	return
}

Add-VaultTab -Name "$erpName BOM" -EntityType File -Action {
	param($selectedFile)
	$script:itemNumber = $selectedFile._PartNumber
	if ($script:itemNumber) { $script:itemNumber = $script:itemNumber.ToUpper() } #TODO: Business Central converts Item Number to upper case

	Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, Autodesk.DataManagement.Client.Framework.Forms
	$xamlFile = [xml](Get-Content "$PSScriptRoot\$filePrefix.Vault-Tab-ErpBom.xaml")
	$tab_control = [Windows.Markup.XamlReader]::Load( (New-Object System.Xml.XmlNodeReader $xamlFile) )
	ApplyVaultTheme $tab_control

	#region Pre-Checks
	$tab_control.FindName('ButtonTransferBom').Visibility = "Collapsed"

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
	
	if ($itemNumber.Length -gt 20) { #TODO: Business Central validate Part Number length
		$tab_control.FindName('StatusIcon').Source = 'pack://application:,,,/powerGate.UI;component/Resources/status_error.png'
		$tab_control.FindName('Title').Content = "The Part Number is $($itemNumber.Length) characters long! It must not exceed 20 characters to create an Item in $erpName."
		return $tab_control
	}

	$tab_control.FindName('ButtonTransferBom').Visibility = "Visible"
	#endregion

	$erpBom = Get-ERPObject -EntitySet $bomHeaderEntitySet -Keys @{ Number = $itemNumber } -Expand 'Children/Item'
	if ($? -eq $false) { return }

	$tab_control.FindName('ButtonTransferBom').IsEnabled = $true

	if (-not $erpBom) {
		$tab_control.FindName('StatusIcon').Source = 'pack://application:,,,/powerGate.UI;component/Resources/status_new.png'
		$tab_control.FindName('Title').Content = "$erpName BOM '$itemNumber' does not exist. Would you like to create it?"
		$tab_control.FindName('ButtonTransferBom').Content = "Create new ERP BOM..."
	}
	else {
		$tab_control.FindName('StatusIcon').Source = 'pack://application:,,,/powerGate.UI;component/Resources/status_identical.png'
		$tab_control.FindName('Title').Content = "$erpName BOM '$itemNumber'"
		$tab_control.FindName('ButtonTransferBom').Content = "Update ERP BOM..."
		$sortByColumn = New-Object System.ComponentModel.SortDescription 'Position','Ascending'
		$tab_control.FindName('BomRowsTable').Items.SortDescriptions.Add($sortByColumn)
		$tab_control.DataContext = $erpBom
	}

	$tab_control.FindName('ButtonTransferBom').add_Click({
		$script:refreshNeeded = $false
		Show-BomWindow -Entity $selectedFile
		if ($refreshNeeded) {
			[System.Windows.Forms.SendKeys]::SendWait('{F5}') #BUG: Refresh doesn't work reliably after Show-BOMWindow. The BOM tab remains unchanged
		}
	}.GetNewClosure())


	#region Required BOM Window functions, see "https://doc.coolorange.com/projects/powergate/en/stable/code_reference/commandlets/show-bomwindow/required-functions/"
	function global:Get-BomRows($file) {
		# Purchased Part
		if ($file.Bom_Structure -eq 'Purchased') { return @() }

		# Raw Material - this has been added before and doesn't need to be processed again
		if ($file._FileClass -eq 'rawMaterial') { return @() }

		# Virtual Component
		if ($null -eq $file._EntityTypeID) {
			# Get the virtual components metadata from the ERP system
			$erpItem = Get-ERPObject -EntitySet $itemEntitySet -Keys @{ Number = $file.'BOM_Part Number'.ToUpper() } #TODO: Business Central converts Item Number to upper case
			if ($erpItem) {
				$file | Update-BomWindowEntity -Status $file._Status -Properties @{
					'_FileClass'   = "virtualErp"
					'_Name'        = $erpItem.Number
					'Name'         = $erpItem.Number
					'_PartNumber'  = $erpItem.Number
					'Part Number'  = $erpItem.Number
					'_Title'       = $erpItem.Title
					'Title'        = $erpItem.Title
					'_Description' = $erpItem.Description
					'Description'  = $erpItem.Description

					#TODO: Add other fields if needed
				}				
			} else {
				$file | Update-BomWindowEntity -Status $file._Status -Properties @{
					'_FileClass'   = "virtual"
					'_Name'        = $file.'Bom_Part Number'
					'Name'         = $file.'Bom_Part Number'
					'_PartNumber'  = $file.'Bom_Part Number'
					'Part Number'  = $file.'Bom_Part Number'
					'_Title'       = $file.Bom_Title
					'Title'        = $file.Bom_Title
					'_Description' = $file.Description
					'Description'  = $file.Description

					#TODO: Add other fields if needed
				}	
			}

			return @()
		}

		# Raw Material
		if ($file._Extension -eq 'ipt' -and $file.'Raw Material Number') { 
			# Get the raw material metadata from the ERP system
			$erpItem = Get-ERPObject -EntitySet $itemEntitySet -Keys @{ Number = $file.'Raw Material Number'.ToUpper() } # TODO: Business Central item number to upper case
			$rawMaterial = New-Object PsObject -Property @{
				'_FileClass'         = "rawMaterial"
				'_Name'              = $file.'Raw Material Number'
				'Name'               = $file.'Raw Material Number'
				'_PartNumber'        = $file.'Raw Material Number'
				'Part Number'        = $file.'Raw Material Number'
				'_Title'             = $erpItem.Title
				'Title'              = $erpItem.Title
				'_Description'       = $erpItem.Description
				'Description'        = $erpItem.Description
				'Bom_Part Number'    = $file.'Raw Material Number'				
				'Bom_Quantity'       = $file.'Raw Material Quantity'
				'Bom_Position'       = '1'
				'Bom_PositionNumber' = '1'

				#TODO: Add other fields if needed
			}
			return @($rawMaterial)
		}
		
		# Simplified Part
		# https://help.autodesk.com/view/INVNTOR/2024/ENU/?guid=GUID-A106F1D6-2B7C-4BAB-9356-1DB87CA4767A
		if ($file._Extension -eq 'ipt') { 
			$assocs = Get-VaultFileAssociations -File $file._FullPath -Dependencies
			if ($assocs.Count -eq 1 -and $assocs[0]._Extension -eq "iam") {
				$fileBomRows = Get-VaultFileBom -file $assocs[0]._FullPath -GetChildrenBy LatestVersion
			} else {
				return @()
			}
		} else {
			# Assembly
			$fileBomRows = Get-VaultFileBom -File $file._FullPath -GetChildrenBy LatestVersion
		}
		
		return $fileBomRows
	}

	function global:Check-Items($files) {
		foreach ($vaultFile in $files) {
			#region Validation
			if (-not $vaultFile._PartNumber) {
				$vaultFile | Update-BomWindowEntity -Status Error -StatusDetails "'Part Number' cannot be empty"
				continue
			}
			
			if ($vaultFile._PartNumber.Contains("/") -or $vaultFile._PartNumber.Contains("%2F") -or $vaultFile._PartNumber.Contains("\") -or $vaultFile._PartNumber.Contains("%5C")) {		
				$vaultFile | Update-BomWindowEntity -Status Error -StatusDetails "'Part Number' contains unsupported slashes or backslashes"
				continue
			}

			if ($vaultFile._FileClass -eq 'rawMaterial' ) { 
				$vaultFile | Update-BomWindowEntity -Status Identical -StatusDetails "Raw materials are not transferred to $erpName"
				continue
			}

			if ($vaultFile._FileClass -eq 'virtualErp' ) { 
				$vaultFile | Update-BomWindowEntity -Status Identical -StatusDetails "Virtual Components are not transferred to $erpName"
				continue
			}
		
			if ($vaultFile._PartNumber.Length -gt 20) { #TODO: Business Central validate Part Number length
				$vaultFile | Update-BomWindowEntity -Status Error -StatusDetails "'Part Number' must not exceed 20 characters"
				continue
			}			
			
			if (-not $vaultFile.'_Title') { #TODO: Business Central validate Title
				$vaultFile | Update-BomWindowEntity -Status Error -StatusDetails "'Title' cannot be empty"
				continue
			}
			#endregion

			$erpItem = Get-ERPObject -EntitySet $itemEntitySet -Keys @{ Number = $vaultFile._PartNumber.ToUpper() } # TODO: Business Central item number to upper case
			if ($? -eq $false) {continue}

			if (-not $erpItem) {
				# Mark the ERP Item as new when it doesn't exist in ERP
				$vaultFile | Update-BomWindowEntity -Status New -StatusDetails "Item does not exist in $erpName and will be created"
			}
			else {
				# Get the ERP Item from the mapping
				$newErpItem = GetErpItemFromVaultFile $vaultFile

				# Compare the existing ERP item with the new ERP item from the mapping
				#TODO: Exclude the ERP properties that are not mapped to Vault properties
				$differences = GetErpObjectDifferences $erpItem $newErpItem @("Price", "Stock", "MakeBuy", "Blocked", "Supplier")
				if ($differences.Length -gt 0) {
					$vaultFile | Update-BomWindowEntity -Status Different -StatusDetails "Update required:`n$($differences -Join "`n")"
				}
				else {
					$vaultFile | Update-BomWindowEntity -Status Identical -StatusDetails "Item is up-to-date in $erpName"
				}
			}
		}
	}

	function global:Transfer-Items($files) {
		foreach ($vaultFile in $files) {
			# Get the ERP Item from the mapping
			$erpItem = GetErpItemFromVaultFile $vaultFile
			
			# Add/update the ERP Item
			if ($vaultFile._Status -eq 'New') {
				Add-ErpObject -EntitySet $itemEntitySet -Properties $erpItem
				if ($? -eq $false) {continue}
				$vaultFile | Update-BomWindowEntity -Status Identical
			}
			elseif ($vaultFile._Status -eq 'Different') {
				Update-ERPObject -EntitySet $itemEntitySet -Key $erpItem._Keys -Properties $erpItem._Properties
				if ($? -eq $false) { continue }
				$vaultFile | Update-BomWindowEntity -Status Identical
			}
			else {
				$vaultFile | Update-BomWindowEntity -Status $vaultFile._Status
			}
		}

		# Refresh the Vault UI after 'Transfer' has been clicked
		$script:refreshNeeded = $true
	}

	function global:Check-Boms($boms) {
		foreach ($vaultBom in $boms) {
			$erpBom = Get-ERPObject -EntitySet $bomHeaderEntitySet -Keys @{ Number = $vaultBom._PartNumber.ToUpper() } -Expand 'Children/Item' # TODO: Business Central item number to upper case
			if ($? -eq $false) {
				foreach ($vaultBomRow in $vaultBom.Children) {
					$vaultBomRow | Update-BomWindowEntity -Status Error -StatusDetails $vaultBom._StatusDetails
				}
				continue
			}

			if (-not $erpBom) {
				# If the BOM Header does not exist, then simply put header and rows to new
				$vaultBom | Update-BomWindowEntity -Status New -StatusDetails "BOM Header does not exist in $erpName and will be created"
				foreach ($vaultBomRow in $vaultBom.Children) {
					$vaultBomRow | Update-BomWindowEntity -Status New -StatusDetails "BOM Row does not exist in $erpName and will be created"
				}
			}
			else {
				# If the BOM Header exists, check header and rows			
				$newErpBom = GetErpBomHeaderFromVaultFile $vaultBom
				$differences = GetErpObjectDifferences $erpBom $newErpBom @("Children")
				if ($differences.Length -gt 0) {
					$vaultBom | Update-BomWindowEntity -Status Different -StatusDetails "Update required:`n$($differences -Join "`n")"
				}
				else {
					$vaultBom | Update-BomWindowEntity -Status Identical -StatusDetails "BOM Header is up-to-date in $erpName"
				}
				
				# Remove rows which are marked as removed from previous check
				$erpBom.Children | Where-Object { $_._Status -eq 'Remove' } | ForEach-Object { $_ | Remove-BomWindowEntity }

				# Temporary set all rows to identical for rows that have not been removed
				foreach ($vaultBomRow in $vaultBom.Children | Where-Object { $_._Status -ne 'Remove' } ) {
					$vaultBomRow | Update-BomWindowEntity -Status Identical -StatusDetails "BOM Row is up-to-date in $erpName"
				}

				# Update status message for all rows which are marked as removed
				foreach ($vaultBomRow in $vaultBom.Children | Where-Object { $_._Status -eq 'Remove' } ) {
					$vaultBomRow | Update-BomWindowEntity -Status Remove -StatusDetails $vaultBomRow._StatusDetails
				}

				# Compare the Vault BOM with the ERP BOM
				foreach ($vaultBomRow in $vaultBom.Children) {
					#TODO: Change the key matching according [DataServiceKey] definition of the powerGate plugin
					$erpBomRow = $erpBom.Children | Where-Object { $_.ChildNumber -eq $vaultBomRow._PartNumber -and $_.Position -eq $vaultBomRow.Bom_PositionNumber }
					if ($erpBomRow) {
						$newErpBomRow = GetErpBomRowFromVaultFile $vaultBom $vaultBomRow
						$differences = GetErpObjectDifferences $erpBomRow $newErpBomRow @("Item")
						if ($differences.Length -gt 0) {
							$vaultBomRow | Update-BomWindowEntity -Status Different -StatusDetails "Update required:`n$($differences -Join "`n")"
							#TODO: Uncomment the next line in case the BOM Header needs to be updated whenever a child BOM Row is created, updated or removed
							#$vaultBom | Update-BomWindowEntity -Status Different -StatusDetails "BOM Rows are different between Vault and $erpName"
						}
					}
					else {
						$vaultBomRow | Update-BomWindowEntity -Status New -StatusDetails "BOM Row does not exist in $erpName and will be created"
						#TODO: Uncomment the next line in case the BOM Header needs to be updated whenever a child BOM Row is created, updated or removed
						#$vaultBom | Update-BomWindowEntity -Status Different -StatusDetails "BOM Rows are different between Vault and $erpName"
					}
				}
			}

			# Mark ERP rows as removed which are not present in the Vault BOM
			foreach ($erpBomRow in $erpBom.Children) {
				#TODO: Change the key matching according [DataServiceKey] definition of the powerGate plugin
				$vaultBomRow = $vaultBom.Children | Where-Object { $_._PartNumber -eq $erpBomRow.ChildNumber -and $_.Bom_PositionNumber -eq $erpBomRow.Position }
				if ($null -eq $vaultBomRow) {
					$vaultBomRow = Add-BomWindowEntity -Parent $vaultBom -Type BomRow -Properties @{
						"_Name"            = $erpBomRow.ChildNumber 
						"Name"             = $erpBomRow.ChildNumber #BUG: Name is not displayed in the BOM Window
						"_PartNumber"      = $erpBomRow.ChildNumber
						"Part Number"      = $erpBomRow.ChildNumber
						'_Title'           = $erpBomRow.Item.Title
						'Title'            = $erpBomRow.Item.Title
						'_Description'     = $erpBomRow.Item.Description
						'Description'      = $erpBomRow.Item.Description									
						Bom_Number         = $erpBomRow.ChildNumber
						Bom_PositionNumber = $erpBomRow.Position
						Bom_Quantity       = $erpBomRow.Quantity

						#TODO: Add other fields if needed
					}

					$vaultBomRow | Update-BomWindowEntity -Status Remove -StatusDetails "Bom Row does not exist in Vault BOM and will be deleted in $erpName"
					#TODO: Uncomment the next line in case the BOM Header needs to be updated whenever a child BOM Row is created, updated or removed
					#$vaultBom | Update-BomWindowEntity -Status Different -StatusDetails "BOM Rows are different between Vault and $erpName"
				}
			}
		}
	}

	function global:Transfer-Boms($boms) {
		[array]::Reverse($boms)
		foreach ($vaultBom in $boms) {
			# If the BOM header is different or any BOM row changed, update the header and all rows
			if ($vaultBom._Status -eq 'New' -or $vaultBom._Status -eq 'Different' -or $vaultBom.Children._Status -contains 'New' -or $vaultBom.Children._Status -contains 'Different' -or $vaultBom.Children._Status -contains 'Remove') {
				# Get the ERP BOM Header from the mapping
				$erpBom = GetErpBomHeaderFromVaultFile $vaultBom

				#region BOM Header
				if (($vaultBom._Status -eq 'New')) {
					# Add the ERP BOM Header
					Add-ERPObject -EntitySet $bomHeaderEntitySet -Properties $erpBom
					if ($? -eq $false) {
						# In case of an error, mark the BOM Header and all BOM Rows as error and continue with the next BOM Header
						$vaultBom | Update-BomWindowEntity -Status Error -StatusDetails "Error while processing BOM Header."
						foreach ($vaultBomRow in $vaultBom.Children) {
							$vaultBomRow | Update-BomWindowEntity -Status Error -StatusDetails $vaultBom._StatusDetails
						}
						continue
					}

					# In case of success, mark the BOM Header as identical
					$vaultBom | Update-BomWindowEntity -Status Identical -StatusDetails "BOM Header has been created in $erpName"					
				}
				elseif (($vaultBom._Status -eq 'Different')) {
					#TODO: Exclude Navigation Properties from _Properties field in case there are navigation properties other than "Children" and "Item"
					#Update-ERPObject -EntitySet $bomHeaderEntitySet -Keys $erpBom._Keys -Properties ($erpBom._Properties | Select-Object -Property * -ExcludeProperty Children,Item)
					Update-ERPObject -EntitySet $bomHeaderEntitySet -Keys $erpBom._Keys  -Properties @{}
					if ($? -eq $false) {
						# In case of an error, mark the BOM Header and all BOM Rows as error and continue with the next BOM Header
						$vaultBom | Update-BomWindowEntity -Status Error -StatusDetails "Error while processing BOM Header."
						foreach ($vaultBomRow in $vaultBom.Children) {
							$vaultBomRow | Update-BomWindowEntity -Status Error -StatusDetails $vaultBom._StatusDetails
						}
						continue
					}

					# In case of success, mark the BOM Header as identical
					$vaultBom | Update-BomWindowEntity -Status Identical -StatusDetails "BOM Header has been updated in $erpName"					
				}
				else {
					# No changes, mark the BOM Header as identical
					$vaultBom | Update-BomWindowEntity -Status Identical
				}
				#endregion

				#region BOM Rows
				# Remove BOM rows (remove all rows first to avoid duplicate key errors while adding new rows wiht same position numbers that have not been removed before)
				foreach ($vaultBomRow in $vaultBom.Children | Where-Object { $_._Status -eq "Remove" }) {
					if ($vaultBomRow._Status -eq 'Remove') {
						# Get the ERP BOM Row from the mapping
						$erpBomRow = GetErpBomRowFromVaultFile $vaultBom $vaultBomRow
						Remove-ERPObject -EntitySet $bomRowEntitySet -Keys $erpBomRow._Keys
						if ($? -eq $false) {
							# In case of an error, mark the BOM Header and all BOM Rows as error and continue with the next BOM Row
							$vaultBom | Update-BomWindowEntity -Status Error -StatusDetails "Error while processing BOM Row. See BOM rows for more information."
							$vaultBomRow | Update-BomWindowEntity -Status Error -StatusDetails $vaultBomRow._StatusDetails
							continue
						}

						# In case of success, remove the row from the dialog
						$vaultBomRow | Remove-BomWindowEntity
						continue
					}
				}

				# Add/update the BOM rows that not have been removed
				foreach ($vaultBomRow in $vaultBom.Children | Where-Object { $_._Status -ne "Remove" }) {
					# Get the ERP BOM Row from the mapping
					$erpBomRow = GetErpBomRowFromVaultFile $vaultBom $vaultBomRow

					if ($vaultBomRow._Status -eq 'New') {
						Add-ERPObject -EntitySet $bomRowEntitySet -Properties $erpBomRow
						if ($? -eq $false) {
							# In case of an error, mark the BOM Header and all BOM Rows as error and continue with the next BOM Row
							$vaultBom | Update-BomWindowEntity -Status Error -StatusDetails "Error while processing BOM Row. See BOM rows for more information."
							$vaultBomRow | Update-BomWindowEntity -Status Error -StatusDetails $vaultBomRow._StatusDetails
							continue
						}

						# In case of success, mark the BOM row as identical and continue with the next BOM Row
						$vaultBomRow | Update-BomWindowEntity -Status Identical -StatusDetails "BOM Row has been added to $erpName"
						continue
					}

					if ($vaultBomRow._Status -eq 'Different') {
						#TODO: Exclude Navigation Properties from _Properties field in case there are navigation properties other than "Item"
						Update-ERPObject -EntitySet $bomRowEntitySet -Keys $erpBomRow._Keys -Properties ($erpBomRow._Properties | Select-Object -Property * -ExcludeProperty Item)
						if ($? -eq $false) {
							# In case of an error, mark the BOM Header and all BOM Rows as error and continue with the next BOM Row
							$vaultBom | Update-BomWindowEntity -Status Error -StatusDetails "Error while processing BOM Row. See BOM rows for more information."
							$vaultBomRow | Update-BomWindowEntity -Status Error -StatusDetails $vaultBomRow._StatusDetails
							continue
						}

						# In case of success, mark the BOM row as identical and continue with the next BOM Row
						$vaultBomRow | Update-BomWindowEntity -Status Identical -StatusDetails "BOM Row has been updated in $erpName"
						continue
					}

					# No changes, mark the BOM Row as identical
					$vaultBomRow | Update-BomWindowEntity -Status Identical -StatusDetails $vaultBomRow._StatusDetails
				}
				#endregion

				# If BOM Header or BOM Rows changed, continue with the next BOM Header
				continue
			}

			# If the BOM Header and all BOM Rows are identical, update just the status
			$vaultBom | Update-BomWindowEntity -Status $vaultBom._Status
			foreach ($vaultBomRow in $vaultBom.Children) {
				$vaultBomRow | Update-BomWindowEntity -Status $vaultBomRow._Status
			}
		}

		# Refresh the Vault UI after 'Transfer' has been clicked
		$script:refreshNeeded = $true
	}
	#endregion

	return $tab_control
}