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

            data = {
                'name': {
                    'base': name,
                    'lower': name[0].lower() + name[1:],
                    'upper': name[0].upper() + name[1:]
                }
            }

            params = []
            for param in _matches(config['regex']['field'], contents):
                x = {
                    'name': {
                        'base': param['name'],
                        'lower': param['name'][0].lower() + param['name'][1:],
                        'upper': param['name'][0].upper() + param['name'][1:]
                    },
                    'type': param['type']
                }
                x.update(attributes(param['type'], matches))

                if x['array']:
                    element = _match(config['regex']['array'], param['type'])
                    name = element['name']
                    x['element'] = {
                        'name': {
                            'base': name,
                            'lower': name[0].lower() + name[1:],
                            'upper': name[0].upper() + name[1:]
                        }
                    }
                    x['element'].update(attributes(name, matches))

                params.append(x)
            
            data['params'] = params
            result[namespace].append(data)

    return result
