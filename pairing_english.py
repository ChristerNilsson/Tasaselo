"""English-named Python translation of the Tasaselo pairing logic.

This module keeps the pairing algorithm in memory. It does not read or write
the original VB random-access files (`TULOS`, `PARIT`, `KILPAILU`). The caller
passes player state in, and receives the generated pairings back.
"""

from __future__ import annotations

from dataclasses import dataclass, field
import random
from typing import Iterable


NO_COLOR = 0
WHITE = 1
BLACK = 2
NO_COLOR_CHOICE = 9


@dataclass
class GameRecord:
    opponent_no: int = 0
    color: int = NO_COLOR


@dataclass
class PlayerState:
    player_no: int
    rating: int
    active: bool = True
    previous_games: list[GameRecord] = field(default_factory=list)


@dataclass
class Pairing:
    board_no: int
    white_player_no: int
    black_player_no: int


@dataclass
class PairingState:
    sorted_player_indexes: list[int]
    round_pairs: list[list[int]]
    pair_search_indexes: list[list[int]]
    color_choice: int = NO_COLOR_CHOICE


def build_round_pairings(
    players: Iterable[PlayerState],
    round_number: int,
    *,
    enforce_color_rule: bool = True,
    randomize_first_three: bool = False,
    first_three_pairs: list[tuple[int, int]] | None = None,
    max_attempts_before_relaxing_colors: int = 400,
) -> list[Pairing]:
    """Build pairings for one round.

    `first_three_pairs` uses positions in the sorted active list, not original
    player numbers, matching the VB input convention for the first round.
    Example: `(4, 1)` means sorted active player 4 against sorted active player 1.
    """

    player_list = list(players)
    player_count = len(player_list)
    active_players = [player for player in player_list if player.active]
    active_player_count = len(active_players)
    if active_player_count % 2:
        raise ValueError("The number of active players must be even.")

    sorted_active_players = sorted(
        active_players,
        key=lambda player: (-player.rating, player.player_no),
    )

    if round_number == 1:
        return build_first_round_pairings(
            sorted_active_players,
            player_count,
            randomize_first_three=randomize_first_three,
            first_three_pairs=first_three_pairs,
        )

    return build_later_round_pairings(
        sorted_active_players,
        player_list,
        round_number,
        enforce_color_rule=enforce_color_rule,
        max_attempts_before_relaxing_colors=max_attempts_before_relaxing_colors,
    )


def build_first_round_pairings(
    sorted_active_players: list[PlayerState],
    player_count: int,
    *,
    randomize_first_three: bool = False,
    first_three_pairs: list[tuple[int, int]] | None = None,
) -> list[Pairing]:
    sorted_player_numbers = [player.player_no for player in sorted_active_players]
    first_three_entered = False

    if first_three_pairs is not None:
        sorted_player_numbers = enter_first_three_pairs(sorted_player_numbers, first_three_pairs)
        first_three_entered = True
    elif randomize_first_three:
        sorted_player_numbers = randomize_first_three_pairs(sorted_player_numbers)

    ratings_by_player_no = {player.player_no: player.rating for player in sorted_active_players}
    pairings: list[Pairing] = []

    for candidate1_index in range(0, len(sorted_player_numbers), 2):
        player1_no = sorted_player_numbers[candidate1_index]
        player2_no = sorted_player_numbers[candidate1_index + 1]
        pair_index = len(pairings)

        if first_three_entered and pair_index < 3:
            color1 = WHITE
        else:
            color1 = (
                WHITE
                if select_color_by_rating(
                    ratings_by_player_no[player1_no],
                    ratings_by_player_no[player2_no],
                )
                == 0
                else BLACK
            )

        if color1 == WHITE:
            white_player_no = player1_no
            black_player_no = player2_no
        else:
            white_player_no = player2_no
            black_player_no = player1_no

        pairings.append(
            Pairing(
                board_no=pair_index + 1,
                white_player_no=white_player_no,
                black_player_no=black_player_no,
            )
        )

    return pairings


def randomize_first_three_pairs(sorted_player_numbers: list[int]) -> list[int]:
    """Randomize the first three pairs with the same swap pattern as the VB code."""

    randomized = list(sorted_player_numbers)
    if len(randomized) < 6:
        return randomized

    random_swap(randomized, pair_first_index=1, option_count=3, smallest_index=2)
    random_swap(randomized, pair_first_index=2, option_count=4, smallest_index=2)
    random_swap(randomized, pair_first_index=3, option_count=3, smallest_index=3)
    random_swap(randomized, pair_first_index=4, option_count=2, smallest_index=4)
    return randomized


def random_swap(
    player_numbers: list[int],
    *,
    pair_first_index: int,
    option_count: int,
    smallest_index: int,
) -> None:
    random_index = random.randrange(smallest_index, smallest_index + option_count)
    player_numbers[pair_first_index], player_numbers[random_index] = (
        player_numbers[random_index],
        player_numbers[pair_first_index],
    )


def enter_first_three_pairs(
    sorted_player_numbers: list[int],
    first_three_pairs: list[tuple[int, int]],
) -> list[int]:
    if len(first_three_pairs) != 3:
        raise ValueError("Exactly three first-round pairs are required.")

    entered_positions = [position for pair in first_three_pairs for position in pair]
    if sorted(entered_positions) != [1, 2, 3, 4, 5, 6]:
        raise ValueError("First-round pair positions must be the numbers 1 through 6 once each.")

    reordered = list(sorted_player_numbers)
    for index, position in enumerate(entered_positions):
        reordered[index] = sorted_player_numbers[position - 1]
    return reordered


def build_later_round_pairings(
    sorted_active_players: list[PlayerState],
    players: list[PlayerState],
    round_number: int,
    *,
    enforce_color_rule: bool = True,
    max_attempts_before_relaxing_colors: int = 400,
) -> list[Pairing]:
    active_player_count = len(sorted_active_players)
    colors, previous_opponents = build_color_and_opponent_tables(players, round_number)
    sorted_player_indexes = [player.player_no for player in sorted_active_players]

    active_player_numbers = {player.player_no for player in sorted_active_players}
    white_leaning = 0
    black_leaning = 0
    equal_colors = 0
    for player_no in active_player_numbers:
        color_row = colors[player_no]
        color_row["active"] = True
        if color_row["white_count"] > color_row["black_count"]:
            white_leaning += 1
        elif color_row["white_count"] < color_row["black_count"]:
            black_leaning += 1
        else:
            equal_colors += 1

    white_black_imbalance = white_leaning - black_leaning
    if white_black_imbalance != 0:
        balance_colors(colors, players, white_black_imbalance)

    state = PairingState(
        sorted_player_indexes=sorted_player_indexes,
        round_pairs=[],
        pair_search_indexes=[],
    )

    pair_count = 0
    search_direction = 1
    attempt_counter = 0
    start_index = 0
    end_index = active_player_count - 1
    color_rule_active = enforce_color_rule

    while active_player_count - 2 * pair_count > 0:
        attempt_counter += 1
        if (
            max_attempts_before_relaxing_colors > 0
            and attempt_counter % max_attempts_before_relaxing_colors == 0
        ):
            color_rule_active = False

        candidate1_index = first_unpaired_index(
            state.sorted_player_indexes,
            start_index,
            end_index,
            search_direction,
        )
        if candidate1_index is None:
            break

        found = False
        candidate2_index = candidate1_index
        for current_candidate2_index in stepped_range(start_index, end_index, search_direction):
            state.color_choice = NO_COLOR_CHOICE
            if (
                state.sorted_player_indexes[current_candidate2_index] > 0
                and candidate1_index != current_candidate2_index
            ):
                player1_no = state.sorted_player_indexes[candidate1_index]
                player2_no = state.sorted_player_indexes[current_candidate2_index]
                found = player2_no not in previous_opponents[player1_no]

                if found:
                    state.color_choice = choose_later_round_color(
                        player1_no,
                        player2_no,
                        players,
                        colors,
                        round_number,
                        enforce_color_rule=color_rule_active,
                    )

                if found and state.color_choice != NO_COLOR_CHOICE:
                    pair_count += 1
                    make_pair(state, candidate1_index, current_candidate2_index, pair_count)
                    candidate2_index = current_candidate2_index
                    break
                found = False

        if pair_count == 0:
            color_rule_active = False

        if not found:
            if pair_count > 0:
                candidate1_index, candidate2_index = undo_pair(state, pair_count)
                pair_count -= 1
                candidate1_index -= search_direction
            if candidate1_index == end_index:
                candidate1_index -= search_direction
                color_rule_active = False
            start_index = candidate1_index

        if pair_count % 2 == 0:
            search_direction = 1
            start_index = 0
            end_index = active_player_count - 1
        else:
            search_direction = -1
            start_index = active_player_count - 1
            end_index = 0

    if active_player_count - 2 * pair_count != 0:
        raise RuntimeError("Could not build complete pairings.")

    assign_board_numbers(state.round_pairs)
    return [
        Pairing(board_no=pair[2], white_player_no=pair[0], black_player_no=pair[1])
        for pair in sorted(state.round_pairs, key=lambda pair: pair[2])
    ]


def build_color_and_opponent_tables(
    players: list[PlayerState],
    round_number: int,
) -> tuple[dict[int, dict[str, int | bool]], dict[int, set[int]]]:
    colors: dict[int, dict[str, int | bool]] = {}
    previous_opponents: dict[int, set[int]] = {}

    for player in players:
        white_count = 0
        black_count = 0
        color_history_code = 0
        bye_count = 0
        opponents: set[int] = set()

        for previous_round_index in range(round_number - 1):
            game = (
                player.previous_games[previous_round_index]
                if previous_round_index < len(player.previous_games)
                else GameRecord()
            )
            if game.opponent_no != 0:
                opponents.add(game.opponent_no)
                if game.color == WHITE:
                    white_count += 1
                if game.color == BLACK:
                    black_count += 1
                color_history_code += (2 ** (previous_round_index + 1)) * game.color
            else:
                bye_count += 1

        colors[player.player_no] = {
            "white_count": white_count,
            "black_count": black_count,
            "color_history_code": color_history_code,
            "active": False,
            "bye_count": bye_count,
        }
        previous_opponents[player.player_no] = opponents

    return colors, previous_opponents


def choose_later_round_color(
    player1_no: int,
    player2_no: int,
    players: list[PlayerState],
    colors: dict[int, dict[str, int | bool]],
    round_number: int,
    *,
    enforce_color_rule: bool,
) -> int:
    player_by_no = {player.player_no: player for player in players}
    player1_colors = colors[player1_no]
    player2_colors = colors[player2_no]

    player1_white_count = int(player1_colors["white_count"])
    player1_black_count = int(player1_colors["black_count"])
    player2_white_count = int(player2_colors["white_count"])
    player2_black_count = int(player2_colors["black_count"])
    player1_color_history = int(player1_colors["color_history_code"])
    player2_color_history = int(player2_colors["color_history_code"])
    player1_bye_count = int(player1_colors["bye_count"])
    player2_bye_count = int(player2_colors["bye_count"])

    color_difference1 = player1_white_count - player1_black_count
    color_difference2 = player2_white_count - player2_black_count
    color_choice = NO_COLOR_CHOICE

    if round_number % 2 == 0:
        if color_difference1 + color_difference2 == 0:
            color_choice = 0 if color_difference1 < color_difference2 else 1
        if (player1_bye_count + player2_bye_count) % 2 == 1 and abs(
            color_difference1 + color_difference2
        ) == 1:
            color_choice = 0 if color_difference1 < color_difference2 else 1
        if not enforce_color_rule:
            color_choice = 0 if color_difference1 < color_difference2 else 1

    if round_number % 2 == 1:
        if (player1_bye_count + player2_bye_count) % 2 == 1 and abs(
            color_difference1 + color_difference2
        ) == 1:
            color_choice = 0 if color_difference1 < color_difference2 else 1
        elif (player1_bye_count + player2_bye_count) % 2 == 0 and (
            color_difference1 == color_difference2
        ):
            if player1_color_history == player2_color_history:
                color_choice = select_color_by_rating(
                    player_by_no[player1_no].rating,
                    player_by_no[player2_no].rating,
                )
            if player1_color_history > player2_color_history:
                color_choice = 0
            if player1_color_history < player2_color_history:
                color_choice = 1
        elif player1_bye_count + player2_bye_count == 0:
            color_choice = 0 if color_difference1 < color_difference2 else 1

    if color_choice == NO_COLOR_CHOICE and not enforce_color_rule:
        if player1_white_count < player2_white_count:
            color_choice = 0
        if player1_white_count > player2_white_count:
            color_choice = 1
        if player1_white_count == player2_white_count:
            if player1_color_history < player2_color_history:
                color_choice = 1
            if player1_color_history > player2_color_history:
                color_choice = 0
            if player1_color_history == player2_color_history:
                color_choice = select_color_by_rating(
                    player_by_no[player1_no].rating,
                    player_by_no[player2_no].rating,
                )

    return color_choice


def balance_colors(
    colors: dict[int, dict[str, int | bool]],
    players: list[PlayerState],
    white_black_imbalance: int,
) -> None:
    """Adjust the color table before searching, following the VB heuristic."""

    for player in sorted(players, key=lambda item: item.rating):
        if white_black_imbalance == 0:
            return
        color_row = colors[player.player_no]
        if not color_row["active"]:
            continue
        if color_row["white_count"] == color_row["black_count"]:
            if white_black_imbalance > 0:
                color_row["white_count"] = int(color_row["white_count"]) - 1
                color_row["black_count"] = int(color_row["black_count"]) + 1
                white_black_imbalance -= 1
            elif white_black_imbalance < 0:
                color_row["white_count"] = int(color_row["white_count"]) + 1
                color_row["black_count"] = int(color_row["black_count"]) - 1
                white_black_imbalance += 1

    for player in sorted(players, key=lambda item: item.rating):
        if white_black_imbalance == 0:
            return
        color_row = colors[player.player_no]
        if not color_row["active"]:
            continue
        step = 2 if white_black_imbalance % 2 == 0 else 1
        white_count = int(color_row["white_count"])
        black_count = int(color_row["black_count"])
        if abs(white_count - black_count) <= 1:
            if white_black_imbalance > 0 and white_count > black_count:
                color_row["white_count"] = white_count - 1
                color_row["black_count"] = black_count + 1
                white_black_imbalance -= step
            elif white_black_imbalance < 0 and white_count < black_count:
                color_row["white_count"] = white_count + 1
                color_row["black_count"] = black_count - 1
                white_black_imbalance += step


def select_color_by_rating(mutable_rating1: int, mutable_rating2: int) -> int:
    if mutable_rating1 == mutable_rating2:
        return random.randint(0, 1)

    for digit_index in range(4):
        digit1 = int(mutable_rating1 / 10**digit_index) % 10
        digit2 = int(mutable_rating2 / 10**digit_index) % 10
        if digit1 < digit2:
            return 0
        if digit1 > digit2:
            return 1
    return 0


def make_pair(
    state: PairingState,
    candidate1_index: int,
    candidate2_index: int,
    pair_count: int,
) -> None:
    if state.color_choice == 0:
        white_player_no = state.sorted_player_indexes[candidate1_index]
        black_player_no = state.sorted_player_indexes[candidate2_index]
    elif state.color_choice == 1:
        white_player_no = state.sorted_player_indexes[candidate2_index]
        black_player_no = state.sorted_player_indexes[candidate1_index]
    else:
        raise ValueError("Cannot make a pair without a valid color choice.")

    state.round_pairs.append([white_player_no, black_player_no, pair_count])
    state.sorted_player_indexes[candidate1_index] = -state.sorted_player_indexes[candidate1_index]
    state.sorted_player_indexes[candidate2_index] = -state.sorted_player_indexes[candidate2_index]
    state.pair_search_indexes.append([candidate1_index, candidate2_index])


def undo_pair(state: PairingState, pair_count: int) -> tuple[int, int]:
    if pair_count < 1 or pair_count > len(state.pair_search_indexes):
        raise ValueError("Invalid pair count for undo.")

    candidate1_index, candidate2_index = state.pair_search_indexes.pop()
    state.sorted_player_indexes[candidate1_index] = -state.sorted_player_indexes[candidate1_index]
    state.sorted_player_indexes[candidate2_index] = -state.sorted_player_indexes[candidate2_index]
    state.round_pairs.pop()
    return candidate1_index, candidate2_index


def first_unpaired_index(
    sorted_player_indexes: list[int],
    start_index: int,
    end_index: int,
    search_direction: int,
) -> int | None:
    for index in stepped_range(start_index, end_index, search_direction):
        if sorted_player_indexes[index] > 0:
            return index
    return None


def stepped_range(start_index: int, end_index: int, search_direction: int) -> range:
    stop = end_index + 1 if search_direction > 0 else end_index - 1
    return range(start_index, stop, search_direction)


def assign_board_numbers(round_pairs: list[list[int]]) -> None:
    pair_count = len(round_pairs)
    board_no = 1
    if pair_count % 2 == 1:
        for index in range(0, pair_count, 2):
            round_pairs[index][2] = board_no
            board_no += 1
        for index in range(pair_count - 2, 0, -2):
            round_pairs[index][2] = board_no
            board_no += 1
    else:
        for index in range(0, pair_count - 1, 2):
            round_pairs[index][2] = board_no
            board_no += 1
        for index in range(pair_count - 1, 0, -2):
            round_pairs[index][2] = board_no
            board_no += 1


# PascalCase aliases matching the translated names in lottningskod_utdrag_english.md.
BuildRoundPairings = build_round_pairings
BuildFirstRoundPairings = build_first_round_pairings
RandomizeFirstThreePairs = randomize_first_three_pairs
EnterFirstThreePairs = enter_first_three_pairs
RandomSwap = random_swap
BuildLaterRoundPairings = build_later_round_pairings
BalanceColors = balance_colors
SelectColorByRating = select_color_by_rating
MakePair = make_pair
UndoPair = undo_pair
