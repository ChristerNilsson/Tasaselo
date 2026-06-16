from pairing_english import BLACK, WHITE, GameRecord, PlayerState, build_round_pairings


def print_pairings(title, pairings):
    print(title)
    for pairing in pairings:
        print(
            f"Board {pairing.board_no}: "
            f"White {pairing.white_player_no} - Black {pairing.black_player_no}"
        )
    print()


def main():
    first_round_players = [
        PlayerState(player_no=1, rating=2000),
        PlayerState(player_no=2, rating=1900),
        PlayerState(player_no=3, rating=1800),
        PlayerState(player_no=4, rating=1700),
    ]

    first_round_pairings = build_round_pairings(first_round_players, round_number=1)
    print_pairings("Round 1 pairings", first_round_pairings)

    second_round_players = [
        PlayerState(1, 2000, previous_games=[GameRecord(2, WHITE)]),
        PlayerState(2, 1900, previous_games=[GameRecord(1, BLACK)]),
        PlayerState(3, 1800, previous_games=[GameRecord(4, WHITE)]),
        PlayerState(4, 1700, previous_games=[GameRecord(3, BLACK)]),
    ]

    second_round_pairings = build_round_pairings(second_round_players, round_number=2)
    print_pairings("Round 2 pairings", second_round_pairings)


if __name__ == "__main__":
    main()
