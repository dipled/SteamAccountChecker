import re

def convert_to_steam64(steam_id: str) -> str:
    steam_id = steam_id.replace("STEAM_", "")
    split = steam_id.split(':')
    if len(split) != 3:
        raise ValueError("Invalid SteamId format")
    return str(76561197960265728 + int(split[2]) * 2 + int(split[1]))

def get_string_between(source: str, start: str, end: str) -> str:
    start_index = source.find(start)
    if start_index != -1:
        end_index = source.find(end, start_index + len(start))
        if end_index != -1:
            return source[start_index + len(start):end_index]
    return ""

def is_proper_email(s: str) -> bool:
    pattern = r"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@" \
              r"(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z"
    return re.match(pattern, s, re.IGNORECASE) is not None