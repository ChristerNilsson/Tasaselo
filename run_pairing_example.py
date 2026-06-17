from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
import re
from urllib.request import urlopen

from pairing_english import (
    BLACK,
    WHITE,
    GameRecord,
    Pairing,
    PlayerState,
    build_round_pairings,
    expected_score,
    updated_rating,
)


SOURCE_URL = "https://www.shakki.net/tasaselo/ty550629.txt"
LOCAL_FILE = Path("ty550629.txt")


@dataclass
class ParsedPlayer:
    player_no: int
    name: str
    club: str
    group: str
    rating: int
    games: list[GameRecord]


def read_text_source(source: str) -> str:
    if source.startswith(("http://", "https://")):
        with urlopen(source, timeout=20) as response:
            data = response.read()
    else:
        data = Path(source).read_bytes()

    for encoding in ("utf-8", "latin-1"):
        try:
            return data.decode(encoding)
        except UnicodeDecodeError:
            pass
    return data.decode("latin-1", errors="replace")


def parse_tasaselo_file(text: str) -> list[ParsedPlayer]:
    players: list[ParsedPlayer] = []
    player_line_pattern = re.compile(r"^\s*(\d+)\s+(.+?)\s+(U?\d{4})\s+(.+)$")
    group_pattern = re.compile(r"^\s*([A-Z])-ryhm")
    game_pattern = re.compile(r"([vm])([+\-=])\s*(\d+)")
    current_group = ""

    for line in text.splitlines():
        if "TASASELOTURNAUS" in line:
            break

        group_match = group_pattern.match(line)
        if group_match:
            current_group = group_match.group(1)
            continue

        match = player_line_pattern.match(line)
        if not match:
            continue

        player_no = int(match.group(1))
        name_and_club = match.group(2).rstrip()
        rating = int(match.group(3).lstrip("U"))
        rest = match.group(4)

        games = [
            GameRecord(
                opponent_no=int(game_match.group(3)),
                color=WHITE if game_match.group(1) == "v" else BLACK,
                score=result_to_score(game_match.group(2)),
            )
            for game_match in game_pattern.finditer(rest)
        ]
        if not games:
            continue

        name, club = split_name_and_club(name_and_club)
        players.append(
            ParsedPlayer(
                player_no=player_no,
                name=name,
                club=club,
                group=current_group,
                rating=rating,
                games=games,
            )
        )

    return players


def split_name_and_club(name_and_club: str) -> tuple[str, str]:
    parts = name_and_club.split()
    if len(parts) < 2:
        return name_and_club, ""
    return " ".join(parts[:-1]), parts[-1]


def result_to_score(result_code: str) -> float:
    if result_code == "+":
        return 1.0
    if result_code == "=":
        return 0.5
    if result_code == "-":
        return 0.0
    raise ValueError(f"Unknown result code: {result_code}")


def to_player_states(parsed_players: list[ParsedPlayer]) -> list[PlayerState]:
    return [
        PlayerState(player_no=player.player_no, rating=player.rating)
        for player in sorted(parsed_players, key=lambda player: player.player_no)
    ]


def add_actual_round_history(
    players: list[PlayerState],
    parsed_players: list[ParsedPlayer],
    round_number: int,
) -> None:
    parsed_by_player_no = {
        player.player_no: player
        for player in parsed_players
    }
    players_by_player_no = {
        player.player_no: player
        for player in players
    }

    for player_no, parsed_player in parsed_by_player_no.items():
        game = parsed_player.games[round_number - 1]
        players_by_player_no[player_no].previous_games.append(game)


def actual_round_pairings(parsed_players: list[ParsedPlayer], round_number: int):
    by_player_no = {player.player_no: player for player in parsed_players}
    seen: set[frozenset[int]] = set()
    pairings = []

    for player in sorted(parsed_players, key=lambda item: item.player_no):
        game = player.games[round_number - 1]
        opponent = by_player_no[game.opponent_no]
        pair_key = frozenset({player.player_no, opponent.player_no})
        if pair_key in seen:
            continue
        seen.add(pair_key)

        if game.color == WHITE:
            white_player_no = player.player_no
            black_player_no = opponent.player_no
        else:
            white_player_no = opponent.player_no
            black_player_no = player.player_no

        pairings.append((white_player_no, black_player_no))

    return pairings


def actual_round_pairing_objects(
    parsed_players: list[ParsedPlayer],
    round_number: int,
) -> list[Pairing]:
    return [
        Pairing(
            board_no=board_no,
            white_player_no=white_player_no,
            black_player_no=black_player_no,
        )
        for board_no, (white_player_no, black_player_no)
        in enumerate(actual_round_pairings(parsed_players, round_number), start=1)
    ]


def verify_round_consistency(parsed_players: list[ParsedPlayer], round_number: int) -> None:
    parsed_by_player_no = {
        player.player_no: player
        for player in parsed_players
    }
    for player in parsed_players:
        game = player.games[round_number - 1]
        opponent = parsed_by_player_no[game.opponent_no]
        opponent_game = opponent.games[round_number - 1]
        if opponent_game.opponent_no != player.player_no:
            raise AssertionError(
                f"Round {round_number}: player {player.player_no} points to "
                f"{game.opponent_no}, but the opponent points to {opponent_game.opponent_no}."
            )
        if game.color == opponent_game.color:
            raise AssertionError(
                f"Round {round_number}: players {player.player_no} and "
                f"{opponent.player_no} have the same color."
            )
        if game.score is not None and opponent_game.score is not None:
            if game.score + opponent_game.score != 1.0:
                raise AssertionError(
                    f"Round {round_number}: scores for players {player.player_no} and "
                    f"{opponent.player_no} do not sum to 1.0."
                )


def derive_first_three_pairs(parsed_players: list[ParsedPlayer]) -> list[tuple[int, int]]:
    """Return manual first-round pairs as sorted-list positions.

    Tasaselo's first-round manual input refers to positions 1..6 in the
    rating-sorted active-player list. The order in each tuple is significant:
    first position gets White, second position gets Black.
    """

    sorted_players = sorted(parsed_players, key=lambda player: (-player.rating, player.player_no))
    top_six_numbers = [player.player_no for player in sorted_players[:6]]
    position_by_player_no = {
        player_no: index + 1
        for index, player_no in enumerate(top_six_numbers)
    }
    first_round_by_player_no = {
        player.player_no: player.games[0]
        for player in parsed_players
    }

    position_pairs: list[tuple[int, int]] = []
    used_positions: set[int] = set()
    for first_position in range(1, 7):
        if first_position in used_positions:
            continue
        player_no = top_six_numbers[first_position - 1]
        opponent_no = first_round_by_player_no[player_no].opponent_no
        if opponent_no not in position_by_player_no:
            raise ValueError("The first six sorted players are not paired among themselves.")

        opponent_position = position_by_player_no[opponent_no]
        player_game = first_round_by_player_no[player_no]
        if player_game.color == WHITE:
            position_pair = (first_position, opponent_position)
        else:
            position_pair = (opponent_position, first_position)

        position_pairs.append(position_pair)
        used_positions.add(first_position)
        used_positions.add(opponent_position)

    if sorted(position for pair in position_pairs for position in pair) != [1, 2, 3, 4, 5, 6]:
        raise ValueError("Could not derive valid first-three-pair positions.")

    return position_pairs


def print_pairings(title, pairings):
    print(title)
    for pairing in pairings:
        print(
            f"Board {pairing.board_no}: "
            f"White {pairing.white_player_no} - Black {pairing.black_player_no}"
        )
    print()


def score_text(score: float) -> str:
    if score == int(score):
        return f"{int(score)}.0"
    return f"{score:.1f}"


def print_standings_after_final_round(parsed_players: list[ParsedPlayer]) -> None:
    rows = []
    for player in parsed_players:
        total_score = sum(game.score or 0.0 for game in player.games)
        rows.append((
            player.group,
            total_score,
            player.player_no,
            player.name,
            player.club,
            player.rating,
        ))

    rows.sort(key=lambda row: (row[0], -row[1], row[2]))

    print("Standings after final round")
    print("Group Place  No  Player                         Club      Rating  Score")
    previous_group = None
    previous_score = None
    place = 0
    group_index = 0
    for group, total_score, player_no, name, club, rating in rows:
        if group != previous_group:
            previous_group = group
            previous_score = None
            place = 0
            group_index = 0
            print(f"{group}-group")

        group_index += 1
        if total_score != previous_score:
            place = group_index
            previous_score = total_score
        print(
            f"      {place:>5} {player_no:>3}  "
            f"{name:<30.30} {club:<8.8} {rating:>6}  {score_text(total_score):>5}"
        )
    print()


def calculate_intermediate_ratings(parsed_players: list[ParsedPlayer]) -> dict[int, list[int]]:
    ratings_by_player_no = {
        player.player_no: player.rating
        for player in parsed_players
    }
    rating_history = {
        player.player_no: [player.rating]
        for player in parsed_players
    }
    parsed_by_player_no = {
        player.player_no: player
        for player in parsed_players
    }
    round_count = len(parsed_players[0].games)

    for round_number in range(1, round_count + 1):
        next_ratings = dict(ratings_by_player_no)
        seen: set[frozenset[int]] = set()

        for player in parsed_players:
            game = player.games[round_number - 1]
            pair_key = frozenset({player.player_no, game.opponent_no})
            if pair_key in seen:
                continue
            seen.add(pair_key)

            opponent = parsed_by_player_no[game.opponent_no]
            opponent_game = opponent.games[round_number - 1]
            player_rating = ratings_by_player_no[player.player_no]
            opponent_rating = ratings_by_player_no[opponent.player_no]
            next_ratings[player.player_no] = updated_rating(
                game.score or 0.0,
                player_rating,
                opponent_rating,
            )
            next_ratings[opponent.player_no] = updated_rating(
                opponent_game.score or 0.0,
                opponent_rating,
                player_rating,
            )

        ratings_by_player_no = next_ratings
        for player in parsed_players:
            rating_history[player.player_no].append(ratings_by_player_no[player.player_no])

    return rating_history


def print_intermediate_ratings(parsed_players: list[ParsedPlayer]) -> None:
    rating_history = calculate_intermediate_ratings(parsed_players)
    round_count = len(parsed_players[0].games)
    header = " No  Player                         Start " + " ".join(
        f"R{round_number:>2}"
        for round_number in range(1, round_count + 1)
    )
    print("Intermediate mutable MSELO ratings used for pairing")
    print("Note: this is column 8 in the comparison table, not final Selo/column 7.")
    print(header)
    for player in sorted(parsed_players, key=lambda item: item.player_no):
        ratings = rating_history[player.player_no]
        rating_text = " ".join(f"{rating:>4}" for rating in ratings[1:])
        print(f"{player.player_no:>3}  {player.name:<30.30} {ratings[0]:>5} {rating_text}")
    print()


def score_with_comma(score: float) -> str:
    return f"{score:.1f}".replace(".", ",")


def decimal_with_comma(value: float) -> str:
    return f"{value:.2f}".replace(".", ",")


def performance_rating(score_percent: int, opponent_average: int) -> int:
    if score_percent == 50:
        return opponent_average

    rating_limits = [
        4, 11, 18, 26, 33, 40, 47, 54, 62, 69, 77, 84, 92, 99, 107, 114,
        122, 130, 138, 146, 154, 163, 171, 180, 189, 198, 207, 216, 226,
        236, 246, 257, 268, 279, 291, 303, 316, 329, 345, 358, 375, 392,
        412, 433, 457, 485, 518, 560, 620, 736, 999,
    ]
    if score_percent > 50:
        limit_index = score_percent - 51
        rating_addition = (rating_limits[limit_index] + rating_limits[limit_index + 1]) / 2
    else:
        limit_index = 49 - score_percent
        rating_addition = -(rating_limits[limit_index] + rating_limits[limit_index + 1]) / 2

    if rating_addition > 400:
        rating_addition = 400
    if rating_addition < -400:
        rating_addition = -400
    return int(opponent_average + rating_addition + 0.5)


def final_selo_k_value(rating: int) -> float:
    if rating >= 2050:
        return 20.0
    if rating >= 1950:
        return 25.0
    if rating >= 1850:
        return 30.0
    if rating >= 1750:
        return 35.0
    if rating >= 1650:
        return 40.0
    return 45.0


def final_unofficial_selo(
    player: ParsedPlayer,
    parsed_by_player_no: dict[int, ParsedPlayer],
    *,
    round_factor: float = 0.3,
) -> int:
    change_sum = 0.0
    played_games = 0
    for game in player.games:
        opponent = parsed_by_player_no[game.opponent_no]
        opponent_rating = opponent.rating
        expected_percent = 100 * expected_score(player.rating, opponent_rating)
        k_value = final_selo_k_value(player.rating)
        factor = 0.15 if round_factor == 0.3 and player.rating >= 2300 else round_factor
        change_sum += factor * k_value * (100 * (game.score or 0.0) - expected_percent)
        played_games += 1

    played_bonus = 10 * played_games
    return int(player.rating + (change_sum + played_bonus) / 100 + 0.5)


def comparison_table_rows(parsed_players: list[ParsedPlayer]):
    parsed_by_player_no = {
        player.player_no: player
        for player in parsed_players
    }
    rating_history = calculate_intermediate_ratings(parsed_players)
    rows = []

    for player in parsed_players:
        total_score = sum(game.score or 0.0 for game in player.games)
        opponent_ratings = [
            parsed_by_player_no[game.opponent_no].rating
            for game in player.games
        ]
        opponent_average = int(sum(opponent_ratings) / len(opponent_ratings) + 0.5)
        expected_sum = sum(
            expected_score(player.rating, parsed_by_player_no[game.opponent_no].rating)
            for game in player.games
        )
        score_minus_expected = total_score - expected_sum
        normal_points_percent = int((100 * total_score / len(player.games)) + 0.5)
        performance = performance_rating(normal_points_percent, opponent_average)
        buchholz = sum(
            sum(opponent_game.score or 0.0 for opponent_game in parsed_by_player_no[game.opponent_no].games)
            for game in player.games
        )
        rows.append({
            "group": player.group,
            "score": total_score,
            "player_no": player.player_no,
            "name": player.name,
            "start_selo": player.rating,
            "performance": performance,
            "opponent_average": opponent_average,
            "score_minus_expected": score_minus_expected,
            "final_selo": final_unofficial_selo(player, parsed_by_player_no),
            "mutable_selo": rating_history[player.player_no][-1],
            "buchholz": buchholz,
        })

    rows.sort(key=lambda row: (row["group"], -row["score"], row["player_no"]))
    return rows


def print_comparison_table(parsed_players: list[ParsedPlayer]) -> None:
    print("Recreated comparison table")
    print(" No  Player                         Selo G    P  Perf  Avg   T-Exp  New  MSELO Buch")
    previous_group = None
    for row in comparison_table_rows(parsed_players):
        if row["group"] != previous_group:
            previous_group = row["group"]
            print(f"{row['group']}-group")
        print(
            f"{row['player_no']:>3}  "
            f"{row['name']:<30.30} "
            f"{row['start_selo']:>4} {row['group']} "
            f"{score_with_comma(row['score']):>4} "
            f"{row['performance']:>4} "
            f"{row['opponent_average']:>4} "
            f"{decimal_with_comma(row['score_minus_expected']):>6} "
            f"{row['final_selo']:>4} "
            f"{row['mutable_selo']:>5} "
            f"{score_with_comma(row['buchholz']):>4}"
        )
    print()


def compare_pairings(parsed_players: list[ParsedPlayer], round_number: int, generated_pairings):
    generated_pairs_as_set = {
        (pairing.white_player_no, pairing.black_player_no)
        for pairing in generated_pairings
    }
    file_pairs_as_set = set(actual_round_pairings(parsed_players, round_number=round_number))
    if generated_pairs_as_set != file_pairs_as_set:
        missing = sorted(file_pairs_as_set - generated_pairs_as_set)
        extra = sorted(generated_pairs_as_set - file_pairs_as_set)
        raise AssertionError(
            f"Generated round {round_number} does not match file. "
            f"Missing={missing}, extra={extra}"
        )


def main():
    source = str(LOCAL_FILE) if LOCAL_FILE.exists() else SOURCE_URL
    parsed_players = parse_tasaselo_file(read_text_source(source))
    players = to_player_states(parsed_players)

    first_three_pairs = derive_first_three_pairs(parsed_players)
    print(f"First three manual pair positions: {first_three_pairs}")

    generated_pairings = build_round_pairings(
        players,
        round_number=1,
        first_three_pairs=first_three_pairs,
    )
    print_pairings("Generated round 1 pairings", generated_pairings)
    compare_pairings(parsed_players, 1, generated_pairings)
    print("Round 1 generated pairings match ty550629.txt.")

    round_count = len(parsed_players[0].games)
    for round_number in range(1, round_count + 1):
        verify_round_consistency(parsed_players, round_number)
        file_pairings = actual_round_pairing_objects(parsed_players, round_number)
        print_pairings(f"File round {round_number} pairings", file_pairings)
        print(f"Round {round_number} file data is internally consistent.")

    print_intermediate_ratings(parsed_players)
    print_comparison_table(parsed_players)
    print_standings_after_final_round(parsed_players)


if __name__ == "__main__":
    main()
