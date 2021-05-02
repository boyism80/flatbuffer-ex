import json

config = {}
with open('./config.json', 'r', encoding='utf-8') as f:
    config = json.load(f)