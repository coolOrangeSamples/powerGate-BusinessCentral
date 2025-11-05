#==============================================================================#
# (c) 2022 coolOrange s.r.l.                                                   #
#                                                                              #
# THIS SCRIPT/CODE IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER    #
# EXPRESSED OR IMPLIED, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES  #
# OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, OR NON-INFRINGEMENT.   #
#==============================================================================#

# To disable the Inventor Menu 'Insert ERP Item...', move this script to the %ProgramData%/coolorange/powerEvents/Events/Disabled directory

if ($processName -notin @('Inventor')) {
    return
}

Add-InventorMenuItem -Name "Insert`n$erpName Item..." -Action {  
    $document = $inventor.ActiveDocument

    #region Pre-Checks
    if ($document.IsModifiable -eq $fale) {
        $message = "The current document is not editable! Please re-open the file as editable for creating a link to an Item in $erpName."
    	$title = "powerGate - Document not editable"
    	$null = [Autodesk.DataManagement.Client.Framework.Forms.Library]::ShowMessage($message, $title, [Autodesk.DataManagement.Client.Framework.Forms.Currency.ButtonConfiguration]::Ok)
        return
    }

    if ($document.DocumentType -notin @([Inventor.DocumentTypeEnum]::kAssemblyDocumentObject, [Inventor.DocumentTypeEnum]::kPartDocumentObject)) {
        $message = "This function is available only on parts or assemblies!"
    	$title = "powerGate - Document type not supported"
    	$null = [Autodesk.DataManagement.Client.Framework.Forms.Library]::ShowMessage($message, $title, [Autodesk.DataManagement.Client.Framework.Forms.Currency.ButtonConfiguration]::Ok)
        return
    }
    #endregion
    
    Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, Autodesk.DataManagement.Client.Framework.Forms
    $xamlFile = [xml](Get-Content "$PSScriptRoot\$filePrefix.ErpItemInsert.xaml")
    $window = [Windows.Markup.XamlReader]::Load( (New-Object System.Xml.XmlNodeReader $xamlFile) )
    ApplyVaultTheme $window
    $window.Title = "powerGate - Insert $erpName Item"

    $isAssembly = $document.DocumentType -eq [Inventor.DocumentTypeEnum]::kAssemblyDocumentObject
    if ($isAssembly) {
        $window.FindName('Title').Content = "Insert Virtual Component from $erpName"
    }
    else {
        $window.FindName('Title').Content = "Insert Raw Material from $erpName"
    }

    $searchCriteria = New-Object PsObject -Property @{"SearchTerm" = "" }
    $window.FindName('SearchCriteria').DataContext = $searchCriteria
    $window.FindName('SearchResults').Tag = "Search $erpName Item"

    $window.FindName('ButtonSearch').Add_Click({
        $searchCriteria = $window.FindName('SearchCriteria').DataContext
        if ($null -ne $searchCriteria.SearchTerm -and $searchCriteria.SearchTerm -ne "") {
            $window.Cursor = [System.Windows.Input.Cursors]::Wait
            # TODO: Change the search fields if needed
            $results = Get-ERPObjects -EntitySet $itemEntitySet -Filter "(substringof('$($searchCriteria.SearchTerm.ToUpper())',toupper(Number)) eq true) or (substringof('$($searchCriteria.SearchTerm.ToUpper())',toupper(Title)) eq true) or (substringof('$($searchCriteria.SearchTerm.ToUpper())',toupper(Description)) eq true)  or (substringof('$($searchCriteria.SearchTerm.ToUpper())',toupper(Material)) eq true)"
            $window.FindName('SearchResults').ItemsSource = $results
            $window.FindName('SearchResults').Tag = "No $erpName Item found for '$($searchCriteria.SearchTerm)'"
            $window.FindName('SearchTerm').Cursor = $null
            $window.Cursor = $null
        }
    }.GetNewClosure())

    $window.FindName('SearchTerm').Add_KeyDown({
        if ($_.Key -eq "Enter") { 
            $window.FindName('SearchTerm').Cursor = [System.Windows.Input.Cursors]::Wait
            $window.FindName('ButtonSearch').RaiseEvent((New-Object System.Windows.RoutedEventArgs([System.Windows.Controls.Button]::ClickEvent)))
        }
    }.GetNewClosure())

    $window.FindName('ButtonInsert').Add_Click({
        $selectedElement = $window.FindName('SearchResults').SelectedItems[0]
        $number = $selectedElement.Number
        $quantity = [int]$window.FindName('Quantity').Text

        if ($isAssembly) {
            $message = "By clicking Yes, a Virtual Component with the Number '$number' and Quantity $quantity will be added to the assembly"
        }
        else {
            $message = "By clicking Yes, a Raw Material with the Number '$number' and Quantity $quantity will be added to the part as custom iProperty"
        }
        $title = "powerGate - Confirm operation"
        $answer = [Autodesk.DataManagement.Client.Framework.Forms.Library]::ShowWarning($message, $title, [Autodesk.DataManagement.Client.Framework.Forms.Currency.ButtonConfiguration]::YesNo)
        if ($answer -eq "Yes") {
            if ($isAssembly) {
                $occur = $document.ComponentDefinition.Occurrences
                foreach($oc in $occur){
                    if($oc.Definition.DisplayName -eq $number){
                        $null = [Autodesk.DataManagement.Client.Framework.Forms.Library]::ShowError("This item has already been added!", "powerGate - Virtual Component already exists")
                        return
                    }
                }
                $null = $occur.AddVirtual($number, $inventor.TransientGeometry.CreateMatrix())
                $BOM = $document.ComponentDefinition.BOM
                if (-not $BOM.StructuredViewEnabled) {
                    $BOM.StructuredViewEnabled = $true
                }
                $structBomView = $BOM.BOMViews | Where-Object { $_.ViewType -eq [Inventor.BOMViewTypeEnum]::kStructuredBOMViewType } | Select-Object -First 1
                $bomCom = $structBomView.BOMRows | Where-Object { ($_.ComponentDefinitions | Select-Object -First 1).DisplayName -eq $number }
                $bomCom.TotalQuantity = $quantity
            }
            else {
                $customProp = $document.PropertySets.Item('Inventor User Defined Properties') | Where-Object { $_.Name -eq 'Raw Material Number' }
                if ($customProp) { $customProp.Delete() }
                $document.PropertySets.Item('Inventor User Defined Properties').Add($number, 'Raw Material Number') 
                $customProp = $document.PropertySets.Item('Inventor User Defined Properties') | Where-Object { $_.Name -eq 'Raw Material Quantity' }
                if ($customProp) { $customProp.Delete() }
                $document.PropertySets.Item('Inventor User Defined Properties').Add($quantity, 'Raw Material Quantity') 
            }
            $window.Close()
        }
    }.GetNewClosure())

    $null = $window.FindName('SearchTerm').Focus()
    $null = $window.ShowDialog()
}