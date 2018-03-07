Module mod_vyfakturuj

#Region "Nastavení"
    'Zde třeba nastavit vlastní data
    Public VF_LOGIN As String = ""
    Public VF_APIKEY As String = ""

    'cesta k adresáři pro uložení faktur (dokumenty uživatele)
    Public VF_FolderPathFaktury As String = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FA")
    'ukládat fakturu do adresáře
    Public SavePDFNoveVytvoreneFaktury As Boolean = True
    'pokud se fa ukládá je možno ji tisknout
    Public VF_TisknoutFaktury As Boolean = True
    'nastavení názvu tiskárny - jinak se nastaví defaultní
    Public VF_PrintName As String = PrintExistNameOrDefaultName("RICOH SP 204SN")
    'počet kopií fa, většinou dvě (pro zákazníka a pro svou firmu)
    Public VF_PocetCopyTiskuFaktury As Integer = 2  ' 1 kopie  2 kopie
#End Region
    

    ''čteme fakturu (dle id)
    'Dim VYFAKTURUJ_GET_FA = mod_vyfakturuj.VytvoritNovouFakturu()
    ''Dim VYFAKTURUJ_GET_FA = mod_vyfakturuj.NactiFakturu("147342")

    'Dim NEW_FA_id As Integer = VYFAKTURUJ_GET_FA.id
    'Dim NEW_FA_number As String = VYFAKTURUJ_GET_FA.number
    'Dim NEW_FA_url_download_pdf As String = VYFAKTURUJ_GET_FA.url_download_pdf
    'Dim NEW_FA_url_download_pdf_no_stamp As String = VYFAKTURUJ_GET_FA.url_download_pdf_no_stamp
    'Dim NEW_FA_url_public_webpage As String = VYFAKTURUJ_GET_FA.url_public_webpage
    'Dim NEW_FA_eet_status As Integer = VYFAKTURUJ_GET_FA.eet_status
    'Dim NEW_FA_EET_bkp As String = VYFAKTURUJ_GET_FA.eet_data.bkp
    'Dim NEW_FA_EET_dat_trzby As String = VYFAKTURUJ_GET_FA.eet_data.dat_trzby
    'Dim NEW_FA_EET_fik As String = VYFAKTURUJ_GET_FA.eet_data.fik
    'Dim NEW_FA_EET_id_pokl As String = VYFAKTURUJ_GET_FA.eet_data.id_pokl
    'Dim NEW_FA_EET_id_provoz As String = VYFAKTURUJ_GET_FA.eet_data.id_provoz
    'Dim NEW_FA_EET_pkp As String = VYFAKTURUJ_GET_FA.eet_data.pkp
    'Dim NEW_FA_EET_rezim As String = VYFAKTURUJ_GET_FA.eet_data.rezim

    Public Function VytvoritNovouFakturu() As vfapi.Faktura
        VytvoritNovouFakturu = Nothing
        Try
            Dim sDatum As String = Date.Now.ToString("yyyy-MM-dd")
            'Vytvoření instance faktury (ŠABLONA)
            Dim VYFAKTURUJ_NEW_FA As New vfapi.Faktura()
            'typ dokladu
            VYFAKTURUJ_NEW_FA.type = "1"  ' Typ dokladu - 1 - Faktura
            'forma úhrady
            VYFAKTURUJ_NEW_FA.payment_method = "2"  '1 - Bankovní převod , 2 - Hotovost , 4 - Dobírkou , 8 - Kartou online
            'zobrazit text :  Již uhrazeno
            VYFAKTURUJ_NEW_FA.mark_already_paid = False   ' Zobrazit text: Již uhrazeno
            'plátce DPH
            VYFAKTURUJ_NEW_FA.calculate_vat = 5   ' Výpočet DPH  -  5 - Neplátce DPH
            'zaokrouhlení cen
            VYFAKTURUJ_NEW_FA.round_invoice = 2
            '--------------------------------------------------------------------------------
            'položky faktury
            VYFAKTURUJ_NEW_FA.VS = ""  ' pokud nevyplníme, vloží se systémem vyfakturuj číslo faktury
            VYFAKTURUJ_NEW_FA.SS = ""
            VYFAKTURUJ_NEW_FA.KS = "2255"
            VYFAKTURUJ_NEW_FA.X_1 = "456"  ' možno dle vlastní potřeby - třeba dle vlastího systému pole ID FA tabulky
            VYFAKTURUJ_NEW_FA.X_2 = ""  ' možno dle vlastní potřeby
            VYFAKTURUJ_NEW_FA.X_3 = ""  ' možno dle vlastní potřeby
            VYFAKTURUJ_NEW_FA.bank_account_number = ""  '  Číslo bankovního účtu
            VYFAKTURUJ_NEW_FA.bank_IBAN = ""
            VYFAKTURUJ_NEW_FA.bank_BIC = ""

            '--------------------------------------------------------------------------------
            'vytvořit
            VYFAKTURUJ_NEW_FA.date_created = sDatum  '"0000-00-00"   'Datum vytvoření
            'uhradit
            VYFAKTURUJ_NEW_FA.date_paid = sDatum  '"0000-00-00"   'Datum platby
            VYFAKTURUJ_NEW_FA.date_taxable_supply = sDatum  '"0000-00-00"   ' Datum zdaňitelného plnění
            '--------------------------------------------------------------------------------
            'zaslat eet
            VYFAKTURUJ_NEW_FA.action_after_create_send_to_eet = True    ' Akce: po vytvoření dokladu, zaslat automaticky do EET.
            '--------------------------------------------------------------------------------
            'Dodavatel
            VYFAKTURUJ_NEW_FA.supplier_IC = "11122333"
            VYFAKTURUJ_NEW_FA.supplier_DIC = "CZ11122333"
            VYFAKTURUJ_NEW_FA.supplier_name = "Můj název firmy"
            VYFAKTURUJ_NEW_FA.supplier_street = "Moje adresa"
            VYFAKTURUJ_NEW_FA.supplier_city = "Moje město"
            VYFAKTURUJ_NEW_FA.supplier_zip = "30100"
            VYFAKTURUJ_NEW_FA.supplier_country = "Česká republika"
            '--------------------------------------------------------------------------------
            'Odběratel
            VYFAKTURUJ_NEW_FA.customer_IC = "123456789"
            VYFAKTURUJ_NEW_FA.customer_DIC = "CZ123456789"
            VYFAKTURUJ_NEW_FA.customer_name = Form1.txtOdberatel.Text   '  "Ukázková firma"
            VYFAKTURUJ_NEW_FA.customer_street = "Ukázková ulice"
            VYFAKTURUJ_NEW_FA.customer_city = "Ukázkové město"
            VYFAKTURUJ_NEW_FA.customer_zip = "30100"
            VYFAKTURUJ_NEW_FA.customer_country = "Česká republika"
            'VYFAKTURUJ_NEW_FA.mail_to(0) = "emailodberatele@email.cz"
            '--------------------------------------------------------------------------------

            'Stop

            'inicializace položky - počet array polí
            ReDim VYFAKTURUJ_NEW_FA.items(2)
            '------------------------------------------------------
            'instance 1 položky
            VYFAKTURUJ_NEW_FA.items(0) = New vfapi.items()
            VYFAKTURUJ_NEW_FA.items(0).quantity = 1
            VYFAKTURUJ_NEW_FA.items(0).text = "Stěrač na ponorku"
            VYFAKTURUJ_NEW_FA.items(0).unit_price = 26.25
            VYFAKTURUJ_NEW_FA.items(0).vat_rate = 15
            '------------------------------------------------------
            'instance 2 položky
            VYFAKTURUJ_NEW_FA.items(1) = New vfapi.items()
            VYFAKTURUJ_NEW_FA.items(1).quantity = 1
            VYFAKTURUJ_NEW_FA.items(1).text = "Kapalina do ostřikovačů 250 ml"
            VYFAKTURUJ_NEW_FA.items(1).unit_price = 12
            VYFAKTURUJ_NEW_FA.items(1).vat_rate = 15
            '------------------------------------------------------
            'instance 3 položky
            VYFAKTURUJ_NEW_FA.items(2) = New vfapi.items()
            VYFAKTURUJ_NEW_FA.items(2).quantity = 1
            VYFAKTURUJ_NEW_FA.items(2).text = "Doprava"
            VYFAKTURUJ_NEW_FA.items(2).unit_price = 30
            VYFAKTURUJ_NEW_FA.items(2).vat_rate = 15
            '------------------------------------------------------
            'FAKTURA PŘIPRAVENA

            '------------------------------------------------------
            'inicializace api vyfakturuj.cz (login,apikey) připojení
            Dim VYFAKTURUJ As New vfapi(VF_LOGIN, VF_APIKEY)
            '------------------------------------------------------

            'Vytvoříme fakturu (z připravené šablony)
            Dim VYFAKTURUJ_GET_FA As vfapi.Faktura = VYFAKTURUJ.CreateFaktura(VYFAKTURUJ_NEW_FA)

            'Vracíme načtenou novou fakturu
            Return VYFAKTURUJ_GET_FA
        Catch ex As Exception
            MessageBox.Show(ex.Message, My.Application.Info.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    Public Function NactiFakturu(ByVal id As String) As vfapi.Faktura
        NactiFakturu = Nothing
        If String.IsNullOrEmpty(id) Then Exit Function
        Try
            'inicializace api vyfakturuj.cz (login,apikey) připojení
            Dim VYFAKTURUJ As vfapi = New vfapi(VF_LOGIN, VF_APIKEY)
            '------------------------------------------------------
            'čteme fakturu (dle id)
            Dim VYFAKTURUJ_GET_FA As vfapi.Faktura = VYFAKTURUJ.GetFaktura(id)
            'Vracíme načtenou fakturu
            Return VYFAKTURUJ_GET_FA
        Catch ex As Exception
            MessageBox.Show(ex.Message, My.Application.Info.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    Public Function TestNewFA() As vfapi.Faktura
        TestNewFA = Nothing
        Try
            Dim sDatum As String = Date.Now.ToString("yyyy-MM-dd")
            'Vytvoření instance faktury (ŠABLONA)
            Dim VYFAKTURUJ_NEW_FA As New vfapi.Faktura()
            'typ dokladu
            VYFAKTURUJ_NEW_FA.type = "1"  ' Typ dokladu - 1 - Faktura
            'forma úhrady
            VYFAKTURUJ_NEW_FA.payment_method = "2"  '1 - Bankovní převod , 2 - Hotovost , 8 - Kartou online
            'zobrazit text :  Již uhrazeno
            VYFAKTURUJ_NEW_FA.mark_already_paid = False   ' Zobrazit text: Již uhrazeno
            'plátce DPH
            VYFAKTURUJ_NEW_FA.calculate_vat = 5   ' Výpočet DPH  -  5 - Neplátce DPH
            'zaokrouhlení cen
            VYFAKTURUJ_NEW_FA.round_invoice = 2
            '--------------------------------------------------------------------------------
            'položky faktury
            VYFAKTURUJ_NEW_FA.VS = ""
            VYFAKTURUJ_NEW_FA.SS = ""
            VYFAKTURUJ_NEW_FA.KS = "2255"
            VYFAKTURUJ_NEW_FA.X_1 = "456"  ' ID FA
            VYFAKTURUJ_NEW_FA.X_2 = ""  ' ID USER
            VYFAKTURUJ_NEW_FA.X_3 = ""  ' ID POKLADNA
            VYFAKTURUJ_NEW_FA.bank_account_number = ""  '  Číslo bankovního účtu
            VYFAKTURUJ_NEW_FA.bank_IBAN = ""
            VYFAKTURUJ_NEW_FA.bank_BIC = ""
            '--------------------------------------------------------------------------------
            'uhradit
            VYFAKTURUJ_NEW_FA.date_paid = sDatum  '"0000-00-00"   'Datum platby
            VYFAKTURUJ_NEW_FA.date_taxable_supply = sDatum  '"0000-00-00"   ' Datum zdaňitelného plnění
            '--------------------------------------------------------------------------------
            'zaslat eet
            VYFAKTURUJ_NEW_FA.action_after_create_send_to_eet = True    ' Akce: po vytvoření dokladu, zaslat automaticky do EET.
            '--------------------------------------------------------------------------------
            'Dodavatel
            VYFAKTURUJ_NEW_FA.supplier_IC = "11122333"
            VYFAKTURUJ_NEW_FA.supplier_DIC = "CZ11122333"
            VYFAKTURUJ_NEW_FA.supplier_name = "ZetComp"
            VYFAKTURUJ_NEW_FA.supplier_street = "Komenského 89 (vnitro blok)"
            VYFAKTURUJ_NEW_FA.supplier_city = "Plzeň"
            VYFAKTURUJ_NEW_FA.supplier_zip = "32300"
            VYFAKTURUJ_NEW_FA.supplier_country = "Česká republika"
            '--------------------------------------------------------------------------------
            'Odběratel
            VYFAKTURUJ_NEW_FA.customer_IC = "123456789"
            VYFAKTURUJ_NEW_FA.customer_DIC = "CZ123456789"
            VYFAKTURUJ_NEW_FA.customer_name = "Ukázková firma"
            VYFAKTURUJ_NEW_FA.customer_street = "Ukázková ulice"
            VYFAKTURUJ_NEW_FA.customer_city = "Plzeň"
            VYFAKTURUJ_NEW_FA.customer_zip = "32300"
            VYFAKTURUJ_NEW_FA.customer_country = "Česká republika"
            '--------------------------------------------------------------------------------

            'Stop

            'inicializace položky - počet array polí
            ReDim VYFAKTURUJ_NEW_FA.items(2)
            '------------------------------------------------------
            'instance 1 položky
            VYFAKTURUJ_NEW_FA.items(0) = New vfapi.items()
            VYFAKTURUJ_NEW_FA.items(0).quantity = 1
            VYFAKTURUJ_NEW_FA.items(0).text = "Stěrač na ponorku"
            VYFAKTURUJ_NEW_FA.items(0).unit_price = 26.25
            VYFAKTURUJ_NEW_FA.items(0).vat_rate = 15
            '------------------------------------------------------
            'instance 2 položky
            VYFAKTURUJ_NEW_FA.items(1) = New vfapi.items()
            VYFAKTURUJ_NEW_FA.items(1).quantity = 1
            VYFAKTURUJ_NEW_FA.items(1).text = "Kapalina do ostřikovačů 250 ml"
            VYFAKTURUJ_NEW_FA.items(1).unit_price = 12
            VYFAKTURUJ_NEW_FA.items(1).vat_rate = 15
            '------------------------------------------------------
            'instance 3 položky
            VYFAKTURUJ_NEW_FA.items(2) = New vfapi.items()
            VYFAKTURUJ_NEW_FA.items(2).quantity = 1
            VYFAKTURUJ_NEW_FA.items(2).text = "Doprava"
            VYFAKTURUJ_NEW_FA.items(2).unit_price = 30
            VYFAKTURUJ_NEW_FA.items(2).vat_rate = 15
            '------------------------------------------------------
            'FAKTURA PŘIPRAVENA

            '------------------------------------------------------
            'inicializace nové faktury (login,apikey)
            Dim VYFAKTURUJ As New vfapi(VF_LOGIN, VF_APIKEY)
            '------------------------------------------------------

            'Vytvoříme fakturu (z připravené šablony)
            Dim VYFAKTURUJ_GET_FA = VYFAKTURUJ.CreateFaktura(VYFAKTURUJ_NEW_FA)
            'Vracíme načtenou novou fakturu
            Return VYFAKTURUJ_GET_FA
        Catch ex As Exception
            MessageBox.Show(ex.Message, My.Application.Info.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    ''' <summary>
    ''' zjistit zda existuje nainstalovaná tiskárna, pokud ne zašle názevev defaultní tiskárny. Jinak String.Empty
    ''' </summary>
    ''' <returns>String</returns>
    ''' <remarks></remarks>
    Public Function PrintExistNameOrDefaultName(ByVal print_name As String) As String
        Dim defaultPrinterSetting As System.Drawing.Printing.PrinterSettings = Nothing
        Dim sDefaultPrintName As String = String.Empty
        For Each printer As String In System.Drawing.Printing.PrinterSettings.InstalledPrinters

            defaultPrinterSetting = New System.Drawing.Printing.PrinterSettings
            defaultPrinterSetting.PrinterName = printer

            If printer = print_name Then
                Return printer
            End If

            If defaultPrinterSetting.IsDefaultPrinter Then
                sDefaultPrintName = defaultPrinterSetting.PrinterName
            End If

        Next

        Return sDefaultPrintName
    End Function

    Public Function ExecuteFile(ByVal FileName As String) As Boolean
        Dim myProcess As New Process
        Try

            myProcess.StartInfo.FileName = FileName
            myProcess.StartInfo.UseShellExecute = True
            myProcess.StartInfo.RedirectStandardOutput = False
            myProcess.Start()
            Return True

        Catch ex As Exception
            MessageBox.Show(ex.Message, My.Application.Info.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        Finally
            myProcess.Dispose()
        End Try
    End Function

End Module
