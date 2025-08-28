import sys
import time
import utils
import requests
import json
import os
from enum import Enum
import threading

class AccountType(Enum):
    UNVERIFIED = 1
    OLD_GAMES = 2
    LVL0 = 3
    CSGO = 4
    NONE = 5

def query_unverified(steam_id64 : str, steam_id : str, player_name : str, data_summary) -> bool:
    if(data_summary == None):
        return False
    if ("profilestate" not in data_summary["response"]["players"][0]) or (data_summary["response"]["players"][0]["profilestate"] == 0):
            print(f"Unverified account found: {steam_id} | https://steamcommunity.com/profiles/{steam_id64}")
            with open("output/unverified_accounts.txt", "a", encoding="utf-8") as f:
                f.write(f"UNVERIFIED ACCOUNT FOUND:\n{steam_id} | {player_name} | https://steamcommunity.com/profiles/{steam_id64}\n\n")
            return True
    return False

def query_old_games(steam_id64 : str, steam_id : str, player_name : str, data_games, data_level, data_badge) -> bool:
    if(data_level == None or data_badge == None or data_games == None):
        return False
    if len(data_level["response"]) == 0:
        return False
    if len(data_games["response"]) == 0 or "games" not in data_games["response"] or (len(data_games["response"]["games"]) == 0):
        return False
    if("badges" not in data_badge["response"]):
        return False
    gameid_list = list(map(lambda x : x["appid"], data_games["response"]["games"]))
    badge_owned_games = list(filter(lambda x: x["badgeid"] == 13, data_badge["response"]["badges"]))

    if(len(badge_owned_games) == 0):
        return False
    
    if(240 in gameid_list or 70 in gameid_list or 10 in gameid_list) and data_level["response"]["player_level"] <= 9 and badge_owned_games[0]['level'] <= 5:
        print(f"Account owns CSS, HL or CS 1.6: {steam_id} | https://steamcommunity.com/profiles/{steam_id64}")
        with open("output/old_games_accounts.txt", "a", encoding="utf-8") as f:
            f.write(f"OLD GAMES ACCOUNT FOUND:\n{steam_id} | {player_name} | https://steamcommunity.com/profiles/{steam_id64}\n\n")
        return True

def query_lvl0(steam_id64 : str, steam_id : str, player_name : str, data_level) -> bool:
    if(data_level == None):
        return False
    if len(data_level["response"]) == 0:
        return False
    if data_level["response"]["player_level"] == 0:
        print(f"Level 0 account found: {steam_id} | https://steamcommunity.com/profiles/{steam_id64}")
        with open("output/lvl0_accounts.txt", "a", encoding="utf-8") as f:
            f.write(f"LEVEL 0 ACCOUNT FOUND:\n{steam_id} | {player_name } | https://steamcommunity.com/profiles/{steam_id64}\n\n")
        return True
    return False

def query_csgo (steam_id64 : str, steam_id : str, player_name : str, data_games, data_level) -> bool :
    if (data_level == None or data_games == None):
        return False
    if len(data_level["response"]) == 0:
        return False
    if len(data_games["response"]) == 0 or ("games" not in data_games["response"]) or (len(data_games["response"]["games"]) == 0):
        return False
    games_played_2weeks = list(filter(lambda x : "playtime_2weeks" in x, data_games["response"]["games"]))
    if(len(games_played_2weeks) != 0):
        return False
    game_csgo = list(filter(lambda x : x["appid"] == 730, data_games["response"]["games"]))
    if(len(game_csgo) == 0):
        return False
    if(game_csgo[0]["playtime_forever"] >= 600 or "playtime_2weeks" in game_csgo[0] or data_level["response"]["player_level"] > 9):
        return False
    else:
        print(f"Abandoned CSGO Account Found: {steam_id} | https://steamcommunity.com/profiles/{steam_id64}")
        with open("output/csgo_accounts.txt", "a", encoding="utf-8") as f:
            f.write(f"ABANDONED CSGO ACCOUNT FOUND:\n{steam_id} | {player_name} | https://steamcommunity.com/profiles/{steam_id64}\n\n")
        return True
        
    

def request_summary(steam_id64 : str):
    url_summary = f"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/"
    params_summary = {
            "key": STEAM_API_KEY,
            "steamids": steam_id64
        }
    try:
        response_summary = requests.get(url_summary, params=params_summary)
        data_summary = response_summary.json()
        if len(data_summary["response"]["players"]) == 0:
            print(f"Non-existent account: {steam_id64}")
            return None
        return data_summary
    except requests.exceptions.ConnectTimeout as e:
        print("Connection timed out. Waiting and retrying")
        time.sleep(5)
        request_summary(steam_id64)
    except Exception as e:
        if(response_summary.status_code == 429):
            print(f"Error: {e} PROBABLY STEAM API LIMIT REACHED, PLEASE SWITCH THE KEY")
            return None
        print(f"Response code: {response_summary.status_code}")
        request_summary(steam_id64)
        

def request_level(steam_id64 : str):
    url_level = "https://api.steampowered.com/IPlayerService/GetSteamLevel/v1/"
    params_level = {
        "key": STEAM_API_KEY,
        "steamid": steam_id64
    }
    try:
        response_level = requests.get(url_level, params=params_level)
        data_level = response_level.json()
        return data_level
    except requests.exceptions.ConnectTimeout as e:
        print("Connection timed out. Waiting and retrying")
        time.sleep(5)
        request_level(steam_id64)
    except Exception as e:
        if(response_level.status_code == 429):
            print(f"Error: {e} PROBABLY STEAM API LIMIT REACHED, PLEASE SWITCH THE KEY")
            return None
        print(f"Response code: {response_level.status_code}")
        request_level(steam_id64)
        

def request_games(steam_id64 : str):
    url_games = "https://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/"
    params_games = {
        "key": STEAM_API_KEY,
        "steamid": steam_id64,
        "format": "json"
    }
    try:
        response_games = requests.get(url_games, params=params_games)
        data_games = response_games.json()
        return data_games
    except requests.exceptions.ConnectTimeout as e:
            print("Connection timed out. Waiting and retrying")
            time.sleep(5)
            request_games(steam_id64)
    except Exception as e:
        if(response_games.status_code == 429):
            print(f"Error: {e} PROBABLY STEAM API LIMIT REACHED, PLEASE SWITCH THE KEY")
            return None
        print(f"Response code: {response_games.status_code}")
        request_games(steam_id64)
        

def request_badge(steam_id64 : str):
    url_badge = "https://api.steampowered.com/IPlayerService/GetBadges/v1/"
    params_badge = {
        "key": STEAM_API_KEY,
        "steamid": steam_id64,
        "format": "json"
    }
    try:
        response_badge = requests.get(url_badge, params=params_badge)
        data_badge = response_badge.json()
        return data_badge
    except requests.exceptions.ConnectTimeout as e:
            print("Connection timed out. Waiting and retrying")
            time.sleep(5)
            request_badge(steam_id64)
    except Exception as e:
        if(response_badge.status_code == 429):
            print(f"Error: {e} PROBABLY STEAM API LIMIT REACHED, PLEASE SWITCH THE KEY")
            return None
        print(f"Response code: {response_badge.status_code}")
        request_badge(steam_id64)
        

def query(server : int, steam_digit : int) -> AccountType:

    # query player summary no matter what, because we use player_name everywhere
    steam_id = f"STEAM_0:{server}:{steam_digit}"
    steam_id64 = utils.convert_to_steam64(steam_id)

    data_summary = request_summary(steam_id64)
    if(data_summary == None):
        return AccountType.NONE
    
    player_name = data_summary["response"]["players"][0]["personaname"]
   
    # unverified accounts query
    if(settings["unverified_accounts"]):
        if(query_unverified(steam_id64, steam_id, player_name, data_summary)):
            return AccountType.UNVERIFIED
        
    # if we"re going to query old games or lvl0 we might as well hit the level ep already
    if(settings["old_games"] or settings["lvl0_accounts"] or settings["csgo_accounts"]):
        data_level = request_level(steam_id64)

    if(settings["old_games"] or settings["csgo_accounts"]):
        data_games = request_games(steam_id64)

    # csgo query
    if(settings["csgo_accounts"]):
        if(query_csgo(steam_id64, steam_id, player_name, data_games, data_level)):
            return AccountType.CSGO

    # old games query
    if(settings["old_games"]):
        data_badge = request_badge(steam_id64)
        if(query_old_games(steam_id64, steam_id, player_name, data_games, data_level, data_badge)):
            return AccountType.OLD_GAMES
        
    # level 0 query
    if settings["lvl0_accounts"]:
        if(query_lvl0(steam_id64, steam_id, player_name, data_level)):
            return AccountType.LVL0
    return AccountType.NONE


def query_handler(start_id: int, end_id: int):
    for i in range(start_id, end_id):
        if stop_event.is_set(): 
            print("Stopping thread gracefully...")
            return
        for j in range(2):
            if stop_event.is_set():
                return
            steam_id = f"STEAM_0:{j}:{i}"
            print(f"\nChecking {steam_id} ...")
            query(j, i)
            time.sleep(0.5)


with open("Settings.json", "r", encoding="utf-8") as f:
    file_contents = f.read()
    settings = json.loads(file_contents)

stop_event = threading.Event()  
os.makedirs("output", exist_ok=True)
STEAM_API_KEY = settings["api_key"]
num_threads = settings["num_threads"]
threads = []
chunk_size = int((settings["end_id"] - settings["start_id"] + 1)/num_threads)
start = settings["start_id"]
end = chunk_size
for i in range(num_threads):
    end = end * (i + 1)
    t = threading.Thread(target=query_handler, args=(start, end))
    threads.append(t)
    start = end + 1

try:
    for t in threads:
        t.start()

    for t in threads:
        t.join()

except KeyboardInterrupt:
    print("\n[!] KeyboardInterrupt detected! Stopping threads...")
    stop_event.set()
    for t in threads:
        t.join()
    print("[!] Program terminated cleanly.")
    sys.exit(0)