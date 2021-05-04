import jinja2
import os
import copy
import modules.extractor as extractor

def loadTemplateFiles(root):
    loader = jinja2.FileSystemLoader(root)
    env = jinja2.Environment(loader=loader)

    return {os.path.splitext(x)[0]:env.get_template(x) for x in os.listdir(root) if x.endswith('.txt')}

templates = loadTemplateFiles('templates')

def __nameset(name):
    return {
        'base': name,
        'upper': name[0].upper() + name[1:],
        'lower': name[0].lower() + name[1:]
    }


def go(root, package, name, params):

    params = copy.deepcopy(params)
    for param in params:
        param['name'] = __nameset(param['name'])
        param['type'] = extractor.py2go(param['type'])
        if 'array' in param:
            param['element']['name'] = __nameset(extractor.py2go(param['element']['name']))

    return templates['go'].render({
        'root': root.replace('\\', '/'),
        'package': package,
        'name': __nameset(name),
        'params': params
    })

def cs(namespace, name, params):
    params = copy.deepcopy(params)
    for param in params:
        param['name'] = __nameset(param['name'])
        param['type'] = extractor.py2cs(param['type'])
        if 'array' in param:
            param['element']['name'] = __nameset(extractor.py2cs(param['element']['name']))

    return templates['cs'].render({
        'namespace': namespace,
        'name': __nameset(name),
        'params': params
    })