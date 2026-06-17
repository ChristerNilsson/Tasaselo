# Pairing Logic

This document explains how the pairing code works at two levels:

1. The tournament/pairing idea in plain language.
2. Where the corresponding logic lives in the Python code.

The code is split into two files:

- `pairing_english.py`: the in-memory pairing engine.
- `run_pairing_example.py`: reads `ty550629.txt`, verifies the first-round pairing, replays the file's rounds, and prints final standings.

## Overview

The pairing logic starts from a list of active players. Each player has:

- a player number
- a rating
- an active/inactive flag
- previous games, including opponent, color and score

For every round the engine sorts active players by rating, strongest first. The first round is special. Later rounds also need previous opponents and previous colors so the program can avoid repeat pairings and balance colors.

The program currently has two separate responsibilities:

- Generate round 1 from the Tasaselo rules, including the manually controlled first three boards.
- Read and verify all rounds from `ty550629.txt`, then print standings grouped by prize group and sorted by points.

## Round 1

Round 1 uses rating order. Normally, players are paired sequentially after sorting by rating:

- sorted player 1 vs sorted player 2
- sorted player 3 vs sorted player 4
- sorted player 5 vs sorted player 6
- and so on

Tasaselo has a special convention for the first three boards. The operator may manually specify how the first six sorted players are paired. The values refer to positions in the sorted list, not player numbers.

For `ty550629.txt`, the file says the first round starts like this:

- player 1 has Black against player 8
- player 2 has White against player 5
- player 4 has White against player 9

The top six players by rating are:

1. player 1
2. player 2
3. player 4
4. player 8
5. player 5
6. player 9

So the manual first-three-pair input becomes:

```python
[(4, 1), (2, 5), (3, 6)]
```

That means:

- sorted position 4 gets White against sorted position 1
- sorted position 2 gets White against sorted position 5
- sorted position 3 gets White against sorted position 6

In `run_pairing_example.py`, this is derived from the result file rather than hard-coded.

Code references:

- `build_round_pairings` dispatches round 1 to first-round logic: [pairing_english.py](c:/github/Tasaselo/pairing_english.py:58)
- `build_first_round_pairings` creates round-1 pairs and handles first-three-pair input: [pairing_english.py](c:/github/Tasaselo/pairing_english.py:214)
- `enter_first_three_pairs` reorders the first six sorted players: [pairing_english.py](c:/github/Tasaselo/pairing_english.py:297)
- `derive_first_three_pairs` derives `[(4, 1), (2, 5), (3, 6)]` from `ty550629.txt`: [run_pairing_example.py](c:/github/Tasaselo/run_pairing_example.py:201)
- `main` uses the derived first-three-pair input and verifies round 1: [run_pairing_example.py](c:/github/Tasaselo/run_pairing_example.py:318)

## Later Rounds

For later rounds the engine needs history:

- who each player has already played
- how many White and Black games each player has had
- an encoded color history
- whether a player has had byes/rest rounds

The intended flow is:

1. Sort active players by current rating.
2. Build previous-opponent and color tables.
3. Pick the first unpaired player.
4. Search for a legal opponent who has not already played that player.
5. Check whether the color assignment is acceptable.
6. If no valid opponent is found, undo the previous pair and try another route.
7. Once all pairs are found, assign board numbers.

This is a backtracking search. A player is marked as paired by negating their entry in the sorted-player list. If the search gets stuck, the latest pair is undone and both players become available again.

Code references:

- `build_later_round_pairings` runs the later-round search: [pairing_english.py](c:/github/Tasaselo/pairing_english.py:314)
- `build_color_and_opponent_tables` reads previous games into color/opponent tables: [pairing_english.py](c:/github/Tasaselo/pairing_english.py:435)
- `choose_later_round_color` applies color rules for a candidate pair: [pairing_english.py](c:/github/Tasaselo/pairing_english.py:477)
- `balance_colors` adjusts the color table before searching: [pairing_english.py](c:/github/Tasaselo/pairing_english.py:552)
- `make_pair` records a pair and marks both players as paired: [pairing_english.py](c:/github/Tasaselo/pairing_english.py:609)
- `undo_pair` backtracks one pair: [pairing_english.py](c:/github/Tasaselo/pairing_english.py:630)
- `assign_board_numbers` assigns final board numbers after search order: [pairing_english.py](c:/github/Tasaselo/pairing_english.py:658)

## Color Choice

Color handling is one of the central constraints. The code tracks:

- how many times each player has had White
- how many times each player has had Black
- color history, encoded as a numeric value
- byes/rest rounds

When two candidate players have identical or compatible color histories, color may be decided from rating digits. This follows the original Tasaselo/VB convention: compare the mutable ratings from the last digit backwards; the smaller differing digit gets White. If the ratings are equal, color is random.

Code references:

- `choose_later_round_color`: [pairing_english.py](c:/github/Tasaselo/pairing_english.py:477)
- `select_color_by_rating`: [pairing_english.py](c:/github/Tasaselo/pairing_english.py:595)

## Results And Ratings

`apply_round_results` is the helper for normal generated tournaments. It appends one round of result history to each player and can update ratings for the next round.

Scores are represented as:

- `(1.0, 0.0)` for White win
- `(0.5, 0.5)` for draw
- `(0.0, 1.0)` for Black win

There are two related but different rating calculations in the original VB code:

1. **MSELO / mutable rating for pairing**
   This is updated round by round in `SelojenLaskenta` using `UusiSelo`. It uses the `seloKerroin` coefficient, initialized as `100` in the VB module. This is the rating value used for later pairings.

2. **Final unofficial SELO / USELO for reporting**
   This is calculated in `Vertailut`. It uses player-dependent K-values based on rating bands:
   `20`, `25`, `30`, `35`, `40`, or `45`, and also applies the round conversion factor `muuntoKerroin`. In `ty550629.txt`, the report says all rounds used factor `0.3`.

The Python function `updated_rating` is the MSELO-style update. It does not yet implement the final report's full USELO calculation with rating-band K-values and `muuntoKerroin`.

Concrete example from `ty550629.txt`, player 1:

```text
1 Sipilä Vilka  2527 A  6,0 2342 2032 -0,37 2527 2473 26,5
```

Using the comparison-table headings, this means:

- column 1, `Selo ennen turnausta`: `2527`
- column 7, `Uusi tarkistamaton selo`: `2527`
- column 8, `Kierroksen jälkeinen muuttuva selo`: `2473`

So `2473` is not the final published Selo. It is the mutable MSELO value after the final round.

Code references:

- `apply_round_results`: [pairing_english.py](c:/github/Tasaselo/pairing_english.py:103)
- `expected_score`: [pairing_english.py](c:/github/Tasaselo/pairing_english.py:178)
- `updated_rating`: [pairing_english.py](c:/github/Tasaselo/pairing_english.py:190)

## Reading `ty550629.txt`

`run_pairing_example.py` reads the Tasaselo text file. It parses:

- player number
- player name
- club
- group
- starting rating
- every round's color, result and opponent

Example result tokens in the file:

- `v+ 5`: White win against player 5
- `m- 1`: Black loss against player 1
- `v=14`: draw as White against player 14

Code references:

- `read_text_source` loads either local file or URL: [run_pairing_example.py](c:/github/Tasaselo/run_pairing_example.py:25)
- `parse_tasaselo_file` parses players, groups and game tokens: [run_pairing_example.py](c:/github/Tasaselo/run_pairing_example.py:40)
- `result_to_score` converts `+`, `=` and `-` into numeric scores: [run_pairing_example.py](c:/github/Tasaselo/run_pairing_example.py:98)
- `actual_round_pairings` reconstructs file pairings from player rows: [run_pairing_example.py](c:/github/Tasaselo/run_pairing_example.py:134)
- `verify_round_consistency` checks that both players agree about opponent, color and score: [run_pairing_example.py](c:/github/Tasaselo/run_pairing_example.py:174)

## Verification Against The File

The example program verifies that generated round 1 matches `ty550629.txt`. For all rounds it also verifies that the file data is internally consistent:

- if player A says they played B, player B must say they played A
- both sides must have opposite colors
- the two scores must sum to 1.0

Code references:

- `compare_pairings` compares generated pairs with file pairs: [run_pairing_example.py](c:/github/Tasaselo/run_pairing_example.py:303)
- `verify_round_consistency`: [run_pairing_example.py](c:/github/Tasaselo/run_pairing_example.py:174)
- the verification loop in `main`: [run_pairing_example.py](c:/github/Tasaselo/run_pairing_example.py:335)

## Final Standings

Final standings are computed from the parsed game scores. They are sorted by:

1. group
2. score descending
3. player number

The current output does not yet apply secondary tie-break rules such as Buchholz. It gives the requested group-first, points-second order.

Code references:

- `print_standings_after_final_round`: [run_pairing_example.py](c:/github/Tasaselo/run_pairing_example.py:263)
- standings are printed from `main`: [run_pairing_example.py](c:/github/Tasaselo/run_pairing_example.py:342)

## Current Limitation

The result file contains all actual pairings for all rounds, but it does not contain a separate board-list file for each round. Therefore `run_pairing_example.py` can verify pair and color data for all rounds, but the board order for rounds read from the file is reconstructed deterministically from player rows.

The Python later-round pairing engine exists in `build_later_round_pairings`, but exact reproduction of Tasaselo's later rounds requires the same mutable rating state and all original runtime choices. Round 1 is fully generated and verified against `ty550629.txt`; later rounds are currently replayed and checked from the file data.
