# Chess tournament pairing code excerpt

Source files:

- `Form3.vb`, lines 151-176: UI menu handler that starts pairing.
- `Module1.vb`, lines 583-1323: pairing implementation and persistence.

The main pairing routine is `KierroksenParit`. It builds a rating-sorted list of active players, delegates round 1 to `EkanKierroksenParit`, delegates later rounds to `MuunKierroksenParit`, and then stores the number of pairs in the tournament file.

## Translated Identifiers

Main procedures:

| Original | English name |
| --- | --- |
| `KierroksenParit` | `BuildRoundPairings` |
| `EkanKierroksenParit` | `BuildFirstRoundPairings` |
| `KolmeEkaaArvotaan` | `RandomizeFirstThreePairs` |
| `KolmeEkaaAnnetaan` | `EnterFirstThreePairs` |
| `arvonta` | `RandomSwap` |
| `MuunKierroksenParit` | `BuildLaterRoundPairings` |
| `TasoitaVarit` | `BalanceColors` |
| `variSel` | `SelectColorByRating` |
| `TeePari` | `MakePair` |
| `Peruuta` | `UndoPair` |
| `ParTyo1` | `LoadPairTable` |
| `VieParit` | `SavePairs` |
| `VieParienLkm` | `SavePairCount` |

Common parameters and local variables:

| Original | English name |
| --- | --- |
| `paritTehty` | `pairingCompleted` |
| `arrLaj` | `sortedActivePlayers` |
| `indakt` | `activeIndex` |
| `paritOK` | `pairingOk` |
| `aktiiviLkm` | `activePlayerCount` |
| `pelaajaLkm` | `playerCount` |
| `kierrosLaskuri` | `roundNumber` |
| `NRO`, `NRO1`, `NRO2` | `playerNo`, `player1No`, `player2No` |
| `V1`, `V2` | `candidate1Index`, `candidate2Index` |
| `KP` | `pairCount` |
| `SS` | `pairIndex` |
| `IND` | `recordBaseIndex` |
| `SELO1`, `SELO2` | `rating1`, `rating2` |
| `VARI1`, `VARI2` | `color1`, `color2` |
| `kolmeAnnetaan` | `firstThreeEntered` |
| `VALS`, `MUSS` | `whiteCount`, `blackCount` |
| `MUSE` | `colorHistoryCode` |
| `hlkm` | `byeCount` |
| `pelVastus` | `previousOpponents` |
| `loytyi` | `found` |
| `variEhto` | `enforceColorRule` |
| `VALMUS` | `whiteBlackImbalance` |
| `VMERO1`, `VMERO2` | `colorDifference1`, `colorDifference2` |
| `LS` | `searchDirection` |
| `aloitus`, `lopetus` | `startIndex`, `endIndex` |
| `rajoitin` | `attemptCounter` |
| `jatkuuko` | `continueSearch` |
| `peruutusInd` | `undoIndex` |
| `valkea`, `musta` | `whitePlayer`, `blackPlayer` |

Important global tables:

| Original | English meaning |
| --- | --- |
| `KPARIT(pair, 0)` | White player number |
| `KPARIT(pair, 1)` | Black player number |
| `KPARIT(pair, 2)` | Board/pair number |
| `LAJ(index)` | Active players in sorted order; negative means already paired |
| `LAJNR(pair, 0/1)` | Saved `LAJ` indexes used for backtracking |
| `VARIT(player, 0)` | Number of White games |
| `VARIT(player, 1)` | Number of Black games |
| `VARIT(player, 2)` | Encoded color history |
| `VARIT(player, 3)` | Active in current round |
| `VARIT(player, 4)` | Number of bye/rest rounds |

The following excerpts use translated English names. They are not meant to be pasted back into the old VB project without also renaming the declarations and call sites.

```vb
Sub BuildRoundPairings(ByRef pairingCompleted As Boolean)
    Dim sortedActivePlayers(activePlayerCount) As Integer
    Dim activeIndex As Integer
    Dim pairingOk As Boolean

    ' Read rating data and put it into the sorting vector.
    filePath = dataDirectory & "\TULOS." & tournamentCode
    FileOpen(8, filePath, OpenMode.Random, , , Len(resultRecord))

    activeIndex = 0
    ' Build the sorting vector.
    ' Because the data must be sorted descending, the player number is also
    ' converted into descending form: player 1 becomes playerCount - 1,
    ' player 2 becomes playerCount - 2, etc.
    ' When the sorting vector is decoded, this conversion must be reversed.
    For i = 1 To playerCount
        FileGet(8, resultRecord, 1000 * roundNumber + i)
        If resultRecord.TYPE = 1 Then
            sortedActivePlayers(activeIndex) = 1000 * resultRecord.RATING + playerCount - i
            activeIndex = activeIndex + 1
        End If
    Next
    FileClose(8)

    Call SortDescending(sortedActivePlayers)

    If roundNumber = 1 Then Call BuildFirstRoundPairings(sortedActivePlayers)
    pairingOk = True
    If roundNumber > 1 Then Call BuildLaterRoundPairings(pairingOk, sortedActivePlayers)

    If pairingOk Then
        Call SavePairCount() ' Store pair count in the tournament file.
        pairingCompleted = True
    Else
        pairingCompleted = False
    End If
End Sub
```

## Round 1

Round 1 pairs the sorted player list sequentially. Before pairing, the program can either randomize the first three pairs or let the operator enter them manually. Colors are then assigned with `variSel`, unless the first three pairs were entered manually, in which case those entered orders define the colors.

```vb
Sub BuildFirstRoundPairings(ByRef sortedActivePlayers)
    ' ...
    pairCount = 0
    For candidate1Index = 0 To activePlayerCount - 1 Step 2
        pairCount = pairCount + 1
        player1No = playerCount - sortedActivePlayers(candidate1Index) Mod 1000
        player2No = playerCount - sortedActivePlayers(candidate1Index + 1) Mod 1000
        roundPairs(pairCount - 1, 0) = player1No
        roundPairs(pairCount - 1, 1) = player2No
        roundPairs(pairCount - 1, 2) = pairCount
    Next candidate1Index

    ' Assign colors for the first round.
    ' SelectColorByRating returns 0 if rating1 implies White.
    ' If the first three pairs were entered manually, their colors were
    ' defined at the same time.
    If firstThreeEntered And pairIndex < 3 Then
        color1 = 1
        color2 = 2
    Else
        selectedColor = SelectColorByRating(rating1, rating2)
        If selectedColor = 0 Then
            color1 = 1
            color2 = 2
        Else
            color1 = 2
            color2 = 1
        End If
    End If
    ' ...
End Sub
```

## Later Rounds

`MuunKierroksenParit` is the core algorithm for later rounds.

It first gathers each player's previous color counts, color history and opponents. The `VARIT` table means:

- column 0: number of games as White
- column 1: number of games as Black
- column 2: encoded color history
- column 3: whether the player is active in the current round
- column 4: number of bye/rest rounds

Then it loads active players into `LAJ` in rating order and tries to build pairs without repeat opponents and with acceptable color balance.

```vb
' Gather all players' color counts from previous rounds.
' Build opponent and color-history tables:
' column 0 = number of games played as White
' column 1 = number of games played as Black
' column 2 = encoded value of Black color history
' column 3 = whether the player participates in the current round
' column 4 = number of bye/rest rounds taken
For i = 0 To pelaajaLkm - 1
    VALS = 0
    MUSS = 0
    MUSE = 0
    hlkm = 0
    For j = 0 To kierrosLaskuri - 2
        FileGet(8, tu, 1000 * (j + 1) + i + 1)
        If tu.VASTUS <> 0 Then
            If tu.VARI = 1 Then VALS = VALS + 1
            If tu.VARI = 2 Then MUSS = MUSS + 1
            MUSE = MUSE + (2 ^ (j + 1)) * tu.VARI
            pelVastus(i, j) = tu.VASTUS
        End If
        If tu.VASTUS = 0 Then hlkm = hlkm + 1
    Next j
    VARIT(i, 0) = VALS
    VARIT(i, 1) = MUSS
    VARIT(i, 2) = MUSE
    VARIT(i, 3) = 0
    VARIT(i, 4) = hlkm
Next i
```

Translated version of the same block:

```vb
For playerIndex = 0 To playerCount - 1
    whiteCount = 0
    blackCount = 0
    colorHistoryCode = 0
    byeCount = 0
    For previousRoundIndex = 0 To roundNumber - 2
        FileGet(8, resultRecord, 1000 * (previousRoundIndex + 1) + playerIndex + 1)
        If resultRecord.OPPONENT <> 0 Then
            If resultRecord.COLOR = 1 Then whiteCount = whiteCount + 1
            If resultRecord.COLOR = 2 Then blackCount = blackCount + 1
            colorHistoryCode = colorHistoryCode + (2 ^ (previousRoundIndex + 1)) * resultRecord.COLOR
            previousOpponents(playerIndex, previousRoundIndex) = resultRecord.OPPONENT
        End If
        If resultRecord.OPPONENT = 0 Then byeCount = byeCount + 1
    Next previousRoundIndex
    colors(playerIndex, 0) = whiteCount
    colors(playerIndex, 1) = blackCount
    colors(playerIndex, 2) = colorHistoryCode
    colors(playerIndex, 3) = 0
    colors(playerIndex, 4) = byeCount
Next playerIndex
```

Main search logic:

```vb
Do While activePlayerCount - 2 * pairCount > 0
    ' Pick first unpaired player in the current search direction.
    For i = startIndex To endIndex Step searchDirection
        If sortedPlayerIndexes(i) > 0 Then Exit For
    Next
    candidate1Index = i

    For candidate2Index = startIndex To endIndex Step searchDirection
        found = False
        colorChoice = 9
        If sortedPlayerIndexes(candidate2Index) > 0 And candidate1Index <> candidate2Index Then
            ' First check that the players have not already met.
            player1No = sortedPlayerIndexes(candidate1Index)
            player2No = sortedPlayerIndexes(candidate2Index)
            found = True
            For j = 0 To roundNumber - 1
                If player2No = previousOpponents(player1No - 1, j) Then
                    found = False
                    Exit For
                End If
            Next

            If found = True Then
                ' If a candidate pair was found, also check color suitability.
                colorDifference1 = player1WhiteCount - player1BlackCount
                colorDifference2 = player2WhiteCount - player2BlackCount
                colorChoice = 9

                ' colorChoice = 0 means candidate1 gets White and candidate2 gets Black.
                ' colorChoice = 1 means candidate1 gets Black and candidate2 gets White.
                ' colorChoice = 9 means no valid color assignment was found.
            End If
        End If

        If found And colorChoice <> 9 Then
            pairCount = pairCount + 1
            Call MakePair(candidate1Index, candidate2Index, pairCount)
            Exit For
        End If
    Next candidate2Index

    If Not found Then
        If pairCount > 0 Then
            Call UndoPair(candidate1Index, candidate2Index, pairCount, undoIndex)
            pairCount = pairCount - 1
            candidate1Index = candidate1Index - searchDirection
        End If
    End If
Loop
```

The search alternates direction after each found pair:

- `LS = 1`: search from start to end.
- `LS = -1`: search from end to start.

If the search gets stuck, it backtracks by undoing the last pair. After 400 attempts, the user is asked whether to continue while ignoring color constraints.

## Color Selection

`variSel` resolves color when two players have the same color history. It compares the mutable ratings digit by digit from the end. The player with the smaller differing digit gets White. If the ratings are equal, color is randomized.

```vb
Function SelectColorByRating(ByVal mutableRating1 As Integer, ByVal mutableRating2 As Integer) As Integer
    ' Selects the first pairing candidate's color using mutable ratings
    ' when the players' color histories are equal.
    ' Returns 0 if the mutableRating1 player gets White, otherwise 1.
    ' The function checks the rating digits from right to left; when a
    ' differing digit is found, the smaller digit gets White.
    ' If the ratings are equal, the color is randomized.
    Dim digit1, digit2, selectedColor, i As Integer
    If mutableRating1 = mutableRating2 Then
        selectedColor = Int(Rnd() + 0.5)
        Return selectedColor
    End If
    For i = 0 To 3
        digit1 = Int(mutableRating1 / 10 ^ i) Mod 10
        digit2 = Int(mutableRating2 / 10 ^ i) Mod 10
        If digit1 < digit2 Then Return 0
        If digit1 > digit2 Then Return 1
    Next
    Return 0
End Function
```

## Pair Insertion, Backtracking And Storage

```vb
Sub MakePair(ByRef candidate1Index As Integer, ByRef candidate2Index As Integer, _
             ByRef pairCount As Integer)
    ' colorChoice is global: 0 if candidate1 is White, 1 if candidate1 is Black.
    ' Mark sortedPlayerIndexes entries negative when an opponent has been found.
    Dim whitePlayer, blackPlayer As Integer
    If colorChoice = 0 Then
        whitePlayer = sortedPlayerIndexes(candidate1Index)
        blackPlayer = sortedPlayerIndexes(candidate2Index)
    End If
    If colorChoice = 1 Then
        whitePlayer = sortedPlayerIndexes(candidate2Index)
        blackPlayer = sortedPlayerIndexes(candidate1Index)
    End If
    roundPairs(pairCount - 1, 0) = whitePlayer
    roundPairs(pairCount - 1, 1) = blackPlayer
    sortedPlayerIndexes(candidate1Index) = -sortedPlayerIndexes(candidate1Index)
    sortedPlayerIndexes(candidate2Index) = -sortedPlayerIndexes(candidate2Index)
    pairSearchIndexes(pairCount - 1, 0) = candidate1Index
    pairSearchIndexes(pairCount - 1, 1) = candidate2Index
End Sub

Sub UndoPair(ByRef candidate1Index As Integer, ByRef candidate2Index As Integer, _
             ByRef pairCount As Integer, ByRef undoIndex As Integer)
    ' Remove the found pair.
    undoIndex = candidate1Index
    candidate1Index = pairSearchIndexes(pairCount - 1, 0)
    candidate2Index = pairSearchIndexes(pairCount - 1, 1)
    sortedPlayerIndexes(candidate1Index) = -sortedPlayerIndexes(candidate1Index)
    sortedPlayerIndexes(candidate2Index) = -sortedPlayerIndexes(candidate2Index)
    roundPairs(pairCount - 1, 0) = 0
    roundPairs(pairCount - 1, 1) = 0
End Sub
```

`VieParit` stores the generated pairs in `PARIT.<tournament id>`, and `VieParienLkm` stores the pair count, active-player count and current round in `KILPAILU.<tournament id>`.

## Key Finnish/Swedish Terms In The Code

- `KierroksenParit`: round pairings
- `EkanKierroksenParit`: first-round pairings
- `MuunKierroksenParit`: pairings for other/later rounds
- `pelaaja`: player
- `kierros`: round
- `parit`: pairs/pairings
- `valkea`: White
- `musta`: Black
- `vastus`: opponent
- `huilaus`: bye/rest round
- `selo`: Finnish chess rating
- `muuttuva selo` / `mselo`: mutable/current rating used during the tournament
- `varit` / `VARIT`: colors/color table
- `lajittelu`: sorting
- `aktiivi`: active player
- `peruuta`: undo/backtrack
