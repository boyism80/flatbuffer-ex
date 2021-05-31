import os
import re
from modules.config import config

def _match(regex, contents):
    regex = re.compile(regex)
    matched = regex.search(contents)
    if not matched:
        return None

    return matched.groupdict()

def _matches(regex, contents):
    regex = re.compile(regex)
    return [{} if not x else x.groupdict() for x in regex.finditer(contents)]

def _load(file):
    with open(file, 'r', encoding='utf8') as f:
        contents = f.read()
        return contents

def py2go(type):
    matched = _match(config['regex']['array'], type)
    if matched:
        return f"[]{py2go(matched['name'])}"

    if type in config['primitive']['go']:
        return config['primitive']['go'][type]

    return type

def py2cs(type):
    matched = _match(config['regex']['array'], type)
    if matched:
        return f"List<{py2cs(matched['name'])}>"

    if type in config['primitive']['cs']:
        return config['primitive']['cs'][type]

    return type

def isPrimitive(type, pool):
    matched = _match(config['regex']['array'], type)
    if matched:
        return isPrimitive(matched['name'], pool)

    return type not in pool

def isArray(type):
    return bool(_match(config['regex']['array'], type))

def isString(type):
    return type == 'string'

def attributes(type, pool):
    result = {
        'primitive': isPrimitive(type, pool),
        'array': isArray(type),
        'string': isString(type)
    }

    result['offset'] = result['array'] or result['string'] or not result['primitive']
    result['declared'] = not result['primitive']

    for key in [x for x in result.keys()]:
        if not result[key]:
            del result[key]

    return result

def load(path):
    files = [os.path.join(path, f) for f in os.listdir(path) if os.path.isfile(os.path.join(path, f)) and f.endswith('.fbs')]

    result = {}
    for file in files:
        contents = _load(file)
        namespace = re.search(config['regex']['namespace'], contents)
        if namespace and 'namespace' in namespace.groupdict():
            namespace = namespace['namespace']
        else:
            namespace = None

        if not namespace:
            continue

        result[namespace] = []
        matches = {x['name']: x['params'] for x in _matches(config['regex']['table'], contents)}
        for name, contents in matches.items():

            params = []
            for param in _matches(config['regex']['field'], contents):
                x = { 'name': param['name'], 'type': param['type'] }
                x.update(attributes(param['type'], matches))

                if 'array' in x:
                    element = _match(config['regex']['array'], param['type'])
                    baseName = element['name']
                    x['element'] = { 'name': baseName }
                    x['element'].update(attributes(baseName, matches))

                params.append(x)

            result[namespace].append({
                'name': name,
                'params': params
            })

    return result
