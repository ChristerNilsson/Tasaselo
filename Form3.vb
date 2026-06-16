Imports System.IO
Public Class Form3

    Private Sub Form3_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Label1.Text = ""
        tiedPolku = hakemisto & "\PERUS." & tunnus
        If Not File.Exists(tiedPolku) Then
            kierrosLaskuri = 0
            ' Jos turnauksen tiedostojen alustusta ei ole vielä tehty,
            ' saadaan pelaajien määrä PELAAJAT-tiedoston rivimäärästä
            Call LuePelaajat()
            ' estetään pelaajamuutokset, parien teko ja tulosten kirjaukset
            EiMuutoksiaUusiKierrosToolStripMenuItem.Enabled = False
            TulostenIlmoittaminenToolStripMenuItem.Enabled = False
            MsgBox("Turnauksen tiedostot on perustamatta, " & vbCrLf & _
                   "siirry takaisin TURNAUKSEN PERUSTOIMET-näytölle.")
            Me.Close()
        End If

        ParitToolStripMenuItem.Enabled = False
        ' ajantasaisen tulospyörityksen voi käynnistää vasta sen jälkeen, kun 
        ' tilannetaulukko on ajettu
        tiedPolku = hakemisto & "\TULOKSET." & tunnus
        If File.Exists(tiedPolku) Then
            AjantasatuloksetToolStripMenuItem.Enabled = True
        Else
            AjantasatuloksetToolStripMenuItem.Enabled = False
        End If

        ' haetaan meneillään olevan kierroksen numero
        tiedPolku = hakemisto & "\KILPAILU." & tunnus
        If File.Exists(tiedPolku) Then
            FileOpen(10, tiedPolku, OpenMode.Random, , , Len(ki))
            FileGet(10, ki, 20)
            kierrosLaskuri = ki.KR
            pelaajaLkm = ki.LKM
        Else
            kierrosLaskuri = 0
        End If
        ' jos yhtään kierrosta ei vielä ole käynnistetty, myös tulostuksiin pääsy ja
        ' tulostenkirjaus estetään
        If kierrosLaskuri = 0 Then
            TulosteetToolStripMenuItem.Enabled = False
            TulostenIlmoittaminenToolStripMenuItem.Enabled = False
            AjantasatuloksetToolStripMenuItem.Enabled = False
            Label2.Text = "Parien teko sallitaan, kun on" & vbCrLf &
                "kuitattu Pelaajamuutokset... -valikon alin kohta" & vbCrLf &
                "Ei Muutoksia  Uusi kierros"
        End If

        If kierrosLaskuri > 0 Then
            FileGet(10, ki, kierrosLaskuri)
            aktiiviLkm = ki.AKTIIVIT ' kierroksen aktiivipelaajien määrä
            KP = ki.KPLKM ' kierroksen parien lukumäärä
            ' Jos kierroksen parien tuloksia ei ole annettu, estetään pelaajamuutokset ja
            ' uusien parien teko.
            If ki.LASKU = 0 Then
                ParitToolStripMenuItem.Enabled = False
                PelaajamuutoksetToolStripMenuItem.Enabled = False
            End If
            Label1.Text = "Kierros " & kierrosLaskuri
        End If
        FileClose(10)
        ' Suljetaan mahdollisesti avoinna oleva Muut toimet ja Lopputulokset -ikkuna
        ' Form4.Close()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Me.Close()
    End Sub

    Private Sub EiMuutoksiaUusiKierrosToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles EiMuutoksiaUusiKierrosToolStripMenuItem.Click

        ' Tässä valikkotapahtumassa siirrytään seuraavalle kierrokselle,
        ' käynnissä olevan kierroksen kaikki tulokset tulee olla annettuin ja uusien selojen laskettuna
        ' sekä kaikkien pelaajamuutosten pitää olla tehtyinä.

        tiedPolku = hakemisto & "\PERUS." & tunnus
        If Not File.Exists(tiedPolku) Then
            ParitToolStripMenuItem.Enabled = False
            Exit Sub
        End If

        If kierrosLaskuri = kierrosMaara Then
            MsgBox("Turnauksen kierrosmäärä " & kierrosMaara & " on täynnä! " &
                   vbCrLf & "Siirry lopputulosten laskentaan" &
                   vbCrLf & "Jos haluat kasvattaa kilpailun" &
                   " kierrosmäärää, siirry Muut toimet -osioon.")
            PelaajamuutoksetToolStripMenuItem.Enabled = False
            ParitToolStripMenuItem.Enabled = False
            Form4.LisataanKilpailuunUusiKierrosToolStripMenuItem.Enabled = True
            FileClose(10)
            Exit Sub
        End If

        ' Lasketaan tulevan kierroksen aktiivien lukumäärä
        tiedPolku = hakemisto & "\TULOS." & tunnus
        FileOpen(8, tiedPolku, OpenMode.Random, , , Len(tu))
        aktiiviLkm = 0
        For i = 1 To pelaajaLkm
            ' aktiivipelaajat on merkattu tulevan kierroksen tietoihin
            FileGet(8, tu, 1000 * (kierrosLaskuri + 1) + i)
            If tu.TYYPPI = 1 Then
                aktiiviLkm = aktiiviLkm + 1
            End If
        Next
        FileClose(8)

        If aktiiviLkm Mod 2 <> 0 Then
            MsgBox("Uuden kierroksen pelaajien määrä on pariton " & "(" & aktiiviLkm & ")" _
                    & vbCrLf & "tee vielä muutoksia pelaajatietoihin")
            Exit Sub
        End If

        tiedPolku = hakemisto & "\KILPAILU." & tunnus
        FileOpen(10, tiedPolku, OpenMode.Random, , , Len(ki))
        ' tietuenumerolla 20 on aktiivisen kierroksen numero 
        ' FileGet(10, ki, 20)

        If kierrosLaskuri = 0 Then
            kierrosLaskuri = 1
        Else
            FileGet(10, ki, kierrosLaskuri)
            If ki.LASKU = 1 Then
                kierrosLaskuri = kierrosLaskuri + 1
            End If
        End If
        FileClose(10)


        ' kierrostieto, aktiivipelaajien määrä ja parien määrä viedään
        ' talteen kierroksen KILPAILU-tietoihin vasta parien teon jälkeen
        ' aliohjelmassa VieParienLKm

        Label1.Text = "Kierros " & kierrosLaskuri
        ' kierroslaskuri viedään KILPAILU-tiedostoon vasta sitten kun parit on tehty
        Label2.Text = ""

        ' Tämän vaiheen jälkeen uusien parien muodostaminen on sallittua,
        ' mutta pelaajatietojen muuttaminen estetään, kunnes
        ' uuden kierroksen tulokset on kirjattu.
        ParitToolStripMenuItem.Enabled = True
        PelaajamuutoksetToolStripMenuItem.Enabled = False
        TulosteetToolStripMenuItem.Enabled = False
        TulostenIlmoittaminenToolStripMenuItem.Enabled = False
        AjantasatuloksetToolStripMenuItem.Enabled = False
        ' Form4.KorjauksetToolStripMenuItem.Enabled = False
    End Sub


    Private Sub ParitToolStripMenuItem_Click(sender As Object, e As EventArgs) _
        Handles ParitToolStripMenuItem.Click
        Dim paritTehty As Boolean

        tiedPolku = hakemisto & "\TULOS." & tunnus
        If Not File.Exists(tiedPolku) Then Exit Sub

        Call KierroksenParit(paritTehty)

        ' kierrostieto viedään talteen KILPAILU-tietoihin vasta
        ' samalla kun parit viedään PARIT-tiedostoon

        ' Jos parien muodostaminen on tehty, estetään uusien parien muodostaminen.
        ' Myöskään uusia seloja ei saa laskea ennen kuin kierroksen tulokset on annettu.
        ' Ajantasaista tulostusta varten ilmoitus että tietoja on muutettu,
        ' ja muodostetaan uusi tilannetaulukko (TULOKSET.tunnus).
        If paritTehty Then
            Call TeeTilannetaulukko()
            tilannePaivitetty = True
            ParitToolStripMenuItem.Enabled = False
            TulosteetToolStripMenuItem.Enabled = True
            ParilistaToolStripMenuItem.Enabled = True
            TulostenIlmoittaminenToolStripMenuItem.Enabled = True
            ' suljetaan edellisen kierroksen tulosten syöttölomake
            TulostenKirjaus.Close()
        Else
            ParitToolStripMenuItem.Enabled = False
            TulosteetToolStripMenuItem.Enabled = True
            PelaajamuutoksetToolStripMenuItem.Enabled = True
            TulostenIlmoittaminenToolStripMenuItem.Enabled = False
            AjantasatuloksetToolStripMenuItem.Enabled = True
            ' palautetaan kierroslaskuri arvoon ennen parien tekoa
            kierrosLaskuri = kierrosLaskuri - 1
            Label1.Text = "Kierros " & kierrosLaskuri
        End If
    End Sub

    Private Sub ParilistaToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ParilistaToolStripMenuItem.Click
        Dim rivi, chrPist, chrPist2, chrT1, chrT2 As String
        Dim nimi, apuseura As String
        Dim NR1, NR2 As Integer
        Dim pelattu, seurapit, nimipit, i As Integer
        Dim sngPist, selopis, norpis As Single

        ' päivitetään KPARIT-taulukko
        Call ParTyo1()

        '  KIERROKSEN (KR) PARIEN TULOSTUS
        tiedPolku = hakemisto & "\PARILIST." & tunnus
        FileOpen(1, tiedPolku, OpenMode.Output)
        tiedPolku = hakemisto & "\PERUS." & tunnus
        FileOpen(6, tiedPolku, OpenMode.Random, , , Len(pe))
        tiedPolku = hakemisto & "\TULOS." & tunnus
        FileOpen(8, tiedPolku, OpenMode.Random, , , Len(tu))

        rivi = "   " & tunnus & "  " & Trim(otsikko) & "     Kierros: " & kierrosLaskuri
        PrintLine(1, rivi)
        PrintLine(1, " ")
        PrintLine(1, " ")
        chrPist = " "
        chrPist2 = " "

        For i = 0 To KP - 1
            NR1 = KPARIT(i, 0)
            NR2 = KPARIT(i, 1)
            FileGet(6, pe, NR1)
            nimi = pe.NIMI & " "
            apuseura = Trim(pe.SEURA)
            nimipit = Len(Trim(nimi))
            seurapit = Len(apuseura)
            ' seuratieto otetaan mukaan, jos se mahtuu riville
            If nimipit + seurapit < 26 And seurapit > 0 Then nimi = nimi.Insert(26 - seurapit, apuseura)
            nimi = nimi.Substring(0, 26)
            rivi = String.Format("{0,3}", i + 1) & String.Format("{0,4}", NR1) & " " & nimi & " "
            chrT1 = " __ "
            chrT2 = " __ "
            ' jos pelin tulos on jo selvillä, se näytetään tulosteella
            FileGet(8, tu, 1000 * kierrosLaskuri + NR1)
            If tu.TULOS <> -10 Then
                Call TeePisteet(tu.TULOS, tu.TYYPPI, tu.VASTUS, chrPist, chrPist2, sngPist, _
                             pelattu, selopis, norpis)
                chrT1 = "  " & chrPist2 & " "
            End If
            FileGet(8, tu, 1000 * kierrosLaskuri + NR2)
            If tu.TULOS <> -10 Then
                Call TeePisteet(tu.TULOS, tu.TYYPPI, tu.VASTUS, chrPist, chrPist2, sngPist, _
                             pelattu, selopis, norpis)
                chrT2 = " " & chrPist2 & "  "
            End If
            rivi = rivi & chrT1 & "-" & chrT2
            FileGet(6, pe, NR2)
            nimi = pe.NIMI & " "
            apuseura = Trim(pe.SEURA)
            nimipit = Len(Trim(nimi))
            seurapit = Len(apuseura)
            ' seuratieto otetaan mukaan, jos se mahtuu riville
            If nimipit + seurapit < 26 And seurapit > 0 Then nimi = nimi.Insert(26 - seurapit, apuseura)
            nimi = nimi.Substring(0, 26)
            rivi = rivi & String.Format("{0,4}", NR2) & " " & nimi
            PrintLine(1, rivi)
            PrintLine(1, " ")
        Next i
        FileClose(1)
        FileClose(6)
        FileClose(8)

        tiedPolku = hakemisto & "\PARILIST." & tunnus
        ' suljetaan mahdollisesti avoinna oleva tulostus-ikkuna
        Tulostus.Close()
        Tulostus.Show()
    End Sub

    Private Sub TulostenIlmoittaminenToolStripMenuItem_Click(sender As Object, e As EventArgs) _
        Handles TulostenIlmoittaminenToolStripMenuItem.Click
        ' Jos tulostenkirjausten yhteydessä havaitaan, että kierroksen kaikkien parien
        ' tulokset on annettu, lasketaan uudet selot, ja viedään tieto laskennasta KILPAILU-tiedostoon.
        Label2.Text = ""
        TulostenKirjaus.Show()
    End Sub

    Private Sub AjantasatuloksetToolStripMenuItem_Click(ByVal sender As Object, ByVal e As EventArgs) _
        Handles AjantasatuloksetToolStripMenuItem.Click
        AjantasTulos.Show()
    End Sub

    Private Sub PelaajamuutoksetToolStripMenuItem_Click(sender As Object, e As EventArgs) _
        Handles PelaajamuutoksetToolStripMenuItem.Click
    End Sub

    Private Sub TulosteetToolStripMenuItem_Click(sender As Object, e As EventArgs) _
        Handles TulosteetToolStripMenuItem.Click
        Label2.Text = ""
    End Sub


    Sub TilannetaulukkoToolStripMenuItem_Click(sender As Object, e As EventArgs) _
        Handles TilannetaulukkoToolStripMenuItem.Click
        Call TeeTilannetaulukko()
        tiedPolku = hakemisto & "\TULOKSET." & tunnus
        ' suljetaan ensin mahdollisesti avoinna oleva tulostus-ikkuna
        Tulostus.Close()
        Tulostus.Show()
        ' tilannetaulukon luonnin jälkeen voidaan ajantasatulostus sallia
        AjantasatuloksetToolStripMenuItem.Enabled = True
    End Sub

    Private Sub PöytälaputToolStripMenuItem_Click(sender As Object, e As EventArgs) _
        Handles PöytälaputToolStripMenuItem.Click
        Dim rivi, apu As String
        Dim valkeantiedot(6), mustantiedot(6) As String
        Dim ip As Integer
        Dim NR1, NR2 As Integer

        '  KIERROKSEN (KR) PARILAPPUJEN TULOSTUS
        tiedPolku = hakemisto & "\LAPUT." & tunnus
        FileOpen(5, tiedPolku, OpenMode.Output)
        tiedPolku = hakemisto & "\PERUS." & tunnus
        FileOpen(6, tiedPolku, OpenMode.Random, , , Len(pe))
        tiedPolku = hakemisto & "\TULOS." & tunnus
        FileOpen(8, tiedPolku, OpenMode.Random, , , Len(tu))

        Call ParTyo1()
        ' muuttujat KPARIT, KP ja KR päivittyivät

        ' luodaan ensin seloRajat-taulu
        Call TeeSeloRajat()

        For ip = 1 To KP
            ' alustetaan valkean ja mustan paritiedot
            For i = 0 To 6
                valkeantiedot(i) = Space(41)
                mustantiedot(i) = Space(41)
            Next
            NR1 = KPARIT(ip - 1, 0)
            NR2 = KPARIT(ip - 1, 1)
            ' PrintLine(5, " ")
            ' valkean parikortin tiedot
            Call TeeLappu(NR1, 1, valkeantiedot)
            Call TeeLappu(NR2, 2, mustantiedot)

            For i = 0 To 6
                rivi = Space(81)
                Mid(rivi, 2, 38) = valkeantiedot(i)
                Mid(rivi, 44) = mustantiedot(i)
                If i = 1 Then
                    apu = "Pari: " & ip
                    Mid(rivi, 3, 9) = apu
                End If
                ' If i = 1 Then Mid(rivi, 2, 11) = " Pari: " & String.Format("{0,4}", ip)
                If i = 3 Then Mid(rivi, 37, 5) = "__-__"
                PrintLine(5, rivi)
            Next
            ' jos kierroksia yli 9, lisätään yksi tyhjä rivi lappujen väliin
            If kierrosLaskuri > 9 Then PrintLine(5, " ")
        Next ip

        FileClose(5)
        FileClose(6)
        FileClose(8)
        tiedPolku = hakemisto & "\LAPUT." & tunnus
        Form5.Show()

    End Sub


    Private Sub PelaajaAloittaaToolStripMenuItem_Click(sender As Object, e As EventArgs) _
        Handles PelaajaAloittaaToolStripMenuItem.Click
        Form6Param = "lisays"
        Form6.ShowDialog()
    End Sub

    Private Sub PelaajaKeskeyttääToolStripMenuItem_Click(sender As Object, e As EventArgs) _
        Handles PelaajaKeskeyttääToolStripMenuItem.Click
        Form7Param = "keskeytys"
        Form7.ShowDialog()
    End Sub

    Private Sub PelaajaHuilaaToolStripMenuItem1_Click(sender As Object, e As EventArgs) _
        Handles PelaajaHuilaaToolStripMenuItem1.Click
        Form7Param = "huilaus"
        Form7.ShowDialog()
    End Sub

    Private Sub PelaajaPalaaToolStripMenuItem_Click(sender As Object, e As EventArgs) _
        Handles PelaajaPalaaToolStripMenuItem.Click
        Form7Param = "takaisin"
        Form7.ShowDialog()
    End Sub

End Class