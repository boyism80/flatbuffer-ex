import os
import re

primitives = {
    'long': 'int64',
    'ulong': 'uint64',
    'int': 'int32',
    'double': 'float64',
    'float': 'float32'
}

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
    matched = _match(r'^\[(?P<name>(\w*))\]$', type)
    if matched:
        return f"[]{py2go(matched['name'])}"

    if type in primitives:
        return primitives[type]

    return type

def isPrimitive(type, pool):
    matched = _match(r'^\[(?P<name>(\w*))\]$', type)
    if matched:
        return isPrimitive(matched['name'], pool)

    return type not in pool

def load(path):
    files = [os.path.join(path, f) for f in os.listdir(path) if os.path.isfile(os.path.join(path, f)) and f.endswith('.fbs')]

    result = {}
    for file in files:
        contents = _load(file)
        namespace = re.search(r'namespace\s+(?P<namespace>[\w.]+);', contents)
        if namespace and 'namespace' in namespace.groupdict():
            namespace = namespace['namespace']
        else:
            namespace = None

        if not namespace:
            continue

        if '.' in namespace:
            namespace = namespace.split('.')[-1]

        result[namespace] = []
        matches = {x['name']: x['params'] for x in _matches(r'(table|struct) (?P<name>\w*)\s{(?P<params>(.|\n)*?)}', contents)}
        for name, contents in matches.items():

            data = {
                'name': {
                    'lower': name[0].lower() + name[1:],
                    'upper': name
                }
            }

            params = []
            for param in _matches(r'\s*(?P<name>\w*)\s*:\s*(?P<type>[\w\[\]]*);', contents):
                x = {
                    'name': {
                        'lower': param['name'],
                        'upper': param['name'][0].upper() + param['name'][1:]
                    },
                    'type': py2go(param['type']),
                    'primitive': isPrimitive(param['type'], matches)
                }

                element = _match(r'^\[(?P<name>(\w*))\]$', param['type'])
                x['slice'] = bool(element)
                if element:
                    name = py2go(element['name'])
                    x['element'] = {
                        'name': {
                            'lower': name[0].lower() + name[1:],
                            'upper': name
                        },
                        'primitive': isPrimitive(name, matches)
                    }

                params.append(x)
            
            data['params'] = params
            result[namespace].append(data)

    return result
