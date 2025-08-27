import sys
from time import time
import utils
import requests
import json
import os
from enum import Enum

class AccountType(Enum):
    UNVERIFIED = 1
    OLD_GAMES = 2
    LVL0 = 3
    NONE = 4

def query_unverified(steam_id64 : str, steam_id : str, player_name : str, data_summary) -> bool:
    if ("profilestate" not in data_summary["response"]["players"][0]) or (data_summary["response"]["players"][0]["profilestate"] == 0):
            print(f"Unverified account found: {steam_id} | https://steamcommunity.com/profiles/{steam_id64}")
            with open("output/unverified_accounts.txt", "a", encoding="utf-8") as f:
                f.write(f"UNVERIFIED ACCOUNT FOUND:\n{steam_id} | {player_name} | https://steamcommunity.com/profiles/{steam_id64}\n\n")
            return True
    return False

def query_old_games(steam_id64 : str, steam_id : str, player_name : str, data_games, data_level, data_badge) -> bool:
    if len(data_games["response"]) == 0 or "games" not in data_games["response"] or len(data_games["response"]["games"] or "badges" not in data_badge["response"]) == 0:
        return False
    else:
        gameid_list = list(map(lambda x : x["appid"], data_games["response"]["games"]))
        badge_list = list(filter(lambda x: x["badgeid"] == 13, data_badge["response"]["badges"]))

        if(len(badge_list) == 0):
            return False
        
        if (240 in gameid_list or 70 in gameid_list or 10 in gameid_list) and data_level["response"]["player_level"] <= 9 and badge_list[0]['level'] <= 5:
            print(f"Account owns CSS, HL or CS 1.6: {steam_id} | https://steamcommunity.com/profiles/{steam_id64}")
            with open("output/old_games_accounts.txt", "a", encoding="utf-8") as f:
                f.write(f"OLD GAMES ACCOUNT FOUND:\n{steam_id} | {player_name} | https://steamcommunity.com/profiles/{steam_id64}\n\n")
            return True

def query_lvl0(steam_id64 : str, steam_id : str, player_name : str, data_level):
    if data_level["response"]["player_level"] == 0:
        print(f"Level 0 account found: {steam_id} | https://steamcommunity.com/profiles/{steam_id64}")
        with open("output/lvl0_accounts.txt", "a", encoding="utf-8") as f:
            f.write(f"LEVEL 0 ACCOUNT FOUND:\n{steam_id} | {player_name } |https://steamcommunity.com/profiles/{steam_id64}\n\n")
        return True
    return False

def query(server : int, steam_digit : int) -> AccountType:

    # query player summary no matter what, because we use player_name everywhere
    steam_id = f"STEAM_0:{server}:{steam_digit}"
    steam_id64 = utils.convert_to_steam64(steam_id)
    url_summary = f"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/"
    params_summary = {
            "key": STEAM_API_KEY,
            "steamids": steam_id64
        }
    try:
        response_summary = requests.get(url_summary, params=params_summary)
        data_summary = response_summary.json()
        if len(data_summary["response"]["players"]) == 0:
            print(f"Non-existent account: {steam_id}")
            return AccountType.NONE
        player_name = data_summary["response"]["players"][0]["personaname"]
    except requests.exceptions.ConnectTimeout as e:
        print("Connection timed out. Waiting and retrying")
        time.sleep(5)
        query(server, steam_digit)
    except Exception as e:
        print(f"Error: {e} PROBABLY STEAM API LIMIT REACHED, PLEASE SWITCH THE KEY")
        sys.exit(1)

    # unverified accounts query
    if(settings["unverified_accounts"]):
        if(query_unverified(steam_id64, steam_id, player_name, data_summary)):
            return AccountType.UNVERIFIED
        
    # if we"re going to query old games or lvl0 we might as well hit the level ep already
    if(settings["old_games"] or settings["lvl0_accounts"]):
        url_level = "https://api.steampowered.com/IPlayerService/GetSteamLevel/v1/"
        params_level = {
            "key": STEAM_API_KEY,
            "steamid": steam_id64
        }
        try:
            response_level = requests.get(url_level, params=params_level)
            data_level = response_level.json()
            if len(data_level["response"]) == 0:
                return AccountType.NONE
        except requests.exceptions.ConnectTimeout as e:
            print("Connection timed out. Waiting and retrying")
            time.sleep(5)
            query(server, steam_digit)
        except Exception as e:
            print(f"Error: {e} PROBABLY STEAM API LIMIT REACHED, PLEASE SWITCH THE KEY")
            sys.exit(1)

    # old games query
    if(settings["old_games"]):
        url_games = "https://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/"
        params_games = {
            "key": STEAM_API_KEY,
            "steamid": steam_id64,
            "format": "json"
        }
        url_badge = "https://api.steampowered.com/IPlayerService/GetBadges/v1/"
        params_badge = {
            "key": STEAM_API_KEY,
            "steamid": steam_id64,
            "format": "json"
        }
        try:
            response_games = requests.get(url_games, params=params_games)
            data_games = response_games.json()
            response_badge = requests.get(url_badge, params=params_badge)
            data_badge = response_badge.json()
            if(query_old_games(steam_id64, steam_id, player_name, data_games, data_level, data_badge)):
                return AccountType.OLD_GAMES
        except requests.exceptions.ConnectTimeout as e:
            print("Connection timed out. Waiting and retrying")
            time.sleep(5)
            query(server, steam_digit)
        except Exception as e:
            print(f"Error: {e} PROBABLY STEAM API LIMIT REACHED, PLEASE SWITCH THE KEY")
            sys.exit(1)
    # level 0 query
    if settings["lvl0_accounts"]:
        if(query_lvl0(steam_id64, steam_id, player_name, data_level)):
            return AccountType.LVL0
    return AccountType.NONE

with open("Settings.json", "r", encoding="utf-8") as f:
    file_contents = f.read()
    settings = json.loads(file_contents)
    
os.makedirs("output", exist_ok=True)
STEAM_API_KEY = settings["api_key"]

try:
    for i in range(settings["start_id"], settings["end_id"]):
        for j in range(2):
            steam_id = f"STEAM_0:{j}:{i}"
            print(f"\nChecking {steam_id} ...")
            query(j, i)
except KeyboardInterrupt:
    print("\nExiting...")
print("\n\nFinished")

