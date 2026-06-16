Imports System.Drawing.Printing
Imports System.IO
Imports System.Math
' Copyright Erkki Latvio & Leo Peltola

Module Module1
    Public PrintPageSettings1 As New PageSettings
    Public PrintPageSettings2 As New PageSettings ' pöytälapuille
    Public testaus As Boolean = False ' vain testauksessa
    Public parienVariEhto As Boolean = True ' parien määrityksessä värit vaikuttavat
    Public tilannePaivitetty As Boolean = False ' saa arvon True, kun uusi tilannetaulukko tehdään
    Public vm As Integer ' apumuuttuja värien määräämisessä
    Public Form6Param As String ' apumuuttuja
    Public Form7Param As String ' apumuuttuja
    Public Form8Param As Integer ' apumuuttuja
    Public seloList() As String
    Public seloListaOts As String
    Public seloRajat(50) As Integer
    Public seloKerroin As Integer = 100
    Public pelaajaList(200) As String       ' enintään 200 pelaajaa 
    Public hakemisto As String
    Public ohjelmaHakemisto As String
    Public tunnus As String
    Public otsikko As String
    Public kiermax As String
    Public kierrosMaara As Integer
    Public tiedPolku As String
    Public pelaajaLkm As Integer
    Public aktiiviLkm As Integer
    Public ryhLkm As Integer ' palkintoryhmien määrä
    Public kierrosLaskuri As Integer
    Public muuntoKerroin(16) As Single ' selojen laskennassa käytettävät korjauskertoimet

    Public KP As Integer ' parien lukumäärä
    Public TAPA As Integer = 1 ' pisteiden talletustapa (1: 10, 5 tai 0)
    ' ( jos TAPA=-1, pisteet talletetaan samalla tavalla (10, 5 0),
    '  mutta TeePisteet-rutiini muutta voiton 3:ksi pisteeksi, tasapelin 1:ksi ja häviön 0:ksi)  
    Public KIRJ As String = "0ABCDEFGHIJKLMN" ' ryhmien nimet

    Public LAJ(200) As Integer       ' Aktiivit: SELO/NRO järjestys  SSSSNNN
    Public KPARIT(200, 2) As Integer ' Kierroksen peliparien nrot, 0-sarakkeella valkea
    '                                  1-sarakkeella musta, 2-sarakkeella parin numero
    '                                  X21: määräämisjärjestyksessä, muuten pöytäjärjestys
    Public LAJNR(200, 2) As Integer  ' Peliparien järj. nrot lajittelussa LAJ, ylitys
    Public RHRIV(12) As Integer      ' Ryhmien kirjainrivin nro
    Public VARIT(200, 4) As Integer  ' sarake 0: valkeat kpl, sarake 1: mustat kpl, sarake 2: väripotenssi
    '                                  sarake 3: tieto onko pelaaja kierroksella mukana 
    '                                  sarake 4: pelaamattomien pelien lukumäärä
    Public stringToPrint As String

    Public Structure TYP6     ' PERUS pelaajat selojärjestyksessä 
        Dim NRO As Integer    ' pelaajan numero kilpailussa
        <VBFixedString(25)> Dim NIMI As String  ' Nimi
        <VBFixedString(8)> Dim SEURA As String  ' Seura
        Dim SELO As Integer   ' Selo
        Dim LKM As Integer    ' selopelien lukumäärä
        Dim SRNRO As Integer  ' pelaajan selo-rekisterinumero (selolistalta) 
        Dim RYHNRO As Integer ' pelaajan ryhmänumero
    End Structure
    Public pe As TYP6
    Public Sub AlustaTYP6()
        pe.NRO = 0
        pe.NIMI = StrDup(25, " ")
        pe.SEURA = StrDup(8, " ")
        pe.SELO = 0
        pe.LKM = 0
        pe.SRNRO = 0
        pe.RYHNRO = 0
    End Sub

    Public Structure TYP8     ' TULOS pelaajien tulokset kultakin kierrokselta
        '  Tietueen avain: 1000*kierroksen nro + pelaajan nro:
        Public KR As Integer  ' Varalla
        Public TYYPPI As Integer ' 1=aktiivi, 0 ja 2=passiivi, 3=huilaus kierroksella
        Public VARI As Integer   ' 0=ei väriä 1=valkea 2=musta
        Public VASTUS As Integer ' Vastustajan nro
        Public TULOS As Integer  ' 10 * todellinen tulos
        ' Pelaamatta  -1:V+  -2:L-  -3:X=  -4:Y-- -10 tulos antamatta
        Public SELO As Integer   ' Muuttuva selo ennen kierrosta
    End Structure
    Public tu As TYP8
    Public Sub AlustaTYP8()
        tu.KR = 0
        tu.TYYPPI = 1 ' alustuksessa merkataan pelaaja aktiiviksi joka kierrokselle
        tu.VARI = 0
        tu.VASTUS = 0
        tu.TULOS = 0
        tu.SELO = 0
    End Sub


    Public Structure TYP9  ' PARIT
        ' TIETUEEN AVAIN: 1000*KIERROKSEN NRO + PARIN NRO
        Public KIERROS As Integer  'kierros
        Public NRO As Integer 'parin nro
        Public VALKEA As Integer 'valkean nro
        Public MUSTA As Integer 'mustan nro
    End Structure
    Public pa As TYP9  'PARIT KIERROKSITTAIN
    Public Sub AlustaTYP9()
        pa.KIERROS = 0
        pa.NRO = 0
        pa.VALKEA = 0
        pa.MUSTA = 0
    End Sub

    Public Structure TYP10          ' KILPAILU.ttt
        '  seuraavien tietojen avain on kierroksen nro:
        Dim KR As Integer       ' voimassaoleva parien määrityksessä viety kierroksen nro
        Dim VARAP As Integer    ' varapelaajan nro
        Dim LKM As Integer      ' pelaajien määrä pelaajaLkm
        Dim RYHMAERO As Integer ' ryhmien erotteluun käytetty luku
        Dim KERROIN As Integer  ' muuttuvien selojen laskennan kerroin
        Dim TAPA As Integer     ' pistelasku  1, -1 tai max-pistemäärä
        Dim VARIESTO As Integer ' 1: värit ei estä parin määräämistä
        Dim AKTIIVIT As Integer ' aktiivipelaajien luku kierroksittain
        Dim LASKU As Integer    ' luku 1 kun kierroksen muuttuvat selo on laskettu
        Dim KPLKM As Integer    ' kierroksen parien lukumäärä
        <VBFixedString(3)> Dim TUNNUS As String    ' turnauksen kolmimerkkinen tunnus
        Dim KIERM As Integer    ' kilpailussa pelattava kierrosmäärä
        Dim VERTLAS As Integer  ' tieto kierroksen vertailujen laskemisesta (1 jos vertailu tehtynä)
        <VBFixedString(50)> Dim KILPNIMI As String  ' kilpailun otsikon tiedot (nimi, ajankohta)
    End Structure
    Public ki As TYP10
    Public Sub AlustaTYP10()
        ki.KR = 0
        ki.VARAP = 0
        ki.LKM = 0
        ki.RYHMAERO = 0
        ki.KERROIN = 0
        ki.TAPA = 0
        ki.VARIESTO = 0
        ki.AKTIIVIT = 0
        ki.LASKU = 0
        ki.KPLKM = 0
        ki.TUNNUS = " "
        ki.KIERM = 0
        ki.VERTLAS = 0
        ki.KILPNIMI = Space(50)
    End Sub

    Public Structure TYP11  ' VERTAUS  Selitykset Vertailut-ohjelmassa
        ' Tietueen avain = pelaajan nro
        Public NORPIS As Single  ' pisteet pelatuista peleistä * 100
        Public SELO As Integer   ' selo ennen kisaa
        Public PRYHMA As Integer ' palkintoryhmä
        Public TULOS As Single   ' pistetulos
        Public SUORL As Single   ' suoritusluku
        Public SELOKA As Single  ' vastustajien selokeskiarvo
        Public TULODO As Single  ' tulos-odotustulos
        Public USELO As Integer  ' uusi epävirallinen selo
        Public MUSELO As Integer ' muuttuva selo kisan lopussa
        Public BUCH As Single    ' Buchholz-vertailu
        Public PELIT As Integer  ' pelatut pelit
        Public UUDETS As Integer ' selo uusien pelaajien kaavalla 
        Public SELONR As Integer ' palknron selojärjestysnro
        Public PALKNR As Integer ' selonron palkintojäjestysnro
    End Structure
    Public VE As TYP11
    Public Sub AlustaTYP11()
        VE.NORPIS = 0
        VE.SELO = 0
        VE.PRYHMA = 0
        VE.TULOS = 0.0
        VE.SUORL = 0.0
        VE.SELOKA = 0.0
        VE.TULODO = 0.0
        VE.USELO = 0
        VE.MUSELO = 0
        VE.BUCH = 0.0
        VE.PELIT = 0
        VE.UUDETS = 0
        VE.SELONR = 0
        VE.PALKNR = 0
    End Sub


    ' Pelaajat-tiedoston lukeminen
    Public Sub LuePelaajat()
        ' Luetaan pelaajatiedoston tiedot taulukkomuuttujan pelaajaList
        ' pelaajaLkm globaali muuttuja (tyyppi integer)
        tiedPolku = hakemisto & "\PELAAJAT." & tunnus
        pelaajaLkm = 0
        If Not File.Exists(tiedPolku) Then
            MsgBox("Pelaajatiedostoa ei ole vielä olemassa", )
        Else
            FileOpen(1, tiedPolku, OpenMode.Input)
            Do Until EOF(1)
                pelaajaList(pelaajaLkm) = LineInput(1)
                pelaajaLkm = pelaajaLkm + 1
            Loop
            FileClose(1)
        End If
    End Sub

    ' Pelaajat-tiedot  PERUS-tieostosta
    Public Sub LuePelaajatPerus()
        Dim i As Integer
        Dim rivi As String
        'Koska turnauksen tiedostot on jo alustettu, haetaan pelaajalukumäärä 
        ' KILPAILU-tiedostosta.
        tiedPolku = hakemisto & "\KILPAILU." & tunnus
        FileOpen(10, tiedPolku, OpenMode.Random, , , Len(ki))
        FileGet(10, ki, 20)
        pelaajaLkm = ki.LKM
        FileClose(10)

        ' Muodostetaan pelaajatiedoista taulukkomuuttuja pelaajaList
        tiedPolku = hakemisto & "\PERUS." & tunnus
        FileOpen(6, tiedPolku, OpenMode.Random, , , Len(pe))

        For i = 1 To pelaajaLkm
            rivi = ""
            FileGet(6, pe, i)
            ' rivi = String.Format("{0,3}", pe.NRO) & " "
            rivi = rivi & pe.NIMI & pe.SEURA
            rivi = rivi & String.Format("{0,5}", pe.SELO) ' seloluku ennen kisaa
            rivi = rivi & String.Format("{0,6}", pe.LKM) ' pelien lukumäärä ennen kisaa
            rivi = rivi & String.Format("{0,2}", pe.RYHNRO) ' pelaajan ryhmänumero
            pelaajaList(i - 1) = rivi
        Next
        FileClose(6)
    End Sub


    ' Selolista käyttöön
    Public Sub SelolistanTiedot()

        Dim StreamSelot As StreamReader
        Dim ApuString As String
        Dim i As Integer
        Dim Tiedpolku = hakemisto & "\selolist.txt"
        ' Selotiedoston muokkaus
        If Not File.Exists(Tiedpolku) Then
            MsgBox("tiedostoa selolist.txt ei löydy", )
        Else
            StreamSelot = New StreamReader(Tiedpolku, System.Text.Encoding.Default)
            'StreamSelot = New StreamReader(Tiedpolku, System.Text.Encoding.UTF7)
            ApuString = StreamSelot.ReadToEnd
            'seloList = ApuString.Split(vbCrLf)
            seloList = ApuString.Split(ControlChars.CrLf.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
            StreamSelot.Close()
            ' Otetaan selolistan otsikko talteen
            For i = 0 To 20
                If InStr(seloList(i), "Selolista ") Then
                    seloListaOts = seloList(i)
                End If
            Next i
            MsgBox("Selolistan tiedot on muokattu ohjelman käyttöön" & vbCrLf & seloListaOts)
        End If

    End Sub

    Sub TeeSeloRajat()
        Dim strDD As String
        strDD = "004011018026033040047054062069077084092099107114122130138146"
        strDD &= "154163171180189198207216226236246257268279291303316"
        strDD &= "329345358375392412433457485518560620736999"
        For I = 0 To 50
            seloRajat(I) = Val(Mid(strDD, 3 * I + 1, 3))
        Next I
    End Sub

    Function OdotusTulos(selo1 As Integer, selo2 As Integer) As Single
        ' pelin lopputulos annetaan kymmenkertaisena (10, 5 tai 0)
        ' funktio palauttaa välillä 0-0.92 olevan yksittäisen pelin odotustuloksen
        Dim i, ero As Integer
        ero = Abs(selo1 - selo2)
        For i = 0 To 50
            If ero < seloRajat(i) Then Exit For
            ' vahvemman pelaajan voiton todennäköisyys ei ole yli 92%
            If selo1 > selo2 And i > 41 Then Exit For
        Next
        Return (50 + Sign(selo1 - selo2) * i) / 100
    End Function

    Function UusiSelo(tulos1 As Integer, selo1 As Integer, selo2 As Integer) As Integer
        ' pelin lopputulos annetaan kymmenkertaisena (10, 5 tai 0)
        Dim i, ero, odotusTulos, muutos As Integer
        ero = Abs(selo1 - selo2)
        For i = 0 To 50
            If ero < seloRajat(i) Then Exit For
            ' muuttuvan selon laskennassa ei käytetä niin sanottua 92%-sääntöä,
            ' eli vahvemman pelaajan voiton todennäköisyys saa olla yli 92%
        Next
        odotusTulos = 50 + Sign(selo1 - selo2) * i
        muutos = (10 * tulos1 - odotusTulos) * seloKerroin
        Return selo1 + muutos / 100
    End Function

    Sub SelojenLaskenta(ByVal kierros As Integer)
        ' Tämä rutiini laskee muuttuvat selot uudestaan kierroksittain kierroksille 1 - kierros.
        ' Normaalisti kierros = kierrosLaskuri, mutta aikaisempien kierrosten tuloksia korjattaessa
        ' rutiinia voidaan kutsua muullakin kierrosnumerolla.
        Dim i, j, tulos10, selo1, selo2 As Integer
        Dim inda1, inda2, vastustajaNro As Integer
        Dim uselo As Integer

        ' luodaan ennen laskentaa seloRajat-taulu
        Call TeeSeloRajat()

        tiedPolku = hakemisto & "\TULOS." & tunnus
        FileOpen(8, tiedPolku, OpenMode.Random, , , Len(tu))
        For i = 1 To kierros
            inda1 = 1000 * i
            inda2 = 1000 * (i + 1)
            For j = 1 To pelaajaLkm
                FileGet(8, tu, inda1 + j)
                selo1 = tu.SELO
                vastustajaNro = tu.VASTUS
                tulos10 = tu.TULOS ' tulos on tiedostossa kymmenkertaisena
                ' haetaan vastustajan seloluku
                FileGet(8, tu, inda1 + vastustajaNro)
                selo2 = tu.SELO
                ' Uusi selo talletetaan seuraavan kierroksen tiedoksi TULOS-tiedostoon.
                ' Jos kyseessä on pelaamaton peli (tulos<0) ei seloa muuteta, mutta
                ' selo viedään kuitenkin seuraavan kierroksen tietueelle.
                If tulos10 < 0 Then
                    uselo = selo1
                Else
                    uselo = UusiSelo(tulos10, selo1, selo2)
                End If
                FileGet(8, tu, inda2 + j)
                tu.SELO = uselo
                FilePut(8, tu, inda2 + j)
            Next j
        Next i
        FileClose(8)
    End Sub

    Sub ShellSort(ByRef sort() As String, ByVal rivilkm As Integer, _
                  ByVal alkusar As Integer, ByVal kentpituus As Integer, ByVal lajsuunta As Integer)
        Dim temp As String
        Dim i, j, span As Integer
        Dim vertkent1, vertkent2 As String

        ' Aliohjelma lajittelee tekstitaulukon rivit halutun kentän (alkusar,kentpituus) mukaan
        ' nousevaan (lajsuunta=1) tai laskevaan (lajsuunta=-1) järjestykseen.
        span = rivilkm \ 2 ' kokonaislukujako
        ' Visual Basicissa taulukon alin indeksi on 0
        ' Tämä ei  sellaisenaan toimi, jos taulukon alin indeksi on 1
        Do While span > 0
            For i = span To rivilkm - 1
                For j = (i - span) To 0 Step -span
                    vertkent1 = Mid(sort(j), alkusar, kentpituus)
                    vertkent2 = Mid(sort(j + span), alkusar, kentpituus)
                    If lajsuunta = -1 Then ' laskeva järjestys
                        If vertkent1 >= vertkent2 Then Exit For
                    End If
                    If lajsuunta = 1 Then ' nouseva järjestys
                        If vertkent1 <= vertkent2 Then Exit For
                    End If
                    temp = sort(j)
                    sort(j) = sort(j + span)
                    sort(j + span) = temp
                Next j
            Next i
            span \= 2
        Loop

    End Sub


    Sub SingleSorter(ByRef arrArray)
        Dim row, j As Integer
        Dim StartingKeyValue, NewKeyValue, swap_pos
        ' Lajittelee numeerisen vektorin alkiot suurimmasta pienimpään.
        For row = 0 To UBound(arrArray) - 1
            'Take a snapshot of the first element
            'in the array because if there is a 
            'greater value elsewhere in the array 
            'we'll need to do a swap.
            StartingKeyValue = arrArray(row)
            NewKeyValue = arrArray(row)
            swap_pos = row

            For j = row + 1 To UBound(arrArray)
                'Start inner loop.
                If arrArray(j) > NewKeyValue Then
                    'This is now the highest number - 
                    'remember it's position.
                    swap_pos = j
                    NewKeyValue = arrArray(j)
                End If
            Next

            If swap_pos <> row Then
                'If we get here then we are about to do a swap
                'within the array.		
                arrArray(swap_pos) = StartingKeyValue
                arrArray(row) = NewKeyValue
            End If
        Next
    End Sub


    Sub TiedostojenAlustus(ByRef alustettu As Boolean)

        ' Dim pe As TYP6
        ' Dim tu As TYP8
        ' Dim ki As TYP10
        Dim i, j, apuselo As Integer
        Dim response As Integer

        ' Suorasaantitiedostojen luonti ja alustus
        tiedPolku = hakemisto & "\PERUS." & tunnus
        If File.Exists(tiedPolku) Then
            response = MsgBox("Turnauksen tiedostot on jo alustettu." & vbCrLf &
                   "Alustus tuhoaa aikaisemmat tiedot," & vbCrLf &
                   "ja turnaus aloitetaan uudelleen kierroksesta 1." & vbCrLf &
                   " Haluatko jatkaa?",
                   MsgBoxStyle.YesNo Or MsgBoxStyle.DefaultButton2 Or MsgBoxStyle.Critical, "Varoitus")
            If Not response = vbYes Then
                alustettu = False
                Exit Sub
            End If
        End If

        ' PERUS.tun
        Call AlustaTYP6()
        ' kierroksittaiset tulokset TULOS.tun
        Call AlustaTYP8()
        ' PARIT.tun
        Call AlustaTYP9()
        ' kilpailutiedot KILPAILU.tun
        Call AlustaTYP10()
        ' VERTAUS.tun
        Call AlustaTYP11()

        ' pelaajatiedot taulukkoon pelaajaList
        Call LuePelaajat()
        'lajiellaan selon mukaan laskevaan järjestykseen
        Call ShellSort(pelaajaList, pelaajaLkm, 35, 4, -1)

        tiedPolku = hakemisto & "\PERUS." & tunnus
        FileOpen(6, tiedPolku, OpenMode.Random, , , Len(pe))

        For i = 0 To pelaajaLkm - 1
            pe.NRO = i + 1  ' juokseva numero selojärjestyksessä
            pe.NIMI = Mid(pelaajaList(i), 1, 25)
            'Mid(PE.NIMI, 24, 2) = Mid(pelaajaRivi, 54, 2) 'syntymävuosi
            pe.SEURA = Mid(pelaajaList(i), 26, 8)
            apuselo = Val(Mid(pelaajaList(i), 35, 4))  ' Alkuselo
            ' Jos seloa ei ole ollenkaan, asetetaan selon arvoksi 1525
            If apuselo = 0 Then apuselo = 1525
            pe.SELO = apuselo
            pe.SRNRO = Val(Mid(pelaajaList(i), 46, 5)) ' Rek. nro
            pe.LKM = Val(Mid(pelaajaList(i), 40, 5)) ' Pelien lukumäärä
            pe.RYHNRO = 0 ' ryhmänumero alustetan nollaksi

            FilePut(6, pe, i + 1)
        Next i
        ' Alustetaan vielä 10 tyhjää tietuetta mahdollisia myöhemmmin
        ' kilpailuun mukaan tulevia varten.
        For i = pelaajaLkm + 1 To pelaajaLkm + 10
            pe.NRO = i
            pe.NIMI = " "
            pe.SEURA = " "
            pe.SELO = 0
            pe.SRNRO = 0
            pe.LKM = 0
            pe.RYHNRO = 0
            FilePut(6, pe, i)
        Next
        FileClose(6)

        ' alustetaan TULOS-tiedosto
        tiedPolku = hakemisto & "\TULOS." & tunnus
        FileOpen(8, tiedPolku, OpenMode.Random, , , Len(tu))

        ' varaudutaan kymmeneen lisäpelaajaan kilpailun aikana
        For i = 1 To kierrosMaara + 1
            For j = 1 To pelaajaLkm + 11
                tu.KR = i     ' kierrosnumero valmiiksi
                tu.TYYPPI = 1 ' alustetaan kaikki aktiivipelaajiksi
                tu.VARI = 0
                tu.VASTUS = 0
                tu.TULOS = -10
                tu.SELO = 0
                FilePut(8, tu, 1000 * i + j)
            Next (j)
        Next (i)

        ' PERUS-tiedoston avulla alustetaan TULOS-tiedoston 1. kierroksen tietueet.
        ' Alustus tehdään vain tällä hetkellä mukaan ilmoitetuille.
        tiedPolku = hakemisto & "\PERUS." & tunnus
        FileOpen(6, tiedPolku, OpenMode.Random, , , Len(pe))
        For j = 1 To pelaajaLkm
            FileGet(6, pe, j)
            FileGet(8, tu, 1000 + j)
            tu.TYYPPI = 1  'aktiivinen
            tu.SELO = pe.SELO
            FilePut(8, tu, 1000 + j)
        Next j
        FileClose(6)
        FileClose(8)

        ' alustetaan PARIT-tiedosto
        tiedPolku = hakemisto & "\PARIT." & tunnus
        FileOpen(9, tiedPolku, OpenMode.Random, , , Len(pa))
        For i = 1 To kierrosMaara + 1
            For j = 1 To (pelaajaLkm + 10) / 2 + 1
                pa.KIERROS = i
                pa.NRO = j
                FilePut(9, pa, 1000 * i + j)
            Next
        Next
        FileClose(9)

        ' alustetaan KILPAILU-tiedoston tietueet
        tiedPolku = hakemisto & "\KILPAILU." & tunnus
        FileOpen(10, tiedPolku, OpenMode.Random, , , Len(ki))
        For i = 1 To kierrosMaara + 1
            ki.KR = i
            ki.VARAP = 0
            ki.LKM = pelaajaLkm
            ki.AKTIIVIT = 0
            ki.LASKU = 0
            ki.VERTLAS = 0
            FilePut(10, ki, i)
        Next
        ' Alustetaan KILPAILU-tiedoston 20. tietue, muilla kilpailukohtaisilla 
        ' tiedoilla; ennestään tietueella on jo kilpailun tunnus, nimi ja kilpailun
        ' kierrosmäärä (3-15); tietueella 20 on myös aina tieto käsittelyssä 
        ' olevasta kierroksesta, ennen ensimmäistä se on nolla.
        FileGet(10, ki, 20)
        ki.KR = 0            ' käynnissä oleva kierros 
        ki.LKM = pelaajaLkm  ' pelaajien kokonaismäärä
        ki.KERROIN = 100     ' oletusarvo selokertoimelle
        ki.TAPA = 1          ' pisteiden talletustapa (1: 10, 5 tai 0)
        ki.AKTIIVIT = 0      ' aktiivipelaajien määrä päivittyy ennen parien muodostamista
        ki.LASKU = 0
        ki.VERTLAS = 0
        FilePut(10, ki, 20)
        FileClose(10)

        ' alustetaan VERTAUS-tiedosto (pelaajien vertailutiedot)
        ' varaudutaan kymmeneen myöhemmin mukaan tulevaan pelaajaan
        tiedPolku = hakemisto & "\VERTAUS." & tunnus
        FileOpen(11, tiedPolku, OpenMode.Random, , , Len(VE))
        For i = 1 To pelaajaLkm + 10
            FilePut(11, VE, i)
        Next
        FileClose(11)

        ' estetään pelaajatietojen muutokset tilapäisesti alustuksen jälkeen
        Form2.ToolStripMenuItem1.Enabled = False
        Form2.SelolistaltaToolStripMenuItem.Enabled = False
        Form2.KysellenToolStripMenuItem.Enabled = False

        ' tiedostojen alustusten jälkeen uusi kierros ja uudet parit sallitaan
        Form3.PelaajamuutoksetToolStripMenuItem.Enabled = True
        Form3.ParitToolStripMenuItem.Enabled = False
        Form3.TulosteetToolStripMenuItem.Enabled = False
        Form3.TulostenIlmoittaminenToolStripMenuItem.Enabled = False
        Form3.AjantasatuloksetToolStripMenuItem.Enabled = False
        alustettu = True

    End Sub

    Sub MuodostaRyhmat(ByRef rlkm, ByRef rajat)
        Dim rn, raja As Integer
        Dim i As Integer

        tiedPolku = hakemisto & "\PERUS." & tunnus
        FileOpen(6, tiedPolku, OpenMode.Random, , , Len(pe))

        FileGet(6, pe, 1)
        rn = 1
        raja = rajat(rn - 1)
        For i = 1 To pelaajaLkm
            If i > raja Then
                rn = rn + 1
                raja = rajat(rn - 1)
            End If
            FileGet(6, pe, i)
            pe.RYHNRO = rn
            FilePut(6, pe, i)
        Next
        FileClose(6)
    End Sub

    Sub KierroksenParit(ByRef paritTehty As Boolean)
        Dim arrLaj(aktiiviLkm) As Integer
        Dim indakt As Integer
        Dim paritOK As Boolean

        ' Luetaan selotiedot ja viedään lajitteluvektoriin
        tiedPolku = hakemisto & "\TULOS." & tunnus
        FileOpen(8, tiedPolku, OpenMode.Random, , , Len(tu))

        indakt = 0
        ' Muodostetaan lajitteluvektori.
        ' Koska tiedot pitää lajitella laskevaan järjestykseen,
        ' pitää pelaajanumero muunta myös laskevaksi,
        ' ts pelaaja 1 on pelaajalkm-1, pelaaja 2 on pelaajalkm-2, jne
        ' Kun lajitteluvektorin tietoja puretaan, täytyy tehdä muunnos
        ' takaisin todelliseksi pelaajanumeroksi.
        For i = 1 To pelaajaLkm
            FileGet(8, tu, 1000 * kierrosLaskuri + i)
            If tu.TYYPPI = 1 Then
                arrLaj(indakt) = 1000 * tu.SELO + pelaajaLkm - i
                indakt = indakt + 1
            End If
        Next
        FileClose(8)

        Call SingleSorter(arrLaj)

        If kierrosLaskuri = 1 Then Call EkanKierroksenParit(arrLaj)
        paritOK = True
        If kierrosLaskuri > 1 Then Call MuunKierroksenParit(paritOK, arrLaj)

        If paritOK Then
            Call VieParienLkm() 'parien määrä KILPAILU-tiedostoon
            paritTehty = True
        Else
            paritTehty = False
        End If
    End Sub

    Sub EkanKierroksenParit(ByRef arrLaj)
        Dim V1, SS As Integer
        Dim NRO1, NRO2 As Integer
        Dim IND As Integer
        Dim SELO1, SELO2, VARI1, VARI2 As Integer
        Dim response, temp, intVari As Integer
        Dim valinta As String = " "
        Dim kolmeAnnetaan As Boolean = False

        response = MsgBox("Annatko ohjelman arpoa " & vbCrLf & _
                          "kolme ensimmäistä paria?", _
               MsgBoxStyle.YesNo)
        If response = vbYes Then
            Call KolmeEkaaArvotaan(arrLaj)
            valinta = "arpa"
        End If
        If valinta = " " Then
            response = MsgBox("Haluatko itse ilmoittaa " & vbCrLf & _
                              "kolmen ensimmäisen parin tiedot?", _
                  MsgBoxStyle.YesNo)
            If response = vbYes Then
                Call KolmeEkaaAnnetaan(arrLaj)
                kolmeAnnetaan = True
            End If
        End If

        ' viedään paritiedot KPARIT-taulukkoon

        KP = 0
        For V1 = 0 To aktiiviLkm - 1 Step 2
            KP = KP + 1 ' Parien lukumäärä
            NRO1 = pelaajaLkm - arrLaj(V1) Mod 1000
            NRO2 = pelaajaLkm - arrLaj(V1 + 1) Mod 1000
            KPARIT(KP - 1, 0) = NRO1
            KPARIT(KP - 1, 1) = NRO2
            KPARIT(KP - 1, 2) = KP
        Next V1

        ' värit
        tiedPolku = hakemisto & "\TULOS." & tunnus
        FileOpen(8, tiedPolku, OpenMode.Random, , , Len(tu))

        IND = 1000 * kierrosLaskuri
        For SS = 0 To KP - 1
            NRO1 = KPARIT(SS, 0)
            NRO2 = KPARIT(SS, 1)
            FileGet(8, tu, IND + NRO1)
            SELO1 = tu.SELO
            FileGet(8, tu, IND + NRO2)
            SELO2 = tu.SELO

            ' värien määrääminen ekalla kierroksella
            ' funktio variSel palauttaa arvon 0, jos SELO1 edellyttä valkeita.
            ' Jos kolme ensimmäistä paria on annettu, niiden värit on samalla määritelty.

            If kolmeAnnetaan And SS < 3 Then
                VARI1 = 1
                VARI2 = 2
            Else
                intVari = variSel(SELO1, SELO2)
                If intVari = 0 Then
                    VARI1 = 1
                    VARI2 = 2
                Else
                    VARI1 = 2
                    VARI2 = 1
                End If
                ' jos vari1 = 2 (musta) pitää KPARIT-taulun rivillä 
                ' sarakkeiden 0 ja 1 arvot vaihtaa keskenään
                If VARI1 = 2 Then
                    temp = KPARIT(SS, 0)
                    KPARIT(SS, 0) = KPARIT(SS, 1)
                    KPARIT(SS, 1) = temp
                End If
            End If

            ' viedään väri-tiedot talteen TULOS-tiedostoon
            ' ja asetetaan myös tulos-tiedolle oletusarvo -10,
            ' tällä on merkitystä, jos kyseessä on kierroksen uusiminen
            FileGet(8, tu, IND + NRO1)
            tu.VASTUS = NRO2
            tu.VARI = VARI1
            tu.TULOS = -10
            FilePut(8, tu, IND + NRO1)
            FileGet(8, tu, IND + NRO2)
            tu.VASTUS = NRO1
            tu.VARI = VARI2
            tu.TULOS = -10
            FilePut(8, tu, IND + NRO2)
        Next SS

        ' paritiedot PARIT-tiedostoon
        Call VieParit(KP, IND)

        ' päivitetään KPARIT-taulukko
        Call ParTyo1()
        FileClose(8)

    End Sub

    Sub KolmeEkaaArvotaan(ByRef taulu() As Integer)

        ' Alustetaan satunnaislukugeneraattori
        Randomize()
        ' järjestetään taulun avulla kolme ekaa paria, kiellettyjä ovat 1-2 ja 1-6
        ' ensimmäisen pelaajan (taulukon indeksi 0) vastustajan indeksi jokin arvoista 2,3,4
        Call arvonta(taulu, 1, 3, 2)
        ' toisen parin eka eli valkean numero (jonkin arvoista 2,3,4,5)
        Call arvonta(taulu, 2, 4, 2)
        ' toisen parin toka eli mustan numero (jokin arvoista 3,4,5)
        Call arvonta(taulu, 3, 3, 3)
        ' kolmannen parin eka eli valkean numero (arvo joko 4 tai 5)
        Call arvonta(taulu, 4, 2, 4)
    End Sub

    Sub arvonta(ByRef taulu() As Integer, _
                ByRef parinEkaInd As Integer, _
                ByRef veLkm As Integer, pieninInd As Integer)
        ' parinEkaInd = kohta, johon parin valkea asetetaan
        ' veLkm = arvonnan tuottamien indeksivaihtoehtojen lkm
        ' pieninInd = pienin sallittu indeksin arvo (indeksit alkaa 0:sta)
        ' toisen parin toka numero
        Dim iarv, apu As Integer

        iarv = Int(Rnd() * veLkm) + pieninInd
        apu = taulu(parinEkaInd)
        taulu(parinEkaInd) = taulu(iarv)
        taulu(iarv) = apu
    End Sub

    Sub KolmeEkaaAnnetaan(ByRef taulu() As Integer)
        Dim prompt, promptapu, vastaus, vastapu As String
        Dim pelurit(5) As Integer
        Dim aputau(5) As Integer
        Dim iapu, ipos, iend, iv, tulo As Integer
        Dim ilmOK As Boolean = False

        promptapu = ""
        prompt = " Anna kolme pelaajaparia, esim 4-1, 2-6, 5-3" & vbCrLf & _
               " jos et anna mitään, toiminto peruutetaan"
        Do Until ilmOK

            vastaus = InputBox(promptapu & prompt)

            If vastaus = "" Then Exit Sub
            vastapu = Replace(vastaus, "-", " ")
            vastapu = Replace(vastapu, ",", " ")
            vastapu = Replace(vastapu, " ", "")

            iend = Len(vastapu)
            If IsNumeric(vastapu) And iend = 6 Then
                ipos = 1
                iv = 0
                Do While ipos <= iend
                    iapu = Val(Mid(vastapu, ipos, 1))
                    ' Vain numerot 1,2,3,4,5,6 kelpaavat.
                    ' Jos kuuden ensimmäisen joukossa on huilaajia, muuttuu muiden
                    ' pelaajanumerot vastaavasti, esim jos alkujärjestyksessä 4 huilaa,
                    ' tulee parien antamisessa 5:sta 4, 6:sta 5 ja 7:sta 6. 
                    If iapu >= 1 And iapu <= 6 Then
                        pelurit(iv) = iapu
                        iv = iv + 1
                    End If
                    ipos = ipos + 1
                Loop
                tulo = 1
                For iapu = 0 To 5
                    tulo = tulo * pelurit(iapu)
                Next
                If iv = 6 And tulo = 720 Then ilmOK = True
            End If
            promptapu = "Parimäärittely " & vastaus & _
                "  ei kelpaa, anna uudestaan" & vbCrLf
        Loop

        For iv = 0 To 5
            aputau(iv) = taulu(pelurit(iv) - 1)
        Next
        For iv = 0 To 5
            taulu(iv) = aputau(iv)
        Next

    End Sub


    Sub MuunKierroksenParit(ByRef paritOK As Boolean, ByRef arrLaj() As Integer)
        Dim V1, V2, LS, VR, jatkuuko As Integer
        Dim i, j, valk, must, tasa, mselo1, mselo2 As Integer
        Dim hlkm, VALS, MUSS, MUSE, VALMUS As Integer
        Dim NRO, inro, pariNumero As Integer
        Dim aloitus, lopetus, rajoitin, peruutusInd As Integer
        Dim loytyi, variEhto As Boolean
        Dim pelVastus(0 To pelaajaLkm, 0 To kierrosLaskuri - 1) As Integer
        Dim IND As Integer
        Dim NRO1, NRO2 As Integer
        Dim hlkm1, hlkm2, VAL1, VAL2, MUS1, MUS2, MUSE1, MUSE2, VMERO1, VMERO2 As Integer
        Dim huomautus As String

        tiedPolku = hakemisto & "\TULOS." & tunnus
        FileOpen(8, tiedPolku, OpenMode.Random, , , Len(tu))

        ' Kerätään kaikkien pelaajien edellisten kierrosten värien määrät.
        ' Muodostetaan pelihistoriasta vastustaja- ja väritaulukot,
        ' sarakkeella 0 on valkeilla pelattujen pelien määrä,
        ' sarakkeella 1 mustilla pelattujen pelien määrä,
        ' sarakkeella 2 mustien värien väriarvo,
        ' sarakkeelle 3 merkataan, onko pelaaja kierroksella mukana,
        ' sarakkeelle 4 merkataan pidettyjen huilikierrosten lkm
        ' Taulujen rivi-indeksi on pelaajan numero -1 ja sarake-indeksi on kierros -1.
        ' TULOS-tiedostossa väri on koodattu siten, 
        ' että 0=pelaamaton (huili), 1=valkea 2=musta.

        For i = 0 To pelaajaLkm - 1
            VALS = 0
            MUSS = 0
            MUSE = 0
            hlkm = 0
            For j = 0 To kierrosLaskuri - 2
                ' For j = 0 To kierrosLaskuri - 1
                FileGet(8, tu, 1000 * (j + 1) + i + 1)
                If tu.VASTUS <> 0 Then
                    If tu.VARI = 1 Then VALS = VALS + 1
                    If tu.VARI = 2 Then MUSS = MUSS + 1
                    MUSE = MUSE + (2 ^ (j + 1)) * tu.VARI ' mustien värien väriarvo
                    pelVastus(i, j) = tu.VASTUS
                End If
                If tu.VASTUS = 0 Then
                    hlkm = hlkm + 1
                End If
            Next j
            VARIT(i, 0) = VALS
            VARIT(i, 1) = MUSS
            VARIT(i, 2) = MUSE
            VARIT(i, 3) = 0 ' alustetaan nollaksi
            VARIT(i, 4) = hlkm
        Next i

        valk = 0
        must = 0
        tasa = 0
        For VR = 0 To aktiiviLkm - 1
            ' arrLaj indeksi alkaa 0:sta
            ' Viedään lajittelun jälkeisen tilanteen mukaisena pelaajanumerot LAJ-taulukkoon
            NRO = pelaajaLkm - arrLaj(VR) Mod 1000
            LAJ(VR) = NRO
            inro = NRO - 1
            If VARIT(inro, 0) > VARIT(inro, 1) Then valk = valk + 1
            If VARIT(inro, 0) < VARIT(inro, 1) Then must = must + 1
            If VARIT(inro, 0) = VARIT(inro, 1) Then tasa = tasa + 1
            ' Merkataan pelaaja aktiiviksi VARIT-taulukkoon
            VARIT(inro, 3) = 1
        Next VR
        huomautus = "Pelaajien värien jakautuminen ennen uusia pareja: " & vbCrLf &
                    "Valkeilla enemmän pelanneita: " & valk & vbCrLf &
                    "Mustillaa enemmän pelanneita: " & must & vbCrLf &
                    "Värit tasan: " & tasa
        Form3.Label2.Text = huomautus

        ' Jos valkeilla tai mustilla enemmän pelanneiten ero ei ole nolla,
        ' suoritetaan värien tasoitus.
        VALMUS = valk - must
        If VALMUS <> 0 Then
            Call TasoitaVarit(VALMUS)
        End If


        ' Etsitään pareja "ikuisessa" silmukassa, joka päättyy viimeistään silloin,
        ' kun kierroksia on suoritettu 400 kertaa.

        KP = 0 ' löytyneiden parien määrä
        LS = 1 ' etsinnän suunta,  1=alusta loppuuun, -1=lopusta alkuun
        rajoitin = 0
        aloitus = 0
        lopetus = aktiiviLkm - 1
        V1 = 0
        variEhto = parienVariEhto

        Do While aktiiviLkm - 2 * KP > 0
            rajoitin = rajoitin + 1
            If rajoitin Mod 400 = 0 Then
                huomautus = "Jatketaanko parien etsimistä?" & vbCrLf & _
                            "(väreistä ei välitetä)" & vbCrLf & _
                            "Jos valitset vaihtoehdon Ei," & vbCrLf & _
                            "parien etsiminen lopetetaan," & vbCrLf & _
                            "ja  voit tehdä lisää pelaajamuutoksia."

                jatkuuko = MsgBox(huomautus, MsgBoxStyle.YesNo)
                If jatkuuko = MsgBoxResult.Yes Then
                    ' neljänsadan yrityksen jälkeen ei käytetä enää väriehtoa
                    variEhto = False
                Else
                    ' siirrytään pois parien etsinnästä ja sallitaan pelaajamuutosten teko
                    paritOK = False
                    FileClose(8)
                    Form3.PelaajamuutoksetToolStripMenuItem.Enabled = True
                    Exit Sub
                End If
            End If

            ' etsitään ensimmäinen V1-ehdokas (LAJ:n ensimmäinen positiivinen 
            ' elementti etsimis-suunnassaan)
            ' peruutusInd = 0
            For i = aloitus To lopetus Step LS
                ' If LAJ(i) > 0 And i <> peruutusInd Then Exit For
                If LAJ(i) > 0 Then Exit For
            Next
            V1 = i
            ' V1 parin ensimmäisen ehdokkaan pointteri LAJ()-taulukossa

            For V2 = aloitus To lopetus Step LS
                loytyi = False
                vm = 9
                If LAJ(V2) > 0 And V1 <> V2 Then
                    ' tutkitaan ensin etteivät ole pelanneet toisiaan vastaan
                    NRO1 = LAJ(V1)
                    NRO2 = LAJ(V2)
                    ' oletusarvona että löytynyt pariehdokas kelpaa
                    loytyi = True
                    For j = 0 To kierrosLaskuri - 1
                        If NRO2 = pelVastus(NRO1 - 1, j) Then
                            loytyi = False
                            Exit For
                        End If
                    Next


                    If loytyi = True Then
                        ' jos löytyi, tutkitaan vielä värien sopivuus
                        ' Lasketaan todellisten värien lukumäärät:
                        VAL1 = VARIT(NRO1 - 1, 0)
                        VAL2 = VARIT(NRO2 - 1, 0)
                        MUS1 = VARIT(NRO1 - 1, 1)
                        MUS2 = VARIT(NRO2 - 1, 1)
                        MUSE1 = VARIT(NRO1 - 1, 2)
                        MUSE2 = VARIT(NRO2 - 1, 2)
                        hlkm1 = VARIT(NRO1 - 1, 4)
                        hlkm2 = VARIT(NRO2 - 1, 4)
                        VMERO1 = VAL1 - MUS1
                        VMERO2 = VAL2 - MUS2
                        vm = 9
                        ' vm-muuttujalle asetetaan tekninen alkuarvo 9.
                        ' Jos alla oleva tarkastelu ei arvoa muuta,
                        ' ehdokasta ei oteta pariin niin kauan kuin väriehto on voimassa. 
                        ' Kun tehdään parillisen kierroksen värejä, VMERO1-VMERO2 saa olla
                        ' joko +1 tai -1 (parittoman kierroksen jälkeen), jolloin värit saadaan tasoitetuksi,
                        ' tai jos pelaajien huilikierrosten summa on pariton,
                        ' saa värierojen summa olla myös -1 tai +1 
                        ' Jos ehdolla olevan parin ensimmäinen pelaaja saa valkeat, on vm=0, muutoin vm=1 

                        ' Parillisen kierroksen pari
                        If kierrosLaskuri Mod 2 = 0 Then
                            If VMERO1 + VMERO2 = 0 Then
                                If VMERO1 < VMERO2 Then vm = 0 Else vm = 1
                            End If
                            ' Jos toinen pelaajista on pitänyt huilikierroksen,
                            ' sallitaan epätasasiset määrät valkeita ja mustia.
                            If (hlkm1 + hlkm2) Mod 2 = 1 And Abs(VMERO1 + VMERO2) = 1 Then
                                If VMERO1 < VMERO2 Then vm = 0 Else vm = 1
                            End If
                            ' Jos väreistä ei välitetä, sallitaan myös parillisen kierroksen jälkeen
                            ' epätasaiset määrät valkeita ja mustia.
                            If variEhto = False Then
                                If VMERO1 < VMERO2 Then vm = 0 Else vm = 1
                            End If
                        End If ' end of parillisen kierroksen pari

                        ' Parittoman kierroksen pari.
                        ' Parillisen kierroksen jälkeen kaikilla pelaajilla pitäisi olla 
                        ' VMERO-muuttuja 0. Jos näin ei ole, väri määräytyy viimeisien mustien
                        ' perusteella. Jos värihistoria on täysin sama, määräytyy väri muuttuvan
                        ' selon perusteella. Jos muuttuva selokin on sama, värit arvotaan.
                        ' Jos parin molempien ehdokkaiden huilikierrosten summa on pariton ja
                        ' jos heillä on sama väriero, ei heitä pariteta vastakkain.
                        If kierrosLaskuri Mod 2 = 1 Then
                            If (hlkm1 + hlkm2) Mod 2 = 1 And Abs(VMERO1 + VMERO2) = 1 Then
                                If VMERO1 < VMERO2 Then vm = 0 Else vm = 1
                            ElseIf (hlkm1 + hlkm2) Mod 2 = 0 And VMERO1 = VMERO2 Then
                                If MUSE1 = MUSE2 Then
                                    FileGet(8, tu, 1000 * kierrosLaskuri + NRO1)
                                    mselo1 = tu.SELO
                                    FileGet(8, tu, 1000 * kierrosLaskuri + NRO2)
                                    mselo2 = tu.SELO
                                    vm = variSel(mselo1, mselo2) ' kts. funktion kuvauksesta
                                End If
                                If MUSE1 > MUSE2 Then vm = 0 ' valkea, musta
                                If MUSE1 < MUSE2 Then vm = 1 ' musta, valkea
                            ElseIf hlkm1 + hlkm2 = 0 Then
                                If VMERO1 < VMERO2 Then vm = 0 Else vm = 1
                            End If
                        End If ' end of parittoman kierroksen pari

                    End If ' end of loytyi = true
                End If ' end of LAJ(V2) > 0

                If loytyi And vm = 9 Then
                    ' jos väreistä ei välitetä, valkeat saa se pelaaja,
                    ' jolla vähemmän valkeita, tai jos valkeitten määrä on tasan, 
                    ' valkeat saa pelaaja, jolla MUSE on suurempi.
                    ' (vm=0 : valkea,musta; vm=1 : musta,valkea)
                    ' Jos värihistoria täysin sama, määräytyy väri muuttuvan selon perusteella.
                    If vm = 9 And (variEhto = False) Then
                        If VAL1 < VAL2 Then vm = 0
                        If VAL1 > VAL2 Then vm = 1
                        If VAL1 = VAL2 Then
                            If MUSE1 < MUSE2 Then vm = 1
                            If MUSE1 > MUSE2 Then vm = 0
                            If MUSE1 = MUSE2 Then
                                FileGet(8, tu, 1000 * kierrosLaskuri + NRO1)
                                mselo1 = tu.SELO
                                FileGet(8, tu, 1000 * kierrosLaskuri + NRO2)
                                mselo2 = tu.SELO
                                vm = variSel(mselo1, mselo2) ' kts. funktion kuvauksesta
                            End If
                        End If
                    End If
                End If

                ' TeePari aliohjelma päivittää 
                ' globaaleja taulukoita LAJ ja KPARIT
                If loytyi And vm <> 9 Then
                    KP = KP + 1
                    Call TeePari(V1, V2, KP)
                    Exit For ' poistutaan v2-for-loopista
                End If
            Next V2

            ' Jos tänne tultaessa ei yhtään paria ole purettavaksi,
            ' täytyy parien teko sallia väreistä huolimatta.
            If KP = 0 Then variEhto = False

            ' Jos parin toisen osapuolen etsintä ei onnistunut,
            ' peruutetaan viimeiseksi tehty pari, ja aloitetaan
            ' etsintä edellisen etsintäsuunnan seuraavasta V1-ehdokkaasta.
            ' ( alusta loppuun etsittäessä V1+1, lopusta alkuun V1-1 )
            If Not loytyi Then
                If KP > 0 Then
                    Call Peruuta(V1, V2, KP, peruutusInd)
                    KP = KP - 1
                    V1 = V1 - LS
                End If
                ' Jos etsinnässä päädytään taulukon päähän,
                ' siirretään alkukohta sunnassaan seuraavaan alkioon
                ' ja sallitaan parit, vaikka väriehto ei toteutuisikaan.
                If V1 = lopetus Then
                    V1 = V1 - LS
                    variEhto = False
                End If
            End If

            If KP Mod 2 = 0 Then
                LS = 1
                aloitus = 0
                lopetus = aktiiviLkm - 1
            End If
            If KP Mod 2 = 1 Then
                LS = -1
                aloitus = aktiiviLkm - 1
                lopetus = 0
            End If
            If Not loytyi Then aloitus = V1

        Loop  ' end of do while (parien määrityksen pääsilmukka)

        If testaus Then
            MsgBox("Kierroksen " & kierrosLaskuri & "  parien teossa " & vbCrLf & _
              " ohjelmasilmukka suoritettiin " & rajoitin & " kertaa")
        End If

        'viedään värit talteen
        IND = 1000 * kierrosLaskuri
        For SS = 0 To KP - 1
            NRO1 = KPARIT(SS, 0)
            NRO2 = KPARIT(SS, 1)

            ' viedään väri-tiedot talteen TULOS-tiedostoon
            ' ja alustetaan tulostiedot; tällä on merkitystä silloin, jos
            ' parit tehdään uudestaan ja osa peleistä on jo ehditty kirjata;
            ' sellainen tilanne pitäisi kyllä olla erittäin harvinainen
            FileGet(8, tu, IND + NRO1)
            tu.VASTUS = NRO2
            tu.VARI = 1 ' valkea
            tu.TULOS = -10
            FilePut(8, tu, IND + NRO1)
            FileGet(8, tu, IND + NRO2)
            tu.VASTUS = NRO1
            tu.VARI = 2 ' musta
            tu.TULOS = -10
            FilePut(8, tu, IND + NRO2)
        Next SS

        ' viedään KPARIT-taulun 2-sarakkeelle parille numero
        ' parit ovat taulussa järjestyksessä 1., viimeinen, 2., toiseksi viimeinen, jne
        If KP Mod 2 = 1 Then
            ' Pareja pariton määrä
            pariNumero = 1
            For i = 1 To KP Step 2
                KPARIT(i - 1, 2) = pariNumero
                pariNumero = pariNumero + 1
            Next i
            For i = KP - 1 To 2 Step -2
                KPARIT(i - 1, 2) = pariNumero
                pariNumero = pariNumero + 1
            Next i
        End If

        If KP Mod 2 = 0 Then
            ' Pareja parillinen määrä
            pariNumero = 1
            For i = 1 To KP - 1 Step 2
                KPARIT(i - 1, 2) = pariNumero
                pariNumero = pariNumero + 1
            Next i
            For i = KP To 2 Step -2
                KPARIT(i - 1, 2) = pariNumero
                pariNumero = pariNumero + 1
            Next i
        End If

        ' paritiedot PARIT-tiedostoon
        Call VieParit(KP, IND)

        ' päivitetään KPARIT-taulukko
        Call ParTyo1()
        FileClose(8)

    End Sub

    Sub TasoitaVarit(ByVal valmus As Integer)
        Dim i, iw As Integer
        ' Tasoitus suoritetaan ensisijaisesti niiden aktiivien pelaajien avulla, 
        ' jotka ovat pelanneet saman verran pelejä sekä valkeilla että mustilla.
        ' Tasoitus aloitetaan MSELO-luvuiltaan heikoimmista.
        i = pelaajaLkm - 1
        Do While i >= 0 And valmus <> 0
            ' vain aktiivien pelaajien väri-tietoja muutetaan
            If VARIT(i, 3) = 1 Then
                If VARIT(i, 0) = VARIT(i, 1) Then
                    ' valkeilla enemmän pelanneita enemmän
                    If valmus > 0 Then
                        VARIT(i, 0) = VARIT(i, 0) - 1
                        VARIT(i, 1) = VARIT(i, 1) + 1
                        valmus = valmus - 1
                    End If
                    ' mustilla enemmän pelanneita enemmän
                    If valmus < 0 Then
                        VARIT(i, 0) = VARIT(i, 0) + 1
                        VARIT(i, 1) = VARIT(i, 1) - 1
                        valmus = valmus + 1
                    End If
                End If
            End If
            i = i - 1
        Loop
        If valmus = 0 Then Exit Sub
        ' Jos edellä oleva ei vielä saanut valmus-muuttujaa nollaksi,
        ' jatketaan tasoitusta.
        i = pelaajaLkm - 1
        Do While i >= 0 And valmus <> 0
            ' vain aktiivien pelaajien väri-tietoja muutetaan
            If VARIT(i, 3) = 1 Then
                ' jos valmus on parillinen, muutos on 2, muutoin 1
                If (valmus Mod 2) = 0 Then iw = 2 Else iw = 1
                ' pelaajan valkeitten ja mustien ero ei saa kasvaa suuremmaksi kuin 2
                If Abs(VARIT(i, 0) - VARIT(i, 1)) <= 1 Then
                    ' valkealla enemmän pelanneita enemmän
                    If valmus > 0 And VARIT(i, 0) > VARIT(i, 1) Then
                        VARIT(i, 0) = VARIT(i, 0) - 1
                        VARIT(i, 1) = VARIT(i, 1) + 1
                        valmus = valmus - iw
                    End If
                    ' mustilla enemmän pelanneita enemmän
                    If valmus < 0 And VARIT(i, 0) < VARIT(i, 1) Then
                        VARIT(i, 0) = VARIT(i, 0) + 1
                        VARIT(i, 1) = VARIT(i, 1) - 1
                        valmus = valmus + iw
                    End If
                End If
            End If
            i = i - 1
        Loop
    End Sub

    Function variSel(ByVal mselo1 As Integer, ByVal mselo2 As Integer) As Integer
        ' Funktiolla valitaan pariehdokkaan ensimmäiselle pelajalle väri muuttuvien 
        ' selojen mukaan silloin, kun pelaajien värihistoriat ovat samat.
        ' Funktio palauttaa nollan jos mselo1-luvun pelaaja pelaa valkeilla, muutoin ykkösen.
        ' Funktio tutkii mseloluvun numerot lopusta alkuun, ja jos jossain kohtaa ero löytyy,
        ' saa pienempi numero valkeat. Jos selot ovat samat, väri arvotaan.
        Dim vika1, vika2, vari, i As Integer
        If mselo1 = mselo2 Then
            vari = Int(Rnd() + 0.5)
            Return vari
        End If
        For i = 0 To 3
            vika1 = Int(mselo1 / 10 ^ i) Mod 10
            vika2 = Int(mselo2 / 10 ^ i) Mod 10
            If vika1 < vika2 Then
                Return 0
            End If
            If vika1 > vika2 Then
                Return 1
            End If
        Next
        ' Funktiossa ei koskaan tulla suorittamaan seuraavaa lausetta,
        ' mutta VB:n syntaksin tarkistaja varoittaa, jos se puuttuu.
        Return 0
    End Function

    Sub TeePari(ByRef V1 As Integer, ByRef V2 As Integer, ByRef KP As Integer)
        ' vm globaali muuttuja: vm=0, jos V1 on valkea, vm=1, jos V1 on musta
        ' Merkitään LAJ# negatiiviseksi, kun vastus on löytynyt.
        Dim valkea, musta As Integer
        If vm = 0 Then
            valkea = LAJ(V1)
            musta = LAJ(V2)
        End If
        If vm = 1 Then
            valkea = LAJ(V2)
            musta = LAJ(V1)
        End If
        KPARIT(KP - 1, 0) = valkea
        KPARIT(KP - 1, 1) = musta
        LAJ(V1) = -LAJ(V1)
        LAJ(V2) = -LAJ(V2)
        LAJNR(KP - 1, 0) = V1
        LAJNR(KP - 1, 1) = V2
    End Sub

    Sub Peruuta(ByRef V1 As Integer, ByRef V2 As Integer, _
                ByRef KP As Integer, ByRef peruutusInd As Integer)
        ' poistetaan löydös
        peruutusInd = V1 'vanha V1
        V1 = LAJNR(KP - 1, 0) 'V1 = peruutetun pelin pelaajan järjnro LAJ-taulukossa
        V2 = LAJNR(KP - 1, 1) 'V2 = vastustajan
        LAJ(V1) = -LAJ(V1)
        LAJ(V2) = -LAJ(V2)
        ' poistetaan vienti
        KPARIT(KP - 1, 0) = 0
        KPARIT(KP - 1, 1) = 0
    End Sub

    ' paritietojen päivitys ohjelman taulukkoon KPARIT
    Sub ParTyo1()
        Dim KR As Integer
        tiedPolku = hakemisto & "\PARIT." & tunnus
        FileOpen(9, tiedPolku, OpenMode.Random, , , Len(pa))
        KR = kierrosLaskuri
        ' Erase KPARIT
        KP = 0
        For J = 1 To aktiiviLkm \ 2
            FileGet(9, pa, 1000 * KR + J)
            KPARIT(KP, 0) = pa.VALKEA
            KPARIT(KP, 1) = pa.MUSTA
            KP = KP + 1
        Next J
        FileClose(9)
    End Sub

    Sub VieParit(ByRef KP As Integer, ByRef IND As Integer)
        ' KP on parien lukumäärä, IND on 1000*kierrosLaskuri
        Dim I, KR As Integer
        Dim PANRO As Integer

        ' viedään paritiedot talteen PARIT-tedostoon
        tiedPolku = hakemisto & "\PARIT." & tunnus
        FileOpen(9, tiedPolku, OpenMode.Random, , , Len(pa))
        KR = kierrosLaskuri

        ' parin numero on talletettu KPARIT-taulun viimeiselle sarakkeelle
        For I = 1 To KP
            pa.KIERROS = KR
            PANRO = KPARIT(I - 1, 2)
            pa.NRO = PANRO  ' parin numero
            ' KPARIT-taulussa on ensimmäisessä sarakkeessa valkean numero ja toisessa mustan
            pa.VALKEA = KPARIT(I - 1, 0)
            pa.MUSTA = KPARIT(I - 1, 1)
            FilePut(9, pa, IND + PANRO)
        Next I
        FileClose(9)

    End Sub

    Sub VieParienLkm()
        Dim KR As Integer
        tiedPolku = hakemisto & "\KILPAILU." & tunnus
        FileOpen(10, tiedPolku, OpenMode.Random, , , Len(ki))
        KR = kierrosLaskuri

        ' viedään aktiivipelaajien määrä talteen kierroksen KILPAILU-tietoihin
        FileGet(10, ki, KR)
        ki.KPLKM = KP
        ki.AKTIIVIT = aktiiviLkm
        ki.LKM = pelaajaLkm
        FilePut(10, ki, KR)

        ' päivitetään myös tietueen 20 tiedot
        FileGet(10, ki, 20)
        ki.KR = kierrosLaskuri
        ki.LKM = pelaajaLkm
        FilePut(10, ki, 20)
        FileClose(10)
    End Sub

    Sub TeePisteet(ByVal tulos As Integer, ByVal tyyppi As Integer, ByVal vastus As Integer, _
                   ByRef chrp As String, ByRef chrp2 As String, ByRef sngp As Single, _
                   ByRef PELATTU As Integer, ByRef SELOPIS As Single, ByRef NORPIS As Single)
        ' Tutkii annetun tuloksen, ja palauttaa tuloksen merkkijonona ja liukulukuna.
        ' tyyppi: 1=normaalisti mukana, 2=keskeyttänyt, 3=huilaaja, 0=uuden pelaajan aikaisemmat kierrokset
        ' Aliohjelma asettaa myös arvot vertailulistan teossa tarvittaville muuttujille 
        ' PELATTU, SELOPIS ja NORPIS.
        ' PELATTU =1, jos vahvuuslaskentaan kuuluva peli, 0= muutoin
        ' SELOPIS selolaskentaan liittyvä tulos (1, 0,5 tai 0)
        ' NORPIS pelaajan lopputulokseen vaikuttava piste (1, 0,5 TAI 0)

        ' Asetetaan kaikille rutiinin palauttamille parametreille oletusarvot
        chrp = " "
        chrp2 = " "
        sngp = 0.0
        PELATTU = 0
        SELOPIS = 0.0
        NORPIS = 0.0

        If tyyppi = 0 Or tyyppi = 2 Then
            chrp = " "  ' uusi myöhemmin mukaan tullut pelaaja tai keskeyttänyt
        End If
        If tyyppi = 1 Then
            If tulos >= 0 Then
                If tulos = 10 Then
                    chrp = "+"
                    If TAPA = -1 Then
                        chrp2 = "3"
                        sngp = 3.0
                    Else
                        chrp2 = "1"
                        sngp = 1.0
                    End If
                    SELOPIS = 1.0
                    NORPIS = 1.0
                End If
                If tulos = 5 Then
                    chrp = "="
                    If TAPA = -1 Then
                        chrp2 = "1"
                        sngp = 1.0
                    Else
                        chrp2 = "½"
                        sngp = 0.5
                    End If
                    SELOPIS = 0.5
                    NORPIS = 0.5
                End If
                If tulos = 0 Then
                    chrp = "-"
                    chrp2 = "0"
                    sngp = 0.0
                    SELOPIS = 0.0
                    NORPIS = 0.0
                End If
                PELATTU = 1
            End If
            If tulos < 0 Then
                If tulos = -1 Then
                    chrp = "V"
                    chrp2 = "V"
                    If TAPA = -1 Then sngp = 3 Else sngp = 1
                    SELOPIS = 0.0
                    NORPIS = 1.0
                End If
                If tulos = -2 Then
                    chrp = "L"
                    chrp2 = "L"
                    sngp = 0
                    SELOPIS = 0.0
                    NORPIS = 0.5
                End If
                If tulos = -3 Then
                    chrp = "X"
                    chrp2 = "X"
                    sngp = 0.5
                    SELOPIS = 0.0
                    NORPIS = 0.5
                End If
                If tulos = -4 Then
                    chrp = "Y"
                    chrp2 = "Y"
                    sngp = 0
                    SELOPIS = 0.0
                    NORPIS = 0.0
                End If
                If tulos = -10 Then
                    chrp = " "
                    chrp2 = " "
                    sngp = 0
                    SELOPIS = 0.0
                    NORPIS = 0.0
                End If
                PELATTU = 0
            End If ' tulos < 0
        End If ' tyyppi=1
        If tyyppi = 3 Then
            If tulos = -3 Then
                chrp = "X"
                chrp2 = "X"
                If TAPA = -1 Then sngp = 1 Else sngp = 0.5
                SELOPIS = 0.0
                NORPIS = 0.5
            End If ' tulos = -3
            PELATTU = 0
        End If ' tyyppi=3
    End Sub

    Sub TeeLappu(ByVal pelNro As Integer, ByVal valmus As Integer, ByRef lapunTiedot() As String)
        ' muodostaa parikortin pelaajatiedot tekstitaulukkoon lapunTiedot
        Dim arivi, peliRivi, vastLkm As String
        Dim omaSelo, vastSelo, ero, vSeloSum, kvah As Integer
        Dim sngPist, pistSum As Single
        Dim apu, chrPist, chrPist2 As String
        Dim i, it, ind, apit As Integer
        Dim odotusTulos, odotusSum As Integer
        Dim pelattu As Integer
        Dim selopis, norpis As Single

        ' Lapun nimirivi:
        FileGet(6, pe, pelNro)
        If valmus = 1 Then arivi = " Valkea:" Else arivi = " Musta: "
        arivi = arivi & String.Format("{0,4}", pelNro) & " " & pe.NIMI.ToUpper
        lapunTiedot(0) = arivi
        lapunTiedot(1) = Space(13) & pe.SEURA
        omaSelo = pe.SELO

        ' Lapun selo ja muuttuva selo:
        If pe.LKM <= 10 Then arivi = " Selo: U" Else arivi = " Selo:  "
        arivi = arivi & String.Format("{0,4}", pe.SELO)
        ind = 1000 * kierrosLaskuri
        FileGet(8, tu, ind + pelNro)
        arivi = arivi & " Muuttuva selo: " & String.Format("{0,4}", tu.SELO)
        lapunTiedot(2) = arivi

        ' Haetaan pelaajan kierroksittaiset vastustajatiedot, lasketaan keskivastus
        ' ja odotustulos. Samalla muodostetaan tekstirivi pelattujen pelien
        ' tiedoista, ja lasketaan saavutetut pisteet.
        peliRivi = ""
        chrPist = " "
        chrPist2 = " "
        apu = " "
        vSeloSum = 0
        vastLkm = 0
        pistSum = 0
        vastSelo = 0
        For i = 1 To kierrosLaskuri
            ind = 1000 * i + pelNro
            FileGet(8, tu, ind)
            If tu.VARI = 0 Then apu = "  "
            If tu.VARI = 1 Then apu = " v"
            If tu.VARI = 2 Then apu = " m"
            If tu.VASTUS <> 0 And (tu.TULOS >= 0 Or tu.TULOS = -10) Then
                'If tu.VASTUS <> 0 Then
                FileGet(6, pe, tu.VASTUS)
                vastSelo = pe.SELO
                vSeloSum = vSeloSum + vastSelo
                vastLkm = vastLkm + 1
            End If
            Call TeePisteet(tu.TULOS, tu.TYYPPI, tu.VASTUS, chrPist, chrPist2, sngPist, _
                            pelattu, selopis, norpis)
            pistSum = pistSum + sngPist
            If pelaajaLkm > 99 Then
                peliRivi = peliRivi & apu & chrPist & String.Format("{0,3}", tu.VASTUS)
            End If
            If pelaajaLkm < 100 Then
                peliRivi = peliRivi & apu & chrPist & String.Format("{0,2}", tu.VASTUS)
            End If
            odotusTulos = 0
            ' Jos on ollut vastustaja, haetaan odotustulos
            If vastSelo > 0 Then
                ero = Abs(omaSelo - vastSelo)
                For it = 0 To 50
                    If ero < seloRajat(it) Then Exit For
                    ' vahvemman pelaajan voiton todennäköisyys ei ole yli 92%
                    If omaSelo > vastSelo And it > 41 Then Exit For
                Next
                odotusTulos = 50 + Sign(omaSelo - vastSelo) * it
            End If
            odotusSum = odotusSum + odotusTulos
        Next i

        If vastLkm > 0 Then kvah = Int(vSeloSum / vastLkm + 0.5)
        arivi = " Keskivastus:" & String.Format("{0,5}", kvah)
        arivi = arivi & "  odotus: " & String.Format("{0:#0.00}", odotusSum / 100)
        lapunTiedot(3) = arivi
        arivi = ""

        ' Jos kierros on enemmän kuin 9, pitää pelitulokset jakaa
        ' kolmelle riville. Jos kierros on välillä 7-9  jaetaan kahdelle riville.
        If kierrosLaskuri <= 6 Then
            lapunTiedot(4) = peliRivi
            arivi = ""
        End If
        If kierrosLaskuri > 6 And kierrosLaskuri <= 9 Then
            If pelaajaLkm > 99 Then apit = 6 * 6 Else apit = 5 * 6
            lapunTiedot(4) = Mid(peliRivi, 1, apit)
            arivi = Mid(peliRivi, apit + 1) & "  "
        End If
        If kierrosLaskuri > 9 Then
            If pelaajaLkm > 99 Then apit = 6 * 6 Else apit = 5 * 6
            lapunTiedot(4) = Mid(peliRivi, 1, apit)
            If kierrosLaskuri <= 12 Then
                lapunTiedot(5) = Mid(peliRivi, apit + 1, apit)
                arivi = ""
            End If
            If kierrosLaskuri > 12 Then
                lapunTiedot(5) = Mid(peliRivi, apit + 1, apit)
                lapunTiedot(6) = Mid(peliRivi, 2 * apit + 1, Len(peliRivi))
                arivi = Mid(peliRivi, 2 * apit + 1) & "  "
            End If
        End If

        If kierrosLaskuri <= 9 Then
            lapunTiedot(5) = arivi & " Pisteet: " & String.Format("{0:0.0}", pistSum)
        Else
            'lapunTiedot(6) = arivi & " Pisteet:" & String.Format("{0,4:0.0}", pistSum)
            lapunTiedot(6) = arivi & " Pisteet: " & String.Format("{0:#0.0}", pistSum)
        End If
    End Sub

    Sub TulostusMarginaalit()
        ' Asetetaan oletusarvot tulosteiden marginaaleille.
        ' Pöytälappujen tulostus käyttää sivuasetuksille objektia PrintPageSettings2,
        ' muut tulosteet käyttävät objektia PrintPageSettings1
        ' Marginaali-asetukset ovat erilaiset.
        ' Marginaalit annetaan järjestyksessä left, right, top, bottom
        ' mittayksikkö mm
        ' Enablemetric on määritelty sekä Form5- että Tulostus-luokissa arvoksi True
        ' (PageSetupDialog1 properties)
        ' haluttu mm pitää kertoa luvulla 3.937, jotta maginaali tulisi oikein
        Tulostus.PageSetupDialog1.EnableMetric = True
        Form5.PageSetupDialog1.EnableMetric = True
        Dim margins1 As New Margins(10 * 3.937, 5 * 3.937, 5 * 3.937, 5 * 3.937)
        Dim margins2 As New Margins(5 * 3.937, 5 * 3.937, 5 * 3.937, 5 * 3.937)
        PrintPageSettings1.Margins = margins1
        PrintPageSettings2.Margins = margins2
        ' tätä moduulia kutsutaan heti ohjelman avauksessa
    End Sub


    Sub Vertailut(ByVal vertkierros As Integer, ByVal ensivert As Integer, _
                  ByVal toinenvert As Integer, ByVal kolmasvert As Integer)
        ' kierroskohtaiset selolaskennan muuntokertoimet ovat
        ' globaalissa taulukossa muuntoKerroin

        Dim NRO, NRR, NRU, SELO1, SUM, PELIT1 As Integer
        Dim PELSUM As Integer
        Dim rivi, edryh, chrp, chrp2 As String
        Dim IND, VNR, SIJJ, i As Integer
        Dim DESI, KRT, LIS, TULOSUM, PROS, RC, RP As Single
        Dim PKPL, SKPL, VASKPL, PELATTU As Integer
        Dim VASSUM, PPISTE, SPISTE, PISSUM, PNORPIS, SNORPIS As Single
        Dim ERO, CERO As Integer
        Dim ODOALK, ODOSEL As Single
        Dim TULOSKOD, TYYPPIKOD, VASTUSKOD As Integer
        Dim odotustulos, selMuutos, sngp, selopis, norpis As Single
        Dim OMASELO, SUORL, HLL As Integer
        Dim PLISA As Integer
        Dim alkuselot(pelaajaLkm) As Integer
        Dim mkerroin As Single
        Dim norAika, nopAika, eriAika As Integer

        ' Muodostetaan lopputulosten tarvitsema VERTAUS-tiedosto, lajitellaan tulostusta
        ' varten tiedot kolmen vertailutekijän mukaan, ja muodostetaan VERTAILU-tuloste.

        tiedPolku = hakemisto & "\VERTAILU." & tunnus
        FileOpen(1, tiedPolku, OpenMode.Output)
        tiedPolku = hakemisto & "\PERUS." & tunnus
        FileOpen(6, tiedPolku, OpenMode.Random, , , Len(pe))
        tiedPolku = hakemisto & "\TULOS." & tunnus
        FileOpen(8, tiedPolku, OpenMode.Random, , , Len(tu))
        tiedPolku = hakemisto & "\VERTAUS." & tunnus
        FileOpen(11, tiedPolku, OpenMode.Random, , , Len(VE))

        ' kierros, jolta vertailulista ajetaan, pitää olla jo kokonaan pelattu
        tiedPolku = hakemisto & "\KILPAILU." & tunnus
        FileOpen(10, tiedPolku, OpenMode.Random, , , Len(ki))
        FileGet(10, ki, vertkierros)
        FileClose(10)
        If ki.LASKU <> 1 Then
            MsgBox("Kierroksen " & vertkierros & " tuloksia puuttuu, " & _
                vbCrLf & "tämän kierroksen vertailulistaa ei voida muodostaa.")
            FileClose(1)
            FileClose(6)
            FileClose(8)
            FileClose(11)
            Exit Sub
        End If

        Call TeeSeloRajat()
        Call alustaTYP11()

        ' Uusien pelaajien alkuselojen laskenta
        For NRU = 1 To pelaajaLkm
            FileGet(6, pe, NRU)
            If pe.LKM <= 10 Then
                SELO1 = pe.SELO    ' uuden pelaajan alkuselo
                PELIT1 = pe.LKM    ' uuden pelaajan pelien lukumäärä
                VASSUM = 0.0
                PISSUM = 0
                PELSUM = 0
                For j = 1 To vertkierros
                    IND = 1000 * j
                    FileGet(8, tu, IND + NRU)
                    ' vain pelatut pelit mukaan
                    If tu.VASTUS <> 0 And tu.TULOS >= 0 Then
                        DESI = tu.TULOS / 10
                        PISSUM = PISSUM + DESI
                        PELSUM = PELSUM + 1
                        VNR = tu.VASTUS
                        FileGet(6, pe, VNR)
                        ' jos vastustaja on uusi pelaaja (pelien lkm=0), käytetään lukua 1525
                        If pe.LKM < 1 Then VASSUM = VASSUM + 1525.0 Else VASSUM = VASSUM + pe.SELO
                    End If
                Next j
                ' uuden pelaajan uusi selo
                If PELIT1 + PELSUM > 0 Then
                    If PELSUM > 0 Then
                        RC = VASSUM / PELSUM  ' vastustajien selojen ka
                        PROS = 100 * PISSUM / PELSUM
                    End If
                    RP = RC + 4 * (PROS - 50.0) 'turnauksesa saatu selo
                    TULOSUM = PELIT1 * SELO1 + PELSUM * RP
                    FileGet(11, VE, NRU)
                    VE.UUDETS = Int(TULOSUM / (PELIT1 + PELSUM) + 0.5)
                    alkuselot(NRU) = VE.UUDETS
                End If
            Else
                ' Muuttuja VE.UUDETS pitää alustaa nollalla, koska muuten 
                ' tiedostoon jää vanhaa roskaa, joka vaikeuttaa testausta.
                VE.UUDETS = 0
                alkuselot(NRU) = pe.SELO
            End If ' end of pe.LKM <=10
            FilePut(11, VE, NRU)
        Next NRU

        ' VERTAILU-tietueelle lasketaan pelaajille uudet selot, ja mahdollisten alkupelaajien
        ' uudet selot otetaan huomioon.
        ' Pelaajan tietojen keräys: 
        For NRO = 1 To pelaajaLkm
            SUM = 0 'vastustajien alkuselojen summa
            FileGet(11, VE, NRO)
            FileGet(6, pe, NRO)
            If pe.LKM <= 10 Then
                OMASELO = VE.UUDETS
            Else
                OMASELO = pe.SELO
            End If
            VE.SELO = pe.SELO

            ' haetaan ensin pelaajan 'omat' pohjatiedot
            NRR = NRO
            Call SUMPIS1(NRR, vertkierros, PELATTU, PKPL, PPISTE, PNORPIS, SKPL, SPISTE, SNORPIS)

            VASKPL = PKPL  ' pelatut pelit
            VE.TULOS = SPISTE 'pistetulos kaikista
            VE.NORPIS = 100 * PNORPIS 'pistetulos pelatuista 1 / 0.5 / 0
            VE.BUCH = 0
            VE.PRYHMA = 100 - pe.RYHNRO ' tulevaa lajittelua varten 'käänteisenä'
            ODOALK = 0.0
            ODOSEL = 0.0
            selMuutos = 0.0

            ' vastustajien tietojen muodostus
            For j = 1 To vertkierros
                mkerroin = muuntoKerroin(j)
                IND = 1000 * j
                FileGet(8, tu, IND + NRO)
                TULOSKOD = tu.TULOS
                TYYPPIKOD = tu.TYYPPI
                VASTUSKOD = tu.VASTUS
                chrp = " "
                chrp2 = " "
                If tu.VASTUS > 0 Then
                    Call TeePisteet(tu.TULOS, tu.TYYPPI, tu.VASTUS, _
                                    chrp, chrp2, sngp, PELATTU, selopis, norpis)
                    ' vastustajan tietojen keräys
                    NRR = tu.VASTUS
                    ' kutsua SUMPIS-rutiiniin tarvitaan vain Buchholz-pisteiden saamiseksi
                    Call SUMPIS1(NRR, vertkierros, PELATTU, PKPL, PPISTE, PNORPIS, _
                                 SKPL, SPISTE, SNORPIS)
                    If TULOSKOD >= 0 Then
                        VE.BUCH = VE.BUCH + PPISTE ' buchholz
                        ' käytetään vastustajille alkuSelot-taulukon tietoja
                        SUM = SUM + alkuselot(NRR) ' SUM vastustajien alkuselojen summa
                        ERO = OMASELO - alkuselot(NRR)
                        CERO = Abs(ERO)
                        ' If CERO > 735 Then CERO = 999
                        For i = 0 To 50
                            If CERO < seloRajat(i) Then Exit For
                            ' vahvemman pelaajan voiton todenäköisyys ei ole yli 92%
                            If OMASELO > alkuselot(NRR) And i > 41 Then Exit For
                        Next i
                        odotustulos = 50 + Sign(OMASELO - alkuselot(NRR)) * i
                        ODOALK = ODOALK + odotustulos
                        ODOSEL = ODOSEL + odotustulos

                        ' selojen muutos lasketaan joka kierrokselta,
                        ' ja niin sanotuille alkupelaajille (pelien lkm <=10)
                        ' käytetään edellä laskettuja uuden pelaajan seloja

                        ' selomuutoksen laskenta
                        If OMASELO >= 2050 Then
                            KRT = 20.0
                        ElseIf OMASELO >= 1950 And OMASELO < 2050 Then
                            KRT = 25.0
                        ElseIf OMASELO >= 1850 And OMASELO < 1950 Then
                            KRT = 30.0
                        ElseIf OMASELO >= 1750 And OMASELO < 1850 Then
                            KRT = 35.0
                        ElseIf OMASELO >= 1650 And OMASELO < 1750 Then
                            KRT = 40.0
                        Else
                            KRT = 45.0
                        End If
                        If Round(mkerroin * 10) = 3 And OMASELO >= 2300 Then mkerroin = 0.15
                        selMuutos = selMuutos + mkerroin * KRT * (100 * selopis - odotustulos)

                    End If ' end of tuloskod >0
                End If ' end of tu.VASTUS > 0
            Next j

            ' pisteet-odotustulos
            VE.TULODO = (VE.NORPIS - ODOALK) / 100
            VE.SELOKA = 0
            If VASKPL > 0 Then VE.SELOKA = Int(SUM / VASKPL + 0.5) ' Vastuskeskiarvo
            VE.PELIT = VASKPL

            SUORL = 0 'alkuarvo
            If VASKPL > 0 Then
                PROS = Int(VE.NORPIS / VASKPL + 0.5)    'VE.NORPIS kok.luku
                If PROS = 50 Then
                    SUORL = Int(SUM / VASKPL + 0.5)
                Else
                    If PROS > 50 Then HLL = PROS - 51
                    If PROS < 50 Then HLL = 49 - PROS
                    LIS = (seloRajat(HLL) + seloRajat(HLL + 1)) / 2
                    If LIS > 400 Then LIS = 400 ' äärilisäys +-400
                    If PROS < 50 Then LIS = -LIS
                    SUORL = Int((SUM / VASKPL) + LIS + 0.5)
                End If
            End If
            VE.SUORL = SUORL

            ' selomuutos kaikista peleistä

            PLISA = 10 * VASKPL  'lisä pelatuista peleistä =100-kert.
            VE.USELO = Int(OMASELO + (selMuutos + PLISA) / 100 + 0.5) 'uusi selo
            If VE.UUDETS > 0 Then VE.USELO = VE.UUDETS
            IND = 1000 * (vertkierros + 1)  'muuttuva selo seuraavalta kierrokselta
            FileGet(8, tu, IND + NRO)
            VE.MUSELO = tu.SELO  ' muuttuva selo
            FilePut(11, VE, NRO)

            ' tietojen lajittelussa käytetään pelaajaList-taulukkoa,
            ' ensimmäisenä tietona on pelaajanumero, joka ei kuulu lajittelukenttään,
            ' viimeiseksi pelaajanumero 'käänteisenä'
            rivi = String.Format("{0,5}", NRO)
            Call VertailuRivi(ensivert, rivi, VE)
            Call VertailuRivi(toinenvert, rivi, VE)
            Call VertailuRivi(kolmasvert, rivi, VE)
            rivi = rivi & String.Format("{0,5}", 1000 - NRO)
            pelaajaList(NRO - 1) = rivi
        Next NRO

        ' lajitellaan lajittelutekijöiden mukaan laskevaan järjestykseen
        Call ShellSort(pelaajaList, pelaajaLkm, 6, 21, -1)

        ' vertailulistan tulostus
        Dim O26(13) As String ' VERTAILUN SELITYKSET  kts. VERTEK
        O26(1) = " 1  Selo ennen turnausta"
        O26(2) = " 2  Palkintoryhmä"
        O26(3) = " 3  Pistetulos"
        O26(4) = " 4  Suoritusluku (keskivastus +- %-tuloksen seloero, enintään +-400)"
        O26(5) = " 5  Vastustajien selojen keskiarvo"
        O26(6) = " 6  Tulos - odotustulos (maximi odotustulos yhdestä pelistä 0,92)"
        O26(7) = " 7  Uusi tarkistamaton selo (0,92 ja +0,1 pistettä)"
        O26(8) = " 8  Kierroksen jälkeinen muuttuva selo"
        O26(9) = " 9  Buchholz eli vastustajien pisteiden summa"
        O26(10) = ""
        O26(11) = "Uuden pelaajan (U) selo on laskuissa uusi selo (sarake 7)"
        O26(12) = "Sarakkeet 4, 5, 6, 7 ja 9 on laskettu pelatuista peleistä"
        O26(13) = "Kun vertailu sama, niin pienemmän nron pelaaja voittaa"


        Dim apuLaj(pelaajaLkm - ryhLkm) As String ' ryhmien suoritusluvuiltaan 2. parhaat
        Dim TST As String = "  TASASELOTURNAUS. "
        Dim ryhma As String
        Dim apui, sarOts1, sarOts2, sarOts3 As Integer
        Dim TUODD As Single
        rivi = TST & "  PELAAJIEN VERTAILULUVUT"
        PrintLine(1, rivi)
        rivi = "  " & tunnus & "   " & Trim(otsikko) & "    Kierros:  " & vertkierros
        PrintLine(1, rivi)
        PrintLine(1, " ")
        For J = 1 To 13
            PrintLine(1, " " & O26(J))
        Next J
        ' muunnetaan vertailumääritykset vastaamaan tulosteen sarakeotsikoita
        Call VsarOsar(ensivert, sarOts1)
        Call VsarOsar(toinenvert, sarOts2)
        Call VsarOsar(kolmasvert, sarOts3)
        rivi = " Valittujen vertailutekijöiden sarakkeet ovat:  "
        rivi = rivi & sarOts1 & "  " & sarOts2 & "  " & sarOts3
        PrintLine(1, rivi)
        PrintLine(1, " ")
        rivi = Space(27) & "      1  2    3    4    5     6    7    8    9"
        If kierrosLaskuri > 10 Then
            rivi = Space(27) & "      1  2    3    4    5     6    7    8     9"
        End If
        PrintLine(1, rivi)
        rivi = " " & StrDup(72, "-")
        If kierrosLaskuri > 10 Then
            rivi = " " & StrDup(73, "-")
        End If
        PrintLine(1, rivi)
        edryh = ""
        SIJJ = 0
        apui = 0

        For i = 1 To pelaajaLkm
            NRO = Val(Mid(pelaajaList(i - 1), 1, 5))
            FileGet(11, VE, i)
            VE.SELONR = NRO  ' selonro palknron paikalle
            FilePut(11, VE, i)
            FileGet(11, VE, NRO)
            VE.PALKNR = i  ' selonron paikalle palknro
            FilePut(11, VE, NRO)

            FileGet(6, pe, NRO)
            ryhma = KIRJ(pe.RYHNRO)
            If edryh <> ryhma And ensivert = 0 Then
                PrintLine(1, " ")
                PrintLine(1, "  " & ryhma & "-ryhmä")
                SIJJ = 0
                edryh = ryhma
            End If
            SIJJ = SIJJ + 1 'sijanumero ryhmässä
            rivi = String.Format("{0,3}", NRO)
            rivi = rivi & "  " & pe.NIMI
            If VE.UUDETS > 0 Then rivi = rivi & "U" Else rivi = rivi & " "
            rivi = rivi & String.Format("{0,4}", VE.SELO)
            rivi = rivi & " " & ryhma
            rivi = rivi & String.Format("{0,5:0.0}", VE.TULOS)
            rivi = rivi & String.Format("{0,5}", VE.SUORL)
            rivi = rivi & String.Format("{0,5}", VE.SELOKA)
            ' LUKU = (VE.TULODO - 1000) / 100
            rivi = rivi & String.Format("{0,6:0.00}", VE.TULODO)
            rivi = rivi & String.Format("{0,5}", VE.USELO)
            rivi = rivi & String.Format("{0,5}", VE.MUSELO)
            If kierrosLaskuri < 11 Then
                rivi = rivi & String.Format("{0,5:0.0}", VE.BUCH)
            Else
                rivi = rivi & String.Format("{0,6:0.0}", VE.BUCH)
            End If

            PrintLine(1, rivi)
            ' tiedot kakkoslajitteluun (ryhmien voittajat jätetään pois)
            If SIJJ <> 1 Then
                rivi = String.Format("{0,6:0.00}", VE.TULODO + 100)
                rivi = rivi & String.Format("{0,4}", 1000 - NRO)
                apuLaj(apui) = rivi
                apui = apui + 1
            End If
        Next i

        ShellSort(apuLaj, apui, 1, 11, -1)
        rivi = " Parhaat sarakkeesta 6 (Tulos-Odotustulos), ryhmien voittajat on jätetty pois."
        PrintLine(1, " ")
        PrintLine(1, rivi)
        ' Ilmoitetaan kuusi parasta (enintään) 
        rivi = ""
        For i = 0 To 5
            If apuLaj(i) = "" Then Exit For
            NRO = 1000 - Val(Mid(apuLaj(i), 7, 4))
            TUODD = CSng(Mid(apuLaj(i), 1, 6))
            ' LUKU = TUODD / 100
            'TUODD = Val(Mid(apuLaj(i), 1, 6)) - 1000
            'LUKU = TUODD / 100
            rivi = rivi & "  " & String.Format("{0,5}", NRO)
            rivi = rivi & String.Format("{0,6:0.00}", TUODD - 100)
        Next
        PrintLine(1, rivi)

        ' Tietoja selolaskentaa varten
        norAika = 0
        nopAika = 0
        eriAika = 0
        For i = 1 To vertkierros
            If Int(muuntoKerroin(i) * 10.0) = 10 Then norAika = norAika + 1
            If Int(muuntoKerroin(i) * 10.0) = 3 Then nopAika = nopAika + 1
            If Int(muuntoKerroin(i) * 10.0) = 5 Then eriAika = eriAika + 1
        Next
        PrintLine(1, " ")
        rivi = "Tietoja selolaskentaan:"
        PrintLine(1, rivi)
        If norAika > 0 Then
            rivi = "Kertoimella 1,0 on pelattu kierrokset "
            For i = 1 To vertkierros
                If Int(muuntoKerroin(i) * 10.0) = 10 Then rivi = rivi & String.Format("{0,3}", i)
            Next
            PrintLine(1, rivi)
        End If
        If nopAika > 0 Then
            rivi = "Kertoimella 0,3 on pelattu kierrokset "
            For i = 1 To vertkierros
                If Int(muuntoKerroin(i) * 10.0) = 3 Then rivi = rivi & String.Format("{0,3}", i)
            Next
            PrintLine(1, rivi)
        End If
        If eriAika > 0 Then
            rivi = "Kertoimella 0,5 on pelattu kierrokset "
            For i = 1 To vertkierros
                If Int(muuntoKerroin(i) * 10.0) = 5 Then rivi = rivi & String.Format("{0,3}", i)
            Next
            PrintLine(1, rivi)
        End If

        FileClose(1)
        FileClose(6)
        FileClose(8)
        ' Viedään KILPAILU-tiedostoon talteen merkinnät vertailujen laskemisesta.
        ' Kierroskohtaisella tietueella:
        ' VERTLAS=0 vertailua i ole tehty ko kierrokselle
        ' VERTLAS=1 palkintoryhmä määräävä
        ' VERTLAS=2 jokin muu vertailu määräävänä
        ' Tietueelle 20 viimeiseksi suoritetun vertailun kierrosnumero.
        tiedPolku = hakemisto & "\KILPAILU." & tunnus
        FileOpen(10, tiedPolku, OpenMode.Random, , , Len(ki))
        FileGet(10, ki, vertkierros)
        If ensivert = 0 Then ki.VERTLAS = 1 Else ki.VERTLAS = 2
        FilePut(10, ki, vertkierros)
        FileGet(10, ki, 20)
        ki.VERTLAS = vertkierros
        FilePut(10, ki, 20)
        FileClose(10)
        FileClose(11)
        tiedPolku = hakemisto & "\VERTAILU." & tunnus
        Tulostus.Show()

    End Sub

    Sub TeeNettiTiedosto()
        ' Yhdistetään tekstitiedostot LOPPUTUL ja VERTAILU
        Dim nettinimi As String
        Dim kuluvaPaiva As String
        Dim rivi As String

        tiedPolku = hakemisto & "\LOPPUTUL." & tunnus
        FileOpen(1, tiedPolku, OpenMode.Input)
        tiedPolku = hakemisto & "\VERTAILU." & tunnus
        FileOpen(2, tiedPolku, OpenMode.Input)

        kuluvaPaiva = DateString
        ' päiväys muodossa MM-DD-YYYY
        ' nettinimi muodostuu seuraavasti: tunnus & vuoden viimeinen numero & kuukausi & päivä
        nettinimi = tunnus & kuluvaPaiva.Substring(9, 1) & kuluvaPaiva.Substring(0, 2)
        nettinimi = nettinimi & kuluvaPaiva.Substring(3, 2) & ".txt"

        tiedPolku = hakemisto & "\" & nettinimi
        FileOpen(3, tiedPolku, OpenMode.Output)

        Do Until EOF(1)
            rivi = LineInput(1)
            PrintLine(3, rivi)
        Loop
        ' tulostetaan pari tyhjää riviä ennen vertailutietoja
        PrintLine(3, " ")
        PrintLine(3, " ")
        Do Until EOF(2)
            rivi = LineInput(2)
            PrintLine(3, rivi)
        Loop

        FileClose(1)
        FileClose(2)
        FileClose(3)
        Form4.Label1.Text = "Tasaselo-sivustolle siirtoa varten" & vbCrLf _
            & "on muodostettu tiedosto " & vbCrLf & tiedPolku
    End Sub


    Sub SUMPIS1(ByVal NRR As Integer, ByVal vertkierros As Integer, _
                ByRef PELATTU As Integer, _
                ByRef PKPL As Integer, ByRef PPISTE As Single, ByRef PNORPIS As Single, _
                ByRef SKPL As Integer, ByRef SPISTE As Single, ByRef SNORPIS As Single)
        Dim JJ, IND As Integer
        Dim SELOPIS, NORPIS, sngp As Single
        Dim chrp As String = " "
        Dim chrp2 As String = " "

        ' alustetaan rutiinin palauttamat parametrit 
        PELATTU = 0
        PKPL = 0
        PPISTE = 0.0
        PNORPIS = 0.0
        SKPL = 0
        SPISTE = 0.0
        SNORPIS = 0.0

        For JJ = 1 To vertkierros
            IND = 1000 * JJ
            FileGet(8, tu, IND + NRR)
            Call TeePisteet(tu.TULOS, tu.TYYPPI, tu.VASTUS, chrp, chrp2, sngp, _
                PELATTU, SELOPIS, NORPIS)
            SKPL = SKPL + 1
            SNORPIS = SNORPIS + NORPIS
            SPISTE = SPISTE + sngp
            If PELATTU > 0 Then
                PKPL = PKPL + 1
                PNORPIS = PNORPIS + NORPIS
                PPISTE = PPISTE + sngp
            End If
        Next JJ
    End Sub

    Sub VertailuRivi(ByVal verTek As Integer, ByRef rivi As String, ByRef ve As Object)
        Select Case verTek
            Case 0
                rivi = rivi & String.Format("{0,5}", ve.PRYHMA)
            Case 1
                rivi = rivi & String.Format("{0,5}", ve.TULOS * 10)
            Case 2
                rivi = rivi & String.Format("{0,5}", ve.SUORL)
            Case 3
                rivi = rivi & String.Format("{0,5}", ve.SELOKA)
            Case 4
                ' tulostetaan tulos-odotustulos lajittelua varten
                ' sadalla liian suurena 
                rivi = rivi & String.Format("{0,6:0.00}", ve.TULODO + 100)
            Case 5
                ' Buchholz-vertailuluku myös kymmenkertaisena
                rivi = rivi & String.Format("{0,5}", ve.BUCH * 10)
        End Select
    End Sub

    Sub VsarOsar(vert As Integer, ByRef sarots As Integer)
        Select Case vert
            Case 0
                sarots = 2 ' palkintoryhmä
            Case 1
                sarots = 3 ' pistetulos
            Case 2
                sarots = 4 ' suoritusluku
            Case 3
                sarots = 5 ' vastustajien selojen keskiarvo
            Case 4
                sarots = 6 ' tulos - odotustulos
            Case 5
                sarots = 9 ' Buchholz
        End Select
    End Sub

    Sub EtsiKayttoHakemisto()
        Dim rivi As String
        Dim ipos As Integer
        Dim inifile As String
        ' Rutiini etsii ohjelman käynnistyshakemistosta tasaselow.ini-tiedostoa ja sieltä
        ' turnauksen tiedostojen hakemistoa. Jos tiedostoa tasaselow.ini-tiedostoa ei ole
        ' olemassa, se muodostetaan, ja käyttöhakemistolle annetaan
        ' oletusarvo C:\muuselo3
        ohjelmaHakemisto = My.Computer.FileSystem.CurrentDirectory
        'MsgBox(ohjelmaHakemisto)
        inifile = ohjelmaHakemisto & "\tasaselow.ini"
        If File.Exists(inifile) Then
            FileOpen(1, inifile, OpenMode.Input)
            rivi = LineInput(1)
            FileClose(1)
            ipos = rivi.IndexOf("hakemisto=")
            If ipos = 0 Then hakemisto = rivi.Substring(10) Else hakemisto = ""
        Else
            Try
                FileOpen(1, inifile, OpenMode.Output)
                PrintLine(1, "hakemisto=C:\muuselo3")
                hakemisto = "C:\muuselo3"
                FileClose(1)
            Catch ex As Exception
                MsgBox("tasaselow.ini-tiedoston luonti epäonnistui", )
            End Try
        End If
    End Sub

    Sub TalletaKayttoHakemisto()
        Dim inifile As String
        inifile = ohjelmaHakemisto & "\tasaselow.ini"
        FileOpen(1, inifile, OpenMode.Output)
        PrintLine(1, "hakemisto=" & hakemisto)
        FileClose(1)
    End Sub

    Sub TeeTilannetaulukko()

        Dim pelNim(pelaajaLkm - 1) As String ' pelaajien nimet
        ' pelLuvut taulukko:
        ' sarake 0 = pe.SELO tai tekninen selo
        ' sarake 1 = pe.LKM (pelien lukumäärä ennen kisaa)
        ' sarake 2 = pe.ryhnro (pelaajan ryhmänumero kisassa)
        Dim pelLuvut(pelaajaLkm - 1, 2) As Integer
        Dim kr, pnr As Integer
        Dim rivi, apurivi, apu, chrp, chrp2, chrPnr As String
        Dim chrAlkuSelo(pelaajaLkm - 1) As String
        Dim pelTulokset(pelaajaLkm - 1) As String
        Dim chrPistSum(pelaajaLkm - 1) As String
        Dim chrVastKa(pelaajaLkm - 1) As String
        Dim chrTulOdo(pelaajaLkm - 1) As String
        Dim chrLoppuSelo(pelaajaLkm - 1) As String
        Dim pelLajittelu(pelaajaLkm - 1) As String
        Dim vassum, pissum, pistSum, selopis, norpis, sngp, odot As Single
        Dim pelsum, vastka, pelattu As Integer
        Dim edryh, ryhma, nimi As String
        Dim nimiPit, sar1, sar2, mselokr As Integer
        Dim copypolku1, copypolku2 As String


        ' Tilannetaulukko muodostetaan käynnissä olevan kierroksen tiedoista.
        ' Jos kierroksen peli ei ole päättynyt, jätetään T-OD -sareke 
        ' ja MSELO -sarake ennalleen (T-OD edelliseltä kierrokselta).
        tiedPolku = hakemisto & "\KILPAILU." & tunnus
        FileOpen(10, tiedPolku, OpenMode.Random, , , Len(ki))
        FileGet(10, ki, kierrosLaskuri)
        kr = ki.KR
        mselokr = kr + 1

        FileClose(10)

        '  KIERROKSEN (kr) TILANNETAULUKON TULOSTUS
        tiedPolku = hakemisto & "\TULOKSET." & tunnus
        FileOpen(1, tiedPolku, OpenMode.Output)
        tiedPolku = hakemisto & "\PERUS." & tunnus
        FileOpen(6, tiedPolku, OpenMode.Random, , , Len(pe))
        tiedPolku = hakemisto & "\TULOS." & tunnus
        FileOpen(8, tiedPolku, OpenMode.Random, , , Len(tu))

        ' Kerätään tiedot
        ' Nimipit-muuttujaan tallettuu nimien pituuden maksimiarvo.
        ' Jos nimen pituus yli 18 merkkiä, otetaan etunimestä vain alkukirjain.
        nimiPit = 0
        For i = 1 To pelaajaLkm
            FileGet(6, pe, i)
            rivi = KIRJ(pe.RYHNRO)
            nimi = Trim(pe.NIMI)
            If Len(nimi) > 18 Then
                sar1 = InStr(nimi, " ")
                ' tutkitaan vielä, löytyykö toista välilyöntiä
                sar2 = InStr(sar1 + 1, nimi, " ")
                If sar2 > 0 And sar2 < 19 Then
                    sar1 = sar2
                    ' tutkitaan vielä, löytyykö kolmas välilyönti
                    sar2 = InStr(sar1 + 1, nimi, " ")
                    If sar2 > 0 And sar2 < 19 Then sar1 = sar2
                End If
                nimi = Mid(nimi, 1, sar1 + 1)
            End If
            If nimiPit < Len(nimi) Then nimiPit = Len(nimi)
            rivi = rivi & nimi
            pelNim(i - 1) = rivi
            pelLuvut(i - 1, 0) = pe.SELO
            pelLuvut(i - 1, 1) = pe.LKM
            pelLuvut(i - 1, 2) = pe.RYHNRO
        Next i

        Call TeeSeloRajat()

        ' Lasketaan pelaajille vahvuuslukuihin perustuvat vastustajien 
        ' selojen keskiarvot ja tulos-odotustulokset, ja muodostetaan lajittelutietue.
        ' Muodostetaan samalla taulukko pelaajien kierroksittaisista tuloksista, ja
        ' otetaan talteen muuttuvien selojen aloitusluku ja viimeisen kierroksen luku.
        For i = 1 To pelaajaLkm
            odot = 0.0
            vassum = 0.0
            pelsum = 0
            pistSum = 0.0
            pissum = 0.0  ' tulos-odotustulos lukujen laskentaan
            apurivi = ""
            chrp = " "
            chrp2 = " "
            apu = ""

            For j = 1 To kr
                FileGet(8, tu, 1000 * j + i)
                Call TeePisteet(tu.TULOS, tu.TYYPPI, tu.VASTUS,
                                   chrp, chrp2, sngp, pelattu, selopis, norpis)
                ' haetaan alkuselo ja pelien lukumäärä ennen kisaa pelaajan perustiedoista
                If j = 1 Then
                    If pelLuvut(i - 1, 1) <= 10 Then 'selo-pelien lkm ennen kisaa
                        chrAlkuSelo(i - 1) = "U" & String.Format("{0,4}", tu.SELO)
                    Else
                        chrAlkuSelo(i - 1) = " " & String.Format("{0,4}", tu.SELO)
                    End If
                End If

                ' vain pelit, joissa on vastustaja, vaikuttavat odotustulokseen
                If tu.VASTUS > 0 And tu.TULOS >= 0 Then
                    'pelsum = pelsum + 1
                    odot = odot + odotusTulos(pelLuvut(i - 1, 0), pelLuvut(tu.VASTUS - 1, 0))
                End If
                ' vastustajien vahvuuslukujen keskiarvoon otetaan mukaan kaikki pelit, joissa on vastustaja
                If tu.VASTUS > 0 Then
                    pelsum = pelsum + 1
                    vassum = vassum + pelLuvut(tu.VASTUS - 1, 0)
                End If

                If tu.VARI = 0 Then apu = "  "
                If tu.VARI = 1 Then apu = " v"
                If tu.VARI = 2 Then apu = " m"
                If tu.VASTUS = 0 Then apu = "  "
                'apurivi = apurivi & apu
                If pelaajaLkm > 99 Then
                    apurivi = apurivi & apu & chrp & String.Format("{0,3}", tu.VASTUS)
                End If
                If pelaajaLkm < 100 Then
                    apurivi = apurivi & apu & chrp & String.Format("{0,2}", tu.VASTUS)
                End If
                pistSum = pistSum + sngp
                pissum = pissum + selopis
            Next j

            pelTulokset(i - 1) = apurivi

            ' Pelaajan viimeisin muuttuva selo otetaan sen kierroksen
            ' tiedoista, jonka perusteella parit on muodostettu.
            ' Kyseessä on siis se muuttuva selo, jonka perusteella parit on muodostettu.
            FileGet(8, tu, 1000 * kr + i)
            ' jos muuttuvaa seloa ei vielä ole laskettu, jäetään tieto listalla tyhjäksi.
            chrLoppuSelo(i - 1) = String.Format("{0,4}", tu.SELO)
            chrPistSum(i - 1) = String.Format("{0,4:0.0}", pistSum)
            ' vastustajien vahvuuslukujen ka lasketaan, jos pelattuja pelejä on
            If pelsum > 0 Then
                vastka = Int(vassum / pelsum + 0.5)
                chrVastKa(i - 1) = String.Format("{0,4:0000}", vastka)
            Else
                vastka = 0
                chrVastKa(i - 1) = String.Format("{0,4:0000}", vastka)
            End If

            chrTulOdo(i - 1) = String.Format("{0,5:0.00}", pissum - odot)

            ' muodostetaan lajittelurivi
            ' "käänteinen ryhmä", pistesumma, vastustajien vahvuus-ka, "käänteinen pelaajanro", pelaajan numero
            ' Lajittelua varten tarvitaa sekä ryhmä että pelaajanumero, siinä muodossa, että
            ' rivit voidaan lajitella laskevaan järjestykseen koko lajittelukentän suhteen.
            apurivi = String.Format("{0,3:000}", 100 - pelLuvut(i - 1, 2))
            apurivi = apurivi & String.Format("{0,4:00.0}", pistSum)
            apurivi = apurivi & chrVastKa(i - 1)
            apurivi = apurivi & String.Format("{0,3}", pelaajaLkm - i)
            apurivi = apurivi & String.Format("{0,3}", i)
            pelLajittelu(i - 1) = apurivi
        Next i

        Call ShellSort(pelLajittelu, pelaajaLkm, 1, 14, -1)
        rivi = "  TILANNETAULUKKO "
        PrintLine(1, rivi)
        rivi = "  " & tunnus & "   " & Trim(otsikko) & "    Kierros:  " & kierrosLaskuri
        PrintLine(1, rivi)
        rivi = " "
        PrintLine(1, rivi)
        rivi = "  (Keskivahvuuden (VAST) laskennassa on mukana kaikkien kierrosten vastustajat,"
        PrintLine(1, rivi)
        rivi = "   mutta tulos-odotustulos (T-OD) on laskettu vain pelatuista peleistä.)"
        PrintLine(1, rivi)
        rivi = "  (alkuselo, tulokset, pisteet, selo-ka, tulos-odotus, muuttuva selo)"
        PrintLine(1, rivi)
        edryh = " "

        For i = 1 To pelaajaLkm
            chrPnr = Mid(pelLajittelu(i - 1), 15, 3)
            pnr = Val(chrPnr)

            ryhma = Mid(pelNim(pnr - 1), 1, 1)
            apu = ""
            If edryh <> ryhma Then
                PrintLine(1, " ")
                rivi = "  " & ryhma & "-RYHMÄ"
                For iots = 1 To kierrosLaskuri
                    If pelaajaLkm > 99 Then
                        apu = apu & String.Format("{0,5}", iots) & "."
                    End If
                    If pelaajaLkm < 100 Then
                        apu = apu & String.Format("{0,4}", iots) & "."
                    End If
                Next
                rivi = rivi & Space(nimiPit - 4) & " SELO" & apu
                rivi = rivi & "    P  VAST  T-OD MSELO"
                PrintLine(1, rivi)
                edryh = ryhma
            End If
            apu = pelNim(pnr - 1) & Space(16)
            apu = Mid(apu, 1, nimiPit + 1)
            Mid(apu, 1, 1) = " "
            rivi = chrPnr & apu & " " & chrAlkuSelo(pnr - 1)
            rivi = rivi & " " & pelTulokset(pnr - 1) & " " & chrPistSum(pnr - 1)
            rivi = rivi & " " & chrVastKa(pnr - 1) & " " & chrTulOdo(pnr - 1)
            rivi = rivi & " " & chrLoppuSelo(pnr - 1)
            PrintLine(1, rivi)
        Next i

        FileClose(1)
        FileClose(6)
        FileClose(8)
        tilannePaivitetty = True

        ' Luodaan vielä tuloksista toinen tiedosto nettisivustoa varten
        copypolku1 = hakemisto & "\TULOKSET." & tunnus
        copypolku2 = hakemisto & "\TULOKSET.TXT"
        Try
            System.IO.File.Copy(copypolku1, copypolku2, True)
        Catch ex As Exception
            MsgBox("Tiedoston " & copypolku2 & " luonti ei onnistunut")
        End Try

    End Sub

End Module