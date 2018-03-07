Imports System
Imports System.IO
Imports System.Net
Imports RestSharp
Imports RestSharp.Authenticators
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

<Serializable()> _
Friend Class vfapi

    'ověřování připojení k internetu
    Private Declare Function InternetGetConnectedState Lib "wininet" (ByRef conn As Long, ByVal val As Long) As Boolean

#Region "deklarace proměnných"
    'zakladni adresa API
    Private Const URL As String = "https://api.vyfakturuj.cz/2.0/"
    'Přihlašovací údaje
    Private LOGIN As String
    Private APIKEY As String
    'Client HTTP
    Private client As RestClient
#End Region

    Public Sub New(ByVal login_name As String, ByVal api_key As String)
        LOGIN = login_name
        APIKEY = api_key
        'Vytvoření http klienta (framework 4.0)
        client = New RestClient()
        ' nastaveni URL adresy naseho API
        client.BaseUrl = New Uri(URL)
        ' autorizace
        client.AddDefaultHeader("X-Authorization", "Basic " & Base64Encode(LOGIN & ":" & APIKEY))
    End Sub

    Public Function CreateFaktura(ByVal sFaktura As Faktura) As Faktura
        CreateFaktura = Nothing

        Dim request = New RestRequest("invoice/", Method.POST)

        request.AddHeader("Content-Type", "application/json")
        request.RequestFormat = DataFormat.Json
        request.RootElement = "fa"
        request.AddBody(sFaktura)

        Dim response As IRestResponse
        Dim Out As Integer

        If InternetGetConnectedState(Out, 0) = True AndAlso My.Computer.Network.Ping("www.vyfakturuj.cz", 1000) Then
            response = client.Execute(Of Faktura)(request)

            'Dim ttText As String = response.Content
            If response IsNot Nothing AndAlso ((response.StatusCode = HttpStatusCode.OK) AndAlso (response.ResponseStatus = ResponseStatus.Completed)) Then
                Dim fFA = loadAllCie(response)
                If fFA IsNot Nothing Then Return fFA
            ElseIf response.ErrorException IsNot Nothing Then
                Dim fFA = loadAllCie(response)
                If fFA IsNot Nothing Then Return fFA
                Throw New Exception("Chyba při načítání odpovědi.", response.ErrorException)
            ElseIf response IsNot Nothing Then
                Throw New Exception("Faktura nenalezena ..")
                'MessageBox.Show(String.Format("Status code is {0} ({1}); response status is {2}", response.StatusCode, response.StatusDescription, response.ResponseStatus), "Žádná faktura", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End If
        Else
            Throw New Exception("Nejste připjeni k interetu nebo server vyfakturuj je nedostupný!! Akci se pokuste opakovat..")
        End If
    End Function

    Public Function GetFaktura(ByVal id As String) As Faktura
        GetFaktura = Nothing
        If String.IsNullOrEmpty(id) Then Exit Function

        Dim request = New RestRequest("invoice/" & id, Method.GET)
        Dim response As IRestResponse
        Dim Out As Integer

        request.AddHeader("Content-Type", "application/json")
        request.RequestFormat = DataFormat.Json

        If InternetGetConnectedState(Out, 0) = True AndAlso My.Computer.Network.Ping("www.vyfakturuj.cz", 1000) Then
            response = client.Execute(request)

            'Dim sTXT As String = response.Content

            If response IsNot Nothing AndAlso ((response.StatusCode = HttpStatusCode.OK) AndAlso (response.ResponseStatus = ResponseStatus.Completed)) Then
                Dim fFA = loadAllCie(response)
                If fFA IsNot Nothing Then Return fFA
            ElseIf response IsNot Nothing Then
                Throw New Exception("Faktura '" & id & "' nenalezena ..")
                'MessageBox.Show(String.Format("Status code is {0} ({1}); response status is {2}", response.StatusCode, response.StatusDescription, response.ResponseStatus), "Žádná faktura", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End If
        Else
            Throw New Exception("Nejste připojeni k internetu nebo server vyfakturuj je nedostupný!! Akci se pokuste opakovat..")
        End If

        Return GetFaktura
    End Function

    Private Function Base64Encode(plainText As String) As String
        Dim plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText)
        Return System.Convert.ToBase64String(plainTextBytes)
    End Function

    Private Shared Function JSONDeserializeFreshDeskCie(repContent As Stream) As Faktura
        Dim rs As Faktura = Nothing
        'Dim test As Object
        Dim serializer As New JsonSerializer()
        Try
            Using sr As New StreamReader(repContent)
                Using jsonTextReader As New JsonTextReader(sr)
                    rs = serializer.Deserialize(Of Faktura)(jsonTextReader)
                End Using
            End Using
        Catch ex As Exception
            Throw ex
        End Try
        Return rs
    End Function

    Private Function loadAllCie(ByVal e As IRestResponse) As Faktura
        Dim rep As Stream = Nothing
        loadAllCie = Nothing
        Dim rs As Faktura
        Try
            rep = New MemoryStream(e.RawBytes())
            'If e.ErrorException IsNot Nothing OrElse e.StatusCode <> Net.HttpStatusCode.OK Then
            '    Dim strError As String = ""
            '    If e.ErrorException IsNot Nothing Then
            '        strError = "Error : " & e.ErrorException.Message & vbCrLf
            '    End If
            '    strError &= "Web Error : " & e.ErrorMessage
            '    strError &= vbCrLf & e.StatusCode.ToString()
            '    MessageBox.Show(strError)
            '    Exit Try
            'End If
            rs = vfapi.JSONDeserializeFreshDeskCie(rep)
            Return rs
        Catch ex As Exception
            Throw ex
        Finally
            If rep IsNot Nothing Then
                rep.Close()
            End If
        End Try
    End Function

    <Serializable()> _
    Friend Class log
        Private m_date As String = ""
        <JsonProperty("date")> _
        Public Property ddate() As String
            Get
                Return m_date
            End Get
            Set(value As String)
                m_date = value
            End Set
        End Property

        Private m_text As String = ""
        <JsonProperty("text")> _
        Public Property text() As String
            Get
                Return m_text
            End Get
            Set(value As String)
                m_text = value
            End Set
        End Property
    End Class

    'Položky faktury
    <Serializable()> _
    Friend Class items
        Private m_quantity As Double = 1
        ''' <summary>
        ''' Množství
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí Množství</returns>
        ''' <remarks></remarks>
        Public Property quantity() As Double
            Get
                Return m_quantity
            End Get
            Set(value As Double)
                m_quantity = value
            End Set
        End Property

        Private m_unit As String = "ks"
        ''' <summary>
        ''' Množstevní jednotka (ks, kg, ...)
        ''' Práva: R/W
        ''' string10
        ''' </summary>
        '''<returns>Vrátí Množstevní jednotka (ks, kg, ...)</returns>
        ''' <remarks></remarks>
        Public Property unit() As String
            Get
                Return m_unit
            End Get
            Set(value As String)
                m_unit = value
            End Set
        End Property

        Private m_text As String = ""
        ''' <summary>
        ''' Text položky
        ''' Práva: R/W
        ''' string255
        ''' </summary>
        '''<returns>Vrátí Text položky</returns>
        ''' <remarks></remarks>
        Public Property text() As String
            Get
                Return m_text
            End Get
            Set(value As String)
                m_text = value
            End Set
        End Property

        Private m_unit_price As Double = 0
        ''' <summary>
        ''' Cena položky
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí Cena položky</returns>
        ''' <remarks></remarks>
        Public Property unit_price() As Double
            Get
                Return m_unit_price
            End Get
            Set(value As Double)
                m_unit_price = value
            End Set
        End Property

        Private m_vat_rate As Double = 0
        ''' <summary>
        ''' Sazba DPH (%)
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí Sazba DPH (%)</returns>
        ''' <remarks></remarks>
        Public Property vat_rate() As Double
            Get
                Return m_vat_rate
            End Get
            Set(value As Double)
                m_vat_rate = value
            End Set
        End Property

        Private m_vat As Double = 0
        ''' <summary>
        ''' Daň
        ''' Práva: R
        ''' </summary>
        '''<returns>Vrátí Daň</returns>
        ''' <remarks></remarks>
        Public Property vat() As Double
            Get
                Return m_vat
            End Get
            Set(value As Double)
                m_vat = value
            End Set
        End Property

        Private m_total As Double = 0
        ''' <summary>
        ''' Částka k úhradě
        ''' Práva: R
        ''' </summary>
        '''<returns>Vrátí Částka k úhradě</returns>
        ''' <remarks></remarks>
        Public Property total() As Double
            Get
                Return m_total
            End Get
            Set(value As Double)
                m_total = value
            End Set
        End Property

        Private m_total_without_vat As Double = 0
        ''' <summary>
        ''' Částka k úhradě bez DPH
        ''' Práva: R
        ''' </summary>
        '''<returns>Vrátí Částka k úhradě bez DPH</returns>
        ''' <remarks></remarks>
        Public Property total_without_vat() As Double
            Get
                Return m_total_without_vat
            End Get
            Set(value As Double)
                m_total_without_vat = value
            End Set
        End Property
    End Class

    'Daně
    <Serializable()> _
    Friend Class vats
        Private m_vat_rate As Double = 0
        ''' <summary>
        ''' Sazba DPH (%)
        ''' Práva: R
        ''' </summary>
        '''<returns>Vrátí Sazba DPH (%)</returns>
        ''' <remarks></remarks>
        Public Property vat_rate() As Double
            Get
                Return m_vat_rate
            End Get
            Set(value As Double)
                m_vat_rate = value
            End Set
        End Property

        Private m_base As Double = 0
        ''' <summary>
        ''' Základ daně
        ''' Práva: R
        ''' </summary>
        '''<returns>Vrátí Základ daně</returns>
        ''' <remarks></remarks>
        Public Property base() As Double
            Get
                Return m_base
            End Get
            Set(value As Double)
                m_base = value
            End Set
        End Property

        Private m_vat As Double = 0
        ''' <summary>
        ''' Daň
        ''' Práva: R
        ''' </summary>
        '''<returns>Vrátí Daň</returns>
        ''' <remarks></remarks>
        Public Property vat() As Double
            Get
                Return m_vat
            End Get
            Set(value As Double)
                m_vat = value
            End Set
        End Property

        Private m_total As Double = 0
        ''' <summary>
        ''' Celkem s DPH
        ''' Práva: R
        ''' </summary>
        '''<returns>Vrátí Celkem s DPH</returns>
        ''' <remarks></remarks>
        Public Property total() As Double
            Get
                Return m_total
            End Get
            Set(value As Double)
                m_total = value
            End Set
        End Property
    End Class

    'EET
    <Serializable()> _
    Friend Class eet_data
        Private m_fik As String = ""
        ''' <summary>
        ''' FIK kód
        ''' </summary>
        '''<returns>Vrátí FIK kód</returns>
        ''' <remarks>Práva: R</remarks>
        Public Property fik() As String
            Get
                Return m_fik
            End Get
            Set(value As String)
                m_fik = value
            End Set
        End Property

        Private m_bkp As String = ""
        ''' <summary>
        ''' BKP kód
        ''' </summary>
        '''<returns>Vrátí BKP kód</returns>
        ''' <remarks>Práva: R</remarks>
        Public Property bkp() As String
            Get
                Return m_bkp
            End Get
            Set(value As String)
                m_bkp = value
            End Set
        End Property

        Private m_pkp As String = ""
        ''' <summary>
        ''' PKP kód
        ''' </summary>
        '''<returns>Vrátí PKP kód</returns>
        ''' <remarks>Práva: R</remarks>
        Public Property pkp() As String
            Get
                Return m_pkp
            End Get
            Set(value As String)
                m_pkp = value
            End Set
        End Property

        Private m_id_provoz As String = ""
        ''' <summary>
        ''' ID provozovny
        ''' </summary>
        '''<returns>Vrátí ID provozovny</returns>
        ''' <remarks>Práva: R</remarks>
        Public Property id_provoz() As String
            Get
                Return m_id_provoz
            End Get
            Set(value As String)
                m_id_provoz = value
            End Set
        End Property

        Private m_id_pokl As String = ""
        ''' <summary>
        ''' ID pokladny
        ''' </summary>
        '''<returns>Vrátí ID pokladny</returns>
        ''' <remarks>Práva: R</remarks>
        Public Property id_pokl() As String
            Get
                Return m_id_pokl
            End Get
            Set(value As String)
                m_id_pokl = value
            End Set
        End Property

        Private m_dat_trzby As String = ""
        ''' <summary>
        ''' Datum tržby
        ''' </summary>
        '''<returns>Vrátí Datum tržby</returns>
        ''' <remarks>Práva: R</remarks>
        Public Property dat_trzby() As String
            Get
                Return m_dat_trzby
            End Get
            Set(value As String)
                m_dat_trzby = value
            End Set
        End Property

        Private m_rezim As String = ""
        ''' <summary>
        ''' Režim 0 - běžný, 1 - zjednodušený
        ''' </summary>
        '''<returns>Vrátí Režim 0 - běžný, 1 - zjednodušený</returns>
        ''' <remarks>Práva: R</remarks>
        Public Property rezim() As String
            Get
                Return m_rezim
            End Get
            Set(value As String)
                m_rezim = value
            End Set
        End Property
    End Class

    'Faktura
    <Serializable()> _
    Friend Class Faktura

        Private m_id As Integer
        ''' <summary>
        ''' ID dokumentu
        ''' Práva: R
        ''' </summary>
        '''<returns>Vrátí ID dokumentu</returns>
        ''' <remarks></remarks>
        <JsonProperty("id")>
        Public Property id() As Integer
            Get
                Return m_id
            End Get
            Set(value As Integer)
                m_id = value
            End Set
        End Property

        Private m_id_customer As Integer
        ''' <summary>
        ''' ID zaznamu v adresáři
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí ID zaznamu v adresáři</returns>
        ''' <remarks></remarks>
        <JsonProperty("id_customer")>
        Public Property id_customer() As Integer
            Get
                Return m_id_customer
            End Get
            Set(value As Integer)
                m_id_customer = value
            End Set
        End Property

        Private m_id_number_series As Integer
        ''' <summary>
        ''' ID číselné řady
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí ID číselné řady</returns>
        ''' <remarks></remarks>
        <JsonProperty("id_number_series")>
        Public Property id_number_series() As Integer
            Get
                Return m_id_number_series
            End Get
            Set(value As Integer)
                m_id_number_series = value
            End Set
        End Property

        Private m_id_payment_method As Integer
        ''' <summary>
        ''' ID platební metody
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí ID platební metody</returns>
        ''' <remarks></remarks>
        Public Property id_payment_method() As Integer
            Get
                Return m_id_payment_method
            End Get
            Set(value As Integer)
                m_id_payment_method = value
            End Set
        End Property

        Private m_id_center As Integer
        ''' <summary>
        ''' ID střediska
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí ID střediska</returns>
        ''' <remarks></remarks>
        Public Property id_center() As Integer
            Get
                Return m_id_center
            End Get
            Set(value As Integer)
                m_id_center = value
            End Set
        End Property

        Private m_id_parent As Integer
        ''' <summary>
        ''' ID nadřazeného dokumentu
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí ID nadřazeného dokumentu</returns>
        ''' <remarks></remarks>
        Public Property id_parent() As Integer
            Get
                Return m_id_parent
            End Get
            Set(value As Integer)
                m_id_parent = value
            End Set
        End Property

        Private m_id_eet_pokl As Integer
        ''' <summary>
        ''' ID EET pokladny
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí ID EET pokladny</returns>
        ''' <remarks></remarks>
        Public Property id_eet_pokl() As Integer
            Get
                Return m_id_eet_pokl
            End Get
            Set(value As Integer)
                m_id_eet_pokl = value
            End Set
        End Property

        Private m_type As Integer
        ''' <summary>
        ''' Typ dokladu - Práva: R/W    
        ''' 1 - Faktura
        ''' 2 - Zálohová faktura
        ''' 4 - Proforma faktura
        ''' 8 - Výzva k platbě
        ''' 16 - Daňový doklad
        ''' 32 - Opravný daňový doklad
        ''' 64 - Příjmový doklad
        ''' 128 - Opravný doklad
        ''' 512 - Objednávka
        ''' </summary>
        '''<returns>Vrátí Typ dokladu</returns>
        ''' <remarks></remarks>
        Public Property type() As Integer
            Get
                Return m_type
            End Get
            Set(value As Integer)
                m_type = value
            End Set
        End Property

        Private m_flags As Integer
        ''' <summary>
        ''' Příznaky - Práva: R  
        ''' 1 - Dokument obsahuje DPH
        ''' 2 - Uhrazeno
        ''' 4 - Odesláno e-mailem zákazníkovi
        ''' 8 - Doklad je stornován
        ''' 16 - Odeslána e-mailem zákazníkovi upomínka
        ''' 32 - Přeplatek
        ''' 64 - Nedoplatek
        ''' 256 - Doklad byl stažen účetním
        ''' </summary>
        '''<returns>Vrátí Příznaky</returns>
        ''' <remarks></remarks>
        Public Property flags() As Integer
            Get
                Return m_flags
            End Get
            Set(value As Integer)
                m_flags = value
            End Set
        End Property

        Private m_tags As Integer
        ''' <summary>
        ''' Použité štítky 
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí Použité štítky</returns>
        ''' <remarks></remarks>
        Public Property tags() As Integer
            Get
                Return m_tags
            End Get
            Set(value As Integer)
                m_tags = value
            End Set
        End Property

        Private m_eet_status As Integer
        ''' <summary>
        ''' Stav EET - Práva: R   
        ''' 1 - Nevstupuje - doklad nesplňuje podmínky pro vstup do EET
        ''' 2 - Neodesílat - při tisku dokladu nedojde k odeslání do EET
        ''' 4 - K odeslání - doklad je určen k odeslání do EET
        ''' 8 - Externě - záznam o tržbě evidovalo jiné zařízení
        ''' 16 - ODESLÁNO - doklad by odeslán do EET.
        ''' 32 - CHYBA - nastala chyba při odeslání.
        ''' </summary>
        '''<returns>Vrátí ID EET pokladny</returns>
        ''' <remarks></remarks>
        Public Property eet_status() As Integer
            Get
                Return m_eet_status
            End Get
            Set(value As Integer)
                m_eet_status = value
            End Set
        End Property

        Private m_eet_data As eet_data
        ''' <summary>
        ''' EET data - array - Práva: R  
        ''' fik - FIK kód
        ''' bkp - BKP kód
        ''' 4 - K odeslání - doklad je určen k odeslání do EET
        ''' 8 - Externě - záznam o tržbě evidovalo jiné zařízení
        ''' 16 - ODESLÁNO - doklad by odeslán do EET.
        ''' 32 - CHYBA - nastala chyba při odeslání.
        ''' </summary>
        '''<returns>Vrátí EET data</returns>
        ''' <remarks></remarks>
        Public Property eet_data() As eet_data
            Get
                Return m_eet_data
            End Get
            Set(value As eet_data)
                m_eet_data = value
            End Set
        End Property

        Private m_number As String = ""
        ''' <summary>
        ''' Číslo dokumentu 
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí Číslo dokumentu</returns>
        ''' <remarks></remarks>
        Public Property number() As String
            Get
                Return m_number
            End Get
            Set(value As String)
                m_number = value
            End Set
        End Property

        Private m_date_created As String = ""
        ''' <summary>
        ''' Datum vytvoření
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí Datum vytvoření</returns>
        ''' <remarks></remarks>
        Public Property date_created() As String
            Get
                Return m_date_created
            End Get
            Set(value As String)
                m_date_created = value
            End Set
        End Property

        Private m_date_due As String = ""
        ''' <summary>
        ''' Datum splatnosti
        ''' Práva: R
        ''' </summary>
        '''<returns>Vrátí Datum splatnosti</returns>
        ''' <remarks></remarks>
        Public Property date_due() As String
            Get
                Return m_date_due
            End Get
            Set(value As String)
                m_date_due = value
            End Set
        End Property

        Private m_date_taxable_supply As String = ""
        ''' <summary>
        ''' Datum zdaňitelného plnění
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí Datum zdaňitelného plnění</returns>
        ''' <remarks></remarks>
        Public Property date_taxable_supply() As String
            Get
                Return m_date_taxable_supply
            End Get
            Set(value As String)
                m_date_taxable_supply = value
            End Set
        End Property

        Private m_date_paid As String = ""
        ''' <summary>
        ''' Datum platby
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí Datum platby</returns>
        ''' <remarks></remarks>
        Public Property date_paid() As String
            Get
                Return m_date_paid
            End Get
            Set(value As String)
                m_date_paid = value
            End Set
        End Property

        Private m_date_reminder As String = ""
        ''' <summary>
        ''' Datum zaslání připomínky
        ''' Práva: R
        ''' </summary>
        '''<returns>Vrátí Datum zaslání připomínky</returns>
        ''' <remarks></remarks>
        Public Property date_reminder() As String
            Get
                Return m_date_reminder
            End Get
            Set(value As String)
                m_date_reminder = value
            End Set
        End Property

        Private m_days_due As Integer = 10
        ''' <summary>
        ''' Splatnost (dny)
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí Splatnost (dny)</returns>
        ''' <remarks></remarks>
        Public Property days_due() As Integer
            Get
                Return m_days_due
            End Get
            Set(value As Integer)
                m_days_due = value
            End Set
        End Property

        Private m_days_reminder As Integer = 3
        ''' <summary>
        ''' Za jak dlouho zaslat připomínku po splatnosti (dny)
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí Za jak dlouho zaslat připomínku po splatnosti (dny)</returns>
        ''' <remarks></remarks>
        Public Property days_reminder() As Integer
            Get
                Return m_days_reminder
            End Get
            Set(value As Integer)
                m_days_reminder = value
            End Set
        End Property

        Private m_supplier_IC As String = ""
        ''' <summary>
        ''' Dodavatel - IČ
        ''' Práva: R/W
        ''' string20
        ''' </summary>
        '''<returns>Vrátí Dodavatel - IČ</returns>
        ''' <remarks></remarks>
        Public Property supplier_IC() As String
            Get
                Return m_supplier_IC
            End Get
            Set(value As String)
                m_supplier_IC = value
            End Set
        End Property

        Private m_supplier_DIC As String = ""
        ''' <summary>
        ''' Dodavatel - DIČ
        ''' Práva: R/W
        ''' string20
        ''' </summary>
        '''<returns>Vrátí Dodavatel - DIČ</returns>
        ''' <remarks></remarks>
        Public Property supplier_DIC() As String
            Get
                Return m_supplier_DIC
            End Get
            Set(value As String)
                m_supplier_DIC = value
            End Set
        End Property

        Private m_supplier_name As String = ""
        ''' <summary>
        ''' Dodavatel - název firmy
        ''' Práva: R/W
        ''' string100
        ''' </summary>
        '''<returns>Vrátí Dodavatel - název firmy</returns>
        ''' <remarks></remarks>
        Public Property supplier_name() As String
            Get
                Return m_supplier_name
            End Get
            Set(value As String)
                m_supplier_name = value
            End Set
        End Property

        Private m_supplier_street As String = ""
        ''' <summary>
        ''' Dodavatel - ulice
        ''' Práva: R/W
        ''' string50
        ''' </summary>
        '''<returns>Vrátí Dodavatel - ulice</returns>
        ''' <remarks></remarks>
        Public Property supplier_street() As String
            Get
                Return m_supplier_street
            End Get
            Set(value As String)
                m_supplier_street = value
            End Set
        End Property

        Private m_supplier_city As String = ""
        ''' <summary>
        ''' Dodavatel - město
        ''' Práva: R/W
        ''' string50
        ''' </summary>
        '''<returns>Vrátí Dodavatel - město</returns>
        ''' <remarks></remarks>
        Public Property supplier_city() As String
            Get
                Return m_supplier_city
            End Get
            Set(value As String)
                m_supplier_city = value
            End Set
        End Property

        Private m_supplier_zip As String = ""
        ''' <summary>
        ''' Dodavatel - PSČ
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí Dodavatel - PSČ</returns>
        ''' <remarks></remarks>
        Public Property supplier_zip() As String
            Get
                Return m_supplier_zip
            End Get
            Set(value As String)
                m_supplier_zip = value
            End Set
        End Property

        Private m_supplier_country As String = ""
        ''' <summary>
        ''' Dodavatel - stát
        ''' Práva: R/W
        ''' string20
        ''' </summary>
        '''<returns>Vrátí Dodavatel - stát</returns>
        ''' <remarks></remarks>
        Public Property supplier_country() As String
            Get
                Return m_supplier_country
            End Get
            Set(value As String)
                m_supplier_country = value
            End Set
        End Property

        Private m_supplier_contact_name As String = ""
        ''' <summary>
        ''' Dodavatel - kontaktní osoba
        ''' Práva: R/W
        ''' string30
        ''' </summary>
        '''<returns>Vrátí Dodavatel - kontaktní osoba</returns>
        ''' <remarks></remarks>
        Public Property supplier_contact_name() As String
            Get
                Return m_supplier_contact_name
            End Get
            Set(value As String)
                m_supplier_contact_name = value
            End Set
        End Property

        Private m_supplier_contact_tel As String = ""
        ''' <summary>
        ''' Dodavatel - kontaktní telefon
        ''' Práva: R/W
        ''' string30
        ''' </summary>
        '''<returns>Vrátí Dodavatel - kontaktní telefon</returns>
        ''' <remarks></remarks>
        Public Property supplier_contact_tel() As String
            Get
                Return m_supplier_contact_tel
            End Get
            Set(value As String)
                m_supplier_contact_tel = value
            End Set
        End Property

        Private m_supplier_contact_mail As String = ""
        ''' <summary>
        ''' Dodavatel - kontaktní mail
        ''' Práva: R/W
        ''' string30
        ''' </summary>
        '''<returns>Vrátí Dodavatel - kontaktní mail</returns>
        ''' <remarks></remarks>
        Public Property supplier_contact_mail() As String
            Get
                Return m_supplier_contact_mail
            End Get
            Set(value As String)
                m_supplier_contact_mail = value
            End Set
        End Property

        Private m_supplier_contact_web As String = ""
        ''' <summary>
        ''' Dodavatel - web
        ''' Práva: R/W
        ''' string100
        ''' </summary>
        '''<returns>Vrátí Dodavatel - web</returns>
        ''' <remarks></remarks>
        Public Property supplier_contact_web() As String
            Get
                Return m_supplier_contact_web
            End Get
            Set(value As String)
                m_supplier_contact_web = value
            End Set
        End Property

        Private m_customer_IC As String = ""
        ''' <summary>
        ''' Odběratel - IČ
        ''' Práva: R/W
        ''' string20
        ''' </summary>
        '''<returns>Vrátí Odběratel - IČ</returns>
        ''' <remarks></remarks>
        Public Property customer_IC() As String
            Get
                Return m_customer_IC
            End Get
            Set(value As String)
                m_customer_IC = value
            End Set
        End Property

        Private m_customer_DIC As String = ""
        ''' <summary>
        ''' Odběratel - DIČ
        ''' Práva: R/W
        ''' string20
        ''' </summary>
        '''<returns>Vrátí Odběratel - DIČ</returns>
        ''' <remarks></remarks>
        Public Property customer_DIC() As String
            Get
                Return m_customer_DIC
            End Get
            Set(value As String)
                m_customer_DIC = value
            End Set
        End Property

        Private m_customer_name As String = ""
        ''' <summary>
        ''' Odběratel - název firmy
        ''' Práva: R/W
        ''' string100
        ''' </summary>
        '''<returns>Vrátí Odběratel - název firmy</returns>
        ''' <remarks></remarks>
        Public Property customer_name() As String
            Get
                Return m_customer_name
            End Get
            Set(value As String)
                m_customer_name = value
            End Set
        End Property

        Private m_customer_street As String = ""
        ''' <summary>
        ''' Odběratel - ulice
        ''' Práva: R/W
        ''' string50
        ''' </summary>
        '''<returns>Vrátí Odběratel - ulice</returns>
        ''' <remarks></remarks>
        Public Property customer_street() As String
            Get
                Return m_customer_street
            End Get
            Set(value As String)
                m_customer_street = value
            End Set
        End Property

        Private m_customer_city As String = ""
        ''' <summary>
        ''' Odběratel - město
        ''' Práva: R/W
        ''' string50
        ''' </summary>
        '''<returns>Vrátí Odběratel - město</returns>
        ''' <remarks></remarks>
        Public Property customer_city() As String
            Get
                Return m_customer_city
            End Get
            Set(value As String)
                m_customer_city = value
            End Set
        End Property

        Private m_customer_zip As String = ""
        ''' <summary>
        ''' Odběratel - PSČ
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí Odběratel - PSČ</returns>
        ''' <remarks></remarks>
        Public Property customer_zip() As String
            Get
                Return m_customer_zip
            End Get
            Set(value As String)
                m_customer_zip = value
            End Set
        End Property

        Private m_customer_country As String = ""
        ''' <summary>
        ''' Odběratel - stát
        ''' Práva: R/W
        ''' string20
        ''' </summary>
        '''<returns>Vrátí Odběratel - stát</returns>
        ''' <remarks></remarks>
        Public Property customer_country() As String
            Get
                Return m_customer_country
            End Get
            Set(value As String)
                m_customer_country = value
            End Set
        End Property

        Private m_bank_account_number As String = ""
        ''' <summary>
        ''' Číslo bankovního účtu
        ''' Práva: R/W
        ''' string20
        ''' </summary>
        '''<returns>Vrátí Číslo bankovního účtu</returns>
        ''' <remarks></remarks>
        Public Property bank_account_number() As String
            Get
                Return m_bank_account_number
            End Get
            Set(value As String)
                m_bank_account_number = value
            End Set
        End Property

        Private m_bank_IBAN As String = ""
        ''' <summary>
        ''' Mezinárodní bankovní číslo (IBAN)
        ''' Práva: R/W
        ''' string60
        ''' </summary>
        '''<returns>Vrátí Mezinárodní bankovní číslo (IBAN)</returns>
        ''' <remarks></remarks>
        Public Property bank_IBAN() As String
            Get
                Return m_bank_IBAN
            End Get
            Set(value As String)
                m_bank_IBAN = value
            End Set
        End Property

        Private m_bank_BIC As String = ""
        ''' <summary>
        ''' Mezinárodní kód banky (SWIFT/BIC)
        ''' Práva: R/W
        ''' string20
        ''' </summary>
        '''<returns>Vrátí Mezinárodní kód banky (SWIFT/BIC)</returns>
        ''' <remarks></remarks>
        Public Property bank_BIC() As String
            Get
                Return m_bank_BIC
            End Get
            Set(value As String)
                m_bank_BIC = value
            End Set
        End Property

        Private m_payment_method As Integer = 2
        ''' <summary>
        ''' Způsob platby - Práva: R/W
        ''' 1 - Bankovní převod
        ''' 2 - Hotovost
        ''' 4 - Dobírka
        ''' 8 - Kartou online
        ''' 128 - PayPal
        ''' 16 - Záloha
        ''' 32 - Zápočet
        ''' </summary>
        '''<returns>Vrátí Způsob platby</returns>
        ''' <remarks></remarks>
        Public Property payment_method() As Integer
            Get
                Return m_payment_method
            End Get
            Set(value As Integer)
                m_payment_method = value
            End Set
        End Property

        Private m_calculate_vat As Integer = 5
        ''' <summary>
        ''' Výpočet DPH - Práva: R/W
        ''' 1 - Položky jsou uvedeny jako zákald daně
        ''' 2 - Položky mají koncovou cenu s DPH
        ''' 3 - DPH je účtováno ve speciálním režimu
        ''' 4 - DPH je v režimu prenesené daňové povinnosti
        ''' 5 - Neplátce DPH
        ''' </summary>
        '''<returns>Vrátí Výpočet DPH</returns>
        ''' <remarks></remarks>
        Public Property calculate_vat() As Integer
            Get
                Return m_calculate_vat
            End Get
            Set(value As Integer)
                m_calculate_vat = value
            End Set
        End Property

        Private m_round_invoice As Integer = 1
        ''' <summary>
        ''' Zaokrouhlení dokumentu - Práva: R/W
        ''' 1 - Nezaokrouhlovat
        ''' 2 - Háleřové vyrovnání
        ''' 3 - Zaokrouhlit DPH
        ''' </summary>
        '''<returns>Vrátí Zaokrouhlení dokumentu</returns>
        ''' <remarks></remarks>
        Public Property round_invoice() As Integer
            Get
                Return m_round_invoice
            End Get
            Set(value As Integer)
                m_round_invoice = value
            End Set
        End Property

        Private m_order_number As String = ""
        ''' <summary>
        ''' ID objednávky
        ''' Práva: R/W
        ''' string255
        ''' </summary>
        '''<returns>Vrátí ID objednávky</returns>
        ''' <remarks></remarks>
        Public Property order_number() As String
            Get
                Return m_order_number
            End Get
            Set(value As String)
                m_order_number = value
            End Set
        End Property

        Private m_text_under_subscriber As String = ""
        ''' <summary>
        ''' Text pod dodavatelem
        ''' Práva: R/W
        ''' string255
        ''' </summary>
        '''<returns>Vrátí Text pod dodavatelem</returns>
        ''' <remarks></remarks>
        Public Property text_under_subscriber() As String
            Get
                Return m_text_under_subscriber
            End Get
            Set(value As String)
                m_text_under_subscriber = value
            End Set
        End Property

        Private m_text_under_customer As String = ""
        ''' <summary>
        ''' Text pod odběratelem
        ''' Práva: R/W
        ''' string255
        ''' </summary>
        '''<returns>Vrátí Text pod odběratelem</returns>
        ''' <remarks></remarks>
        Public Property text_under_customer() As String
            Get
                Return m_text_under_customer
            End Get
            Set(value As String)
                m_text_under_customer = value
            End Set
        End Property

        Private m_text_before_items As String = ""
        ''' <summary>
        ''' Text před položkami faktury
        ''' Práva: R/W
        ''' string255
        ''' </summary>
        '''<returns>Vrátí Text před položkami faktury</returns>
        ''' <remarks></remarks>
        Public Property text_before_items() As String
            Get
                Return m_text_before_items
            End Get
            Set(value As String)
                m_text_before_items = value
            End Set
        End Property

        Private m_text_invoice_footer As String = ""
        ''' <summary>
        ''' Text v patičce dokumentu
        ''' Práva: R/W
        ''' string255
        ''' </summary>
        '''<returns>Vrátí Text v patičce dokumentu</returns>
        ''' <remarks></remarks>
        Public Property text_invoice_footer() As String
            Get
                Return m_text_invoice_footer
            End Get
            Set(value As String)
                m_text_invoice_footer = value
            End Set
        End Property

        Private m_mark_already_paid As Boolean = True
        ''' <summary>
        ''' Zobrazit text: Již uhrazeno
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí Zobrazit text: Již uhrazeno</returns>
        ''' <remarks></remarks>
        Public Property mark_already_paid() As Boolean
            Get
                Return m_mark_already_paid
            End Get
            Set(value As Boolean)
                m_mark_already_paid = value
            End Set
        End Property

        Private m_mail_to() As String = {}
        ''' <summary>
        ''' Emailové adresy kam bude (byl) odeslán email
        ''' Práva: R/W
        ''' array string
        ''' </summary>
        '''<returns>Vrátí Emailové adresy kam bude (byl) odeslán email</returns>
        ''' <remarks></remarks>
        Public Property mail_to() As String()
            Get
                Return m_mail_to
            End Get
            Set(value As String())
                m_mail_to = value
            End Set
        End Property

        Private m_language As String = "cs"
        ''' <summary>
        ''' Jazyková mutace faktury - Práva: R/W
        ''' cs - Česky
        ''' sk - Slovensky
        ''' en - Anglicky
        ''' de - Německy
        ''' </summary>
        '''<returns>Vrátí Jazyková mutace faktury</returns>
        ''' <remarks></remarks>
        Public Property language() As String
            Get
                Return m_language
            End Get
            Set(value As String)
                m_language = value
            End Set
        End Property

        Private m_VS As String = ""
        ''' <summary>
        ''' Variabilní symbol
        ''' Práva: R/W
        ''' string10
        ''' </summary>
        '''<returns>Vrátí Variabilní symbol</returns>
        ''' <remarks></remarks>
        Public Property VS() As String
            Get
                Return m_VS
            End Get
            Set(value As String)
                m_VS = value
            End Set
        End Property

        Private m_KS As String = ""
        ''' <summary>
        ''' Konstantní symbol
        ''' Práva: R/W
        ''' string4
        ''' </summary>
        '''<returns>Vrátí Konstantní symbol</returns>
        ''' <remarks></remarks>
        Public Property KS() As String
            Get
                Return m_KS
            End Get
            Set(value As String)
                m_KS = value
            End Set
        End Property

        Private m_SS As String = ""
        ''' <summary>
        ''' Specifický symbol
        ''' Práva: R/W
        ''' string10
        ''' </summary>
        '''<returns>Vrátí Specifický symbol</returns>
        ''' <remarks></remarks>
        Public Property SS() As String
            Get
                Return m_SS
            End Get
            Set(value As String)
                m_SS = value
            End Set
        End Property

        Private m_currency As String = "CZK"
        ''' <summary>
        ''' Měna
        ''' Práva: R/W
        ''' string5
        ''' </summary>
        '''<returns>Vrátí Měna</returns>
        ''' <remarks></remarks>
        Public Property currency() As String
            Get
                Return m_currency
            End Get
            Set(value As String)
                m_currency = value
            End Set
        End Property

        Private m_exchange_rate As String = "1"
        ''' <summary>
        ''' Převodní kurz měny
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí Převodní kurz měny</returns>
        ''' <remarks></remarks>
        Public Property exchange_rate() As String
            Get
                Return m_exchange_rate
            End Get
            Set(value As String)
                m_exchange_rate = value
            End Set
        End Property

        Private m_total As String = "0"
        ''' <summary>
        ''' Celková částka k úhradě
        ''' Práva: R
        ''' </summary>
        '''<returns>Vrátí Celková částka k úhradě</returns>
        ''' <remarks></remarks>
        Public Property total() As String
            Get
                Return m_total
            End Get
            Set(value As String)
                m_total = value
            End Set
        End Property

        Private m_total_without_vat As String = "0"
        ''' <summary>
        ''' Celková částka k úhradě bez DPH
        ''' Práva: R
        ''' </summary>
        '''<returns>Vrátí Celková částka k úhradě bez DPH</returns>
        ''' <remarks></remarks>
        Public Property total_without_vat() As Double
            Get
                Return m_total_without_vat
            End Get
            Set(value As Double)
                m_total_without_vat = value
            End Set
        End Property

        Private m_webhook_paid As String = ""
        ''' <summary>
        ''' URL která bude zavolána při úhradě dokumentu
        ''' Práva: R/W
        ''' string255
        ''' </summary>
        '''<returns>Vrátí URL která bude zavolána při úhradě dokumentu</returns>
        ''' <remarks></remarks>
        Public Property webhook_paid() As String
            Get
                Return m_webhook_paid
            End Get
            Set(value As String)
                m_webhook_paid = value
            End Set
        End Property

        Private m_url_public_webpage As String = ""
        ''' <summary>
        ''' URL s veřejnou stránkou dokumentu
        ''' Práva: R
        ''' string255
        ''' </summary>
        '''<returns>Vrátí URL s veřejnou stránkou dokumentu</returns>
        ''' <remarks></remarks>
        Public Property url_public_webpage() As String
            Get
                Return m_url_public_webpage
            End Get
            Set(value As String)
                m_url_public_webpage = value
            End Set
        End Property

        Private m_url_download_pdf As String = ""
        ''' <summary>
        ''' URL pro stažení dokumentu v PDF
        ''' Práva: R
        ''' string255
        ''' </summary>
        '''<returns>Vrátí URL pro stažení dokumentu v PDF</returns>
        ''' <remarks></remarks>
        Public Property url_download_pdf() As String
            Get
                Return m_url_download_pdf
            End Get
            Set(value As String)
                m_url_download_pdf = value
            End Set
        End Property

        Private m_url_download_pdf_no_stamp As String = ""
        ''' <summary>
        ''' URL pro stažení dokumentu v PDF bez razítka
        ''' Práva: R
        ''' string255
        ''' </summary>
        '''<returns>Vrátí URL pro stažení dokumentu v PDF bez razítka</returns>
        ''' <remarks></remarks>
        Public Property url_download_pdf_no_stamp() As String
            Get
                Return m_url_download_pdf_no_stamp
            End Get
            Set(value As String)
                m_url_download_pdf_no_stamp = value
            End Set
        End Property

        Private m_disable_automated_mails As Boolean = False
        ''' <summary>
        ''' Vypne automatické zasílání e-mailu (uhrazení, upomínka)
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí Vypne automatické zasílání e-mailu (uhrazení, upomínka)</returns>
        ''' <remarks></remarks>
        Public Property disable_automated_mails() As Boolean
            Get
                Return m_disable_automated_mails
            End Get
            Set(value As Boolean)
                m_disable_automated_mails = value
            End Set
        End Property

        Private m_storno As Boolean = False
        ''' <summary>
        ''' Stornuje doklad
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí Stornuje doklad</returns>
        ''' <remarks></remarks>
        Public Property storno() As Boolean
            Get
                Return m_storno
            End Get
            Set(value As Boolean)
                m_storno = value
            End Set
        End Property

        Private m_need_attention As Boolean = False
        ''' <summary>
        ''' Objednávka vyžaduje pozornost
        ''' Práva: R/W
        ''' </summary>
        '''<returns>Vrátí Objednávka vyžaduje pozornost</returns>
        ''' <remarks></remarks>
        Public Property need_attention() As Boolean
            Get
                Return m_need_attention
            End Get
            Set(value As Boolean)
                m_need_attention = value
            End Set
        End Property

        Private m_log() As log = {}
        ''' <summary>
        ''' Položky dokumentu - array
        ''' Práva: R
        ''' </summary>
        '''<returns>Vrátí Položky dokumentu</returns>
        ''' <remarks></remarks>
        Public Property log() As log()
            Get
                Return m_log
            End Get
            Set(value As log())
                m_log = value
            End Set
        End Property

        Private m_X_1 As String = ""
        ''' <summary>
        ''' Volitelná hodnota 1 (např. ID dokumentu ve Vašem systému)
        ''' Práva: R/W
        ''' string100
        ''' </summary>
        '''<returns>Vrátí Volitelná hodnota 1 (např. ID dokumentu ve Vašem systému)</returns>
        ''' <remarks></remarks>
        Public Property X_1() As String
            Get
                Return m_X_1
            End Get
            Set(value As String)
                m_X_1 = value
            End Set
        End Property

        Private m_X_2 As String = ""
        ''' <summary>
        ''' Volitelná hodnota 2 (např. ID kontaktu ve Vašem systému)
        ''' Práva: R/W
        ''' string100
        ''' </summary>
        '''<returns>Vrátí Volitelná hodnota 2 (např. ID kontaktu ve Vašem systému)</returns>
        ''' <remarks></remarks>
        Public Property X_2() As String
            Get
                Return m_X_2
            End Get
            Set(value As String)
                m_X_2 = value
            End Set
        End Property

        Private m_X_3 As String = ""
        ''' <summary>
        ''' Volitelná hodnota 3 (např. ID pokladna ve Vašem systému)
        ''' Práva: R/W
        ''' string100
        ''' </summary>
        '''<returns>Vrátí Volitelná hodnota 2 (např. ID kontaktu ve Vašem systému)</returns>
        ''' <remarks></remarks>
        Public Property X_3() As String
            Get
                Return m_X_3
            End Get
            Set(value As String)
                m_X_3 = value
            End Set
        End Property

        Private m_action_after_create_send_to_eet As Boolean = False
        ''' <summary>
        ''' Akce: po vytvoření dokladu, zaslat automaticky do EET.
        ''' Práva: W
        ''' </summary>
        '''<returns>Akce: po vytvoření dokladu, zaslat automaticky do EET.</returns>
        ''' <remarks></remarks>
        Public Property action_after_create_send_to_eet() As Boolean
            Get
                Return m_action_after_create_send_to_eet
            End Get
            Set(value As Boolean)
                m_action_after_create_send_to_eet = value
            End Set
        End Property

        Private m_items() As items = {}
        ''' <summary>
        ''' Položky dokumentu - array
        ''' </summary>
        '''<returns>Vrátí Položky dokumentu</returns>
        ''' <remarks>Práva: R/W</remarks>
        Public Property items() As items()
            Get
                Return m_items
            End Get
            Set(value As items())
                m_items = value
            End Set
        End Property

        Private m_related_documents() As String = {}
        ''' <summary>
        ''' Položky dokumentu - array
        ''' </summary>
        '''<returns>Vrátí Položky dokumentu</returns>
        ''' <remarks>Práva: R/W</remarks>
        Public Property related_documents() As String()
            Get
                Return m_related_documents
            End Get
            Set(value As String())
                m_related_documents = value
            End Set
        End Property

        Private m_vats() As vats = {}
        ''' <summary>
        ''' EET data - array - Práva: R
        ''' vat_rate
        ''' base
        ''' vat
        ''' total
        ''' </summary>
        '''<returns>Vrátí EET data</returns>
        ''' <remarks></remarks>
        Public Property vats() As vats()
            Get
                Return m_vats
            End Get
            Set(value As vats())
                m_vats = value
            End Set
        End Property

    End Class

End Class
