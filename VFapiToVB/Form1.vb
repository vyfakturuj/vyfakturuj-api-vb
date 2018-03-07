Public Class Form1
    Private FormLoad As Boolean
    Private PrintEnabled As Boolean

    Private Sub Form1_Load(sender As System.Object, e As System.EventArgs) Handles MyBase.Load
        TextBox1.Text = "kozeluh@zetcomp.cz"
        TextBox2.Text = "e90zKpLAZQ4On7x5VBkH7njvkRYk8q5ayMNVobvw"
        cmbPrinter.Items.Clear()
        Dim defaultPrinterSetting As System.Drawing.Printing.PrinterSettings = Nothing
        Dim sDefaultPrintName As String = String.Empty
        For Each printer As String In System.Drawing.Printing.PrinterSettings.InstalledPrinters
            defaultPrinterSetting = New System.Drawing.Printing.PrinterSettings
            defaultPrinterSetting.PrinterName = printer
            cmbPrinter.Items.Add(printer)
            If defaultPrinterSetting.IsDefaultPrinter Then
                sDefaultPrintName = defaultPrinterSetting.PrinterName
            End If
            PrintEnabled = True
        Next
        If PrintEnabled Then cmbPrinter.Text = sDefaultPrintName
        CheckBox1.Checked = True

        'vytvoření adresáře pro ukládání fakturace, pokud neexistuje
        If Not System.IO.Directory.Exists(VF_FolderPathFaktury) Then
            System.IO.Directory.CreateDirectory(VF_FolderPathFaktury)
        End If

        Dim files() As String
        'Vrátí seznam souborů do pole
        files = IO.Directory.GetFiles(VF_FolderPathFaktury, "*.pdf", IO.SearchOption.TopDirectoryOnly)

        ListBox1.Items.Clear()
        For i As Integer = 0 To files.Count - 1
            Dim tFile As String = IO.Path.GetFileName(files(i))
            ListBox1.Items.Add(tFile)
        Next

        FormLoad = True
    End Sub

    Private Sub btnVytvor_Click(sender As System.Object, e As System.EventArgs) Handles btnVytvor.Click

        If String.IsNullOrEmpty(TextBox1.Text) OrElse String.IsNullOrEmpty(TextBox2.Text) Then
            MessageBox.Show("Nejsou vyplněny přihlašovací údaje.. !  (LOGIN  nebo APIKEY)", My.Application.Info.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
            Exit Sub
        End If
        VF_LOGIN = TextBox1.Text
        VF_APIKEY = TextBox2.Text
        VF_PrintName = cmbPrinter.Text
        VF_PocetCopyTiskuFaktury = CInt(NumericUpDown1.Value)

        SavePDFNoveVytvoreneFaktury = CheckBox1.Checked
        VF_TisknoutFaktury = CheckBox2.Checked

        Dim VYFAKTURUJ_GET_FA As vfapi.Faktura = mod_vyfakturuj.VytvoritNovouFakturu()

        If VYFAKTURUJ_GET_FA IsNot Nothing Then

            Dim sFilePdf As String = VYFAKTURUJ_GET_FA.number & "-" & VYFAKTURUJ_GET_FA.id & ".pdf"
            ListBox1.Items.Add(sFilePdf)
            lblCisloFA.Text = VYFAKTURUJ_GET_FA.number
            lblFIK.Text = VYFAKTURUJ_GET_FA.eet_data.fik
            lblBKP.Text = VYFAKTURUJ_GET_FA.eet_data.bkp
            lblPKP.Text = VYFAKTURUJ_GET_FA.eet_data.pkp

            ''vracíme vytvořené data faktury
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

            'Načteme cestu kam ukládat pdf faktury
            Dim path_doc_pdf As String = System.IO.Path.Combine(VF_FolderPathFaktury, VYFAKTURUJ_GET_FA.number & "-" & VYFAKTURUJ_GET_FA.id & ".pdf")

            '------------------------------------------------------
            'uložení pdf do adresáře FA
            If SavePDFNoveVytvoreneFaktury Then
                'vytvoření adresáře pro ukládání fakturace, pokud neexistuje
                If Not System.IO.Directory.Exists(VF_FolderPathFaktury) Then
                    System.IO.Directory.CreateDirectory(VF_FolderPathFaktury)
                End If
                'uložení pdf
                Dim WC As New System.Net.WebClient
                WC.DownloadFile(VYFAKTURUJ_GET_FA.url_download_pdf_no_stamp, path_doc_pdf)
            End If
            '------------------------------------------------------

            'tisk
            If VF_TisknoutFaktury Then

                Dim pathToExecutable As String = "AcroRd32.exe"
                Dim starter As New ProcessStartInfo(pathToExecutable, "/t """ + path_doc_pdf + """ """ + VF_PrintName + """")
                For iIdx As Integer = 0 To VF_PocetCopyTiskuFaktury - 1
                    Threading.Thread.Sleep(2000)
                    Dim Process As New Process()
                    starter.CreateNoWindow = True
                    starter.WindowStyle = ProcessWindowStyle.Hidden
                    Process.StartInfo = starter
                    Process.Start()
                    Process.WaitForExit(10000)
                    Process.Kill()
                    Process.Close()
                Next
            End If

        End If
        

    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles CheckBox1.CheckedChanged
        If CheckBox1.Checked = False Then
            CheckBox2.Checked = False
            CheckBox2.Enabled = False
        Else
            If PrintEnabled Then
                CheckBox2.Enabled = True
            Else
                CheckBox2.Checked = False
                CheckBox2.Enabled = False
            End If
        End If
    End Sub

    Private Sub ListBox1_DoubleClick(sender As Object, e As System.EventArgs) Handles ListBox1.DoubleClick
        Dim TempString = System.IO.Path.Combine(VF_FolderPathFaktury, ListBox1.Text)
        If IO.File.Exists(TempString) Then mod_vyfakturuj.ExecuteFile(TempString)
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(sender As System.Object, e As System.EventArgs) Handles ListBox1.SelectedIndexChanged
        Dim result As String = ListBox1.SelectedItem.ToString()
        Dim TempString = System.IO.Path.Combine(VF_FolderPathFaktury, result)
        result = IO.Path.GetFileNameWithoutExtension(TempString)
        'Dim TempString As String = ListBox1.SelectedItem.ToString()
        Dim start_pozice As Integer = result.IndexOf("-")
        txtCisloFaGet.Text = result.Substring((start_pozice + 1), result.Length - (start_pozice + 1))
        ' ListBox1.Items.Remove(ListBox1.SelectedItem.ToString)
    End Sub

    Private Sub txtOdberatel_TextChanged(sender As System.Object, e As System.EventArgs) Handles txtOdberatel.TextChanged
        If String.IsNullOrEmpty(txtOdberatel.Text) Then
            btnVytvor.Enabled = False
        Else
            btnVytvor.Enabled = True
        End If
    End Sub

    Private Sub txtCisloFaGet_TextChanged(sender As System.Object, e As System.EventArgs) Handles txtCisloFaGet.TextChanged
        If Not String.IsNullOrEmpty(txtCisloFaGet.Text) AndAlso IsNumeric(txtCisloFaGet.Text) AndAlso txtCisloFaGet.Text > 0 Then
            btnNacti.Enabled = True
        Else
            btnNacti.Enabled = False
        End If
    End Sub

    Private Sub btnNacti_Click(sender As System.Object, e As System.EventArgs) Handles btnNacti.Click
        If String.IsNullOrEmpty(TextBox1.Text) OrElse String.IsNullOrEmpty(TextBox2.Text) Then
            MessageBox.Show("Nejsou vyplněny přihlašovací údaje.. !  (LOGIN  nebo APIKEY)", My.Application.Info.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error)
            Exit Sub
        End If
        VF_LOGIN = TextBox1.Text
        VF_APIKEY = TextBox2.Text
        VF_PrintName = cmbPrinter.Text
        VF_PocetCopyTiskuFaktury = CInt(NumericUpDown1.Value)

        SavePDFNoveVytvoreneFaktury = CheckBox1.Checked
        VF_TisknoutFaktury = CheckBox2.Checked

        Dim VYFAKTURUJ_GET_FA As vfapi.Faktura = mod_vyfakturuj.NactiFakturu(txtCisloFaGet.Text)


        If VYFAKTURUJ_GET_FA IsNot Nothing Then
            Label15.Text = VYFAKTURUJ_GET_FA.number
            Label16.Text = VYFAKTURUJ_GET_FA.eet_data.fik
            Label14.Text = VYFAKTURUJ_GET_FA.eet_data.bkp
            Label13.Text = VYFAKTURUJ_GET_FA.eet_data.pkp
        End If
    End Sub

    Private Sub btnKonec_Click(sender As System.Object, e As System.EventArgs) Handles btnKonec.Click
        Application.Exit()
    End Sub
End Class
