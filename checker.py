from time import time
import utils
import requests
import json
import os
from enum import Enum

class AccountType(Enum):
    UNVERIFIED = 1
    CSS_ONLY = 2
    LVL0 = 3
    NONE = 4

def query(server : int, steam_digit : int) -> AccountType:
    steam_id = f"STEAM_0:{server}:{steam_digit}"
    steam_id64 = utils.convert_to_steam64(steam_id)
    url_summary = f"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/"
    params_summary = {
            'key': STEAM_API_KEY,
            'steamids': steam_id64
        }
    try:
        response_summary = requests.get(url_summary, params=params_summary)
        data_summary = response_summary.json()
        if len(data_summary['response']['players']) == 0:
            print(f"Non-existent account: {steam_id}")
            return AccountType.NONE
        player_name = data_summary['response']['players'][0]['personaname']
    except requests.exceptions.ConnectTimeout as e:
        print("Connection timed out. Waiting and retrying")
        time.sleep(5)
        query(server, steam_digit)
    except Exception as e:
        print(f"Error: {e} PROBABLY STEAM API LIMIT REACHED, PLEASE SWITCH THE KEY")
        return AccountType.NONE
    if(settings['unverified_accounts'] == True):
        if ('profilestate' not in data_summary['response']['players'][0]) or (data_summary['response']['players'][0]['profilestate'] == 0):
            print(f"Unverified account found: {steam_id} | https://steamcommunity.com/profiles/{steam_id64}")
            with open("output/unverified_accounts.txt", "a", encoding="utf-8") as f:
                f.write(f"UNVERIFIED ACCOUNT FOUND:\n{steam_id} | {player_name} | https://steamcommunity.com/profiles/{steam_id64}\n\n")
            return AccountType.UNVERIFIED
    if(settings['css_accounts'] == True):
        url_games = 'https://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/'
        params_games = {
            'key': STEAM_API_KEY,
            'steamid': steam_id64,
            'format': 'json'
        }
        url_level = "https://api.steampowered.com/IPlayerService/GetSteamLevel/v1/"
        params_level = {
            'key': STEAM_API_KEY,
            'steamid': steam_id64
        }
        try:
            response_games = requests.get(url_games, params=params_games)
            data_games = response_games.json()
            response_level = requests.get(url_level, params=params_level)
            data_level = response_level.json()
            if len(data_games['response']) == 0 or 'games' not in data_games['response'] or len(data_games['response']['games']) == 0:
                pass
            else:
                gameid_list = list(map(lambda x : x['appid'], data_games['response']['games']))
                if 240 in gameid_list and not 70 in gameid_list and 10 not in gameid_list and data_level['response']['player_level'] <= 9:
                    print(f"Account owns CSS but not HL or CS 1.6 | https://steamcommunity.com/profiles/{steam_id64}")
                    with open("output/css_accounts.txt", "a", encoding="utf-8") as f:
                        f.write(f"CSS ACCOUNT FOUND:\n{steam_id} | {player_name} | https://steamcommunity.com/profiles/{steam_id64}\n\n")
                    return AccountType.CSS_ONLY
            if settings['lvl0_accounts'] == True:
                if len(data_level['response']) == 0:
                    return AccountType.NONE
                if data_level['response']['player_level'] == 0:
                    print(f"Level 0 account found: {steam_id} | https://steamcommunity.com/profiles/{steam_id64}")
                    with open("output/lvl0_accounts.txt", "a", encoding="utf-8") as f:
                        f.write(f"LEVEL 0 ACCOUNT FOUND:\n{steam_id} | {player_name } |https://steamcommunity.com/profiles/{steam_id64}\n\n")
                    return AccountType.LVL0
        except requests.exceptions.ConnectTimeout as e:
            print("Connection timed out. Waiting and retrying")
            time.sleep(5)
            query(server, steam_digit)
        except Exception as e:
            
            print(f"Error: {e} PROBABLY STEAM API LIMIT REACHED, PLEASE SWITCH THE KEY")
            return AccountType.NONE
    if settings['lvl0_accounts'] == True:
        response_level = requests.get(url_level, params=params_level)
        data_level = response_level.json()
        if len(data_level['response']) == 0:
            return AccountType.NONE
        if data_level['response']['player_level'] == 0:
            print(f"Level 0 account found: {steam_id} | https://steamcommunity.com/profiles/{steam_id64}")
            with open("output/lvl0_accounts.txt", "a", encoding="utf-8") as f:
                f.write(f"LEVEL 0 ACCOUNT FOUND:\n{steam_id} | {player_name } |https://steamcommunity.com/profiles/{steam_id64}\n\n")
            return AccountType.LVL0
    return AccountType.NONE

with open('Settings.json', "r", encoding="utf-8") as f:
    file_contents = f.read()
    settings = json.loads(file_contents)
    
os.makedirs("output", exist_ok=True)
STEAM_API_KEY = settings['api_key']

try:
    for i in range(settings['start_id'], settings['end_id']):
        for j in range(2):
            steam_id = f"STEAM_0:{j}:{i}"
            print(f"\nChecking {steam_id} ...")
            query(j, i)
except KeyboardInterrupt:
    print("\nExiting...")
print("\n\nFinished")
