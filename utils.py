import re

def convert_to_steam64(steam_id: str) -> str:
    steam_id = steam_id.replace("STEAM_", "")
    split = steam_id.split(':')
    if len(split) != 3:
        raise ValueError("Invalid SteamId format")
    return str(76561197960265728 + int(split[2]) * 2 + int(split[1]))
